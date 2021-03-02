using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.Errors;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;

namespace NFun.Interpritation
{
    public static class RuntimeBuilder
    {
        private static readonly List<IFunctionSignature> EmptyUserFunctionsList 
            = new List<IFunctionSignature>();

        public static FunRuntime Build(string script, IFunctionDictionary functionDictionary, IConstantList constants)
        {
            var flow = Tokenizer.ToFlow(script);
            var syntaxTree = Parser.Parse(flow);

            //Set node numbers
            var setNodeNumberVisitor = new SetNodeNumberVisitor();
            syntaxTree.ComeOver(setNodeNumberVisitor);
            syntaxTree.MaxNodeId = setNodeNumberVisitor.LastUsedNumber;
            return Build(syntaxTree, functionDictionary, constants);
        }

        private static FunRuntime Build(
            SyntaxTree syntaxTree,
            IFunctionDictionary functionsDictionary, 
            IConstantList constants)
        {
            #region build user functions
            //get topology sort of the functions call
            //result is the order of functions that need to be compiled
            //functions that not references other functions have to be compiled firstly
            //Then those functions will be compiled
            //that refer to already compiled functions
            var functionSolveOrder = syntaxTree.FindFunctionSolvingOrderOrThrow();
            List<IFunctionSignature> userFunctions;
            IFunctionDictionary functionDictionary;
            if (functionSolveOrder.Length == 0)
            {
                functionDictionary = functionsDictionary;
                userFunctions = EmptyUserFunctionsList;
            }
            else
            {
                userFunctions = new List<IFunctionSignature>();

                var scopeFunctionDictionary = new ScopeFunctionDictionary(functionsDictionary);
                functionDictionary = scopeFunctionDictionary;
                //build user functions
                foreach (var functionSyntaxNode in functionSolveOrder)
                {
                    //todo list capacity
                    var userFun = BuildFunctionAndPutItToDictionary(
                        functionSyntaxNode: functionSyntaxNode,
                        constants: constants,
                        functionsDictionary: scopeFunctionDictionary);
                    userFunctions.Add(userFun);
                }
            }

            #endregion

            var bodyTypeSolving = SolveBodyTypes(syntaxTree, constants, functionDictionary);

            #region build body
            var variables = new VariableDictionary();
            var equations = new List<Equation>();

            foreach (var treeNode in syntaxTree.Nodes)
            {
                if (treeNode is EquationSyntaxNode node)
                {
                    var equation = BuildEquationAndPutItToVariables(node, functionDictionary, variables, bodyTypeSolving);
                    equations.Add(equation);
                    if (Helper.DoesItLooksLikeSuperAnonymousVariable(equation.Id))
                        throw FunParseException.ErrorStubToDo("variable cannot starts with 'it'");
                    if(TraceLog.IsEnabled)
                        TraceLog.WriteLine($"\r\nEQUATION: {equation.Id}:{equation.Expression.Type} = ... \r\n");
                }
                else if (treeNode is VarDefinitionSyntaxNode varDef)
                {
                    if (Helper.DoesItLooksLikeSuperAnonymousVariable(varDef.Id))
                        throw FunParseException.ErrorStubToDo("variable cannot starts with 'it'");

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
                    if(TraceLog.IsEnabled)
                        TraceLog.WriteLine($"\r\nVARIABLE: {variableSource.Name}:{variableSource.Type} = ... \r\n");
                }
                else if (treeNode is UserFunctionDefinitionSyntaxNode)
                    continue;//user function was built above
                else
                    throw new InvalidOperationException($"Type {treeNode} is not supported as tree root");
            }
            #endregion

            foreach (var userFunction in userFunctions)
            {
                if (userFunction is GenericUserFunction generic && generic.BuiltCount == 0)
                {
                    // Generic function is declared but concrete was not built.
                    // We have to build it at least once to search all possible errors
                    GenericUserFunction.CreateSomeConcrete(generic);
                }
            }                           
            return new FunRuntime(equations, variables, userFunctions);
        }


