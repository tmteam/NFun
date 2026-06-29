using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
using NFun.Functions;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.TypeInferenceAdapter;

using static StatePrimitive;

public class TicSetupVisitor : ISyntaxNodeVisitor<bool> {
    private readonly VariableScopeAliasTable _aliasScope;
    private readonly GraphBuilder _ticTypeGraph;
    private readonly IFunctionRegistry _dictionary;
    private readonly IConstantList _constants;
    private readonly TypeInferenceResultsBuilder _resultsBuilder;
    private readonly DialectSettings _dialect;

    public static bool SetupTicForBody(
        SyntaxTree tree,
        GraphBuilder ticGraph,
        TypeInferenceResultsBuilder results, TypeBehaviour typeBehaviour) => SetupTicForBody(
        tree, ticGraph,
        BaseFunctions.GetFunctions(typeBehaviour, Dialects.Origin.ExtensionFunctionsSeparation),
        EmptyConstantList.Instance,
        EmptyAprioriTypesMap.Instance,
        EmptyCustomTypeRegistry.Instance,
        results, Dialects.Origin);

    internal static bool SetupTicForBody(
        SyntaxTree tree,
        GraphBuilder ticGraph,
        IFunctionRegistry functions,
        IConstantList constants,
        IAprioriTypesMap aprioriTypes,
        ICustomTypeRegistry customTypes,
        TypeInferenceResultsBuilder results,
        DialectSettings dialect,
        INamedTypeFieldRegistry namedTypeFieldRegistry = null) {
        var visitor = new TicSetupVisitor(ticGraph, functions, constants, results, aprioriTypes, dialect, customTypes, namedTypeFieldRegistry);

        // Collect user function definitions for named argument resolution (lazy dict)
        foreach (var syntaxNode in tree.Children)
        {
            if (syntaxNode is UserFunctionDefinitionSyntaxNode ufn)
                (visitor._userFunctions ??= new())[(ufn.Id, ufn.Args.Count)] = ufn;
        }
        visitor._hasUserFunctions = visitor._userFunctions is { Count: > 0 };

        foreach (var syntaxNode in tree.Children)
        {
            if (syntaxNode is UserFunctionDefinitionSyntaxNode)
                continue;

            if (!syntaxNode.Accept(visitor))
                return false;
        }

        return true;
    }

    internal static bool SetupTicForUserFunction(
        UserFunctionDefinitionSyntaxNode userFunctionNode,
        GraphBuilder ticGraph,
        IFunctionRegistry functions,
        IConstantList constants,
        TypeInferenceResultsBuilder results,
        DialectSettings dialect,
        ICustomTypeRegistry customTypes = null,
        INamedTypeFieldRegistry namedTypeFieldRegistry = null,
        UserFunctionDefinitionSyntaxNode[] allUserFunctions = null) {
        var visitor = new TicSetupVisitor(ticGraph, functions, constants, results, EmptyAprioriTypesMap.Instance, dialect, customTypes, namedTypeFieldRegistry);
        // Register all user functions for named arg / default param resolution in cross-function calls
        if (allUserFunctions != null)
        {
            foreach (var ufn in allUserFunctions)
                (visitor._userFunctions ??= new())[(ufn.Id, ufn.Args.Count)] = ufn;
        }
        // Always register the current function (for recursive calls)
        (visitor._userFunctions ??= new())[(userFunctionNode.Id, userFunctionNode.Args.Count)] = userFunctionNode;
        visitor._hasUserFunctions = true;
        return userFunctionNode.Accept(visitor);
    }

    /// <summary>
    /// Set up TIC for a group of mutually-recursive user functions in ONE
    /// GraphBuilder. Each function's StateFun signature is registered up-front
    /// (Phase 1) so cross-cycle calls resolve. Then each body is visited in
    /// its own alias scope (Phase 2) — arg names are prefixed
    /// per-function (e.g. <c>isEven'1::n</c>) to prevent collisions across
    /// peers in the cycle.
    ///
    /// One <c>graph.Solve()</c> then resolves all functions' types together,
    /// matching the Damas-Milner let-rec / SML/OCaml <c>let rec ... and ...</c>
    /// semantics.
    /// </summary>
    internal static bool SetupTicForUserFunctionGroup(
        UserFunctionDefinitionSyntaxNode[] group,
        GraphBuilder ticGraph,
        IFunctionRegistry functions,
        IConstantList constants,
        TypeInferenceResultsBuilder results,
        DialectSettings dialect,
        ICustomTypeRegistry customTypes,
        INamedTypeFieldRegistry namedTypeFieldRegistry,
        UserFunctionDefinitionSyntaxNode[] allUserFunctions) {
        var visitor = new TicSetupVisitor(ticGraph, functions, constants, results,
            EmptyAprioriTypesMap.Instance, dialect, customTypes, namedTypeFieldRegistry);
        if (allUserFunctions != null)
            foreach (var ufn in allUserFunctions)
                (visitor._userFunctions ??= new())[(ufn.Id, ufn.Args.Count)] = ufn;
        foreach (var fn in group)
            (visitor._userFunctions ??= new())[(fn.Id, fn.Args.Count)] = fn;
        visitor._hasUserFunctions = true;

        // Per-function arg-name prefixes to avoid collision on shared param names.
        var prefixes = new string[group.Length];
        for (int i = 0; i < group.Length; i++)
            prefixes[i] = $"{group[i].Id}'{group[i].Args.Count}::";

        // Phase 1: register every function's StateFun (so peer-calls resolve).
        for (int i = 0; i < group.Length; i++)
            visitor.RegisterUserFunctionHeader(group[i], prefixes[i]);

        // Phase 2: visit each function's body in its own alias scope.
        for (int i = 0; i < group.Length; i++) {
            var fn = group[i];
            visitor._aliasScope.EnterScope(fn.OrderNumber);
            for (int a = 0; a < fn.Args.Count; a++)
                visitor._aliasScope.AddVariableAlias(fn.Args[a].Id, prefixes[i] + fn.Args[a].Id);

            if (!fn.Body.Accept(visitor)) { visitor._aliasScope.ExitScope(); return false; }

            foreach (var arg in fn.Args) {
                if (arg.HasDefault) {
                    if (!arg.DefaultValue.Accept(visitor)) { visitor._aliasScope.ExitScope(); return false; }
                    visitor._ticTypeGraph.SetDefaultValueConstraint(
                        prefixes[i] + arg.Id, arg.DefaultValue.OrderNumber);
                }
            }
            visitor._aliasScope.ExitScope();
        }
        return true;
    }

    private readonly ICustomTypeRegistry _customTypes;
    private readonly INamedTypeFieldRegistry _namedTypeFieldRegistry;
    internal Dictionary<(string, int), UserFunctionDefinitionSyntaxNode> _userFunctions;
    internal bool _hasUserFunctions;
    private HashSet<string> _narrowedFieldPaths;
    /// <summary>
    /// Registry key prefix for extension functions when ExtensionFunctionsSeparation is enabled.
    /// Extension function "f" is stored as ".f" to avoid collision with regular function "f".
    /// </summary>
    internal const string ExtensionKeyPrefix = ".";

    /// <summary>Returns the registry key for a function, adding extension prefix when needed.</summary>
    internal static string GetRegistryKey(string name, bool isExtension, ExtensionFunctionsSeparation separation)
        => separation == ExtensionFunctionsSeparation.Enabled && isExtension
            ? ExtensionKeyPrefix + name
            : name;

    internal const int SyntheticIdStart = 100000;
    private int _nextSyntheticId = SyntheticIdStart; // synthetic node IDs start high to avoid collision

    private TicSetupVisitor(
        GraphBuilder ticTypeGraph,
        IFunctionRegistry dictionary,
        IConstantList constants,
        TypeInferenceResultsBuilder resultsBuilder,
        IAprioriTypesMap aprioriTypesMap,
        DialectSettings dialect,
        ICustomTypeRegistry customTypes = null,
        INamedTypeFieldRegistry namedTypeFieldRegistry = null) {
        _aliasScope = new VariableScopeAliasTable();
        _dictionary = dictionary;
        _constants = constants;
        _resultsBuilder = resultsBuilder;
        _dialect = dialect;
        _ticTypeGraph = ticTypeGraph;
        _customTypes = customTypes ?? EmptyCustomTypeRegistry.Instance;
        _namedTypeFieldRegistry = namedTypeFieldRegistry;
        _ticTypeGraph.NamedTypeRegistry = namedTypeFieldRegistry;

        foreach (var apriori in aprioriTypesMap)
            _ticTypeGraph.SetVarType(apriori.Name, ConvertType(apriori.Type));
    }

    /// <summary>
    /// Converts FunnyType to TIC state, using the named type field registry for NamedStruct resolution.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ITicNodeState ConvertType(FunnyType t) =>
        _namedTypeFieldRegistry != null
            ? t.ConvertToTiType(_namedTypeFieldRegistry)
            : t.ConvertToTiType();

