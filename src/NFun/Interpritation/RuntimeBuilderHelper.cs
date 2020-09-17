using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.TypeInferenceAdapter;
using NFun.TypeInferenceCalculator;
using NFun.TypeInferenceCalculator.Errors;
using NFun.Types;

namespace NFun.Interpritation
{
    public static class RuntimeBuilderHelper
    {
        public static ConcreteUserFunction BuildConcrete(
            this UserFunctionDefenitionSyntaxNode functionSyntax,
            VarType[] argTypes, 
            VarType returnType,
            IFunctionDictionary functionsDictionary,
            TypeInferenceResults results, 
            TicTypesConverter converter)
        {
#if DEBUG
            TraceLog.WriteLine($"\r\n====BUILD CONCRETE {functionSyntax.Id}(..) ====");
#endif
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
                name: functionSyntax.Id,
                variables: vars.GetAllSources().ToArray(),
                isReturnTypeStrictlyTyped: functionSyntax.ReturnType != VarType.Empty,
                expression: bodyExpression);
            return function;
        }

        public static TypeInferenceResults SolveBodyOrThrow(SyntaxTree syntaxTree,
            IFunctionDictionary functions, IConstantList constants)
        {
            try
            {
#if DEBUG
                TraceLog.WriteLine("\r\n====BODY====");
#endif

                var resultBuilder = new TypeInferenceResultsBuilder();
                var typeGraph = new GraphBuilder();

                //to build body - we have to skip all user-function-syntax-nodes
                var bodyNodes = syntaxTree.Children.Where(n => !(n is UserFunctionDefenitionSyntaxNode));
                if(!TicSetupVisitor.Run(bodyNodes, typeGraph, functions, constants, resultBuilder))
                    throw ErrorFactory.TypesNotSolved(syntaxTree);

                var bodyTypeSolving = typeGraph.Solve();
                if (bodyTypeSolving == null)
                    throw ErrorFactory.TypesNotSolved(syntaxTree);
                resultBuilder.SetResults(bodyTypeSolving);
                return resultBuilder.Build();
            }
            catch (TicException e) { throw ErrorFactory.TranslateTicError(e, syntaxTree); }

        }
        //public static void ThrowIfNotSolved(ISyntaxNode functionSyntaxNode, TiResult types)
        //{
        //    if (types.IsSolved) return;
        //    var failedNodeOrNull = functionSyntaxNode.GetDescendantNodeOrNull(types.FailedNodeId);
        //    ThrowTiError(functionSyntaxNode, types.Result, failedNodeOrNull);
        //}

        //private static void ThrowTiError(ISyntaxNode root, TiSolveResult result,ISyntaxNode failedNodeOrNull)
        //{
        //    switch (result)
        //    {
        //        case TiSolveResult.Solved:
        //            throw new InvalidOperationException();
        //        case TiSolveResult.NotSolvedOverloadWithSeveralCandidates:
        //            throw ErrorFactory.AmbiguousFunctionChoise(failedNodeOrNull);
        //        case TiSolveResult.NotSolvedNoFunctionFits:
        //            throw ErrorFactory.FunctionIsNotExists(failedNodeOrNull);
        //        default:
        //            throw ErrorFactory.TypesNotSolved(root);
        //    }
        //}

        public static void ThrowIfSomeVariablesNotExistsInTheList(this VariableDictionary resultVariables, IEnumerable<string> list )
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
        public static UserFunctionDefenitionSyntaxNode[] FindFunctionSolvingOrderOrThrow(this SyntaxTree syntaxTree)
        {
            var userFunctions = syntaxTree.Children.OfType<UserFunctionDefenitionSyntaxNode>().ToList();

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
                var visitor = new FindFunctionDependenciesVisitor(userFunctionsNames);
                if (!userFunction.ComeOver(visitor))
                    throw new InvalidOperationException("User fun come over");
                dependenciesGraph[j] = visitor.GetFoundDependencies();
                j++;
            }

            var sortResults = GraphTools.SortCycledTopology(dependenciesGraph);

            var functionSolveOrder = new UserFunctionDefenitionSyntaxNode[sortResults.NodeNames.Length];
            for (int k = 0; k < sortResults.NodeNames.Length; k++)
                functionSolveOrder[k] = userFunctions[sortResults.NodeNames[k]];
            
            if (sortResults.HasCycle)
                //if functions has cycle, then function sovle order is cycled
                throw ErrorFactory.ComplexRecursion(functionSolveOrder);
          
            return functionSolveOrder;
        }
        public static VariableSource CreateVariableSourceForArgument(
            TypedVarDefSyntaxNode argSyntax,
            VarType actualType)
        {
            if(argSyntax.VarType != VarType.Empty)
                return VariableSource.CreateWithStrictTypeLabel(argSyntax.Id, actualType, argSyntax.Interval);
            else
                return VariableSource.CreateWithoutStrictTypeLabel(
                    name: argSyntax.Id,
                    type: actualType);
        }
    }
}