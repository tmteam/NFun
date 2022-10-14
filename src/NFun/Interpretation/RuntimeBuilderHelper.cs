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

internal static class RuntimeBuilderHelper {
    public static ConcreteUserFunction BuildConcrete(
        this UserFunctionDefinitionSyntaxNode functionSyntax,
        FunnyType[] argTypes,
        FunnyType returnType,
        IFunctionDictionary functionsDictionary,
        TypeInferenceResults results,
        TicTypesConverter converter,
        DialectSettings dialect) {
        var vars = new VariableDictionary(functionSyntax.Args.Count);
        for (int i = 0; i < functionSyntax.Args.Count; i++)
        {
            var variableSource = CreateVariableSourceForArgument(
                argSyntax: functionSyntax.Args[i],
                actualType: argTypes[i], 
                dialect.Converter);

            if (!vars.TryAdd(variableSource))
                throw Errors.FunctionArgumentDuplicates(functionSyntax, functionSyntax.Args[i]);
        }

        var bodyExpression = ExpressionBuilderVisitor.BuildExpression(
            node: functionSyntax.Body,
            functions: functionsDictionary,
            outputType: returnType,
            variables: vars,
            typeInferenceResults: results,
            typesConverter: converter,
            dialect: dialect);

        vars.ThrowIfSomeVariablesNotExistsInTheList(
            bodyExpression,
            functionSyntax.Args.Select(a => a.Id));

        var function = ConcreteUserFunction.Create(
            isRecursive: functionSyntax.IsRecursive,
            name: functionSyntax.Id,
            variables: vars.GetAllAsArray(),
            expression: bodyExpression);
        return function;
    }

    public static TypeInferenceResults SolveBodyOrThrow(
        SyntaxTree syntaxTree,
        IFunctionDictionary functions,
        IConstantList constants,
        IAprioriTypesMap aprioriTypes,
        DialectSettings dialect) {
        var resultBuilder = new TypeInferenceResultsBuilder();
        var graph = new GraphBuilder(syntaxTree.MaxNodeId);
        try
        {
            if(!TicSetupVisitor.SetupTicForBody(
                tree: syntaxTree,
                ticGraph: graph,
                functions: functions,
                constants: constants,
                aprioriTypes: aprioriTypes,
                results: resultBuilder,
                dialect: dialect))
                AssertChecks.Panic("Types not solved due unknown reasons");

            var bodyTypeSolving = graph.Solve();
            if(bodyTypeSolving==null)
                AssertChecks.Panic("Type graph solve nothing");
            resultBuilder.SetResults(bodyTypeSolving);
            return resultBuilder.Build();
        }
        catch (TicException e)
        {
            throw Errors.TranslateTicError(e, syntaxTree, graph);
        }
    }

    private static void ThrowIfSomeVariablesNotExistsInTheList(
        this IReadonlyVariableDictionary variables,
        IExpressionNode bodyExpression,
        IEnumerable<string> list) {
        
        var hasUnknownVariables = variables.GetAll().Any(u=>!list.Contains(u.Name));
        if (hasUnknownVariables)
        {
            var unknownVariableUsages = variables
                .GetAll()
                .Where(u => !list.Contains(u.Name))
                .Select(bodyExpression.FindFirstUsageOrNull)
                .Where(s=>s!=null)
                .ToList();
                            
            throw Errors.UnknownVariablesInUserFunction(unknownVariableUsages);
        }
    }

    /// <summary>
    /// Gets order of calculating the functions, based on its co using.
    /// </summary>
    public static UserFunctionDefinitionSyntaxNode[] FindFunctionSolvingOrderOrThrow(this SyntaxTree syntaxTree) {
        var userFunctions = syntaxTree.Children.OfType<UserFunctionDefinitionSyntaxNode>().ToArray();
        if (userFunctions.Length == 0)
            return userFunctions;

        var userFunctionsNames = new Dictionary<string, int>(userFunctions.Length, StringComparer.OrdinalIgnoreCase);
        int i = 0;
        foreach (var userFunction in userFunctions)
        {
            var alias = userFunction.GetFunAlias();
            if (userFunctionsNames.ContainsKey(alias))
                throw Errors.UserFunctionWithSameNameAlreadyDeclared(userFunction);
            userFunctionsNames.Add(alias, i);
            i++;
        }

        int[][] dependenciesGraph = new int[i][];
        int j = 0;
        foreach (var userFunction in userFunctions)
        {
            var visitor = new FindFunctionDependenciesVisitor(userFunction.GetFunAlias(), userFunctionsNames);
            if (!userFunction.ComeOver(visitor))
                throw new InvalidOperationException("User fun come over");

            userFunction.IsRecursive = visitor.HasSelfRecursion;

            dependenciesGraph[j] = visitor.GetFoundDependencies();
            j++;
        }

        var sortResults = CycleTopologySorting.Sort(dependenciesGraph);

        var functionSolveOrder = new UserFunctionDefinitionSyntaxNode[sortResults.NodeNames.Length];
        for (int k = 0; k < sortResults.NodeNames.Length; k++)
        {
            var id = sortResults.NodeNames[k];
            functionSolveOrder[k] = userFunctions[id];
        }

        if (sortResults.HasCycle)
            //if functions has cycle, then function solve order is cycled
            throw Errors.ComplexRecursion(functionSolveOrder);

        return functionSolveOrder;
    }

    private static VariableSource CreateVariableSourceForArgument(
        TypedVarDefSyntaxNode argSyntax,
        FunnyType actualType, 
        FunnyConverter typeBehaviour) {
        
        if (argSyntax.FunnyType != FunnyType.Empty)
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