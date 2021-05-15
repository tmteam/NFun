using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.Errors;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Interpritation
{
    public static class RuntimeBuilderHelper
    {
        public static IExpressionNode[] Fork(this IExpressionNode[] nodes, ForkScope scope)
        {
            var res = new IExpressionNode[nodes.Length];
            for (var i = 0; i < res.Length; i++) 
                res[i] = nodes[i].Fork(scope);
            return res;
        }
        public static ConcreteUserFunction BuildConcrete(
            this UserFunctionDefinitionSyntaxNode functionSyntax,
            VarType[] argTypes, 
            VarType returnType,
            IFunctionDictionary functionsDictionary,
            TypeInferenceResults results, 
            TicTypesConverter converter)
        {
            var vars = new VariableDictionary(functionSyntax.Args.Count);
            for (int i = 0; i < functionSyntax.Args.Count; i++)
            {
                var variableSource = RuntimeBuilderHelper.CreateVariableSourceForArgument(
                    argSyntax: functionSyntax.Args[i],
                    actualType: argTypes[i]);

                if (!vars.TryAdd(variableSource))
                    throw ErrorFactory.FunctionArgumentDuplicates(functionSyntax, functionSyntax.Args[i]);
            }

            var bodyExpression = ExpressionBuilderVisitor.BuildExpression(
                node: functionSyntax.Body,
                functions: functionsDictionary,
                outputType: returnType,
                variables: vars,
                typeInferenceResults: results, 
                typesConverter: converter);

            vars.ThrowIfSomeVariablesNotExistsInTheList(
                functionSyntax.Args.Select(a => a.Id));

            var function = ConcreteUserFunction.Create(
                isRecursive: functionSyntax.IsRecursive,
                name: functionSyntax.Id,
                variables: vars.GetAllSources().ToArray(),
                isReturnTypeStrictlyTyped: functionSyntax.ReturnType != VarType.Empty,
                expression: bodyExpression);
            return function;
        }

        public static TypeInferenceResults SolveBodyOrThrow(
            SyntaxTree syntaxTree,
            IFunctionDictionary functions, 
            IConstantList constants, 
            AprioriTypesMap aprioriTypes)
        {
            try
            {
                var resultBuilder = new TypeInferenceResultsBuilder();
                var typeGraph = new GraphBuilder(syntaxTree.MaxNodeId);

                if(!TicSetupVisitor.SetupTicForBody(
                    tree:      syntaxTree, 
                    ticGraph:  typeGraph, 
                    functions: functions, 
                    constants: constants, 
                    aprioriTypes: aprioriTypes, 
                    results:   resultBuilder))
                    throw ErrorFactory.TypesNotSolved(syntaxTree);

                var bodyTypeSolving = typeGraph.Solve();
                if (bodyTypeSolving == null)
                    throw ErrorFactory.TypesNotSolved(syntaxTree);
                resultBuilder.SetResults(bodyTypeSolving);
                return resultBuilder.Build();
            }
            catch (TicException e) { throw ErrorFactory.TranslateTicError(e, syntaxTree); }
        }

        private static void ThrowIfSomeVariablesNotExistsInTheList(this VariableDictionary resultVariables, IEnumerable<string> list )
        {
            var unknownVariables = resultVariables.GetAllUsages()
                .Where(u=> !list.Contains(u.Source.Name)).ToList();
            if (unknownVariables.Any())
            {
                throw ErrorFactory.UnknownVariables(unknownVariables.SelectMany(u => u.Usages));
            }        
        }
        /// <summary>
        /// Gets order of calculating the functions, based on its co using.
        /// </summary>
        public static UserFunctionDefinitionSyntaxNode[] FindFunctionSolvingOrderOrThrow(this SyntaxTree syntaxTree)
        {
            var userFunctions = syntaxTree.Children.OfType<UserFunctionDefinitionSyntaxNode>().ToArray();
            if(userFunctions.Length==0)
                return userFunctions;
            
            var userFunctionsNames = new Dictionary<string, int>();
            int i = 0;
            foreach (var userFunction in userFunctions)
            {
                var alias = userFunction.GetFunAlias();
                if (userFunctionsNames.ContainsKey(alias))
                    throw ErrorFactory.FunctionAlreadyExist(userFunction);
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

            var sortResults = GraphTools.SortCycledTopology(dependenciesGraph);

            var functionSolveOrder = new UserFunctionDefinitionSyntaxNode[sortResults.NodeNames.Length];
            for (int k = 0; k < sortResults.NodeNames.Length; k++)
            {
                var id = sortResults.NodeNames[k];
                functionSolveOrder[k] = userFunctions[id];
            }
            
            if (sortResults.HasCycle)
                //if functions has cycle, then function solve order is cycled
                throw ErrorFactory.ComplexRecursion(functionSolveOrder);
          
            return functionSolveOrder;
        }
        
        private static VariableSource CreateVariableSourceForArgument(
            TypedVarDefSyntaxNode argSyntax,
            VarType actualType)
        {
            if(argSyntax.VarType != VarType.Empty)
                return VariableSource.CreateWithStrictTypeLabel(
                    name: argSyntax.Id, 
                    type: actualType, 
                    typeSpecificationInterval: argSyntax.Interval, 
                    isOutput: false);
            else
                return VariableSource.CreateWithoutStrictTypeLabel(
                    name: argSyntax.Id,
                    type: actualType, 
                    isOutput: false);
        }
    }
}