        private static TypeInferenceResults SolveBodyTypes(
            SyntaxTree syntaxTree,
            IConstantList constants, 
            IFunctionDictionary functionDictionary)
        {
            var bodyTypeSolving = RuntimeBuilderHelper.SolveBodyOrThrow(syntaxTree, functionDictionary, constants);

            var enterVisitor = new ApplyTiResultEnterVisitor(bodyTypeSolving, TicTypesConverter.Concrete);
            var exitVisitor  = new ApplyTiResultsExitVisitor();
            foreach (var syntaxNode in syntaxTree.Nodes)
            {
                //function nodes were solved above
                if (syntaxNode is UserFunctionDefinitionSyntaxNode)
                    continue;

                //set types to nodes
                syntaxNode.ComeOver(enterVisitor, exitVisitor);
            }

            return bodyTypeSolving;
        }

        private static Equation BuildEquationAndPutItToVariables(
            EquationSyntaxNode equation,
            IFunctionDictionary functionsDictionary, 
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
            
            VariableSource outputVariableSource;
            if(equation.OutputTypeSpecified)
                outputVariableSource = VariableSource.CreateWithStrictTypeLabel(
                    name: equation.Id, 
                    type: equation.OutputType, 
                    typeSpecificationIntervalOrNull: equation.TypeSpecificationOrNull.Interval, 
                    attributes: equation.Attributes);
            else
                outputVariableSource = VariableSource.CreateWithoutStrictTypeLabel(equation.Id, equation.OutputType, equation.Attributes);
            
            outputVariableSource.IsOutput = true;

            
            var itVariable = variables.GetSuperAnonymousVariableOrNull();
            if (itVariable != null)
                throw FunParseException.ErrorStubToDo("Variable cannot starts with it");


            if (!variables.TryAdd(outputVariableSource))
            {
                //some equation referenced the source before
                var usages = variables.GetUsages(equation.Id);
                if (usages.Source.IsOutput)
                    throw ErrorFactory.OutputNameWithDifferentCase(equation.Id, equation.Expression.Interval);
                else
                    throw ErrorFactory.CannotUseOutputValueBeforeItIsDeclared(usages);
            }
            
           
            //ReplaceInputType
            if(outputVariableSource.Type != expression.Type)
                throw new ImpossibleException("fitless");            
            return new Equation(equation.Id, expression, outputVariableSource);
        }


        private static IFunctionSignature BuildFunctionAndPutItToDictionary(
            UserFunctionDefinitionSyntaxNode functionSyntaxNode,
            IConstantList constants,
            ScopeFunctionDictionary functionsDictionary)
        {
#if DEBUG
            TraceLog.WriteLine($"\r\n====BUILD {functionSyntaxNode.Id}(..) ====");
#endif
            ////introduce function variable
            var graphBuider = new GraphBuilder();
            var resultsBuilder = new TypeInferenceResultsBuilder();
            ITicResults types;

            try
            {
                if (!TicSetupVisitor.SetupTicForUserFunction(
                    userFunctionNode: functionSyntaxNode, 
                    ticGraph:  graphBuider, 
                    functions: functionsDictionary, 
                    constants: constants,
                    results:   resultsBuilder))
                    throw FunParseException.ErrorStubToDo($"Function '{functionSyntaxNode.Id}' is not solved");
                
                // solve the types
                types = graphBuider.Solve();
            }
            catch (TicException e) { throw ErrorFactory.TranslateTicError(e, functionSyntaxNode);}

            resultsBuilder.SetResults(types);
            var typeInferenceResuls = resultsBuilder.Build();

            if (!types.HasGenerics)
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
                if (TraceLog.IsEnabled) 
                    TraceLog.WriteLine($"\r\n=====> Generic {functionSyntaxNode.Id} {funType}");
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
                if (TraceLog.IsEnabled) 
                    TraceLog.WriteLine($"\r\n=====> Concrete {functionSyntaxNode.Id} {function}");
                return function;
            }
        }
    }
}