    /// <summary>
    /// Resolves named arguments: merges positional + named into correct positional order.
    /// If no named args, returns node.Args as-is.
    /// </summary>
    private ISyntaxNode[] ResolveNamedArgs(FunCallSyntaxNode node) {
        // Try find user function definition (for named args, defaults, params)
        var userFun = FindUserFunctionForCall(node);

        // No user function found → check built-in with ArgProperties
        if (userFun == null)
        {
            if (!node.HasNamedArgs)
                return node.Args;
            return ResolveNamedArgsForBuiltIn(node);
        }

        var paramCount = userFun.Args.Count;

        // Compute argument layout: [positional..., defaults..., params, keyword-only...]
        int fillableCount = 0; // slots that accept positional args (not params, not keyword-only)
        int paramsIndex = -1;
        bool hasDefaults = false;
        bool hasKeywordOnly = false;
        for (int i = 0; i < paramCount; i++)
        {
            var arg = userFun.Args[i];
            if (arg.IsParams) paramsIndex = i;
            else if (arg.IsKeywordOnly) hasKeywordOnly = true;
            else
            {
                fillableCount++;
                if (arg.HasDefault) hasDefaults = true;
            }
        }
        bool hasParams = paramsIndex >= 0;

        // Fast path: no named args, no defaults, no params, no keyword-only, exact match
        if (!node.HasNamedArgs && !hasParams && !hasDefaults && !hasKeywordOnly && paramCount == node.Args.Length)
            return node.Args;

        var result = new ISyntaxNode[paramCount];

        // Fill positional args (up to fillable count — excludes params and keyword-only slots)
        int positionalFillCount = Math.Min(node.Args.Length, fillableCount);
        for (int i = 0; i < positionalFillCount; i++)
            result[i] = node.Args[i];

        // Collect extra positional args into array for params
        if (hasParams)
        {
            if (node.Args.Length > fillableCount)
            {
                var extraArgs = new ISyntaxNode[node.Args.Length - fillableCount];
                for (int i = 0; i < extraArgs.Length; i++)
                    extraArgs[i] = node.Args[fillableCount + i];
                var arr = new ArraySyntaxNode(extraArgs, node.Interval);
                arr.OrderNumber = _nextSyntheticId++;
                result[paramsIndex] = arr;
            }
            else
            {
                var arr = new ArraySyntaxNode(Array.Empty<ISyntaxNode>(), node.Interval);
                arr.OrderNumber = _nextSyntheticId++;
                result[paramsIndex] = arr;
            }
        }

        // Fill named args (match by name against all params including keyword-only)
        foreach (var named in node.NamedArgs)
        {
            int paramIndex = -1;
            for (int i = 0; i < paramCount; i++)
            {
                if (string.Equals(userFun.Args[i].Id, named.Name, StringComparison.OrdinalIgnoreCase))
                { paramIndex = i; break; }
            }
            if (paramIndex < 0)
                throw Errors.UnknownNamedArgument(node.Id, named.Name, named.NameInterval);
            if (paramIndex < positionalFillCount)
                throw Errors.NamedArgOverlapsPositional(node.Id, named.Name, named.NameInterval);
            if (paramIndex == paramsIndex)
                result[paramIndex] = named.Value;
            else if (result[paramIndex] != null)
                throw Errors.DuplicateNamedArgument(node.Id, named.Name, named.NameInterval);
            else
                result[paramIndex] = named.Value;
        }

        // Fill defaults for missing non-params args (including keyword-only)
        for (int i = 0; i < paramCount; i++)
        {
            if (i == paramsIndex || result[i] != null) continue;

            if (!userFun.Args[i].HasDefault)
                throw Errors.MissingArgument(node.Id, userFun.Args[i].Id, node.Interval);

            var defaultExpr = userFun.Args[i].DefaultValue;

            if (userFun.Args[i].PrecomputedDefaultValue != null)
            {
                result[i] = new ConstantSyntaxNode(
                    userFun.Args[i].PrecomputedDefaultValue,
                    userFun.Args[i].PrecomputedDefaultType,
                    defaultExpr.Interval) {
                    OrderNumber = _nextSyntheticId++,
                };
            }
            else
            {
                // Self/mutual recursive call site: PrecomputeDefaultValues runs AFTER
                // body solve, so we can't pre-compute here. The earlier "typed" branch
                // inserted DefaultValueSyntaxNode which evaluates to the TYPE default
                // (e.g. 0 for int) — losing the USER's declared default. Reuse the
                // original default expression in both typed/untyped cases; TIC handles
                // the shared-node case via OrderNumber dedup.
                result[i] = defaultExpr;
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves named arguments for built-in functions using ArgProperties metadata.
    /// </summary>
    private ISyntaxNode[] ResolveNamedArgsForBuiltIn(FunCallSyntaxNode node) {
        var namedNames = new string[node.NamedArgs.Length];
        for (int i = 0; i < node.NamedArgs.Length; i++)
            namedNames[i] = node.NamedArgs[i].Name;

        var signature = _dictionary.FindOrNull(node.Id, node.Args.Length, namedNames);
        var argProps = signature?.ArgProperties;
        if (argProps == null)
            throw Errors.NamedArgsNotSupportedForBuiltIn(node.Id, node.Interval);

        var totalArgs = argProps.Length;

        var result = new ISyntaxNode[totalArgs];

        // Fill positional args
        for (int i = 0; i < node.Args.Length; i++)
            result[i] = node.Args[i];

        // Fill named args
        foreach (var named in node.NamedArgs)
        {
            int paramIndex = -1;
            for (int i = 0; i < argProps.Length; i++)
            {
                if (string.Equals(argProps[i].Name, named.Name, StringComparison.OrdinalIgnoreCase))
                { paramIndex = i; break; }
            }
            if (paramIndex < 0)
                throw Errors.UnknownNamedArgument(node.Id, named.Name, named.NameInterval);
            if (paramIndex < node.Args.Length)
                throw Errors.NamedArgOverlapsPositional(node.Id, named.Name, named.NameInterval);
            if (result[paramIndex] != null)
                throw Errors.DuplicateNamedArgument(node.Id, named.Name, named.NameInterval);
            result[paramIndex] = named.Value;
        }

        // Fill defaults for missing args, verify required args filled
        for (int i = 0; i < totalArgs; i++)
        {
            if (result[i] != null) continue;
            if (argProps[i].HasDefault && argProps[i].DefaultValue != null)
            {
                result[i] = new ConstantSyntaxNode(
                    argProps[i].DefaultValue,
                    signature.ArgTypes[i],
                    node.Interval) { OrderNumber = _nextSyntheticId++ };
            }
            else
                throw Errors.MissingArgument(node.Id, argProps[i].Name, node.Interval);
        }

        return result;
    }

    private UserFunctionDefinitionSyntaxNode FindUserFunctionForCall(FunCallSyntaxNode node) {
        var totalCallArgs = node.Args.Length + node.NamedArgs.Length;

        // Exact match by total arg count
        var exact = FindUserFunctionDefinition(node.Id, totalCallArgs);
        if (exact != null) return exact;

        // Match with defaults/params: function with more params than provided args
        return FindUserFunctionByNameWithDefaults(node.Id, node.Args.Length, totalCallArgs);
    }

    private UserFunctionDefinitionSyntaxNode FindUserFunctionDefinition(string name, int argCount) =>
        _userFunctions != null && _userFunctions.TryGetValue((name, argCount), out var ufn) ? ufn : null;

    /// <summary>Find user function by name where provided arg count fits considering defaults and params</summary>
    private UserFunctionDefinitionSyntaxNode FindUserFunctionByNameWithDefaults(string name, int providedArgs, int maxArgs) {
        if (_userFunctions == null) return null;
        foreach (var ((fn, _), ufn) in _userFunctions)
        {
            if (!string.Equals(fn, name, StringComparison.OrdinalIgnoreCase))
                continue;
            bool hasParams = false;
            var requiredCount = 0;
            var maxCount = ufn.Args.Count;
            foreach (var arg in ufn.Args)
            {
                if (arg.IsParams) hasParams = true;
                else if (!arg.HasDefault && !arg.IsKeywordOnly) requiredCount++;
            }

            if (hasParams)
            {
                // With params: requires at least requiredCount args, no upper limit
                // maxArgs includes both positional and named — either can fill required slots
                if (maxArgs >= requiredCount)
                    return ufn;
            }
            else
            {
                // Without params: requires [requiredCount, maxCount] args
                if (maxArgs >= requiredCount && maxArgs <= maxCount)
                    return ufn;
            }
        }
        return null;
    }

    private FunnyType ResolveType(TypeSyntax syntax) =>
        TypeSyntaxResolver.Resolve(syntax, _customTypes);

    public bool Visit(SyntaxTree node) => VisitChildren(node);

    public bool Visit(EquationSyntaxNode node) {
        VisitChildren(node);
#if DEBUG
        Trace(node, $"{node.Id}:{node.OutputType} = {node.Expression.OrderNumber}");
#endif
        if (node.OutputTypeSpecified)
        {
            var resolvedType = ResolveType(node.TypeSpecificationOrNull.TypeSyntax);
            ThrowIfOptionalTypeDisabled(resolvedType, node.Id, node.Interval);
            var type = ConvertType(resolvedType);
            if (!_ticTypeGraph.TrySetVarType(node.Id, type))
                throw Errors.VariableIsAlreadyDeclared(node.Id, node.Interval);
        }

        _ticTypeGraph.SetDef(node.Id, node.Expression.OrderNumber);
        return true;
    }

    public bool Visit(UserFunctionDefinitionSyntaxNode node) {
        // Single-function path. No arg name prefixing — own graph, no peers.
        RegisterUserFunctionHeader(node, argNamePrefix: null);
        var result = VisitChildren(node);

        // Constrain default expressions: type must be assignable to parameter type.
        // DefaultValue is already visited by Visit(TypedVarDefSyntaxNode) → VisitChildren.
        foreach (var arg in node.Args)
        {
            if (arg.HasDefault)
                _ticTypeGraph.SetDefaultValueConstraint(arg.Id, arg.DefaultValue.OrderNumber);
        }

        return result;
    }

    /// <summary>
    /// Register the function's TIC signature (arg type vars + StateFun) without
    /// visiting body. Used by <see cref="Visit(UserFunctionDefinitionSyntaxNode)"/>
    /// for single-function setup, and by SetupTicForUserFunctionGroup for
    /// mutually-recursive groups (where all signatures must exist before any
    /// body is visited so peer-calls resolve).
    /// </summary>
    /// <param name="argNamePrefix">
    /// Optional namespace prefix for arg names. Used by group setup to avoid
    /// collisions when multiple functions in one TIC graph share arg names
    /// (e.g. both isEven and isOdd take an arg named "n"). Caller establishes
    /// aliasing in the body's scope so <c>NamedIdSyntaxNode("n")</c> lookups
    /// route through the prefix.
    /// </param>
    private void RegisterUserFunctionHeader(UserFunctionDefinitionSyntaxNode node, string argNamePrefix) {
        var argNames = new string[node.Args.Count];
        for (int i = 0; i < node.Args.Count; i++)
        {
            var arg = node.Args[i];
            argNames[i] = argNamePrefix != null ? argNamePrefix + arg.Id : arg.Id;
            if (arg.TypeSyntax is not TypeSyntax.EmptyType)
            {
                var resolvedType = ResolveType(arg.TypeSyntax);
                ThrowIfOptionalTypeDisabled(resolvedType, arg.Id, arg.Interval);
                _ticTypeGraph.SetVarType(argNames[i], ConvertType(resolvedType));
            }
        }

        ITypeState returnType = null;
        if (node.ReturnTypeSyntax is not TypeSyntax.EmptyType)
            returnType = (ITypeState)ConvertType(ResolveType(node.ReturnTypeSyntax));

#if DEBUG
        TraceLog.WriteLine(
            $"Enter {node.OrderNumber}. UFun {node.Id}({string.Join(",", argNames)})->{node.Body.OrderNumber}:{returnType?.ToString() ?? "empty"}");
#endif
        var fun = _ticTypeGraph.SetFunDef(
            name: $"{node.Id}'{node.Args.Count}",
            returnId: node.Body.OrderNumber,
            returnType: returnType,
            varNames: argNames);
        var ufRegistryName = GetRegistryKey(node.Id, node.IsExtension, _dialect.ExtensionFunctionsSeparation);
        _resultsBuilder.RememberUserFunctionSignature(ufRegistryName, fun);
    }

    public bool Visit(ArraySyntaxNode node) {
        VisitChildren(node);
        var elementIds = new int[node.Expressions.Count];
        for (int i = 0; i < node.Expressions.Count; i++)
            elementIds[i] = node.Expressions[i].OrderNumber;
#if DEBUG
        Trace(node, $"[{string.Join(",", elementIds)}]");
#endif

        // When all elements share the SAME named-struct type
        // (post-NamedTypeElaborator OutputType = NamedStructOf), pre-resolve the element
        // type slot to the full named recursive shape. Otherwise the element-LCA node
        // starts as an empty Constraints and absorbs only the literal's raw post-Pull
        // state — for default-`none` recursive fields, the "None desc → skip" rule
        // leaves `next:None`, and the array element type collapses to a degenerate
        // shape like `{next:none}` instead of preserving `t`'s recursive identity.
        ITicNodeState elementHint = null;
        if (node.Expressions.Count > 0 && _namedTypeFieldRegistry != null)
        {
            string commonName = null;
            bool allSameNamed = true;
            foreach (var expr in node.Expressions)
            {
                if (expr.OutputType.BaseType == BaseFunnyType.NamedStruct)
                {
                    var name = expr.OutputType.NamedStructTypeName;
                    if (commonName == null) commonName = name;
                    else if (!string.Equals(commonName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        allSameNamed = false; break;
                    }
                }
                else { allSameNamed = false; break; }
            }
            if (allSameNamed && commonName != null
                && _namedTypeFieldRegistry.TryGetFields(commonName, out _))
            {
                elementHint = ConvertType(FunnyType.NamedStructOf(commonName));
            }
        }

        if (elementHint != null)
            _ticTypeGraph.SetSoftArrayInit(node.OrderNumber, elementIds, elementHint);
        else
            _ticTypeGraph.SetSoftArrayInit(node.OrderNumber, elementIds);
        return true;
    }

    public bool Visit(SuperAnonymFunctionSyntaxNode node) {
        _aliasScope.EnterScope(node.OrderNumber);
        //try to figure out signature with parent type specification
        var argType = _parentFunctionArgType.FunTypeSpecification;
        string[] originArgNames;
        string[] aliasArgNames;
        if (argType == null)
        {
            //need to scan the body to figure out the names of function syntax node
            bool hasSimpleIt = false;
            int maxNotSimpleItNum = -1;

            node.Body.ComeOver(visiting => {
                if (visiting is SuperAnonymFunctionSyntaxNode)
                    return DfsEnterResult.Skip;
                // `it` / `itN` can appear either as a NamedId reference (e.g., `it + 1`)
                // or as the callee of a higher-order call (e.g., `it()` — invoking it as a
                // function value). Both forms count as implicit-parameter usage and must
                // contribute to the inferred arity. (MR4Bug2.)
                string idForLookup = null;
                Interval intervalForError = default;
                if (visiting is NamedIdSyntaxNode named) {
                    idForLookup = named.Id;
                    intervalForError = named.Interval;
                } else if (visiting is FunCallSyntaxNode call) {
                    idForLookup = call.Id;
                    intervalForError = call.Interval;
                }
                if (idForLookup != null && Helper.DoesItLooksLikeSuperAnonymousVariable(idForLookup, out int num))
                {
                    //we found variable!
                    if (num == -1)
                        hasSimpleIt = true;
                    else if (num == 0 || num > 3)
                        throw Errors.InvalidSuperAnonymousVariableName(intervalForError, idForLookup);
                    else
                        maxNotSimpleItNum = Math.Max(maxNotSimpleItNum, num);
                }

                return DfsEnterResult.Continue;
            });

            if (hasSimpleIt && maxNotSimpleItNum > 0)
            {
                //user mix 'it' and 'itN' arguments
                var resultInterval = node.Body.Find(
                    predicate: n => n is NamedIdSyntaxNode named && Helper.DoesItLooksLikeSuperAnonymousVariable(named.Id, out int num) && num == -1,
                    enterCondition: n => n is not SuperAnonymFunctionSyntaxNode).Interval;
                throw Errors.CannotUseSuperAnonymousVariableHereBecauseHasNumberedVariables(resultInterval);
            }

            if (hasSimpleIt)
                originArgNames = new[] { "it" };
            else if (maxNotSimpleItNum == -1)
                originArgNames = new String[] { };
            else
                originArgNames = GetSuperAnonymousArgNames(maxNotSimpleItNum);
        }
        else if (argType.Inputs.Length == 1)
            originArgNames = new[] { "it" };
        else
            originArgNames = GetSuperAnonymousArgNames(argType.Inputs.Length);

        aliasArgNames = new string[originArgNames.Length];

        for (var i = 0; i < originArgNames.Length; i++)
        {
            var originName = originArgNames[i];
            var aliasName = MakeAnonVariableName(node, originName);
            _aliasScope.AddVariableAlias(originName, aliasName);
            aliasArgNames[i] = aliasName;
        }

        VisitChildren(node);
#if DEBUG
        Trace(node, $"f({string.Join(" ", originArgNames)}):{node.OutputType}= {{{node.OrderNumber}}}");
#endif
        _ticTypeGraph.CreateLambda(node.Body.OrderNumber, node.OrderNumber, aliasArgNames);

        _aliasScope.ExitScope();
        return true;
    }

    private static string[] GetSuperAnonymousArgNames(int count) {
        var originArgNames = new string[count];
        for (int i = 0; i < count; i++)
            originArgNames[i] = $"it{i + 1}";
        return originArgNames;
    }

    /// <summary>
    /// Returns true iff a function with the given name is visible AS A CALL at the
    /// specified arity — either a built-in / user-defined function in the global
    /// registry (`_dictionary`) OR a user function currently being defined
    /// (`_resultsBuilder.UserFunctionSignatures`). The latter store holds in-progress
    /// signatures so self-recursive call sites can resolve before the function's body
    /// finishes TIC.
    ///
    /// Use this predicate in any dispatch site that decides between "treat `id` as
    /// a function call" vs "fall through to field-call / hi-order / unknown-name".
    /// Consulting only `_dictionary.ContainsName` (without `_resultsBuilder`) caused
    /// MR6Bug1 — a self-recursive `?.foo()` inside `foo`'s body bypassed user-fn
    /// dispatch because `foo` wasn't yet in the global dict.
    /// </summary>
    private bool IsCallableAtArity(string name, int arity) =>
        _dictionary.GetOrNull(name, arity) != null
        || _resultsBuilder.GetUserFunctionSignature(name, arity) != null;

    public bool Visit(StructFieldAccessSyntaxNode node) {
        if (!node.Source.Accept(this))
            return false;

        // Field path narrowing: if "s.age" is narrowed, unwrap the optional field result.
        // SetFieldAccess produces opt(T) for optional fields. We wrap it with SetNarrowedVariable
        // to produce T instead.
        if (_narrowedFieldPaths != null && !node.IsSafeAccess
            && node.Source is NamedIdSyntaxNode srcVar) {
            var fieldPath = srcVar.Id + "." + node.FieldName;
            if (_narrowedFieldPaths.Contains(fieldPath)) {
                // Field is narrowed: access produces opt(T), we want T.
                // Use the same pattern as safe access narrowing:
                // 1. Create element type variable T
                // 2. Field access result = opt(T) via normal SetFieldAccess
                // 3. Merge field result with opt(T) to extract T
                // 4. Override node result to T
                _ticTypeGraph.SetFieldAccess(node.Source.OrderNumber, node.OrderNumber, node.FieldName);
                var fieldNode = _ticTypeGraph.GetOrCreateNode(node.OrderNumber);
                var elementNode = _ticTypeGraph.CreateVarType();
                var optNode = _ticTypeGraph.CreateVarType(StateOptional.Of(elementNode));
                SolvingFunctions.MergeInplace(optNode, fieldNode);
                // Replace field result with the unwrapped element
                fieldNode.State = new StateRefTo(elementNode);
                return true;
            }
        }

        if (node.IsSafeAccess || HasSafeAccessAncestor(node.Source))
            _ticTypeGraph.SetSafeFieldAccess(node.Source.OrderNumber, node.OrderNumber, node.FieldName);
        else
            _ticTypeGraph.SetFieldAccess(node.Source.OrderNumber, node.OrderNumber, node.FieldName,
                sourceTypeNameHint: GetNamedSourceTypeNameOrNull(node.Source));

        return true;
    }

    /// <summary>
    /// When the field-access source is a named variable bound to a named struct type,
    /// return that named TypeName. Used by SetFieldAccess to stamp the synthesized open
    /// struct with the same identity so downstream LCA preserves the μ-recursive cycle
    /// repair anchor
    /// </summary>
    private string GetNamedSourceTypeNameOrNull(ISyntaxNode source) {
        if (source is not NamedIdSyntaxNode varNode) return null;
        var localId = _aliasScope.GetVariableAlias(varNode.Id);
        if (!_ticTypeGraph.HasNamedNode(localId)) return null;
        var namedNode = _ticTypeGraph.GetNamedNode(localId).GetNonReference();
        return namedNode.State switch {
            StateStruct s => s.TypeName,
            ConstraintsState cs
                when cs.HasDescendant && cs.Descendant is StateStruct ds => ds.TypeName,
            _ => null
        };
    }

    private static bool HasSafeAccessAncestor(ISyntaxNode node) {
        var current = node;
        while (current is StructFieldAccessSyntaxNode f) {
            if (f.IsSafeAccess) return true;
            current = f.Source;
        }
        return false;
    }

    public bool Visit(StructInitSyntaxNode node) {
        if (!VisitChildren(node))
            return false;
        _ticTypeGraph.SetStructInit(
            node.Fields.SelectToArray(f => f.Name),
            node.Fields.SelectToArray(f => f.Node.OrderNumber), node.OrderNumber);
        // If OutputType is pre-set (from named type constructor expansion),
        // set ancestor constraint so TIC knows the struct shape for recursive types.
        // This ensures `none` defaults are recognized as Optional at any nesting depth.
        if (node.OutputType.BaseType != BaseFunnyType.Empty) {
            var ancestorState = ConvertType(node.OutputType);
            _ticTypeGraph.SetStructInitType(node.OrderNumber, ancestorState);

        }
        return true;
    }

    public bool Visit(DefaultValueSyntaxNode node) {
        _ticTypeGraph.SetGenericConst(node.OrderNumber);
        return true;
    }

    public bool Visit(AnonymFunctionSyntaxNode node) {
        _aliasScope.EnterScope(node.OrderNumber);
        foreach (var syntaxNode in node.ArgumentsDefinition)
        {
            //setup arguments.
            string originName;
            string anonymName;
            switch (syntaxNode)
            {
                case TypedVarDefSyntaxNode typed:
                {
                    //argument has type definition on it
                    originName = typed.Id;
                    anonymName = MakeAnonVariableName(node, originName);
                    if (typed.TypeSyntax is not TypeSyntax.EmptyType)
                    {
                        var ticType = ConvertType(ResolveType(typed.TypeSyntax));
                        _ticTypeGraph.SetVarType(anonymName, ticType);
                    }

                    break;
                }
                case NamedIdSyntaxNode varNode:
                    //argument has no type definition on it
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(node, originName);
                    break;
                default:
                    throw Errors.AnonymousFunArgumentIsIncorrect(syntaxNode);
            }

            _aliasScope.AddVariableAlias(originName, anonymName);
        }

        VisitChildren(node);

        var aliasNames = new string[node.ArgumentsDefinition.Count];
        for (var i = 0; i < node.ArgumentsDefinition.Count; i++)
        {
            var syntaxNode = node.ArgumentsDefinition[i];
            if (syntaxNode is TypedVarDefSyntaxNode typed)
                aliasNames[i] = _aliasScope.GetVariableAlias(typed.Id);
            else if (syntaxNode is NamedIdSyntaxNode varNode)
                aliasNames[i] = _aliasScope.GetVariableAlias(varNode.Id);
        }

#if DEBUG
        Trace(node, $"f({string.Join(" ", aliasNames)}):{node.OutputType}= {{{node.OrderNumber}}}");
#endif
        if (node.ReturnTypeSyntax is TypeSyntax.EmptyType)
            _ticTypeGraph.CreateLambda(node.Body.OrderNumber, node.OrderNumber, aliasNames);
        else
        {
            var retType = (ITypeState)ConvertType(ResolveType(node.ReturnTypeSyntax));
            _ticTypeGraph.CreateLambda(
                node.Body.OrderNumber,
                node.OrderNumber,
                retType,
                aliasNames);
        }

        _aliasScope.ExitScope();
        return true;
    }

    /// <summary>
    /// If we handle function call -
    /// it shows type of argument that currently handling
    /// if it is known
    /// </summary>
    private FunnyType _parentFunctionArgType = FunnyType.Empty;

    public bool Visit(FunCallSyntaxNode node) {
        // Resolve named arguments if needed
        ISyntaxNode[] allArgs;
        if (_hasUserFunctions || node.HasNamedArgs)
        {
            allArgs = ResolveNamedArgs(node);
            _resultsBuilder.RememberResolvedCallArgs(node.OrderNumber, allArgs);
        }
        else
        {
            allArgs = node.Args;
        }

        IFunctionSignature signature;
        if (_dialect.ExtensionFunctionsSeparation == ExtensionFunctionsSeparation.Enabled
            && node.IsPipeForward)
        {
            // Piped call: search the extension dict — functions registered as CallStyle.Both
            // (built-ins, plus user functions when separation is Disabled) also live there.
            signature = _dictionary.GetOrNull(node.Id, allArgs.Length, CallStyle.Extension);
        }
        else if (_dialect.ExtensionFunctionsSeparation == ExtensionFunctionsSeparation.Enabled
                 && !node.IsPipeForward && !node.IsOperator)
        {
            // Direct call: search the direct dict only.
            signature = _dictionary.GetOrNull(node.Id, allArgs.Length, CallStyle.Direct);
        }
        else
        {
            // No separation or operator: search across both dicts (legacy GetOrNull).
            signature = _dictionary.GetOrNull(node.Id, allArgs.Length);
        }

        // Fallback: built-in with default args, called with fewer positional args
        if (signature == null && !node.IsOperator)
        {
            var found = _dialect.ExtensionFunctionsSeparation == ExtensionFunctionsSeparation.Enabled
                ? _dictionary.FindOrNull(node.Id, allArgs.Length, Array.Empty<string>(),
                    node.IsPipeForward ? CallStyle.Extension : CallStyle.Direct)
                : _dictionary.FindOrNull(node.Id, allArgs.Length, Array.Empty<string>());
            if (found?.ArgProperties != null)
            {
                var props = found.ArgProperties;
                var extended = new ISyntaxNode[found.ArgTypes.Length];
                for (int i = 0; i < allArgs.Length; i++)
                    extended[i] = allArgs[i];
                bool valid = true;
                for (int i = allArgs.Length; i < extended.Length; i++)
                {
                    if (props[i].HasDefault && props[i].DefaultValue != null)
                        extended[i] = new ConstantSyntaxNode(
                            props[i].DefaultValue, found.ArgTypes[i], node.Interval)
                            { OrderNumber = _nextSyntheticId++ };
                    else { valid = false; break; }
                }
                if (valid)
                {
                    allArgs = extended;
                    signature = found;
                    _resultsBuilder.RememberResolvedCallArgs(node.OrderNumber, allArgs);
                }
            }
        }

        // Store signature on node directly — avoids dict write + dict read in ExpressionBuilder
        node.ResolvedSignature = signature;

        //Apply visitor to child types
        for (int i = 0; i < allArgs.Length; i++)
        {
            if (signature != null)
                _parentFunctionArgType = signature.ArgTypes[i];
            allArgs[i].Accept(this);
        }

        // ?? (null coalesce) — TIC special form: (opt(U), V) → LCA(U, V)
        // Unwraps left Optional, computes LCA with right. Supports optional right operand.
        if (node.Id == CoreFunNames.NullCoalesce && allArgs.Length == 2)
        {
#if DEBUG
            Trace(node, $"Coalesce({allArgs[0].OrderNumber},{allArgs[1].OrderNumber},{node.OrderNumber})");
#endif
            _ticTypeGraph.SetCoalesce(
                allArgs[0].OrderNumber,
                allArgs[1].OrderNumber,
                node.OrderNumber);
            return true;
        }

        // Safe array access (?[]) — handled as TIC graph operation (like ?.)
        // to properly flatten opt(opt(T)) via LCA instead of generic function wrapping
        if (node.Id == CoreFunNames.SafeGetElementName)
        {
#if DEBUG
            Trace(node, $"SafeArrayAccess({allArgs[0].OrderNumber},{allArgs[1].OrderNumber},{node.OrderNumber})");
#endif
            _ticTypeGraph.SetSafeArrayAccess(
                allArgs[0].OrderNumber,
                allArgs[1].OrderNumber,
                node.OrderNumber);
            return true;
        }

        //Setup ids arrays
        var ids = new int[allArgs.Length + 1];
        for (int i = 0; i < allArgs.Length; i++)
            ids[i] = allArgs[i].OrderNumber;
        ids[^1] = node.OrderNumber;

        // ?.method() on a struct-field-function (no callable named `Id` at this arity
        // anywhere — neither global registry nor currently-defining user functions):
        // emit as a single TIC special form so Pull threads opt→struct→fun in one cascade.
        // The legacy three-stage setup (unwrap + field-access + call below) created
        // three disjoint subgraphs that only joined via stale Pull edges — the chain
        // never resolved and the function's concrete return type was lost as Any?.
        // Mirrors SetSafeFieldAccess / SetSafeArrayAccess / SetCoalesce pattern. (MR5Bug7.)
        //
        // Function visibility must consult BOTH the global dictionary AND the
        // in-progress user-function-signatures table — otherwise a self-recursive
        // `?.foo()` inside `foo`'s body is mis-routed to SetSafeMethodCall (because
        // `foo` is not yet in the global dict during its own definition), bypassing
        // user-fn dispatch and crashing at runtime with "key 'foo' not in dict" when
        // SafePipedCallExpressionNode tries to access `foo` as a struct field. The
        // unified `IsCallableAtArity` predicate consolidates that visibility check.
        // (MR6Bug1.)
        if (node is FunCallSyntaxNode { IsSafeAccess: true, IsPipeForward: true } safePipedNode
            && signature == null && allArgs.Length >= 1
            && !IsCallableAtArity(node.Id, allArgs.Length))
        {
            var callArgIds = new int[ids.Length - 2]; // exclude source and result
            for (int i = 1; i < ids.Length - 1; i++)
                callArgIds[i - 1] = ids[i];
#if DEBUG
            Trace(node, $"SafeMethodCall {node.Id}({string.Join(",", callArgIds)}) on {ids[0]} -> {ids[^1]}");
#endif
            _ticTypeGraph.SetSafeMethodCall(ids[0], callArgIds, ids[^1], node.Id);
            safePipedNode.IsFieldCall = true;
            return true;
        }

        // ?.builtinMethod() — safe piped call on Optional where the method IS a known
        // built-in or user function (e.g. `arr?.count()`). Source is opt(T), function
        // expects T. Unwrap for the call, wrap result in opt(R).
        if (node is FunCallSyntaxNode { IsSafeAccess: true, IsPipeForward: true })
        {
            var sourceId = ids[0];
            var resultId = ids[^1];

            // ?.method() creates the same kind of opt-sourced constraint graph as
            // SetSafeFieldAccess — mark recursion so cycle-aware passes (visited-pair
            // guard in StagesExtension.Invoke, ScCClosurePass, etc.) are active.
            // Without this, anonymous-struct + fn-field cycles loop forever in
            // Destruction. (MR5Bug4.)
            _ticTypeGraph.IsRecursion = true;

            // 1. Unwrap source: create synthetic node for T, constrain source = opt(T)
            var unwrappedId = _nextSyntheticId++;
            var unwrappedNode = _ticTypeGraph.GetOrCreateNode(unwrappedId);
            _ticTypeGraph.SetCallArgument(
                StateOptional.Of(unwrappedNode), sourceId);
            ids[0] = unwrappedId; // function sees T, not opt(T)

            // 2. Wrap result: raw function result → actual result = LCA(raw, None) = opt(R)
            var rawResultId = _nextSyntheticId++;
            var rawResultNode = _ticTypeGraph.GetOrCreateNode(rawResultId);
            var noneNode = _ticTypeGraph.CreateVarType(None);
            // Both raw result and None are subtypes of the actual result → LCA gives opt(R)
            var actualResultNode = _ticTypeGraph.GetOrCreateNode(resultId);
            rawResultNode.AddAncestor(actualResultNode);
            noneNode.AddAncestor(actualResultNode);
            ids[^1] = rawResultId; // function writes to raw result
        }

        // #10: operators are never user functions — skip user function lookup.
        // For piped calls (arr.count()), prefer the builtin when one exists with matching name/arity.
        // This prevents accidental self-recursion when a user function wraps a builtin of the same name.
        // Pure user functions (no builtin match) still support piped recursive calls: fb(n).fb().
        var isPipedWithBuiltin = signature != null && node is FunCallSyntaxNode { IsPipeForward: true };
        StateFun userFunction;
        if (node.IsOperator || isPipedWithBuiltin)
        {
            userFunction = null;
        }
        else if (_dialect.ExtensionFunctionsSeparation == ExtensionFunctionsSeparation.Enabled)
        {
            // With separation: piped calls look for extension user functions, direct calls for regular ones.
            var ufKey = node.IsPipeForward ? ExtensionKeyPrefix + node.Id : node.Id;
            userFunction = _resultsBuilder.GetUserFunctionSignature(ufKey, allArgs.Length);
        }
        else
        {
            userFunction = _resultsBuilder.GetUserFunctionSignature(node.Id, allArgs.Length);
        }

        if (userFunction != null)
        {
            //Call user-function if it is being built at the same time as the current expression is being built
            //for example: recursive calls, or if function relates to global variables
#if DEBUG
            Trace(node, $"Call UF{node.Id}({string.Join(",", ids)})");
#endif
            _ticTypeGraph.SetCall(userFunction, ids);
            //in the case of generic user function  - we dont know generic arg types yet
            //we need to remember generic TIC signature to used it at the end of interpritation
            _resultsBuilder.RememberRecursiveCall(node.OrderNumber, userFunction);
            // Clear ResolvedSignature — it may point to a built-in with the same name/arity.
            // ExpressionBuilderVisitor must find the user function via _functions registry instead.
            node.ResolvedSignature = null;
            return true;
        }

        if (signature == null)
        {
            // Pipe-forward with no matching function at ANY arity: reinterpret a.f(args) as (a.f)(args).
            // Access struct field 'f' from the first arg, then call the result with remaining args.
            // Only when the name is not a known function — if it exists at another arity, it's a user typo,
            // and the "function not found" error is more helpful than a struct field type error.
            if (node is FunCallSyntaxNode { IsPipeForward: true } pipedNode && allArgs.Length >= 1
                && !_dictionary.ContainsName(node.Id))
            {
                var fieldNodeId = _nextSyntheticId++;
                // 1. Field access: target the (possibly already-unwrapped) source. When
                // IsSafeAccess+IsPipeForward fired at the top of this method, ids[0] was
                // remapped from `allArgs[0].OrderNumber` (the outer Optional source) to a
                // synthetic unwrappedNode (= the inner T). Using ids[0] here keeps the
                // field-access targeting the struct shape, not the Optional wrapper —
                _ticTypeGraph.SetFieldAccess(ids[0], fieldNodeId, node.Id);
                // 2. Call the field (lambda) with remaining args
                var callIds = new int[allArgs.Length]; // (remaining args) + result
                for (int i = 1; i < allArgs.Length; i++)
                    callIds[i - 1] = ids[i];
                callIds[^1] = ids[^1]; // result
#if DEBUG
                Trace(node, $"FieldCall {node.Id}: field@{fieldNodeId}, call({string.Join(",", callIds)})");
#endif
                _ticTypeGraph.SetCall(fieldNodeId, callIds);
                pipedNode.IsFieldCall = true;
                return true;
            }

            //Functional variable
            // Apply alias resolution symmetrically to NamedIdSyntaxNode (line 1480) — the
            // callee may be a lambda's implicit parameter (e.g. `rule it()` where `it` is
            // the rule's parameter and must resolve to the anonymous-scope alias
            // `anonymous_N::it`). Without this, `it` resolves to a fresh free variable
            // with arity matching the call site, bypassing the rule parameter's actual
            // arity and silently producing wrong-arity invocations. (MR4Bug2.)
            var calleeName = _aliasScope.GetVariableAlias(node.Id);
#if DEBUG
            Trace(node, $"Call hi order {calleeName}({string.Join(",", ids)})");
#endif
            _ticTypeGraph.SetCall(calleeName, ids);
            return true;
        }
        //Normal function call
#if DEBUG
        Trace(node, $"Call {node.Id}({string.Join(",", ids)})");
#endif
        // Special case: pow with non-constant or negative exponent → force (real, real) -> real
        if (node.Id == CoreFunNames.Pow && signature is PureGenericFunctionBase)
        {
            bool isConstNonNegativeInt = allArgs[1] is GenericIntSyntaxNode intNode
                && !(intNode.Value is long l && l < 0);

            if (!isConstNonNegativeInt)
            {
                // Force T = Real by constraining the generic type to [Real..Real]
                var genericType = _ticTypeGraph.InitializeVarNode(Real, Real, false);
                _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, new[] { genericType });
                _ticTypeGraph.SetCall(genericType, ids);
                return true;
            }
            // else: fall through to standard PureGenericFunctionBase handling
        }

        if (signature is PureGenericFunctionBase pure)
        {
            // Сase of (T,T):T signatures
            // This case is most common, so the call is optimized
            var genericType = InitializeGenericType(pure.Constrains[0]);
            _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, new[] { genericType });
            _ticTypeGraph.SetCall(genericType, ids);
            return true;
        }

        StateRefTo[] genericTypes;
        if (signature is GenericFunctionBase t)
        {
            // Optimization
            // Remember generic arguments to use it again at the built time
            genericTypes = InitializeGenericTypes(t.Constrains);
            // save refernces to generic types, for use at 'apply tic results' step
            _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, genericTypes);

            // Propagate preferred for generics that only appear in return type.
            // These are safe: no call-site arg can conflict with the preferred type.
            // Generics shared with arg types stay wide UNLESS at least one call-site
            // arg is the literal `none` — then the generic can't be pinned by callers
            // and the body's Preferred is the right default (GH #126).
            bool anyArgIsNone = false;
            for (int i = 0; i < allArgs.Length; i++) {
                var argNode = _ticTypeGraph.GetOrCreateNode(allArgs[i].OrderNumber).GetNonReference();
                if (argNode.State is StatePrimitive p && p == None) {
                    anyArgIsNone = true;
                    break;
                }
            }
            PropagateReturnOnlyPreferred(t.Constrains, t.ArgTypes, t.ReturnType, genericTypes, anyArgIsNone);
        }
        else genericTypes = Array.Empty<StateRefTo>();

        var types = new ITicNodeState[signature.ArgTypes.Length + 1];
        for (int i = 0; i < signature.ArgTypes.Length; i++)
            types[i] = ConvertFunArgType(signature.ArgTypes[i], genericTypes);
        types[^1] = ConvertFunArgType(signature.ReturnType, genericTypes);

        // Prevent IsOptionalElement leak: when a generic node appears both as element of
        // StateOptional and directly as StateRefTo, create a fresh proxy for the Optional element.
        // This isolates IsOptionalElement flag from the shared generic node.
        IsolateSharedOptionalElements(types);

        _ticTypeGraph.SetCall(types, ids);
        return true;
    }

    /// <summary>
    /// Converts a function argument FunnyType to TIC state, using the named type registry
    /// when available. This ensures NamedStruct types are properly expanded instead of
    /// being converted to ConstraintsState.Empty (which happens without the registry).
    /// </summary>
    private ITicNodeState ConvertFunArgType(FunnyType type, StateRefTo[] genericTypes) {
        if (genericTypes.Length == 0 && _namedTypeFieldRegistry != null)
            return type.ConvertToTiType(_namedTypeFieldRegistry);
        // When the function is generic (genericTypes non-empty), we still need to
        // expand NamedStruct args via the registry. Without the registry threading, a
        // generic function with a named-struct param (e.g. `applyFn(g, h:s) = g(h)`)
        // would surface NamedStruct as ConstraintsState.Empty at SetCallArgument, throwing
        // a bare NotSupportedException at the call-site setup.
        return type.ConvertToTiType(genericTypes, _namedTypeFieldRegistry);
    }

    /// <summary>
    /// When a generic TicNode appears both inside StateOptional and directly as StateRefTo,
    /// the IsOptionalElement flag leaks from the Optional context to all other uses.
    /// Fix: replace the shared element with a fresh proxy linked via MergeInplace (RefTo).
    /// The proxy gets IsOptionalElement; the original generic node stays clean.
    /// </summary>
    private void IsolateSharedOptionalElements(ITicNodeState[] types) {
        // Collect all nodes used directly (as StateRefTo)
        HashSet<TicNode> directNodes = null;
        for (int i = 0; i < types.Length; i++) {
            if (types[i] is StateRefTo refTo) {
                directNodes ??= new();
                directNodes.Add(refTo.Node);
            }
        }
        if (directNodes == null) return;

        // For any StateOptional whose element is also used directly, replace with fresh proxy
        for (int i = 0; i < types.Length; i++) {
            if (types[i] is StateOptional opt && directNodes.Contains(opt.ElementNode)) {
                var fresh = _ticTypeGraph.CreateVarType();
                SolvingFunctions.MergeInplace(opt.ElementNode, fresh);
                // fresh.State = RefTo(opt.ElementNode) — types unify, flags isolated
                types[i] = new StateOptional(fresh);
            }
        }
    }

    public bool Visit(ComparisonChainSyntaxNode node) {
        var ids = node.Operands.SelectToArray(o => {
            o.Accept(this);
            return o.OrderNumber;
        });
        var generics = node.Operators.SelectToArray(o => {
            var constrains = (o.Type == TokType.Equal || o.Type == TokType.NotEqual)
                ? GenericConstrains.Any
                : GenericConstrains.Comparable;
            return InitializeGenericType(constrains);
        });
        _ticTypeGraph.SetCompareChain(node.OrderNumber, generics, ids);
        return true;
    }

    public bool Visit(ResultFunCallSyntaxNode node) {
        VisitChildren(node);

        var ids = new int[node.Args.Length + 1];
        for (int i = 0; i < node.Args.Length; i++)
            ids[i] = node.Args[i].OrderNumber;
        ids[^1] = node.OrderNumber;

        _ticTypeGraph.SetCall(node.ResultExpression.OrderNumber, ids);
        return true;
    }

    public bool Visit(IfThenElseSyntaxNode node) {
        if (_dialect.OptionalTypesSupport == OptionalTypesSupport.Enabled)
            return VisitIfThenElseWithNarrowing(node);
        VisitChildren(node);
        SetupIfElseConstraints(node);
        return true;
    }

    private bool VisitIfThenElseWithNarrowing(IfThenElseSyntaxNode node) {
        // Accumulate WhenFalse from all conditions for the else branch.
        // if(x==none) ... if(z==none) ... else x+z
        // → else reached only when ALL conditions are false → union of all WhenFalse
        //
        // Progressive narrowing for multi-elif:
        // if(x == none) -1              ← condition[0] visited normally
        // if(x < 0) 0                   ← condition[1] visited with accumulated WhenFalse from [0]
        // else x                         ← else visited with all accumulated WhenFalse
        // When condition[0] is false → x != none → condition[1] and its expression see x narrowed.
        HashSet<string> elseNarrowed = null;

        for (int i = 0; i < node.Ifs.Length; i++) {
            var ifCase = node.Ifs[i];
            // Visit condition with accumulated narrowing from previous conditions being false.
            // For i=0, elseNarrowed is null → condition visited normally.
            // For i>0, accumulated WhenFalse from conditions[0..i-1] applies.
            if (elseNarrowed is { Count: > 0 })
                { if (!VisitWithNarrowing(ifCase.Condition, elseNarrowed, node.OrderNumber)) return false; }
            else
                { if (!ifCase.Condition.Accept(this)) return false; }
            var narrowing = NarrowingAnalyzer.Analyze(ifCase.Condition);
            // Expression sees: accumulated WhenFalse (from previous conditions) + current WhenTrue
            var exprNarrowed = Union(elseNarrowed, narrowing.WhenTrue);
            if (exprNarrowed is { Count: > 0 })
                { if (!VisitWithNarrowing(ifCase.Expression, exprNarrowed, node.OrderNumber)) return false; }
            else
                { if (!ifCase.Expression.Accept(this)) return false; }
            // Accumulate else narrowing: union of all WhenFalse
            if (narrowing.WhenFalse.Count > 0) {
                if (elseNarrowed == null) elseNarrowed = new(narrowing.WhenFalse);
                else elseNarrowed.UnionWith(narrowing.WhenFalse);
            }
        }

        // Else branch: all conditions were false → all WhenFalse facts apply
        if (elseNarrowed is { Count: > 0 })
            { if (!VisitWithNarrowing(node.ElseExpr, elseNarrowed, node.OrderNumber)) return false; }
        else
            { if (!node.ElseExpr.Accept(this)) return false; }
        SetupIfElseConstraints(node);
        return true;
    }

    private bool VisitWithNarrowing(ISyntaxNode expr, HashSet<string> narrowedVars, int scopeId) {
        if (narrowedVars.Count == 0) return expr.Accept(this);
        _aliasScope.EnterScope(scopeId);
        foreach (var id in narrowedVars) {
            if (NarrowingAnalyzer.IsFieldPath(id)) {
                // Field path narrowing: "s.age" → track for Visit(StructFieldAccessSyntaxNode)
                (_narrowedFieldPaths ??= new()).Add(id);
                continue;
            }
            // Inside lambdas, variables are aliased (e.g., "it" → "42::it").
            // Try the raw name first, then fall back to the aliased name.
            var resolvedId = id;
            if (!_ticTypeGraph.HasNamedNode(id)) {
                var aliasedId = _aliasScope.GetVariableAlias(id);
                if (aliasedId == id || !_ticTypeGraph.HasNamedNode(aliasedId))
                    continue;
                resolvedId = aliasedId;
            }
            var varNode = _ticTypeGraph.GetNamedNode(resolvedId);
            if (varNode.State is StatePrimitive
                || (varNode.State is ICompositeState cs && cs is not StateOptional))
                continue;
            var alias = scopeId + "~" + resolvedId;
            _aliasScope.AddVariableAlias(id, alias);
            _ticTypeGraph.SetNarrowedVariable(resolvedId, alias);
        }
        var result = expr.Accept(this);
        _aliasScope.ExitScope();
        // Clean up field paths after scope exit
        if (_narrowedFieldPaths != null) {
            foreach (var id in narrowedVars)
                if (NarrowingAnalyzer.IsFieldPath(id))
                    _narrowedFieldPaths.Remove(id);
        }
        return result;
    }

    /// <summary>Union of two narrowing sets, either of which may be null.</summary>
    private static HashSet<string> Union(
        HashSet<string> a,
        HashSet<string> b) {
        if (a == null || a.Count == 0) return b;
        if (b == null || b.Count == 0) return a;
        var result = new HashSet<string>(a);
        result.UnionWith(b);
        return result;
    }

    private void SetupIfElseConstraints(IfThenElseSyntaxNode node) {
        var conditions = node.Ifs.SelectToArray(i => i.Condition.OrderNumber);
        var expressions = node.Ifs.SelectToArrayAndAppendTail(
            tail: node.ElseExpr.OrderNumber,
            mapFunc: i => i.Expression.OrderNumber);
#if DEBUG
        Trace(node, $"if({string.Join(",", conditions)}): {string.Join(",", expressions)}");
#endif
        _ticTypeGraph.SetIfElse(conditions, expressions, node.OrderNumber);
    }

    public bool Visit(IfCaseSyntaxNode node) => VisitChildren(node);

    public bool Visit(ConstantSyntaxNode node) {
#if DEBUG
        Trace(node, $"Constant {node.Value}:{node.ClrTypeName}");
#endif
        var type = ConvertType(node.OutputType);

        if (type is StatePrimitive p)
            _ticTypeGraph.SetConst(node.OrderNumber, p);
        else if (type is StateArray a && a.Element is StatePrimitive primitiveElement)
            _ticTypeGraph.SetArrayConst(node.OrderNumber, primitiveElement);
        else if (type is StateOptional opt)
            // Optional-typed constant — e.g. an `int? = 5` parameter default.
            _ticTypeGraph.SetOptionalConst(node.OrderNumber, opt);
        else if (type is StateStruct s && s.IsSolved)
            // Constant-folded struct literal — e.g. `opts:{x:int}={x=0}` parameter
            // default, or named-type ctor default `a: p = p{x=0,y=0}` (after
            // NamedTypeElaborator expansion). Bind to the solved struct shape.
            _ticTypeGraph.SetStructConst(node.OrderNumber, s);
        else
            throw new InvalidOperationException("Complex constant type is not supported");
        return true;
    }

    public bool Visit(GenericIntSyntaxNode node) {
#if DEBUG
        Trace(node, $"IntConst {node.Value}:{(node.IsHexOrBin ? "hex" : "int")}");
#endif
        if (node.IsHexOrBin)
        {
            //hex or bin constant
            //can be u8:< c:< i96
            ulong actualValue;
            if (node.Value is long l)
            {
                if (l > 0) actualValue = (ulong)l;
                else
                {
                    //negative constant
                    if (l >= sbyte.MinValue)
                        _ticTypeGraph.SetGenericConst(node.OrderNumber, I8, I64, I32);
                    else if (l >= Int16.MinValue)
                        _ticTypeGraph.SetGenericConst(node.OrderNumber, I16, I64, I32);
                    else if (l >= Int32.MinValue)
                        _ticTypeGraph.SetGenericConst(node.OrderNumber, I32, I64, I32);
                    else _ticTypeGraph.SetConst(node.OrderNumber, I64);
                    return true;
                }
            }
            else if (node.Value is ulong u)
                actualValue = u;
            else
                throw new NFunImpossibleException("Generic token has to be ulong or long");

            //positive constant
            // 0..127 — fits U4 (lattice bottom): widens to ANY numeric incl. I8.
            if (actualValue <= (ulong)SByte.MaxValue)
                _ticTypeGraph.SetGenericConst(node.OrderNumber, U4, I96, I32);
            else if (actualValue <= byte.MaxValue)
                _ticTypeGraph.SetGenericConst(node.OrderNumber, U8, I96, I32);
            else if (actualValue <= (ulong)Int16.MaxValue)
                _ticTypeGraph.SetGenericConst(node.OrderNumber, U12, I96, I32);
            else if (actualValue <= (ulong)UInt16.MaxValue)
                _ticTypeGraph.SetGenericConst(node.OrderNumber, U16, I96, I32);
            else if (actualValue <= (ulong)Int32.MaxValue)
                _ticTypeGraph.SetGenericConst(node.OrderNumber, U24, I96, I32);
            else if (actualValue <= (ulong)UInt32.MaxValue)
                _ticTypeGraph.SetGenericConst(node.OrderNumber, U32, I96, I64);
            else if (actualValue <= (ulong)Int64.MaxValue)
                _ticTypeGraph.SetGenericConst(node.OrderNumber, U48, I96, I64);
            else
                _ticTypeGraph.SetConst(node.OrderNumber, U64);
        }
        else
        {
            //1,2,3
            //Can be u8:<c:<real
            StatePrimitive descendant;
            ulong actualValue;
            if (node.Value is long l)
            {
                if (l > 0) actualValue = (ulong)l;
                else
                {
                    descendant = l switch {
                                     //negative constant
                                     >= sbyte.MinValue => I8,
                                     >= Int16.MinValue => I16,
                                     >= Int32.MinValue => I32,
                                     _                 => I64
                                 };
                    // Negative literals are at most I64.MinValue, always fit Int64; Preferred I32
                    // is enough for values >= I32.MinValue, fall to I64 for larger negatives.
                    var preferred = descendant == I64 ? I64 : GetPreferredIntConstantType();
                    _ticTypeGraph.SetGenericConst(node.OrderNumber, descendant, Real, preferred);
                    return true;
                }
            }
            else if (node.Value is ulong u)
                actualValue = u;
            else
                throw new NFunImpossibleException("Generic token has to be ulong or long");

            //positive constant
            // For values that exceed Int32.Max, the Preferred I32 cannot represent the value,
            // and unconstrained resolution would fall through to the Ancestor (Real) — silently
            // widening integer literals to Real (MR4Bug1: `out = 4294967295 → Real`).
            // Mirror hex/bin's strategy: switch Preferred to I64 when the value > Int32.Max,
            // so resolution picks Int64 instead of Real. Ancestor stays Real so generic
            // contexts (user functions, Arithmetics operators) still accept Real operands.
            StatePrimitive posPreferred;
            // 0..127 — fits U4 (lattice bottom): widens to ANY numeric incl. I8.
            if (actualValue <= (ulong)SByte.MaxValue) { descendant = U4; posPreferred = GetPreferredIntConstantType(); }
            else if (actualValue <= byte.MaxValue) { descendant = U8; posPreferred = GetPreferredIntConstantType(); }
            else if (actualValue <= (ulong)Int16.MaxValue) { descendant = U12; posPreferred = GetPreferredIntConstantType(); }
            else if (actualValue <= (ulong)UInt16.MaxValue) { descendant = U16; posPreferred = GetPreferredIntConstantType(); }
            else if (actualValue <= (ulong)Int32.MaxValue) { descendant = U24; posPreferred = GetPreferredIntConstantType(); }
            else if (actualValue <= (ulong)UInt32.MaxValue) { descendant = U32; posPreferred = I64; }
            else if (actualValue <= (ulong)Int64.MaxValue) { descendant = U48; posPreferred = I64; }
            else {
                _ticTypeGraph.SetConst(node.OrderNumber, U64);
                return true;
            }
            _ticTypeGraph.SetGenericConst(node.OrderNumber, descendant, Real, posPreferred);
        }

        return true;
    }

    private StatePrimitive GetPreferredIntConstantType() =>
        _dialect.IntegerPreferredType switch {
            IntegerPreferredType.I32  => I32,
            IntegerPreferredType.I64  => I64,
            IntegerPreferredType.Real => Real,
            _                         => null
        };

    public bool Visit(IpAddressConstantSyntaxNode node) {
        _ticTypeGraph.SetConst(node.OrderNumber, Ip);
        return true;
    }

    public bool Visit(NamedIdSyntaxNode node) {
        var id = node.Id;
#if DEBUG
        Trace(node, $"VAR {id} ");
#endif
        //nfun syntax allows multiple variables to have the same name depending on whether they are functions or not
        //need to know what type of argument is expected - is it variableId, or functionId?
        //if it is function - how many arguments are expected ?
        var argType = _parentFunctionArgType;
        if (argType.BaseType == BaseFunnyType.Fun) // functional argument is expected
        {
            var argsCount = argType.FunTypeSpecification.Inputs.Length;
            var signature = _dictionary.GetOrNull(id, argsCount);
            if (signature != null)
            {
                if (signature is GenericFunctionBase genericFunction)
                {
                    var generics = InitializeGenericTypes(genericFunction.Constrains);
                    _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, generics);

                    _ticTypeGraph.SetVarType(
                        $"g'{argsCount}'{id}",
                        genericFunction.GetTicFunType(generics));
                    _ticTypeGraph.SetVar($"g'{argsCount}'{id}", node.OrderNumber);

                    node.IdType = NamedIdNodeType.GenericFunction;
                }
                else
                {
                    _ticTypeGraph.SetVarType($"f'{argsCount}'{id}", signature.GetTicFunType());
                    _ticTypeGraph.SetVar($"f'{argsCount}'{id}", node.OrderNumber);

                    node.IdType = NamedIdNodeType.ConcreteFunction;
                }

                _resultsBuilder.RememberFunctionalVariable(node.OrderNumber, signature);
                return true;
            }
        }
        // At this point we are sure - ID is not a function

        // ID can be constant or apriori variable or usual (not apriori) variable
        // if ID exists in ticTypeGraph - then ID is Variable
        // else if ID exists in constant list - then ID is constant
        // else ID is variable

        if (!_ticTypeGraph.HasNamedNode(id) && _constants.TryGetConstant(id, out var constant))
        {
            //ID is constant
            node.IdType = NamedIdNodeType.Constant;
            node.IdContent = constant;

            var tiType = ConvertType(constant.Type);
            switch (tiType)
            {
                case StatePrimitive primitive:
                    _ticTypeGraph.SetConst(node.OrderNumber, primitive);
                    break;
                case StateArray { Element: StatePrimitive primitiveElement }:
                    _ticTypeGraph.SetArrayConst(node.OrderNumber, primitiveElement);
                    break;
                case StateStruct @struct:
                {
                    if (@struct.IsSolved)
                        _ticTypeGraph.SetStructConst(node.OrderNumber, @struct);
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        "Type " +
                        constant.Type +
                        " is not supported for constants");
            }
        }
        else
        {
            //ID is variable
            var localId = _aliasScope.GetVariableAlias(node.Id);

            _ticTypeGraph.SetVar(localId, node.OrderNumber);
            node.IdType = NamedIdNodeType.Variable;
        }

        return true;
    }

