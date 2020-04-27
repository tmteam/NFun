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
using NFun.TypeInference;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Interpritation
{
    public static class RuntimeBuilder
    {
        public static FunRuntime Build(
            SyntaxTree syntaxTree,
            FunctionsDictionary functionsDictionary)
        {
            throw new NotImplementedException();

            //#region build user functions
            ////get topology sort of the functions call
            ////result is the order of functions that need to be compiled
            ////functions that not references other functions have to be compiled firstly
            ////Then those functions will be compiled
            ////that refer to already compiled functions
            //var functionSolveOrder  = syntaxTree.FindFunctionSolvingOrderOrThrow();

            ////build user functions
            //var userFunctions = new List<UserFunction>();
            //foreach (var functionSyntaxNode in functionSolveOrder)
            //{
            //   var userFunction =  BuildFunctionAndPutItToDictionary(functionSyntaxNode, functionsDictionary);
            //   userFunctions.Add(userFunction);
            //}
            //#endregion

            //#region solve body types
            ////Solve types for all equations nodes
            //var bodyTypeSolving = RuntimeBuilderHelper.SolveOrThrow(syntaxTree, functionsDictionary);

            //foreach (var syntaxNode in syntaxTree.Children)
            //{
            //    //function nodes were solved above
            //    if(syntaxNode is UserFunctionDefenitionSyntaxNode)
            //        continue;

            //    //set types to nodes
            //    syntaxNode.ComeOver(
            //        enterVisitor: new ApplyTiResultEnterVisitor(bodyTypeSolving, TiToLangTypeConverter.SetGenericsToAny),
            //        exitVisitor:  new ApplyTiResultsExitVisitor());
            //}
            //#endregion

            //#region build body
            //var variables = new VariableDictionary(); 
            //var equations = new List<Equation>();

            //foreach (var treeNode in syntaxTree.Nodes)
            //{
            //    if (treeNode is EquationSyntaxNode node)
            //    {
            //        var equation = BuildEquationAndPutItToVariables(node, functionsDictionary, variables);
            //        equations.Add(equation);
            //    }
            //    else if (treeNode is VarDefenitionSyntaxNode varDef)
            //    {
            //        var variableSource = VariableSource.CreateWithStrictTypeLabel(
            //            varDef.Id, 
            //            varDef.VarType, 
            //            varDef.Interval, 
            //            varDef.Attributes);
            //        if (!variables.TryAdd(variableSource))
            //        {
            //            var allUsages = variables.GetUsages(variableSource.Name);
            //            throw ErrorFactory.VariableIsDeclaredAfterUsing(allUsages);
            //        }
            //    }
            //    else if(treeNode is UserFunctionDefenitionSyntaxNode)
            //        continue;//user function was built above
            //    else 
            //        throw  new InvalidOperationException($"Type {treeNode} is not supported as tree root");
            //}   
            //#endregion
            //return new FunRuntime(equations, variables, userFunctions);
        }

        private static Equation BuildEquationAndPutItToVariables(
            EquationSyntaxNode equation,
            FunctionsDictionary functionsDictionary, 
            VariableDictionary variables)
        {
            var expression = ExpressionBuilderVisitor.BuildExpression(
                node: equation.Expression, 
                functions: functionsDictionary,
                outputType: equation.OutputType,
                variables: variables);

            
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
            FunctionsDictionary functionsDictionary)
        {
            throw new NotImplementedException();

            //var funAlias = functionSyntaxNode.GetFunAlias();

            ////introduce function variable here
            //var visitorInitState = CreateVisitorStateFor(functionSyntaxNode);

            ////solving each function
            //var typeSolving = LangTiHelper.SetupTiOrNull(functionSyntaxNode.Body, functionsDictionary, visitorInitState);

            //if (typeSolving==null)
            //    throw FunParseException.ErrorStubToDo($"Function '{functionSyntaxNode.Id}' is not solved");

            //var setFunTypeResult = visitorInitState.CurrentSolver.SetFunDefenition(funAlias,
            //    functionSyntaxNode.OrderNumber,
            //    functionSyntaxNode.Body.OrderNumber);
            //if (!setFunTypeResult.IsSuccesfully)
            //{
            //    if (setFunTypeResult.Error == SetTypeResultError.VariableDefenitionDuplicates)
            //        throw ErrorFactory.FunctionAlreadyExist(functionSyntaxNode);
            //    else
            //        throw ErrorFactory.FunctionTypesNotSolved(functionSyntaxNode);

            //}


            //// solve the types
            //var types = typeSolving.Solve();
            //RuntimeBuilderHelper.ThrowIfNotSolved(functionSyntaxNode, types);

            //var isGeneric = types.GenericsCount > 0;
            ////set types to nodes
            //functionSyntaxNode.ComeOver(
            //    enterVisitor:new ApplyTiResultEnterVisitor(types, TiToLangTypeConverter.SaveGenerics),
            //    exitVisitor: new ApplyTiResultsExitVisitor());
            //var funType =  types.GetVarType(funAlias, TiToLangTypeConverter.SaveGenerics);

            //if (isGeneric)
            //{
            //    var prototype = new GenericUserFunctionPrototype(functionSyntaxNode.Id,
            //        funType.FunTypeSpecification.Output,
            //        funType.FunTypeSpecification.Inputs);
            //    //add prototype to dictionary for future use
            //    functionsDictionary.Add(prototype);
            //    return BuildGenericFunction(functionSyntaxNode, prototype, functionsDictionary);
            //}
            //else
            //{
            //    //make function prototype
            //    var prototype = new ConcreteUserFunctionPrototype(functionSyntaxNode.Id,
            //        funType.FunTypeSpecification.Output,
            //        funType.FunTypeSpecification.Inputs);
            //    //add prototype to dictionary for future use
            //    functionsDictionary.Add(prototype);
            //    return BuildConcreteFunction(functionSyntaxNode, prototype, functionsDictionary);
            //}
        }



        private static UserFunction BuildGenericFunction(
            UserFunctionDefenitionSyntaxNode functionSyntax, 
            GenericUserFunctionPrototype     functionPrototype, 
            FunctionsDictionary              functionsDictionary)
        {
            var vars = new VariableDictionary();
            for (int i = 0; i < functionSyntax.Args.Count ; i++)
            {
                var id = functionSyntax.Args[i].Id;
                if (!vars.TryAdd(VariableSource.CreateWithoutStrictTypeLabel(id, functionPrototype.ArgTypes[i])))
                {
                    throw ErrorFactory.FunctionArgumentDuplicates(functionSyntax, functionSyntax.Args[i]);
                }

            }
            var expression = ExpressionBuilderVisitor
                .BuildExpression(functionSyntax.Body, functionsDictionary, vars);
            
            vars.ThrowIfSomeVariablesNotExistsInTheList(
                 functionSyntax.Args.Select(a=>a.Id));
            
            var function = new UserFunction(
                name: functionSyntax.Id, 
                variables: vars.GetAllSources(),
                isReturnTypeStrictlyTyped: functionSyntax.ReturnType!= VarType.Empty, 
                expression: expression);
            
            functionPrototype.SetActual(function, functionSyntax.Interval);
            
            return function;
        }
        
        private static UserFunction BuildConcreteFunction(
            UserFunctionDefenitionSyntaxNode functionSyntax, 
            ConcreteUserFunctionPrototype functionPrototype, 
            FunctionsDictionary functionsDictionary)
        {
            var vars = new VariableDictionary();
            for (int i = 0; i < functionSyntax.Args.Count ; i++)
            {
                var variableSource = RuntimeBuilderHelper.CreateVariableSourceForArgument(
                    argSyntax:  functionSyntax.Args[i], 
                    actualType: functionPrototype.ArgTypes[i]);
                
                if (!vars.TryAdd(variableSource))
                    throw ErrorFactory.FunctionArgumentDuplicates(functionSyntax, functionSyntax.Args[i]);
            }
            
            var bodyExpression = ExpressionBuilderVisitor.BuildExpression(
                    node: functionSyntax.Body, 
                    functions: functionsDictionary, 
                    outputType: functionSyntax.ReturnType== VarType.Empty
                                    ?functionSyntax.Body.OutputType
                                    :functionSyntax.ReturnType,
                    variables: vars);
            
            vars.ThrowIfSomeVariablesNotExistsInTheList(
                 functionSyntax.Args.Select(a=>a.Id));
            
            var function = new UserFunction(
                name:                      functionSyntax.Id, 
                variables:                 vars.GetAllSources(), 
                isReturnTypeStrictlyTyped: functionSyntax.ReturnType!= VarType.Empty, 
                expression:                bodyExpression);
            functionPrototype.SetActual(function, functionSyntax.Interval);
            return function;
        }

        private static SetupTiState CreateVisitorStateFor(UserFunctionDefenitionSyntaxNode node)
        {
            throw new NotImplementedException();
            //var visitorState = new SetupTiState(new TiLanguageSolver());
            
            ////Add user function as a functional variable
            ////make outputType
            //var outputType = visitorState.CreateTypeNode(node.ReturnType);
            
            ////create input variables
            //var argTypes = new List<SolvingNode>();
            //foreach (var argNode in node.Args)
            //{
            //    if (visitorState.HasAlias(argNode.Id))
            //        throw ErrorFactory.FunctionArgumentDuplicates(node, argNode);

            //    var inputAlias = LangTiHelper.GetArgAlias(argNode.Id, node.GetFunAlias());

            //    //make aliases for input variables
            //    visitorState.AddVariableAliase(argNode.Id, inputAlias);
                
            //    if (argNode.VarType.BaseType == BaseVarType.Empty)
            //    {
            //        //variable type is not specified
            //        var genericVarType = visitorState.CurrentSolver.SetNewVarOrThrow(inputAlias);
            //        argTypes.Add(genericVarType);
            //    }
            //    else
            //    {
            //        //variable type is specified
            //        var hmType = argNode.VarType.ConvertToTiType();
            //        visitorState.CurrentSolver.SetVarType(inputAlias, hmType);
            //        argTypes.Add(SolvingNode.CreateStrict(hmType));
            //    }
                    
            //}
            ////set function variable defenition
            //visitorState.CurrentSolver
            //    .SetVarType(node.GetFunAlias(), TiType.Fun(outputType, argTypes.ToArray()));
            //return visitorState;
        }
    }
}
