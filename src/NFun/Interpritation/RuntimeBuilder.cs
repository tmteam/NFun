using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Interpritation
{
    public static class RuntimeBuilder
    {
        public static FunRuntime Build(
            SyntaxTree syntaxTree,
            FunctionDictionary functionsDictionary)
        {
            #region build user functions
            //get topology sort of the functions call
            //result is the order of functions that need to be compiled
            //functions that not references other functions have to be compiled firstly
            //Then those functions will be compiled
            //that refer to already compiled functions
            var functionSolveOrder = syntaxTree.FindFunctionSolvingOrderOrThrow();
            
            var scopeFunctionDictionary = new ScopeFunctionDictionary(functionsDictionary);
            //build user functions
            foreach (var functionSyntaxNode in functionSolveOrder)
                BuildFunctionAndPutItToDictionary(functionSyntaxNode, scopeFunctionDictionary);

            #endregion

            #region solve body types
            //Solve types for all equations nodes
            var bodyTypeSolving = RuntimeBuilderHelper.SolveOrThrow(syntaxTree, scopeFunctionDictionary);

            foreach (var syntaxNode in syntaxTree.Children)
            {
                //function nodes were solved above
                if (syntaxNode is UserFunctionDefenitionSyntaxNode)
                    continue;

                //set types to nodes
                syntaxNode.ComeOver(
                    enterVisitor: new ApplyTiResultEnterVisitor(bodyTypeSolving, new TypeInferenceOnlyConcreteInterpriter()),
                    exitVisitor: new ApplyTiResultsExitVisitor());
            }
            #endregion

            #region build body
            var variables = new VariableDictionary();
            var equations = new List<Equation>();

            foreach (var treeNode in syntaxTree.Nodes)
            {
                if (treeNode is EquationSyntaxNode node)
                {
                    var equation = BuildEquationAndPutItToVariables(node, scopeFunctionDictionary, variables, bodyTypeSolving);
                    equations.Add(equation);
                }
                else if (treeNode is VarDefenitionSyntaxNode varDef)
                {
                    var variableSource = VariableSource.CreateWithStrictTypeLabel(
                        varDef.Id,
                        varDef.VarType,
                        varDef.Interval,
                        varDef.Attributes);
                    if (!variables.TryAdd(variableSource))
                    {
                        var allUsages = variables.GetUsages(variableSource.Name);
                        throw ErrorFactory.VariableIsDeclaredAfterUsing(allUsages);
                    }
                }
                else if (treeNode is UserFunctionDefenitionSyntaxNode)
                    continue;//user function was built above
                else
                    throw new InvalidOperationException($"Type {treeNode} is not supported as tree root");
            }
            #endregion
            return new FunRuntime(equations, variables, new List<UserFunction>());
        }

        private static Equation BuildEquationAndPutItToVariables(
            EquationSyntaxNode equation,
            IFunctionDicitionary functionsDictionary, 
            VariableDictionary variables, 
            TypeInferenceResults typeInferenceResults)
        {
            var expression = ExpressionBuilderVisitor.BuildExpression(
                node:       equation.Expression, 
                functions:  functionsDictionary,
                outputType: equation.OutputType,
                variables:  variables,
                typeInferenceResults: typeInferenceResults);
            
            VariableSource newSource;
            if(equation.OutputTypeSpecified)
                newSource = VariableSource.CreateWithStrictTypeLabel(
                    name: equation.Id, 
                    type: equation.OutputType, 
                    typeSpecificationIntervalOrNull: equation.TypeSpecificationOrNull.Interval, 
                    attributes: equation.Attributes);
            else
                newSource = VariableSource.CreateWithoutStrictTypeLabel(equation.Id, equation.OutputType, equation.Attributes);
            
            newSource.IsOutput = true;
          
            if (!variables.TryAdd(newSource))
            {
                //some equation referenced the source before
                var usages = variables.GetUsages(equation.Id);
                if (usages.Source.IsOutput)
                    throw ErrorFactory.OutputNameWithDifferentCase(equation.Id, equation.Expression.Interval);
                else
                    throw ErrorFactory.CannotUseOutputValueBeforeItIsDeclared(usages);
            }
            
           
            //ReplaceInputType
            if(newSource.Type != expression.Type)
                throw new ImpossibleException("fitless");            
            return new Equation(equation.Id, expression);
        }

        private static UserFunction BuildFunctionAndPutItToDictionary(
            UserFunctionDefenitionSyntaxNode functionSyntaxNode,
            ScopeFunctionDictionary functionsDictionary)
        {
            ////introduce function variable
            var graphBuider = new GraphBuilder();
            //Setup user function signature
            foreach (var argument in functionSyntaxNode.Args)
            {
                if(argument.VarType!= VarType.Empty)
                    graphBuider.SetVarType(argument.Id, argument.VarType.ConvertToTiType());
            }
            if (functionSyntaxNode.ReturnType != VarType.Empty)
                graphBuider.SetVarType("$result", functionSyntaxNode.ReturnType.ConvertToTiType());
            //setup return type
            graphBuider.SetDef("$result", functionSyntaxNode.Body.OrderNumber);

            //setup body type inference
            var resultsBuilder = new TypeInferenceResultsBuilder();
            var typeSolving = LangTiHelper.SetupTiOrNull(
                functionSyntaxNode.Body, 
                functionsDictionary, 
                resultsBuilder, 
                new SetupTiState(graphBuider));
            if (typeSolving==null)
                throw FunParseException.ErrorStubToDo($"Function '{functionSyntaxNode.Id}' is not solved");

            // solve the types
            var types = typeSolving.Solve();
            if(types.GenericsCount>0)
                throw new NotImplementedException("Generic user functions are not supported");
            resultsBuilder.SetResults(types);
            var typeInferenceResuls = resultsBuilder.Build(); 


            //set types to nodes
            var converter = new TypeInferenceOnlyConcreteInterpriter();
            functionSyntaxNode.ComeOver(
                enterVisitor:new ApplyTiResultEnterVisitor(
                    solving:               typeInferenceResuls,  
                    tiToLangTypeConverter: converter),
                exitVisitor: new ApplyTiResultsExitVisitor());

            var returnType = converter.Convert(typeInferenceResuls.GetVariableType("$result"));
            var argTypes = functionSyntaxNode
                .Args
                .Select(a => converter.Convert(typeInferenceResuls.GetVariableType(a.Id)))
                .ToArray();


            //make function prototype
            var prototype = new ConcreteUserFunctionPrototype(functionSyntaxNode.Id, returnType, argTypes);
            //add prototype to dictionary for future use
            functionsDictionary.Add(prototype);
            var function = BuildConcreteFunction(functionSyntaxNode, prototype, functionsDictionary, typeInferenceResuls);
            prototype.SetActual(function, functionSyntaxNode.Interval);
            return function;
        }

        private static UserFunction BuildConcreteFunction(
            UserFunctionDefenitionSyntaxNode functionSyntax,
            ConcreteUserFunctionPrototype functionPrototype,
            IFunctionDicitionary functionsDictionary, 
            TypeInferenceResults results)
        {
            var vars = new VariableDictionary();
            for (int i = 0; i < functionSyntax.Args.Count; i++)
            {
                var variableSource = RuntimeBuilderHelper.CreateVariableSourceForArgument(
                    argSyntax: functionSyntax.Args[i],
                    actualType: functionPrototype.ArgTypes[i]);

                if (!vars.TryAdd(variableSource))
                    throw ErrorFactory.FunctionArgumentDuplicates(functionSyntax, functionSyntax.Args[i]);
            }

            var bodyExpression = ExpressionBuilderVisitor.BuildExpression(
                    node: functionSyntax.Body,
                    functions: functionsDictionary,
                    outputType: functionPrototype.ReturnType,
                    variables: vars, 
                    typeInferenceResults: results);

            vars.ThrowIfSomeVariablesNotExistsInTheList(
                 functionSyntax.Args.Select(a => a.Id));

            var function = new UserFunction(
                name: functionSyntax.Id,
                variables: vars.GetAllSources(),
                isReturnTypeStrictlyTyped: functionSyntax.ReturnType != VarType.Empty,
                expression: bodyExpression);
            return function;
        }



    }
}
