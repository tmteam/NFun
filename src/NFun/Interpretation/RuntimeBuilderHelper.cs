using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.Errors;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Interpretation;

using Topology;

internal static class RuntimeBuilderHelper {
    public static ConcreteUserFunction BuildConcrete(
        this UserFunctionDefinitionSyntaxNode functionSyntax,
        FunnyType[] argTypes,
        FunnyType returnType,
        IFunctionRegistry functionsRegistry,
        TypeInferenceResults results,
        TicTypesConverter converter,
        DialectSettings dialect,
        int[] sharedRecursionDepth = null) {
        var vars = new VariableDictionary(functionSyntax.Args.Count);
        var argumentSources = new VariableSource[functionSyntax.Args.Count];
        for (int i = 0; i < functionSyntax.Args.Count; i++)
        {
            var variableSource = CreateVariableSourceForArgument(
                argSyntax: functionSyntax.Args[i],
                actualType: argTypes[i],
                dialect.Converter);

            if (!vars.TryAdd(variableSource))
                throw Errors.FunctionArgumentDuplicates(functionSyntax, functionSyntax.Args[i]);
            argumentSources[i] = variableSource;
        }

        var bodyExpression = ExpressionBuilderVisitor.BuildExpression(
            node: functionSyntax.Body,
            functions: functionsRegistry,
            outputType: returnType,
            variables: vars,
            typeInferenceResults: results,
            typesConverter: converter,
            dialect: dialect);

        vars.ThrowIfSomeVariablesNotExistsInTheList(
            bodyExpression,
            functionSyntax.Args.Select(a => a.Id));

        // Locals = block-introduced bindings inside the body (everything in
        // `vars` that isn't an argument). For recursive functions these need
        // per-call save/restore so an inner frame doesn't clobber an outer
        // frame's local slot (see BugHuntStatementsResults #1/#2).
        var argNames = new HashSet<string>(
            functionSyntax.Args.Select(a => a.Id), StringComparer.OrdinalIgnoreCase);
        var localSources = vars.GetAll()
            .Where(v => !argNames.Contains(v.Name))
            .ToArray();

        var function = ConcreteUserFunction.Create(
            isRecursive: functionSyntax.IsRecursive,
            name: functionSyntax.Id,
            variables: argumentSources,
            expression: bodyExpression,
            localSources: localSources,
            sharedRecursionDepth: sharedRecursionDepth);
        return function;
    }

    public static TypeInferenceResults SolveBodyOrThrow(
        SyntaxTree syntaxTree,
        IFunctionRegistry functions,
        IConstantList constants,
        IAprioriTypesMap aprioriTypes,
        ICustomTypeRegistry customTypes,
        DialectSettings dialect,
        out bool typesApplied,
        INamedTypeFieldRegistry namedTypeFieldRegistry = null
        ) {
        typesApplied = false;
        // Fast path: primitive-only bodies solved with interval arithmetic.
        // IsSimpleBody determined during numbering pass — zero-cost gate.
        if (namedTypeFieldRegistry == null && syntaxTree.IsSimpleBody) {
            var fastResult = SimplePrimitiveSolver.SolveOrNull(
                syntaxTree, functions, constants, aprioriTypes, dialect,
                out typesApplied, customTypes);
            if (fastResult != null) return fastResult;
        }

        var resultBuilder = new TypeInferenceResultsBuilder(syntaxTree.MaxNodeId);
        var graph = new GraphBuilder(syntaxTree.MaxNodeId);
        try
        {
            if(!TicSetupVisitor.SetupTicForBody(
                tree: syntaxTree,
                ticGraph: graph,
                functions: functions,
                constants: constants,
                aprioriTypes: aprioriTypes,
                customTypes: customTypes,
                results: resultBuilder,
                dialect: dialect,
                namedTypeFieldRegistry: namedTypeFieldRegistry
                ))
                AssertChecks.Panic("Types not solved due unknown reasons");

            var bodyTypeSolving = graph.Solve().NotNull("Type graph solve nothing");
            resultBuilder.SetResults(bodyTypeSolving);
            return resultBuilder.Build();
        }
        catch (TicException e)
        {
            throw Errors.TranslateTicError(e, syntaxTree, graph, functions);
        }
    }

