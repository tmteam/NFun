using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.Errors;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Interpretation;

internal static class RuntimeBuilder {
    internal static FunnyRuntime Build(
        string script,
        IFunctionRegistry functionRegistry,
        DialectSettings dialect,
        IConstantList constants = null,
        IAprioriTypesMap aprioriTypesMap = null,
        ICustomTypeRegistry customTypes = null) {

        var flow = Tokenizer.ToFlow(script, dialect.AllowNewlineInStrings == AllowNewlineInStrings.Deny);
        var syntaxTree = Parser.Parse(flow);

        // Named types: elaborate only when enabled, skip entirely otherwise
        INamedTypeFieldRegistry namedTypeFieldRegistry = null;
        TypeRegistry typeRegistry = null;
        if (dialect.NamedTypesSupport == NamedTypesSupport.Enabled)
        {
            syntaxTree = NamedTypeElaborator.Elaborate(syntaxTree, out var namedTypes);
            if (namedTypes.Count > 0)
            {
                // Pass 1: register all struct type names as NamedStruct (for forward refs)
                foreach (var nt in namedTypes)
                    if (!nt.Value.IsAlias)
                        customTypes = customTypes.CloneWith(nt.Key, FunnyType.NamedStructOf(nt.Key));

                // Collect aliases and detect which need by-name pre-registration.
                // An alias body that references ANY alias name (its own or another's)
                // participates in a recursion — self-recursive (`type x = rule()->x?`)
                // or mutual (`type a = rule()->b?; type b = rule()->a?`). Such aliases
                // get a NamedStructOf placeholder seeded before pass 2 so their bodies
                // resolve cleanly with self/mutual references carried by name through
                // TIC and the runtime.
                var aliases = new List<KeyValuePair<string, NamedTypeDefinition>>();
                var aliasNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var nt in namedTypes)
                    if (nt.Value.IsAlias)
                    {
                        aliases.Add(nt);
                        aliasNames.Add(nt.Key);
                    }
                foreach (var alias in aliases)
                {
                    if (TypeSyntaxContainsAnyName(alias.Value.AliasTypeSyntax, aliasNames))
                        customTypes = customTypes.CloneWith(alias.Key, FunnyType.NamedStructOf(alias.Key));
                }

                // Pass 2: resolve aliases. May need multiple passes for chaining (type b = a; type a = int).
                // Most aliases resolve in one pass. Second pass only if there were unresolved forward refs.
                // For recursive aliases the pre-registered NamedStructOf placeholder makes self-references
                // resolve cleanly; the alias's final resolved type contains NamedStructOf at the recursive
                // positions, which carries the by-name identity through TIC and the runtime.
                for (int pass = 0; pass < aliases.Count + 1; pass++)
                {
                    bool anyUnresolved = false;
                    foreach (var alias in aliases)
                    {
                        // For recursion-touched aliases the placeholder is already
                        // present — we still want to overwrite it with the fully-
                        // resolved type.
                        bool isRecursionTouched = TypeSyntaxContainsAnyName(alias.Value.AliasTypeSyntax, aliasNames);
                        if (!isRecursionTouched && customTypes.TryResolve(alias.Key, out _))
                            continue; // already resolved (non-recursive forward-ref case)
                        try {
                            var resolved = TypeSyntaxResolver.Resolve(alias.Value.AliasTypeSyntax, customTypes);
                            customTypes = customTypes.CloneWith(alias.Key, resolved);
                        } catch {
                            anyUnresolved = true; // forward ref — try next pass
                        }
                    }
                    if (!anyUnresolved) break;
                }

                // Pass 3: build field registry for struct types
                var hasStructTypes = false;
                var registry = new NamedTypeFieldRegistry();
                foreach (var nt in namedTypes)
                {
                    if (nt.Value.IsAlias)
                        continue;
                    hasStructTypes = true;
                    var fields = new (string name, FunnyType type)[nt.Value.Fields.Count];
                    for (int i = 0; i < nt.Value.Fields.Count; i++)
                    {
                        var f = nt.Value.Fields[i];
                        var fieldType = f.TypeSyntax is TypeSyntax.EmptyType
                            ? InferTypeFromConstantOrAny(f.DefaultValue)
                            : TypeSyntaxResolver.Resolve(f.TypeSyntax, customTypes);
                        // Eager-validate constant default against declared type. Per Basics.md
                        // Construction stage: "checking the correctness of the script and
                        // calculating the types of all expressions in the script". A bad default
                        // (e.g. `type t = {x:int = 'hello'}`) must be rejected at declaration
                        // time, not lazily when the default is finally triggered. Skip when the
                        // default is a non-constant expression (Any fallback) — those still go
                        // through TIC at use site. (MR4Bug4.)
                        if (f.HasType && f.HasDefault) {
                            var inferred = InferTypeFromConstantOrAny(f.DefaultValue);
                            if (inferred.BaseType != BaseFunnyType.Any
                                && VarTypeConverter.GetConverterOrNull(
                                    dialect.Converter.TypeBehaviour, inferred, fieldType) == null) {
                                throw Errors.TypeFieldDefaultMismatch(
                                    nt.Key, f.Name, fieldType, inferred, f.DefaultValue.Interval);
                            }
                        }
                        fields[i] = (f.Name, fieldType);
                    }
                    registry.Register(nt.Key, fields);
                }
                if (hasStructTypes)
                    namedTypeFieldRegistry = registry;

                // Build TypeRegistry for runtime introspection
                var typeInfos = new Dictionary<string, NamedTypeInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var nt in namedTypes) {
                    if (nt.Value.IsAlias) {
                        customTypes.TryResolve(nt.Key, out var resolved);
                        typeInfos[nt.Key] = new NamedTypeInfo(nt.Key, resolved);
                    } else {
                        var fieldInfos = new NamedTypeFieldInfo[nt.Value.Fields.Count];
                        for (int i = 0; i < nt.Value.Fields.Count; i++) {
                            var f = nt.Value.Fields[i];
                            var ft = f.TypeSyntax is TypeSyntax.EmptyType
                                ? InferTypeFromConstantOrAny(f.DefaultValue)
                                : TypeSyntaxResolver.Resolve(f.TypeSyntax, customTypes);
                            fieldInfos[i] = new NamedTypeFieldInfo(f.Name, ft, f.HasDefault);
                        }
                        customTypes.TryResolve(nt.Key, out var structType);
                        typeInfos[nt.Key] = new NamedTypeInfo(nt.Key, structType, fieldInfos);
                    }
                }
                typeRegistry = new TypeRegistry(typeInfos);
            }
        }

        //Set node numbers
        var setNodeNumberVisitor = new SetNodeNumberVisitor(0);
        syntaxTree.ComeOver(setNodeNumberVisitor);
        syntaxTree.MaxNodeId = setNodeNumberVisitor.LastUsedNumber;
        syntaxTree.IsSimpleBody = setNodeNumberVisitor.IsSimpleBody;

        return Build(
            syntaxTree,
            functionRegistry,
            EnsureBuiltInConstants(constants, dialect.Converter),
            aprioriTypesMap?? EmptyAprioriTypesMap.Instance,
            customTypes, dialect,
            namedTypeFieldRegistry,
            typeRegistry);
    }

    /// <summary>
    private static IConstantList EnsureBuiltInConstants(IConstantList userConstants, FunnyConverter converter) {
        if (userConstants == null)
            return converter.TypeBehaviour is RealIsDoubleTypeBehaviour
                ? BuiltInConstantList.Double
                : BuiltInConstantList.Decimal;
        if (userConstants is ConstantList cl) {
            cl.AddBuiltIns();
            return cl;
        }
        var list = new ConstantList(converter);
        list.AddBuiltIns();
        return list;
    }

    private static FunnyRuntime Build(
        SyntaxTree syntaxTree,
        IFunctionRegistry functionsRegistry,
        IConstantList constants,
        IAprioriTypesMap aprioriTypes,
        ICustomTypeRegistry customTypes,
        DialectSettings dialect,
        INamedTypeFieldRegistry namedTypeFieldRegistry = null,
        TypeRegistry typeRegistry = null) {
        #region build user functions

        //get topology sort of the functions call
        //result is the order of functions that need to be compiled
        //functions that not references other functions have to be compiled firstly
        //Then those functions will be compiled
        //that refer to already compiled functions
        var solveGroups = syntaxTree.FindFunctionSolvingOrderOrThrow(dialect.ExtensionFunctionsSeparation);

        // Flatten groups for the existing single-function build path that needs all
        // user functions (for cross-function name resolution in TIC setup).
        int totalFunctions = 0;
        foreach (var g in solveGroups) totalFunctions += g.Length;
        var flatOrder = new UserFunctionDefinitionSyntaxNode[totalFunctions];
        int flatIdx = 0;
        foreach (var g in solveGroups)
            foreach (var fn in g) flatOrder[flatIdx++] = fn;

        IUserFunction[] userFunctions;
        IFunctionRegistry functionRegistry;
        if (totalFunctions == 0)
        {
            functionRegistry = functionsRegistry;
            userFunctions = Array.Empty<IUserFunction>();
        }
        else
        {
            userFunctions = new IUserFunction[totalFunctions];

            var scopeFunctionDictionary = new ScopeFunctionRegistry(functionsRegistry, totalFunctions);
            functionRegistry = scopeFunctionDictionary;

            int builtCount = 0;
            foreach (var group in solveGroups)
            {
                foreach (var fn in group)
                {
                    if (dialect.AllowUserFunctions == AllowUserFunctions.DenyUserFunctions)
                        throw Errors.UserFunctionIsDenied(fn.Interval);
                    if (dialect.AllowUserFunctions == AllowUserFunctions.DenyRecursive && fn.IsRecursive)
                        throw Errors.RecursiveUserFunctionIsDenied(fn.Interval);
                }

                if (group.Length > 1)
                {
                    // Mutual recursion SCC: solve all functions in ONE TIC graph,
                    // then build each runtime from the shared solve. Damas-Milner
                    // let-rec / ML's `let rec ... and ...` semantics.
                    var built = BuildMutualRecursiveGroup(
                        group,
                        constants,
                        scopeFunctionDictionary,
                        dialect,
                        customTypes,
                        namedTypeFieldRegistry,
                        flatOrder);
                    for (int k = 0; k < built.Length; k++)
                        userFunctions[builtCount++] = built[k];
                }
                else
                {
                    userFunctions[builtCount++] = BuildFunctionAndPutItToDictionary(
                        group[0],
                        constants,
                        scopeFunctionDictionary,
                        dialect,
                        customTypes,
                        namedTypeFieldRegistry,
                        flatOrder);
                }
            }
        }

        #endregion


        if(TraceLog.IsEnabled)
            TraceLog.WriteLine($"\r\n==== BUILD BODY ====");

        var bodyTypeSolving = SolveBodyTypes(syntaxTree, constants, functionRegistry, aprioriTypes, customTypes, dialect, namedTypeFieldRegistry);

        #region build body

        var variables = new VariableDictionary();
        var equations = new List<Equation>();

        foreach (var treeNode in syntaxTree.Nodes)
        {
            if (treeNode is EquationSyntaxNode node)
            {
                var equation =
                    BuildEquationAndPutItToVariables(node, functionRegistry, variables, bodyTypeSolving, dialect);
                equations.Add(equation);

                if (!variables.TryAdd(equation.OutputVariableSource))
                {
                    var alreadyExist = variables.GetOrNull(equation.OutputVariableSource.Name);
                    var usage = equations.FindFirstUsageOrNull(alreadyExist);
                    //some equation referenced the source before
                    if (equation.OutputVariableSource.IsOutput)
                        throw Errors.OutputNameWithDifferentCase(equation.Id, usage?.Interval ?? equation.Expression.Interval);
                    else
                        throw Errors.CannotUseOutputValueBeforeItIsDeclared(alreadyExist, usage);
                }

                if (Helper.DoesItLooksLikeSuperAnonymousVariable(equation.Id))
                    throw Errors.CannotUseSuperAnonymousVariableHere(
                        new Interval(node.Interval.Start, node.Interval.Start + node.Id.Length),
                        equation.Id);
                if (TraceLog.IsEnabled)
                    TraceLog.WriteLine($"\r\nEQUATION: {equation.Id}:{equation.Expression.Type} = ... \r\n");
            }
            else if (treeNode is VarDefinitionSyntaxNode varDef)
            {
                if (Helper.DoesItLooksLikeSuperAnonymousVariable(varDef.Id))
                    throw Errors.CannotUseSuperAnonymousVariableHere(varDef.Interval, varDef.Id);

                var resolvedType = TypeSyntaxResolver.Resolve(varDef.TypeSyntax, customTypes);
                var variableSource = VariableSource.CreateWithStrictTypeLabel(
                    varDef.Id,
                    resolvedType,
                    varDef.Interval,
                    FunnyVarAccess.Input,
                    dialect.Converter,
                    varDef.Attributes);
                if (!variables.TryAdd(variableSource))
                {
                    var alreadyExisted = variables.GetOrNull(variableSource.Name);
                    // The variable already exists. Two distinct cases:
                    //   - it was USED in a prior equation but never declared
                    //     → "used before declared" with the usage's interval.
                    //   - it was DEFINED in a prior equation (`y = 5`) and now
                    //     gets a separate type-annotation (`y:int`) → clean
                    //     "already declared" (BugHunt-stmt #74; previously
                    //     crashed with raw InvalidOperationException "Sequence
                    //     contains no matching element" because the find-usage
                    //     fall-through assumed the prior reference was a use).
                    var usage = equations.FindFirstUsageOrNull(alreadyExisted);
                    if (usage != null)
                        throw Errors.VariableIsDeclaredAfterUsing(variableSource.Name, usage.Interval);
                    throw Errors.VariableIsAlreadyDeclared(variableSource.Name, varDef.Interval);
                }

                if (TraceLog.IsEnabled)
                    TraceLog.WriteLine($"\r\nVARIABLE: {variableSource.Name}:{variableSource.Type} = ... \r\n");
            }
            else if (treeNode is UserFunctionDefinitionSyntaxNode)
                continue; //user function was built above
            else
                throw new InvalidOperationException($"Type {treeNode} is not supported as tree root");
        }

        #endregion


        foreach (var userFunction in userFunctions)
        {
            if (userFunction is GenericUserFunction generic && generic.BuiltCount == 0)
            {
                // Generic function is declared but concrete was not built.
                // We have to build it at least once to search all possible errors and figure out - is it recursive or not
                GenericUserFunction.CreateSomeConcrete(generic);
            }

            var source = variables.GetOrNull(userFunction.Name);
            if(source!=null)
            {
                var usage = equations.FindFirstUsageOrNull(source);
                throw Errors.FunctionNameAndVariableNameConflict(source, usage);
            }
        }

        return new FunnyRuntime(equations, variables, userFunctions, dialect.Converter, typeRegistry);
    }

    private static TypeInferenceResults SolveBodyTypes(
        SyntaxTree syntaxTree,
        IConstantList constants,
        IFunctionRegistry functionRegistry,
        IAprioriTypesMap aprioriTypes,
        ICustomTypeRegistry customTypes,
        DialectSettings dialect,
        INamedTypeFieldRegistry namedTypeFieldRegistry = null) {

        var bodyTypeSolving = RuntimeBuilderHelper.SolveBodyOrThrow(
            syntaxTree, functionRegistry, constants, aprioriTypes, customTypes, dialect,
            out var typesApplied, namedTypeFieldRegistry);

        // When SPS already applied types to syntax nodes, skip the ComeOver walk
        if (!typesApplied) {
            var enterVisitor = new ApplyTiResultEnterVisitor(bodyTypeSolving, TicTypesConverter.Concrete);
            foreach (var syntaxNode in syntaxTree.Nodes)
            {
                //function nodes were solved above
                if (syntaxNode is UserFunctionDefinitionSyntaxNode)
                    continue;

                //set types to nodes
                syntaxNode.ComeOver(enterVisitor);
            }
        }

        return bodyTypeSolving;
    }

    private static Equation BuildEquationAndPutItToVariables(
        EquationSyntaxNode equation,
        IFunctionRegistry functionsRegistry,
        VariableDictionary mutableVariables,
        TypeInferenceResults typeInferenceResults,
        DialectSettings dialect) {
        if(TraceLog.IsEnabled)
            TraceLog.WriteLine($"\r\n==== BUILD EQUATION '{equation.Id}' ====");

        var expression = ExpressionBuilderVisitor.BuildExpression(
            node: equation.Expression,
            functions: functionsRegistry,
            outputType: equation.OutputType,
            variables: mutableVariables,
            typeInferenceResults: typeInferenceResults,
            typesConverter: TicTypesConverter.Concrete,
            dialect: dialect);

        // Use expression.Type when the builder corrected the output type
        // (e.g., coalesce strips Optional when right operand is non-optional).
        var resolvedType = equation.OutputTypeSpecified ? equation.OutputType : expression.Type;

        VariableSource outputVariableSource;
        if (equation.OutputTypeSpecified)
            outputVariableSource = VariableSource.CreateWithStrictTypeLabel(
                name: equation.Id,
                type: resolvedType,
                typeSpecificationIntervalOrNull: equation.TypeSpecificationOrNull.Interval,
                access: FunnyVarAccess.Output,
                typeBehaviour: dialect.Converter,
                attributes: equation.Attributes
            );
        else
            outputVariableSource = VariableSource.CreateWithoutStrictTypeLabel(
                name: equation.Id,
                type: resolvedType,
                access: FunnyVarAccess.Output,
                dialect.Converter,
                equation.Attributes
            );

        var itVariable = mutableVariables
            .GetAll()
            .FirstOrDefault(c => Helper.DoesItLooksLikeSuperAnonymousVariable(c.Name));
        if (itVariable!=null)
        {
            var expressionNode = expression.FindFirstUsageOrNull(itVariable);
            throw Errors.CannotUseSuperAnonymousVariableHere(expressionNode.Interval, itVariable.Name);
        }

        if(outputVariableSource.Type != expression.Type)
            AssertChecks.Panic("fitless");

        return new Equation(equation.Id, expression, outputVariableSource);
    }


    private static IUserFunction BuildFunctionAndPutItToDictionary(
        UserFunctionDefinitionSyntaxNode functionSyntaxNode,
        IConstantList constants,
        ScopeFunctionRegistry functionsRegistry,
        DialectSettings dialect,
        ICustomTypeRegistry customTypes = null,
        INamedTypeFieldRegistry namedTypeFieldRegistry = null,
        UserFunctionDefinitionSyntaxNode[] allUserFunctions = null) {

        if(TraceLog.IsEnabled)
            TraceLog.WriteLine($"\r\n==== BUILD {functionSyntaxNode.Id}(..) ====");

        ////introduce function variable
        var graph = new GraphBuilder();
        // Pre-analysis already determined recursion in FindFunctionSolvingOrder
        // (FindFunctionDependenciesVisitor → IsRecursive). Propagate that
        // determination to TIC so cycle-aware destruction-time passes can
        // early-exit when no μ-recursion is possible.
        if (functionSyntaxNode.IsRecursive)
            graph.IsRecursion = true;
        var resultsBuilder = new TypeInferenceResultsBuilder();
        ITicResults types;

        try
        {
            if(!TicSetupVisitor.SetupTicForUserFunction(
                userFunctionNode: functionSyntaxNode,
                ticGraph: graph,
                functions: functionsRegistry,
                constants: constants,
                results: resultsBuilder,
                dialect: dialect,
                customTypes: customTypes,
                namedTypeFieldRegistry: namedTypeFieldRegistry,
                allUserFunctions: allUserFunctions))
                AssertChecks.Panic($"User Function '{functionSyntaxNode.Head}' was not solved due unknown reasons ");
            // solve the types. We ignore prefered types to get most common ancestor for function argument types instead of preferred type
            types = graph.Solve(ignorePrefered: true);
        }
        catch (TicException e)
        {
            throw Errors.TranslateTicError(e, functionSyntaxNode, graph, functionsRegistry);
        }

        resultsBuilder.SetResults(types);
        var typeInferenceResuls = resultsBuilder.Build();

        // Post-body-solve freeze pass. Once function body solving completes, the signature's
        // structural shape is determined. Mark reachable StateStructs as IsFrozen=true so script-
        // body Pull cannot widen them by absorbing caller's fields. Algebraic analog of TAPL §22.6
        // generalization closing the type at the let-boundary.
        // Only anonymous recursive-cycle structs are frozen by this pass (named structs are
        // frozen at namedTypeFieldRegistry time; non-recursive anonymous structs stay
        // row-polymorphic for caller-side merging). Anonymous μ-cycles in a function's signature
        // arise only from a recursive call pattern, so the function must be IsRecursive — gate
        // the walk to spend zero time on non-recursive user functions.
        if (functionSyntaxNode.IsRecursive)
            FreezeFunctionSignatureStructs(typeInferenceResuls, functionSyntaxNode);

        // Classify by EXTERNAL signature, not by body's residual ConstraintsState. Operators like
        // `==` and `+` inside a body leave CS placeholders in TypeInferenceResults.Generics even
        // when the function's actual arg/return types are fully concrete. Routing those to the
        // generic path triggers GenericFunctionBase ctor failure ("Type X has wrong generic
        // definition") because argTypes/retType carry no Generic(i) positions. Probe the
        // signature directly: if it is fully solved, take the concrete path regardless of body CS.
        var ticSignature = (Tic.SolvingStates.StateFun)typeInferenceResuls.GetVariableType(
            functionSyntaxNode.Id + "'" + functionSyntaxNode.Args.Count);
        bool signatureIsConcrete = SignatureIsFullyConcrete(ticSignature);

        if (!types.HasGenerics || signatureIsConcrete)
        {
            #region concreteFunction


            //set types to nodes
            functionSyntaxNode.ComeOver(
                enterVisitor: new ApplyTiResultEnterVisitor(
                    solving: typeInferenceResuls,
                    tiToLangTypeConverter: TicTypesConverter.Concrete));

            // Precompute default values AFTER ApplyTiResults so expression nodes have OutputType
            PrecomputeDefaultValues(functionSyntaxNode, typeInferenceResuls, functionsRegistry, dialect, customTypes);

            var funType = TicTypesConverter.Concrete.Convert(
                typeInferenceResuls.GetVariableType(functionSyntaxNode.Id + "'" + functionSyntaxNode.Args.Count));

            var returnType = funType.FunTypeSpecification.Output;
            var argTypes = funType.FunTypeSpecification.Inputs;

            // For named struct params, use NamedStructOf in the external prototype.
            // The function body uses the structural expansion (argTypes).
            // Call sites use NamedStructOf to avoid depth-mismatch in recursive types.
            var protoArgTypes = argTypes;
            if (namedTypeFieldRegistry != null) {
                FunnyType[] overridden = null;
                for (int i = 0; i < functionSyntaxNode.Args.Count && i < argTypes.Length; i++) {
                    if (functionSyntaxNode.Args[i].TypeSyntax is TypeSyntax.EmptyType)
                        continue;
                    var resolved = TypeSyntaxResolver.Resolve(
                        functionSyntaxNode.Args[i].TypeSyntax, customTypes);
                    if (resolved.BaseType == BaseFunnyType.NamedStruct) {
                        if (overridden == null) {
                            overridden = new FunnyType[argTypes.Length];
                            Array.Copy(argTypes, overridden, argTypes.Length);
                        }
                        overridden[i] = resolved;
                    }
                }
                if (overridden != null) protoArgTypes = overridden;
            }

            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"\r\n=====> Generic {functionSyntaxNode.Id} {funType}");
            //make function prototype
            var effectiveCallStyle = EffectiveCallStyle(functionSyntaxNode.IsExtension, dialect);
            var prototype = new ConcreteUserFunctionPrototype(functionSyntaxNode.Id, returnType, protoArgTypes, effectiveCallStyle);
            //add prototype to dictionary for future use
            var registryKey = TicSetupVisitor.GetRegistryKey(prototype.Name, prototype.CallStyle == CallStyle.Extension, dialect.ExtensionFunctionsSeparation);
            functionsRegistry.Add(registryKey, prototype);
            var function =
                functionSyntaxNode.BuildConcrete(
                    argTypes: argTypes,
                    returnType: returnType,
                    functionsRegistry: functionsRegistry,
                    results: typeInferenceResuls,
                    converter: TicTypesConverter.Concrete,
                    dialect: dialect);

            prototype.SetActual(function);
            return function;

            #endregion
        }
        else
        {
            // For generic functions, precompute defaults with best-effort type resolution
            PrecomputeDefaultValues(functionSyntaxNode, typeInferenceResuls, functionsRegistry, dialect, customTypes);
            var function = GenericUserFunction.Create(
                typeInferenceResuls, functionSyntaxNode, functionsRegistry,
                dialect, EffectiveCallStyle(functionSyntaxNode.IsExtension, dialect),
                namedTypeFieldRegistry);
            var genRegistryKey = TicSetupVisitor.GetRegistryKey(function.Name, function.CallStyle == CallStyle.Extension, dialect.ExtensionFunctionsSeparation);
            functionsRegistry.Add(genRegistryKey, function);
            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"\r\n=====> Concrete {functionSyntaxNode.Id} {function}");
            return function;
        }
    }

    /// <summary>
    /// Build a mutually-recursive function group as ONE TIC solve, then produce
    /// runtime functions for each member from the shared results.
    ///
    /// Phase A: register prototypes for every function so they can reference
    /// each other in their bodies. Phase B: build each runtime body — peer
    /// calls resolve through the prototype registry.
    /// </summary>
    private static IUserFunction[] BuildMutualRecursiveGroup(
        UserFunctionDefinitionSyntaxNode[] group,
        IConstantList constants,
        ScopeFunctionRegistry functionsRegistry,
        DialectSettings dialect,
        ICustomTypeRegistry customTypes,
        INamedTypeFieldRegistry namedTypeFieldRegistry,
        UserFunctionDefinitionSyntaxNode[] allUserFunctions) {
        if (TraceLog.IsEnabled)
            TraceLog.WriteLine($"\r\n==== BUILD GROUP [{string.Join(",", System.Linq.Enumerable.Select(group, f => f.Id))}] ====");

        var graph = new GraphBuilder { IsRecursion = true };
        var resultsBuilder = new TypeInferenceResultsBuilder();
        ITicResults types;

        try
        {
            if (!TicSetupVisitor.SetupTicForUserFunctionGroup(
                    group, graph, functionsRegistry, constants, resultsBuilder,
                    dialect, customTypes, namedTypeFieldRegistry, allUserFunctions))
                AssertChecks.Panic("Mutual recursion group not solved");
            types = graph.Solve(ignorePrefered: true);
        }
        catch (TicException e)
        {
            // Attribute the error to the first function in the group — error
            // reporter expects a representative function for position info.
            throw Errors.TranslateTicError(e, group[0], graph, functionsRegistry);
        }

        resultsBuilder.SetResults(types);
        var typeInferenceResults = resultsBuilder.Build();

        // Phase A: apply types + register prototypes for the entire group.
        var prototypes = new ConcreteUserFunctionPrototype[group.Length];
        var protoArgTypes = new FunnyType[group.Length][];
        var protoReturnType = new FunnyType[group.Length];

        for (int k = 0; k < group.Length; k++)
        {
            var fn = group[k];
            FreezeFunctionSignatureStructs(typeInferenceResults, fn);

            fn.ComeOver(enterVisitor: new ApplyTiResultEnterVisitor(
                solving: typeInferenceResults,
                tiToLangTypeConverter: TicTypesConverter.Concrete));
            PrecomputeDefaultValues(fn, typeInferenceResults, functionsRegistry, dialect, customTypes);

            var funType = TicTypesConverter.Concrete.Convert(
                typeInferenceResults.GetVariableType(fn.Id + "'" + fn.Args.Count));
            var retType = funType.FunTypeSpecification.Output;
            var argTypes = funType.FunTypeSpecification.Inputs;

            // Named struct override (mirror single-function path)
            var pArgTypes = argTypes;
            if (namedTypeFieldRegistry != null)
            {
                FunnyType[] overridden = null;
                for (int i = 0; i < fn.Args.Count && i < argTypes.Length; i++)
                {
                    if (fn.Args[i].TypeSyntax is TypeSyntax.EmptyType) continue;
                    var resolved = TypeSyntaxResolver.Resolve(fn.Args[i].TypeSyntax, customTypes);
                    if (resolved.BaseType == BaseFunnyType.NamedStruct)
                    {
                        if (overridden == null)
                        {
                            overridden = new FunnyType[argTypes.Length];
                            System.Array.Copy(argTypes, overridden, argTypes.Length);
                        }
                        overridden[i] = resolved;
                    }
                }
                if (overridden != null) pArgTypes = overridden;
            }

            var prototype = new ConcreteUserFunctionPrototype(fn.Id, retType, pArgTypes, EffectiveCallStyle(fn.IsExtension, dialect));
            var registryKey = TicSetupVisitor.GetRegistryKey(fn.Id, fn.IsExtension, dialect.ExtensionFunctionsSeparation);
            functionsRegistry.Add(registryKey, prototype);
            prototypes[k] = prototype;
            protoArgTypes[k] = argTypes;
            protoReturnType[k] = retType;
        }

        // Phase B: build each body — now they see each other via prototypes.
        // One shared depth counter across the whole SCC so combined
        // f→g→f→g recursion is bounded.
        var sharedDepth = new int[1];
        var result = new IUserFunction[group.Length];
        for (int k = 0; k < group.Length; k++)
        {
            var fn = group[k];
            var concrete = fn.BuildConcrete(
                argTypes: protoArgTypes[k],
                returnType: protoReturnType[k],
                functionsRegistry: functionsRegistry,
                results: typeInferenceResults,
                converter: TicTypesConverter.Concrete,
                dialect: dialect,
                sharedRecursionDepth: sharedDepth);
            prototypes[k].SetActual(concrete);
            result[k] = concrete;
        }
        return result;
    }

    /// <summary>
    /// True iff every leaf state in the function's TIC signature (arg types
    /// and return type) is solved — no <c>ConstraintsState</c> reachable
    /// through composite traversal. Used to classify a function as concrete
    /// even when the body has residual CS from internal operator dispatches.
    /// Cycle guard: μ-recursive types (named structs cycling through Optional/
    /// Array fields) self-reference, so a HashSet of visited TicNodes prevents
    /// infinite descent.
    /// </summary>
    /// <summary>
    /// Walk function signature subgraph and freeze every reachable StateStruct. After body solve
    /// completes, signature structural shape is determined; no script-body caller may extend it
    /// via width propagation. Damas-Milner '82 let-generalization closes the type at the
    /// let-boundary; analog here is that the function signature is "let-generalized" at
    /// function-build. Cycle-guarded via visited set (μ-recursive structs).
    /// </summary>
    private static void FreezeFunctionSignatureStructs(
        TypeInferenceResults results,
        UserFunctionDefinitionSyntaxNode functionSyntaxNode) {
        var ticName = functionSyntaxNode.Id + "'" + functionSyntaxNode.Args.Count;
        var sig = results.GetVariableType(ticName) as Tic.SolvingStates.StateFun;
        if (sig == null) return;
        var visited = new HashSet<TicNode>();
        foreach (var arg in sig.ArgNodes)
            FreezeStructsRecursive(arg, visited);
        FreezeStructsRecursive(sig.RetNode, visited);
    }

    private static void FreezeStructsRecursive(TicNode node, HashSet<TicNode> visited) {
        var nr = node.GetNonReference();
        if (!visited.Add(nr)) return;
        switch (nr.State) {
            case Tic.SolvingStates.StateStruct s when !s.IsFrozen && s.TypeName == null:
                // Freeze ONLY anonymous structs that are part of a recursive cycle (self-RefTo
                // through fields). Non-recursive structs are genuinely row-polymorphic — a
                // function like `fun1(x,y) = x.age + y.size` expects callers to merge bounds
                // across args, which requires width propagation; freezing them breaks let-poly
                // inference. Recursive structs (μ-shape) MUST be frozen: their bound determines
                // the polymorphic skeleton; widening at call sites pollutes the shared signature.
                if (SolvingFunctions.StructIsRecursiveCycle(s, nr))
                    s.IsFrozen = true;
                foreach (var (_, fieldNode) in s.Fields)
                    FreezeStructsRecursive(fieldNode, visited);
                break;
            case Tic.SolvingStates.StateStruct s:
                foreach (var (_, fieldNode) in s.Fields)
                    FreezeStructsRecursive(fieldNode, visited);
                break;
            case Tic.SolvingStates.ConstraintsState cs:
                if (cs.HasStructBound) {
                    // RecursiveBound's struct: freeze too (idempotent if already frozen by lift)
                    cs.StructBound.IsFrozen = true;
                    foreach (var (_, fieldNode) in cs.StructBound.Fields)
                        FreezeStructsRecursive(fieldNode, visited);
                }
                break;
            case Tic.SolvingStates.ICompositeState comp:
                for (int i = 0; i < comp.MemberCount; i++)
                    FreezeStructsRecursive(comp.GetMember(i), visited);
                break;
        }
    }

    private static bool SignatureIsFullyConcrete(Tic.SolvingStates.StateFun signature) {
        var visited = new HashSet<TicNode>();
        foreach (var arg in signature.ArgNodes)
            if (!StateIsSolved(arg.GetNonReference().State, visited)) return false;
        return StateIsSolved(signature.RetNode.GetNonReference().State, visited);
    }

    private static bool StateIsSolved(Tic.SolvingStates.ITicNodeState state, HashSet<TicNode> visited) {
        switch (state) {
            case Tic.SolvingStates.StateRefTo r: return StateIsSolved(r.Node.GetNonReference().State, visited);
            case Tic.SolvingStates.ConstraintsState: return false;
            case Tic.SolvingStates.StateStruct s when s.TypeName != null: return true;
            case Tic.SolvingStates.ICompositeState c:
                for (int i = 0; i < c.MemberCount; i++) {
                    var member = c.GetMember(i);
                    if (!visited.Add(member)) continue; // cycle guard for μ-types
                    if (!StateIsSolved(member.GetNonReference().State, visited)) return false;
                }
                return true;
            default: return true;
        }
    }

    private static void PrecomputeDefaultValues(
        UserFunctionDefinitionSyntaxNode functionSyntax,
        TypeInferenceResults results,
        IFunctionRegistry functions,
        DialectSettings dialect,
        ICustomTypeRegistry customTypes = null) {
        for (int i = 0; i < functionSyntax.Args.Count; i++)
        {
            var arg = functionSyntax.Args[i];
            if (!arg.HasDefault)
                continue;
            // none default → skip precomputation, use DefaultValueSyntaxNode at call site
            if (arg.DefaultValue is ConstantSyntaxNode { Value: FunnyNone })
                continue;

            // Resolve param type: from annotation or from TIC inference
            FunnyType paramType;
            if (arg.TypeSyntax is not TypeSyntax.EmptyType)
            {
                // customTypes pass-through: without it, a user-defined named type
                // (`a: p = p{...}`) fails TypeSyntaxResolver with FU406 (BugHunt-stmt #52).
                paramType = TypeSyntaxResolver.Resolve(arg.TypeSyntax, customTypes);
            }
            else
            {
                // Untyped param: get type from TIC results (default expression was visited in function TIC)
                var ticType = results.GetSyntaxNodeTypeOrNull(arg.DefaultValue.OrderNumber);
                if (ticType == null) continue;
                // Unwrap RefTo first, then resolve constraints to preferred type
                if (ticType is Tic.SolvingStates.StateRefTo refTo)
                    ticType = refTo.GetNonReference();
                if (ticType is Tic.SolvingStates.ConstraintsState cs)
                    ticType = cs.Preferred ?? cs.Descendant;
                if (ticType is not Tic.SolvingStates.StatePrimitive and not Tic.SolvingStates.StateArray)
                    continue;
                try { paramType = TicTypesConverter.Concrete.Convert(ticType); }
                catch (Exception) { continue; }
            }

            try
            {
                // Ensure OutputType is set on default expression nodes (may not be set for generic functions)
                ApplyTiResultToSubtree(arg.DefaultValue, results);

                var defaultExprNode = ExpressionBuilderVisitor.BuildExpression(
                    node: arg.DefaultValue,
                    functions: functions,
                    outputType: paramType,
                    variables: new VariableDictionary(),
                    typeInferenceResults: results,
                    typesConverter: TicTypesConverter.Concrete,
                    dialect: dialect);
                arg.PrecomputedDefaultValue = defaultExprNode.Calc();
                arg.PrecomputedDefaultType = paramType;
            }
            catch (Exception) { /* conversion failed or non-constant — caller will use original expression */ }
        }
    }

    /// <summary>
    /// Infers a FunnyType from a constant expression syntax node (used for named type fields
    /// with no explicit type annotation but a default value, e.g. {retries = 3}).
    /// Falls back to FunnyType.Any if the expression is null or type cannot be determined.
    /// </summary>
    private static FunnyType InferTypeFromConstantOrAny(ISyntaxNode defaultExpr) =>
        defaultExpr switch {
            ConstantSyntaxNode c => c.OutputType,
            GenericIntSyntaxNode => FunnyType.Int32,
            IpAddressConstantSyntaxNode => FunnyType.Ip,
            _ => FunnyType.Any
        };

    /// <summary>
    /// True iff the type syntax tree contains a Named reference to any name in
    /// <paramref name="names"/>. Used to detect aliases that participate in
    /// self- or mutual-recursion so they can be pre-registered with a placeholder
    /// before resolution.
    /// </summary>
    private static bool TypeSyntaxContainsAnyName(TypeSyntax syntax, HashSet<string> names) {
        switch (syntax) {
            case TypeSyntax.Named n:
                return names.Contains(n.Name);
            case TypeSyntax.OptionalOf o:
                return TypeSyntaxContainsAnyName(o.Element, names);
            case TypeSyntax.ArrayOf a:
                return TypeSyntaxContainsAnyName(a.Element, names);
            case TypeSyntax.FunOf f:
                if (TypeSyntaxContainsAnyName(f.ReturnType, names)) return true;
                foreach (var arg in f.ArgTypes)
                    if (TypeSyntaxContainsAnyName(arg, names)) return true;
                return false;
            case TypeSyntax.StructOf s:
                foreach (var field in s.Fields)
                    if (TypeSyntaxContainsAnyName(field.FieldType, names)) return true;
                return false;
            default:
                return false;
        }
    }

    /// <summary>Set OutputType on all nodes in a subtree from TIC results (for precomputing defaults).
    /// Resolves generic constraints to preferred/descendant types for precomputation.</summary>
    private static void ApplyTiResultToSubtree(ISyntaxNode node, TypeInferenceResults results) {
        var ticType = results.GetSyntaxNodeTypeOrNull(node.OrderNumber);
        if (ticType != null)
        {
            // Unwrap RefTo → resolve constraints to concrete preferred type
            var resolved = ticType;
            if (resolved is Tic.SolvingStates.StateRefTo refTo)
                resolved = refTo.GetNonReference();
            if (resolved is Tic.SolvingStates.ConstraintsState cs)
                resolved = cs.Preferred ?? cs.Descendant;
            if (resolved != null)
            {
                try { node.OutputType = TicTypesConverter.Concrete.Convert(resolved); }
                catch (Exception) { /* truly generic — leave as Empty */ }
            }
        }
        foreach (var child in node.Children)
            ApplyTiResultToSubtree(child, results);
    }

    /// <summary>
    /// Maps user-function syntax (<c>x.f()</c> vs <c>f(x)</c>) to its dialect-effective
    /// <see cref="CallStyle"/> at registration time. Under
    /// <see cref="ExtensionFunctionsSeparation.Disabled"/> (default) both syntaxes are
    /// aliases — register as <see cref="CallStyle.Both"/>. Under
    /// <see cref="ExtensionFunctionsSeparation.Enabled"/> the syntax restricts the
    /// reachable call site — Extension or Direct accordingly.
    /// </summary>
    private static CallStyle EffectiveCallStyle(bool isExtensionSyntax, DialectSettings dialect)
        => dialect.ExtensionFunctionsSeparation == ExtensionFunctionsSeparation.Enabled
            ? (isExtensionSyntax ? CallStyle.Extension : CallStyle.Direct)
            : CallStyle.Both;
}