    public bool Visit(TypedVarDefSyntaxNode node) {
        VisitChildren(node);
#if DEBUG
        Trace(node, $"Tvar {node.Id}:{ResolveType(node.TypeSyntax)}  ");
#endif
        if (node.TypeSyntax is not TypeSyntax.EmptyType)
        {
            ThrowIfOptionalTypeDisabled(ResolveType(node.TypeSyntax), node.Id, node.Interval);
            var type = ConvertType(ResolveType(node.TypeSyntax));
            if (!_ticTypeGraph.TrySetVarType(node.Id, type))
                throw Errors.VariableIsAlreadyDeclared(node.Id, node.Interval);
        }

        return true;
    }

    public bool Visit(VarDefinitionSyntaxNode node) {
        VisitChildren(node);

#if DEBUG
        Trace(node, $"VarDef {node.Id}:{ResolveType(node.TypeSyntax)}  ");
#endif
        ThrowIfOptionalTypeDisabled(ResolveType(node.TypeSyntax), node.Id, node.Interval);
        var type = ConvertType(ResolveType(node.TypeSyntax));
        if (!_ticTypeGraph.TrySetVarType(node.Id, type))
            throw Errors.VariableIsAlreadyDeclared(node.Id, node.Interval);
        return true;
    }

    public bool Visit(ListOfExpressionsSyntaxNode node) => VisitChildren(node);