    private static void ThrowIfSomeVariablesNotExistsInTheList(
        this IReadonlyVariableDictionary variables,
        IExpressionNode bodyExpression,
        IEnumerable<string> list) {

        // Skip block-local variables (Output) — they are created during block expression building
        var hasUnknownVariables = variables.GetAll().Any(u=> !u.IsOutput && !list.Contains(u.Name));
        if (hasUnknownVariables)
        {
            var unknownVariableUsages = variables
                .GetAll()
                .Where(u => !u.IsOutput && !list.Contains(u.Name))
                .Select(bodyExpression.FindFirstUsageOrNull)
                .Where(s=>s!=null)
                .ToList();

            throw Errors.UnknownVariablesInUserFunction(unknownVariableUsages);
        }
    }

    /// <summary>
    /// Gets order of calculating the functions, based on its co using.
    /// Returned as SCC groups in topological order. Singleton groups are
    /// acyclic functions; size&gt;1 groups are mutually-recursive cycles.
    /// </summary>
    public static UserFunctionDefinitionSyntaxNode[][] FindFunctionSolvingOrderOrThrow(
        this SyntaxTree syntaxTree,
        ExtensionFunctionsSeparation extensionSeparation = ExtensionFunctionsSeparation.Disabled) {
        var userFunctions = syntaxTree.Children.OfType<UserFunctionDefinitionSyntaxNode>().ToArray();
        if (userFunctions.Length == 0)
            return System.Array.Empty<UserFunctionDefinitionSyntaxNode[]>();

        var userFunctionsNames = new Dictionary<string, int>(userFunctions.Length, StringComparer.OrdinalIgnoreCase);
        int i = 0;
        foreach (var userFunction in userFunctions)
        {
            // When extension separation is enabled, extension and regular functions with the same
            // name+arity use different aliases, allowing them to coexist.
            var alias = extensionSeparation == ExtensionFunctionsSeparation.Enabled
                ? userFunction.GetFunAliasWithExtension()
                : userFunction.GetFunAlias();
            if(!userFunctionsNames.TryAdd(alias, i))
                throw Errors.UserFunctionWithSameNameAlreadyDeclared(userFunction);
            i++;
        }

        int[][] dependenciesGraph = new int[i][];
        int j = 0;
        foreach (var userFunction in userFunctions)
        {
            var alias = extensionSeparation == ExtensionFunctionsSeparation.Enabled
                ? userFunction.GetFunAliasWithExtension()
                : userFunction.GetFunAlias();
            var visitor = new FindFunctionDependenciesVisitor(
                alias, userFunctionsNames,
                userFunctions: userFunctions,
                extensionSeparation: extensionSeparation == ExtensionFunctionsSeparation.Enabled);
            if (!userFunction.ComeOver(visitor))
                throw new InvalidOperationException("User fun come over");

            userFunction.IsRecursive = visitor.HasSelfRecursion;

            dependenciesGraph[j] = visitor.GetFoundDependencies();
            j++;
        }

        // SCC-grouped topological sort. Size>1 SCCs are mutually-recursive
        // function groups; we resolve them together with pre-registered prototypes.
        var sccGroups = CycleTopologySorting.SortIntoGroups(dependenciesGraph);
        var groups = new UserFunctionDefinitionSyntaxNode[sccGroups.Length][];
        for (int g = 0; g < sccGroups.Length; g++)
        {
            var members = sccGroups[g];
            var group = new UserFunctionDefinitionSyntaxNode[members.Length];
            for (int k = 0; k < members.Length; k++)
                group[k] = userFunctions[members[k]];

            // Mutual recursion SCC: every member is part of a cycle. Mark all
            // recursive so freeze + recursive runtime paths apply.
            if (members.Length > 1)
                foreach (var fn in group) fn.IsRecursive = true;
            groups[g] = group;
        }

        return groups;
    }

    private static VariableSource CreateVariableSourceForArgument(
        TypedVarDefSyntaxNode argSyntax,
        FunnyType actualType,
        FunnyConverter typeBehaviour) {

        if (argSyntax.TypeSyntax is not TypeSyntax.EmptyType)
            return VariableSource.CreateWithStrictTypeLabel(
                name: argSyntax.Id,
                type: actualType,
                typeSpecificationIntervalOrNull: argSyntax.Interval,
                access: FunnyVarAccess.Input,typeBehaviour);
        else
            return VariableSource.CreateWithoutStrictTypeLabel(
                name: argSyntax.Id,
                type: actualType,
                access: FunnyVarAccess.Input,typeBehaviour);
    }
}
