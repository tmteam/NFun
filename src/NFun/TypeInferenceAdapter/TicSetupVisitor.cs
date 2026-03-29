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
    private readonly IFunctionDictionary _dictionary;
    private readonly IConstantList _constants;
    private readonly TypeInferenceResultsBuilder _resultsBuilder;
    private readonly DialectSettings _dialect;

    public static bool SetupTicForBody(
        SyntaxTree tree,
        GraphBuilder ticGraph,
        TypeInferenceResultsBuilder results, TypeBehaviour typeBehaviour) => SetupTicForBody(
        tree, ticGraph,
        BaseFunctions.GetFunctions(typeBehaviour),
        EmptyConstantList.Instance,
        EmptyAprioriTypesMap.Instance,
        EmptyCustomTypeRegistry.Instance,
        results, Dialects.Origin);

    internal static bool SetupTicForBody(
        SyntaxTree tree,
        GraphBuilder ticGraph,
        IFunctionDictionary functions,
        IConstantList constants,
        IAprioriTypesMap aprioriTypes,
        ICustomTypeRegistry customTypes,
        TypeInferenceResultsBuilder results,
        DialectSettings dialect) {
        var visitor = new TicSetupVisitor(ticGraph, functions, constants, results, aprioriTypes, dialect, customTypes);

        // Collect user function definitions for named argument resolution
        foreach (var syntaxNode in tree.Children)
        {
            if (syntaxNode is UserFunctionDefinitionSyntaxNode ufn)
                visitor._userFunctions[(ufn.Id, ufn.Args.Count)] = ufn;
        }

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
        IFunctionDictionary functions,
        IConstantList constants,
        TypeInferenceResultsBuilder results,
        DialectSettings dialect,
        ICustomTypeRegistry customTypes = null) {
        var visitor = new TicSetupVisitor(ticGraph, functions, constants, results, EmptyAprioriTypesMap.Instance, dialect, customTypes);
        // Register this function for named arg resolution in recursive calls
        visitor._userFunctions[(userFunctionNode.Id, userFunctionNode.Args.Count)] = userFunctionNode;
        return userFunctionNode.Accept(visitor);
    }

    private readonly ICustomTypeRegistry _customTypes;
    internal readonly Dictionary<(string, int), UserFunctionDefinitionSyntaxNode> _userFunctions = new();
    internal const int SyntheticIdStart = 100000;
    private int _nextSyntheticId = SyntheticIdStart; // synthetic node IDs start high to avoid collision

    private TicSetupVisitor(
        GraphBuilder ticTypeGraph,
        IFunctionDictionary dictionary,
        IConstantList constants,
        TypeInferenceResultsBuilder resultsBuilder,
        IAprioriTypesMap aprioriTypesMap,
        DialectSettings dialect,
        ICustomTypeRegistry customTypes = null) {
        _aliasScope = new VariableScopeAliasTable();
        _dictionary = dictionary;
        _constants = constants;
        _resultsBuilder = resultsBuilder;
        _dialect = dialect;
        _ticTypeGraph = ticTypeGraph;
        _customTypes = customTypes ?? EmptyCustomTypeRegistry.Instance;

        foreach (var apriori in aprioriTypesMap)
            _ticTypeGraph.SetVarType(apriori.Name, apriori.Type.ConvertToTiType());
    }

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

        // Fast path: no named args, no defaults, no params, exact match
        bool hasParams = userFun.Args.Count > 0 && userFun.Args[^1].IsParams;
        bool hasDefaults = false;
        foreach (var arg in userFun.Args) if (arg.HasDefault) { hasDefaults = true; break; }

        if (!node.HasNamedArgs && !hasParams && !hasDefaults && userFun.Args.Count == node.Args.Length)
            return node.Args;

        var paramCount = userFun.Args.Count;
        var nonParamsCount = hasParams ? paramCount - 1 : paramCount;
        var result = new ISyntaxNode[paramCount];

        // Fill positional args (up to non-params count)
        int positionalFillCount = Math.Min(node.Args.Length, nonParamsCount);
        for (int i = 0; i < positionalFillCount; i++)
            result[i] = node.Args[i];

        // Collect extra positional args into array for params
        if (hasParams)
        {
            var paramsIndex = paramCount - 1;
            if (node.Args.Length > nonParamsCount)
            {
                // Extra args → synthetic array
                var extraArgs = new ISyntaxNode[node.Args.Length - nonParamsCount];
                for (int i = 0; i < extraArgs.Length; i++)
                    extraArgs[i] = node.Args[nonParamsCount + i];
                var arr = new ArraySyntaxNode(extraArgs, node.Interval);
                arr.OrderNumber = _nextSyntheticId++;
                result[paramsIndex] = arr;
            }
            else
            {
                // No extra args → empty synthetic array
                var arr = new ArraySyntaxNode(Array.Empty<ISyntaxNode>(), node.Interval);
                arr.OrderNumber = _nextSyntheticId++;
                result[paramsIndex] = arr;
            }
        }

        // Fill named args
        var paramsIdx = hasParams ? paramCount - 1 : -1;
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
            // Named arg targeting params slot replaces the synthetic array
            if (paramIndex == paramsIdx)
                result[paramIndex] = named.Value;
            else if (result[paramIndex] != null)
                throw Errors.DuplicateNamedArgument(node.Id, named.Name, named.NameInterval);
            else
                result[paramIndex] = named.Value;
        }

        // Fill defaults for missing non-params args.
        for (int i = 0; i < nonParamsCount; i++)
        {
            if (result[i] == null)
            {
                if (!userFun.Args[i].HasDefault)
                    throw Errors.MissingArgument(node.Id, userFun.Args[i].Id, node.Interval);

                var defaultExpr = userFun.Args[i].DefaultValue;

                if (userFun.Args[i].PrecomputedDefaultValue != null)
                {
                    // Precomputed: create ConstantSyntaxNode with correct type + fresh OrderNumber
                    result[i] = new ConstantSyntaxNode(
                        userFun.Args[i].PrecomputedDefaultValue,
                        userFun.Args[i].PrecomputedDefaultType,
                        defaultExpr.Interval) {
                        OrderNumber = _nextSyntheticId++,
                    };
                }
                else if (userFun.Args[i].TypeSyntax is not TypeSyntax.EmptyType)
                {
                    // Typed param but precomputation failed (none, empty containers, etc.)
                    // Use DefaultValueSyntaxNode: TIC constrains via SetCall,
                    // ExpressionBuilder uses GetDefaultFunnyValue() for the type.
                    result[i] = new DefaultValueSyntaxNode(defaultExpr.Interval) {
                        OrderNumber = _nextSyntheticId++,
                    };
                }
                else
                {
                    result[i] = defaultExpr;
                }
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

        // Verify all args filled
        for (int i = 0; i < totalArgs; i++)
        {
            if (result[i] == null)
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
        _userFunctions.TryGetValue((name, argCount), out var ufn) ? ufn : null;

    /// <summary>Find user function by name where provided arg count fits considering defaults and params</summary>
    private UserFunctionDefinitionSyntaxNode FindUserFunctionByNameWithDefaults(string name, int providedArgs, int maxArgs) {
        foreach (var ((fn, _), ufn) in _userFunctions)
        {
            if (!string.Equals(fn, name, StringComparison.OrdinalIgnoreCase))
                continue;
            bool hasParams = ufn.Args.Count > 0 && ufn.Args[^1].IsParams;
            var requiredCount = 0;
            var maxCount = ufn.Args.Count;
            foreach (var arg in ufn.Args)
                if (!arg.HasDefault && !arg.IsParams) requiredCount++;

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
            var type = resolvedType.ConvertToTiType();
            if (!_ticTypeGraph.TrySetVarType(node.Id, type))
                throw Errors.VariableIsAlreadyDeclared(node.Id, node.Interval);
        }

        _ticTypeGraph.SetDef(node.Id, node.Expression.OrderNumber);
        return true;
    }

    public bool Visit(UserFunctionDefinitionSyntaxNode node) {
        var argNames = new string[node.Args.Count];
        int i = 0;
        foreach (var arg in node.Args)
        {
            argNames[i] = arg.Id;
            i++;
            if (arg.TypeSyntax is not TypeSyntax.EmptyType)
            {
                var resolvedType = ResolveType(arg.TypeSyntax);
                ThrowIfOptionalTypeDisabled(resolvedType, arg.Id, arg.Interval);
                _ticTypeGraph.SetVarType(arg.Id, resolvedType.ConvertToTiType());
            }
        }

        ITypeState returnType = null;
        if (node.ReturnTypeSyntax is not TypeSyntax.EmptyType)
            returnType = (ITypeState)ResolveType(node.ReturnTypeSyntax).ConvertToTiType();

#if DEBUG
        TraceLog.WriteLine(
            $"Enter {node.OrderNumber}. UFun {node.Id}({string.Join(",", argNames)})->{node.Body.OrderNumber}:{returnType?.ToString() ?? "empty"}");
#endif
        var fun = _ticTypeGraph.SetFunDef(
            name: $"{node.Id}'{node.Args.Count}",
            returnId: node.Body.OrderNumber,
            returnType: returnType,
            varNames: argNames);
        _resultsBuilder.RememberUserFunctionSignature(node.Id, fun);

        var result = VisitChildren(node);

        // Visit default value expressions explicitly for TYPED parameters only.
        // ExitVisitorBase.Visit(TypedVarDefSyntaxNode) returns true without visiting
        // children, so DefaultValue is never visited by VisitChildren.
        // Then constrain with parameter type so TIC propagates annotations.
        foreach (var arg in node.Args)
        {
            if (arg.HasDefault && arg.TypeSyntax is not TypeSyntax.EmptyType)
            {
                arg.DefaultValue.Accept(this);
                _ticTypeGraph.SetDefaultValueConstraint(arg.Id, arg.DefaultValue.OrderNumber);
            }
        }

        return result;
    }

    public bool Visit(ArraySyntaxNode node) {
        VisitChildren(node);
        var elementIds = new int[node.Expressions.Count];
        for (int i = 0; i < node.Expressions.Count; i++)
            elementIds[i] = node.Expressions[i].OrderNumber;
#if DEBUG
        Trace(node, $"[{string.Join(",", elementIds)}]");
#endif
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
                if (visiting is NamedIdSyntaxNode named && Helper.DoesItLooksLikeSuperAnonymousVariable(named.Id, out int num))
                {
                    //we found variable!
                    if (num == -1)
                        hasSimpleIt = true;
                    else if (num == 0 || num > 3)
                        throw Errors.InvalidSuperAnonymousVariableName(named.Interval, named.Id);
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

    public bool Visit(StructFieldAccessSyntaxNode node) {
        if (!node.Source.Accept(this))
            return false;
        if (node.IsSafeAccess || HasSafeAccessAncestor(node.Source))
            _ticTypeGraph.SetSafeFieldAccess(node.Source.OrderNumber, node.OrderNumber, node.FieldName.ToLower());
        else
            _ticTypeGraph.SetFieldAccess(node.Source.OrderNumber, node.OrderNumber, node.FieldName.ToLower());
        return true;
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
            node.Fields.SelectToArray(f => f.Name.ToLower()),
            node.Fields.SelectToArray(f => f.Node.OrderNumber), node.OrderNumber);
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
                        var ticType = ResolveType(typed.TypeSyntax).ConvertToTiType();
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
            var retType = (ITypeState)ResolveType(node.ReturnTypeSyntax).ConvertToTiType();
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
        // Resolve named arguments → build merged positional arg list
        var allArgs = ResolveNamedArgs(node);
        _resultsBuilder.RememberResolvedCallArgs(node.OrderNumber, allArgs);

        var signature = _dictionary.GetOrNull(node.Id, allArgs.Length);
        if (signature != null)
            _resultsBuilder.RememberResolvedCallSignature(node.OrderNumber, signature);

        //Apply visitor to child types
        for (int i = 0; i < allArgs.Length; i++)
        {
            if (signature != null)
                _parentFunctionArgType = signature.ArgTypes[i];
            allArgs[i].Accept(this);
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

        var userFunction = _resultsBuilder.GetUserFunctionSignature(node.Id, allArgs.Length);
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
            return true;
        }

        if (signature == null)
        {
            //Functional variable
#if DEBUG
            Trace(node, $"Call hi order {node.Id}({string.Join(",", ids)})");
#endif
            _ticTypeGraph.SetCall(node.Id, ids);
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
        }
        else genericTypes = Array.Empty<StateRefTo>();

        var types = new ITicNodeState[signature.ArgTypes.Length + 1];
        for (int i = 0; i < signature.ArgTypes.Length; i++)
            types[i] = signature.ArgTypes[i].ConvertToTiType(genericTypes);
        types[^1] = signature.ReturnType.ConvertToTiType(genericTypes);

        _ticTypeGraph.SetCall(types, ids);
        return true;
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
        VisitChildren(node);

        var conditions = node.Ifs.SelectToArray(i => i.Condition.OrderNumber);
        var expressions = node.Ifs.SelectToArrayAndAppendTail(
            tail: node.ElseExpr.OrderNumber,
            mapFunc: i => i.Expression.OrderNumber);

#if DEBUG
        Trace(node, $"if({string.Join(",", conditions)}): {string.Join(",", expressions)}");
#endif

        _ticTypeGraph.SetIfElse(
            conditions,
            expressions,
            node.OrderNumber);
        return true;
    }

    public bool Visit(IfCaseSyntaxNode node) => VisitChildren(node);

    public bool Visit(ConstantSyntaxNode node) {
#if DEBUG
        Trace(node, $"Constant {node.Value}:{node.ClrTypeName}");
#endif
        var type = node.OutputType.ConvertToTiType();

        if (type is StatePrimitive p)
            _ticTypeGraph.SetConst(node.OrderNumber, p);
        else if (type is StateArray a && a.Element is StatePrimitive primitiveElement)
            _ticTypeGraph.SetArrayConst(node.OrderNumber, primitiveElement);
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
                    if (l >= Int16.MinValue)
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
            if (actualValue <= byte.MaxValue)
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
                                     >= Int16.MinValue => I16,
                                     >= Int32.MinValue => I32,
                                     _                 => I64
                                 };
                    var preferred = GetPreferredIntConstantType();
                    _ticTypeGraph.SetGenericConst(node.OrderNumber, descendant, Real, preferred);
                    return true;
                }
            }
            else if (node.Value is ulong u)
                actualValue = u;
            else
                throw new NFunImpossibleException("Generic token has to be ulong or long");

            //positive constant
            if (actualValue <= byte.MaxValue) descendant = U8;
            else if (actualValue <= (ulong)Int16.MaxValue) descendant = U12;
            else if (actualValue <= (ulong)UInt16.MaxValue) descendant = U16;
            else if (actualValue <= (ulong)Int32.MaxValue) descendant = U24;
            else if (actualValue <= (ulong)UInt32.MaxValue) descendant = U32;
            else if (actualValue <= (ulong)Int64.MaxValue) descendant = U48;
            else descendant = U64;
            _ticTypeGraph.SetGenericConst(
                node.OrderNumber, descendant, Real,
                GetPreferredIntConstantType());
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

            var tiType = constant.Type.ConvertToTiType();
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
            var type = ResolveType(node.TypeSyntax).ConvertToTiType();
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
        var type = ResolveType(node.TypeSyntax).ConvertToTiType();
        if (!_ticTypeGraph.TrySetVarType(node.Id, type))
            throw Errors.VariableIsAlreadyDeclared(node.Id, node.Interval);
        return true;
    }

    public bool Visit(ListOfExpressionsSyntaxNode node) => VisitChildren(node);

    #region privates

    private void ThrowIfOptionalTypeDisabled(FunnyType funnyType, string varId, Interval interval) {
        if (_dialect.OptionalTypesSupport == OptionalTypesSupport.ExperimentalEnabled)
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
            genericTypes[i] = InitializeGenericType(constrains[i]);

        return genericTypes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StateRefTo InitializeGenericType(GenericConstrains constrains) =>
        _ticTypeGraph.InitializeVarNode(
            constrains.Descendant,
            constrains.Ancestor,
            constrains.IsComparable);

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
}