    #region privates

    private void ThrowIfOptionalTypeDisabled(FunnyType funnyType, string varId, Interval interval) {
        if (_dialect.OptionalTypesSupport == OptionalTypesSupport.Enabled)
            return;
        if (ContainsOptional(funnyType))
            throw Errors.OptionalTypeNotSupported(varId, interval);
    }

    private static bool ContainsOptional(FunnyType type) =>
        type.BaseType switch {
            BaseFunnyType.Optional => true,
            BaseFunnyType.ArrayOf  => ContainsOptional(type.ArrayTypeSpecification.FunnyType),
            BaseFunnyType.None     => true,
            _                      => false
        };

    private StateRefTo[] InitializeGenericTypes(GenericConstrains[] constrains) {
        var genericTypes = new StateRefTo[constrains.Length];
        for (int i = 0; i < constrains.Length; i++)
            genericTypes[i] = InitializeGenericType(constrains[i], genericTypes);

        return genericTypes;
    }

    /// <summary>
    /// For each generic that has Preferred, set Preferred on the call-site constraint
    /// node when it is safe to do so. Two safe cases:
    ///   1. T appears ONLY in return type (no arg can conflict)
    ///       — covers f() = 2+3 → resolves to Int32.
    ///   2. T appears in args AND at least one call-site arg is the literal `none`
    ///       — none doesn't pin T (none ≤ opt(anything)), so the body's Preferred
    ///         is the right default. Covers GH #126 (s(none) returning Real instead
    ///         of Int).
    /// Polymorphic functions like fib(n) where T is pinned by a typed-or-untyped
    /// input variable still stay wide (no arg is StatePrimitive.None there).
    /// </summary>
    private static void PropagateReturnOnlyPreferred(
        GenericConstrains[] constrains, FunnyType[] argTypes, FunnyType returnType,
        StateRefTo[] genericTypes, bool anyCallArgIsNone) {
        for (int i = 0; i < constrains.Length; i++) {
            if (constrains[i].Preferred == null)
                continue;
            // Check if this generic index appears in any arg type
            bool appearsInArgs = false;
            for (int a = 0; a < argTypes.Length; a++) {
                if (TypeContainsGeneric(argTypes[a], i)) {
                    appearsInArgs = true;
                    break;
                }
            }
            bool safe = !appearsInArgs || anyCallArgIsNone;
            if (safe) {
                var node = genericTypes[i].Node.GetNonReference();
                if (node.State is ConstraintsState cs && cs.Preferred == null)
                    cs.Preferred = constrains[i].Preferred;
            }
        }
    }

