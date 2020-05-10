using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.TypeInferenceAdapter;
using NFun.TypeInferenceCalculator;
using NFun.TypeInferenceCalculator.Errors;

namespace NFun.Interpritation
{
    public static class RuntimeBuilder
    {
        public static FunRuntime Build(
            SyntaxTree syntaxTree,
            FunctionDictionary functionsDictionary)
        {
            var userFunctionsList = new List<IFunctionSignature>();
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
            {
                var userFun =BuildFunctionAndPutItToDictionary(functionSyntaxNode, scopeFunctionDictionary);
                userFunctionsList.Add(userFun);
            }

            #endregion

            #region solve body types
            //Solve types for all equations nodes
            var bodyTypeSolving = RuntimeBuilderHelper.SolveBodyOrThrow(syntaxTree, scopeFunctionDictionary);

            foreach (var syntaxNode in syntaxTree.Children)
            {
                //function nodes were solved above
                if (syntaxNode is UserFunctionDefenitionSyntaxNode)
                    continue;

                //set types to nodes
                syntaxNode.ComeOver(
                    enterVisitor: new ApplyTiResultEnterVisitor(bodyTypeSolving, TicTypesConverter.Concrete),
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
            return new FunRuntime(equations, variables, userFunctionsList);
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
                typeInferenceResults: typeInferenceResults, 
                typesConverter: TicTypesConverter.Concrete);
            
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


        private static IFunctionSignature BuildFunctionAndPutItToDictionary(
            UserFunctionDefenitionSyntaxNode functionSyntaxNode,
            ScopeFunctionDictionary functionsDictionary)
        {
            TraceLog.WriteLine($"\r\n====BUILD {functionSyntaxNode.Id}(..) ====");

            ////introduce function variable
            var graphBuider = new GraphBuilder();
            var resultsBuilder = new TypeInferenceResultsBuilder();
            FinalizationResults types;

            try
            {
                //setup body type inference
                if (!LangTiHelper.SetupTiOrNull(
                    functionSyntaxNode,
                    functionsDictionary,
                    resultsBuilder,
                    new SetupTiState(graphBuider)))
                    throw FunParseException.ErrorStubToDo($"Function '{functionSyntaxNode.Id}' is not solved");

                // solve the types
                types = graphBuider.Solve();
            }
            catch (TicException e) {
                throw FunParseException.ErrorStubToDo($"Types not solved. {e}");
            }

            resultsBuilder.SetResults(types);
            var typeInferenceResuls = resultsBuilder.Build();

            if (types.GenericsCount == 0)
            {
                #region concreteFunction

                //set types to nodes
                functionSyntaxNode.ComeOver(
                    enterVisitor: new ApplyTiResultEnterVisitor(
                        solving: typeInferenceResuls,
                        tiToLangTypeConverter: TicTypesConverter.Concrete),
                    exitVisitor: new ApplyTiResultsExitVisitor());

                var funType = TicTypesConverter.Concrete.Convert(
                    typeInferenceResuls.GetVariableType(functionSyntaxNode.Id + "'" + functionSyntaxNode.Args.Count));

                var returnType = funType.FunTypeSpecification.Output;
                var argTypes = funType.FunTypeSpecification.Inputs;

                //make function prototype
                var prototype = new ConcreteUserFunctionPrototype(functionSyntaxNode.Id, returnType, argTypes);
                //add prototype to dictionary for future use
                functionsDictionary.Add(prototype);
                var function =
                    functionSyntaxNode.BuildConcrete(
                        argTypes:   argTypes, 
                        returnType: returnType, 
                        functionsDictionary: functionsDictionary, 
                        results:    typeInferenceResuls, 
                        converter:  TicTypesConverter.Concrete);
                
                prototype.SetActual(function, functionSyntaxNode.Interval);
                return function;
                #endregion
            }
            else
            {
                var function = GenericUserFunction.Create(typeInferenceResuls, functionSyntaxNode, functionsDictionary);
                functionsDictionary.Add(function);
                //Нужно интерпритировать какой либо тип функции, что бы проверить ошибки
                GenericUserFunction.CreateSomeConcrete(function);
                
                return function;
            }
        }
    }
}