    private static bool TypeContainsGeneric(FunnyType type, int genericIndex) {
        if (type.BaseType == BaseFunnyType.Generic)
            return type.GenericId == genericIndex;
        if (type.BaseType == BaseFunnyType.ArrayOf)
            return TypeContainsGeneric(type.ArrayTypeSpecification.FunnyType, genericIndex);
        if (type.BaseType == BaseFunnyType.Optional)
            return TypeContainsGeneric(type.OptionalTypeSpecification.ElementType, genericIndex);
        if (type.BaseType == BaseFunnyType.Fun) {
            var spec = type.FunTypeSpecification;
            if (TypeContainsGeneric(spec.Output, genericIndex)) return true;
            foreach (var input in spec.Inputs)
                if (TypeContainsGeneric(input, genericIndex)) return true;
        }
        if (type.BaseType == BaseFunnyType.Struct)
            foreach (var field in type.StructTypeSpecification)
                if (TypeContainsGeneric(field.Value, genericIndex)) return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StateRefTo InitializeGenericType(GenericConstrains constrains, StateRefTo[] genericTypes = null) {
        if (constrains.HasStructDescendant && genericTypes != null)
        {
            // Convert the FunnyType struct to a TIC StateStruct, resolving
            // generic field references (Generic(i)) to already-initialized type variables.
            var ticStruct = (ITypeState)constrains.StructDescendant.ConvertToTiType(genericTypes);
            return _ticTypeGraph.InitializeVarNode(ticStruct, constrains.Ancestor, constrains.IsComparable);
        }
        // F-bounded generic at call site: initialize empty CS here; StructBound is populated
        // in the second pass of InitializeGenericTypes (which has all genericTypes ready
        // for resolving self-RefTo via Generic(i) markers).
        if (constrains.HasStructBound)
        {
            return _ticTypeGraph.InitializeVarNode(null, null, false);
        }
        // Do NOT propagate preferred into call-site TIC graph.
        // Preferred is used only in CreateSomeConcrete (fallback for uncalled functions).
        // At call sites, preferred arrives naturally via TryConvertConstToRef from literal args.
        return _ticTypeGraph.InitializeVarNode(
            constrains.Descendant,
            constrains.Ancestor,
            constrains.IsComparable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Trace(ISyntaxNode node, string text) {
#if DEBUG
        if (TraceLog.IsEnabled)
            TraceLog.WriteLine($"Exit:{node.OrderNumber}. {text} ");
#endif
    }

    private static string MakeAnonVariableName(ISyntaxNode node, string id)
        => LangTiHelper.GetArgAlias("anonymous_" + node.OrderNumber, id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool VisitChildren(ISyntaxNode node) {
        foreach (var child in node.Children)
        {
            if (!child.Accept(this))
                return false;
        }

        return true;
    }

    #endregion

    public bool Visit(BinOperatorSyntaxNode node) {
        var signature = _dictionary.GetOrNull(node.Id, 2);
        node.ResolvedSignature = signature;

        if (signature != null) _parentFunctionArgType = signature.ArgTypes[0];
        node.Left.Accept(this);

        // Progressive narrowing in AND: after visiting left side of `x != none and x > 0`,
        // the right side `x > 0` should see x as narrowed (non-optional).
        // Progressive narrowing in OR: after visiting left side of `x == none or x < 0`,
        // if left is false (x != none), right side should see x as narrowed.
        if (_dialect.OptionalTypesSupport == OptionalTypesSupport.Enabled) {
            var leftNarrowing = NarrowingAnalyzer.Analyze(node.Left);
            var narrowSet = node.Op == BinOp.And ? leftNarrowing.WhenTrue
                          : node.Op == BinOp.Or  ? leftNarrowing.WhenFalse
                          : null;
            if (narrowSet is { Count: > 0 }) {
                if (signature != null) _parentFunctionArgType = signature.ArgTypes[1];
                return VisitWithNarrowing(node.Right, narrowSet, node.OrderNumber)
                    && FinishBinOp(node, signature);
            }
        }

        if (signature != null) _parentFunctionArgType = signature.ArgTypes[1];
        node.Right.Accept(this);

        return FinishBinOp(node, signature);
    }

    private bool FinishBinOp(BinOperatorSyntaxNode node, IFunctionSignature signature) {
        // Special case: pow with non-constant or negative exponent
        if (node.Op == BinOp.Pow && signature is PureGenericFunctionBase)
        {
            bool isConstNonNegativeInt = node.Right is GenericIntSyntaxNode intNode
                && !(intNode.Value is long l && l < 0);
            if (!isConstNonNegativeInt)
            {
                var genericType = _ticTypeGraph.InitializeVarNode(Real, Real, false);
                _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, new[] { genericType });
                _ticTypeGraph.SetCall(genericType,
                    node.Left.OrderNumber, node.Right.OrderNumber, node.OrderNumber);
                return true;
            }
        }

        if (signature is PureGenericFunctionBase pure)
        {
            var genericType = InitializeGenericType(pure.Constrains[0]);
            _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, new[] { genericType });
            _ticTypeGraph.SetCall(genericType,
                node.Left.OrderNumber, node.Right.OrderNumber, node.OrderNumber);
            return true;
        }

        // Fallback for non-pure generic operators (rare)
        var ids = new[] { node.Left.OrderNumber, node.Right.OrderNumber, node.OrderNumber };
        if (signature == null) { _ticTypeGraph.SetCall(node.Id, ids); return true; }
        if (signature is GenericFunctionBase g) {
            var genericTypes = InitializeGenericTypes(g.Constrains);
            _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, genericTypes);
            var types = new ITicNodeState[] {
                signature.ArgTypes[0].ConvertToTiType(genericTypes),
                signature.ArgTypes[1].ConvertToTiType(genericTypes),
                signature.ReturnType.ConvertToTiType(genericTypes) };
            _ticTypeGraph.SetCall(types, ids);
        } else {
            var types = new ITicNodeState[] {
                ConvertType(signature.ArgTypes[0]),
                ConvertType(signature.ArgTypes[1]),
                ConvertType(signature.ReturnType) };
            _ticTypeGraph.SetCall(types, ids);
        }
        return true;
    }

    public bool Visit(UnaryOperatorSyntaxNode node) {
        var signature = _dictionary.GetOrNull(node.Id, 1);
        node.ResolvedSignature = signature;

        if (signature != null) _parentFunctionArgType = signature.ArgTypes[0];
        node.Operand.Accept(this);

        if (signature is PureGenericFunctionBase pure)
        {
            var genericType = InitializeGenericType(pure.Constrains[0]);
            _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, new[] { genericType });
            _ticTypeGraph.SetCall(genericType,
                node.Operand.OrderNumber, node.OrderNumber);
            return true;
        }

        var ids = new[] { node.Operand.OrderNumber, node.OrderNumber };
        if (signature == null) { _ticTypeGraph.SetCall(node.Id, ids); return true; }
        if (signature is GenericFunctionBase g) {
            var genericTypes = InitializeGenericTypes(g.Constrains);
            _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, genericTypes);
            var types = new ITicNodeState[] {
                signature.ArgTypes[0].ConvertToTiType(genericTypes),
                signature.ReturnType.ConvertToTiType(genericTypes) };
            _ticTypeGraph.SetCall(types, ids);
        } else {
            var types = new ITicNodeState[] {
                ConvertType(signature.ArgTypes[0]),
                ConvertType(signature.ReturnType) };
            _ticTypeGraph.SetCall(types, ids);
        }
        return true;
    }

    public bool Visit(TypeDeclarationSyntaxNode node) =>
        throw new NFunImpossibleException("TypeDeclarationSyntaxNode should be removed during elaboration");

    public bool Visit(NamedTypeConstructorSyntaxNode node) =>
        throw new NFunImpossibleException("NamedTypeConstructorSyntaxNode should be removed during elaboration");

    public bool Visit(TryCatchSyntaxNode node) {
        // Visit try expression (no error variable scope)
        node.TryExpr.Accept(this);

        if (node.ErrorVariableName != null) {
            // Enter scope for catch body — error variable visible only inside
            _aliasScope.EnterScope(node.OrderNumber);
            var aliasName = node.OrderNumber + "~" + node.ErrorVariableName;
            _aliasScope.AddVariableAlias(node.ErrorVariableName, aliasName);

            // Create error variable: {message: text, data: any}
            var charType = new StatePrimitive(PrimitiveTypeName.Char);
            var textType = (ITicNodeState)new StateArray(
                TicNode.CreateTypeVariableNode(charType));
            var any = (ITicNodeState)new StatePrimitive(
                PrimitiveTypeName.Any);
            var errorType = StateStruct.Of(
                ("message", textType),
                ("data", any));
            _ticTypeGraph.SetVarType(aliasName, errorType);

            // Visit catch expression inside scope
            node.CatchExpr.Accept(this);
            _aliasScope.ExitScope();
        } else {
            node.CatchExpr.Accept(this);
        }

        // try and catch branches unify like if-else: result = LCA(try, catch)
        var expressions = new[] { node.TryExpr.OrderNumber, node.CatchExpr.OrderNumber };
        _ticTypeGraph.SetIfElse(Array.Empty<int>(), expressions, node.OrderNumber);
        return true;
    }
}
