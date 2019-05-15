using System;
using System.Collections.Generic;
using System.Linq;
using NFun.HindleyMilner;
using NFun.HindleyMilner.Tyso;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.Parsing;
using NFun.Runtime;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.Interpritation
{
    public static class RuntimeBuilder
    {
        public static FunRuntime Build(
            SyntaxTree syntaxTree,
            FunctionsDictionary functionsDictionary)
        {
            //get topology sort of the functions
            var functionSolveOrder  = FindFunctionsSolvingOrderOrThrow(syntaxTree);
            
            //build user functions
            foreach (var functionSyntaxNode in functionSolveOrder)
                BuildFunctionAndPutItToDictionary(functionSyntaxNode, functionsDictionary);
            
            //solve body
            var bodyTypeSolving = new HmAlgorithmAdapter(functionsDictionary).Apply(syntaxTree);
            if (!bodyTypeSolving.IsSolved)
                throw new InvalidOperationException("Types not solved");
            
            foreach (var syntaxNode in syntaxTree.Children)
            {
                //function nodes were solved above
                if(syntaxNode is UserFunctionDefenitionSyntaxNode)
                    continue;
                
                //set types to nodes
                syntaxNode.ComeOver(new ApplyHmResultVisitor(bodyTypeSolving, new RealTypeConverter()));
            }
            
            var variables = new VariableDictionary(); 
            var equations = new List<Equation>();

            foreach (var lexRoot in syntaxTree.Nodes)
            {
                if (lexRoot is EquationSyntaxNode node)
                {
                    var equation = BuildEquationAndPutItToVariables(node, functionsDictionary, variables);
                    equations.Add(equation);
                }
                else if (lexRoot is VarDefenitionSyntaxNode varDef)
                {
                    var variableSource = new VariableSource(varDef.Id, varDef.VarType, varDef.Attributes);
                    if (!variables.TryAdd(variableSource))
                    {
                        var allUsages = variables.GetUsages(variableSource.Name);
                        throw ErrorFactory.VariableIsDeclaredAfterUsing(allUsages);
                    }
                }
                else if(lexRoot is UserFunctionDefenitionSyntaxNode)
                    continue;//user function was built above
                else 
                    throw  new InvalidOperationException($"Type {lexRoot} is not supported as tree root");
            }   
            return new FunRuntime(equations, variables);
        }
        

        private static Equation BuildEquationAndPutItToVariables(EquationSyntaxNode equation,FunctionsDictionary functionsDictionary, VariableDictionary variables)
        {
            var expression = ExpressionBuilderVisitor.BuildExpression(equation.Expression, functionsDictionary, variables);
            
            
            var newSource = new VariableSource(equation.Id, equation.OutputType, equation.Attributes)
            {
                IsOutput = true
            };
            if (!variables.TryAdd(newSource))
            {
                //some equation referenced the source before
                var usages = variables.GetUsages(equation.Id);
                if (usages.Source.IsOutput)
                    throw ErrorFactory.OutputNameWithDifferentCase(equation.Id, equation.Expression.Interval);
                else
                    throw ErrorFactory.CannotUseOutputValueBeforeItIsDeclared(usages, equation.Id);
            }

            //ReplaceInputType
            if(newSource.Type != expression.Type)
                throw new InvalidOperationException("Equation types mismatch");
            
            return new Equation(equation.Id, expression);
        }

        private static void BuildFunctionAndPutItToDictionary(UserFunctionDefenitionSyntaxNode functionSyntaxNode,
            FunctionsDictionary functionsDictionary)
        {
            var funAlias = functionSyntaxNode.GetFunAlias();

            //introduce function variable here
            var visitorInitState = CreateVisitorStateFor(functionSyntaxNode);

            //solving each function
            var typeSolving = new HmAlgorithmAdapter(functionsDictionary, visitorInitState);

            visitorInitState.CurrentSolver.SetFunDefenition(funAlias, functionSyntaxNode.NodeNumber,
                functionSyntaxNode.Body.NodeNumber);
            // solve the types
            var types = typeSolving.Apply(functionSyntaxNode);
            if (!types.IsSolved)
                throw new FunParseException(-4, $"Function '{functionSyntaxNode.Id}' is not solved", 0, 0);

            //set types to nodes
            functionSyntaxNode.ComeOver(new ApplyHmResultVisitor(types,new RealTypeConverter()));

            var funType = types.GetVarType(funAlias, new RealTypeConverter());
            //make function prototype
            var prototype = new FunctionPrototype(functionSyntaxNode.Id,
                funType.FunTypeSpecification.Output,
                funType.FunTypeSpecification.Inputs);
            //add prototype to dictionary for future use
            functionsDictionary.Add(prototype);
            BuildFunction(functionSyntaxNode, prototype, functionsDictionary);
        }
        private static void BuildFunction(
            UserFunctionDefenitionSyntaxNode lexFunction, 
            FunctionPrototype prototype, 
            FunctionsDictionary functionsDictionary)
        {
            var vars = new VariableDictionary();
            for (int i = 0; i < lexFunction.Args.Count ; i++)
            {
                var id = lexFunction.Args[i].Id;
                if (!vars.TryAdd(new VariableSource(id, prototype.ArgTypes[i])))
                {
                    throw ErrorFactory.FunctionArgumentDuplicates(lexFunction, lexFunction.Args[i]);
                }

            }
            var expression = ExpressionBuilderVisitor
                .BuildExpression(lexFunction.Body, functionsDictionary, vars);
            
            ExpressionHelper.CheckForUnknownVariables(
                lexFunction.Args.Select(a=>a.Id).ToArray(), vars);
            
            var function = new UserFunction(lexFunction.Id, vars.GetAllSources(), expression);
            prototype.SetActual(function, lexFunction.Interval);
        }
        
        public static HmVisitorState CreateVisitorStateFor(UserFunctionDefenitionSyntaxNode node)
        {
            var visitorState = new HmVisitorState(new NsHumanizerSolver());
            
            //Add user function as a functional variable

            //make outputType
            var outputType =  visitorState.CreateTypeNode(node.SpecifiedType);
            //create input variables
            var argTypes = new List<SolvingNode>();
            foreach (var argNode in node.Args)
            {
                var inputAlias = AdpterHelper.GetArgAlias(argNode.Id, node.GetFunAlias());

                //make aliases for input variables
                visitorState.AddVariableAliase(argNode.Id, inputAlias);
                
                if (argNode.VarType.BaseType == BaseVarType.Empty)
                {
                    //variable type is not specified
                    var genericVarType = visitorState.CurrentSolver.SetNewVar(inputAlias);
                    argTypes.Add(genericVarType);
                }
                else
                {
                    //variable type is specified
                    var hmType = argNode.VarType.ConvertToHmType();
                    visitorState.CurrentSolver.SetVarType(inputAlias, hmType);
                    argTypes.Add(SolvingNode.CreateStrict(hmType));
                }
                    
            }
            //set function variable defenition
            visitorState.CurrentSolver
                .SetVarType(node.GetFunAlias(), FType.Fun(outputType, argTypes.ToArray()));
            return visitorState;
        }
        
        /// <summary>
        /// Gets order of calculating the functions, based on its co using.
        /// </summary>
        private static UserFunctionDefenitionSyntaxNode[] FindFunctionsSolvingOrderOrThrow(SyntaxTree syntaxTree)
        {
            var userFunctions = syntaxTree.Children.OfType<UserFunctionDefenitionSyntaxNode>().ToList();

            var userFunctionsNames = new Dictionary<string, int>();
            int i = 0;
            foreach (var userFunction in userFunctions)
            {
                userFunctionsNames.Add(userFunction.Id + "(" + userFunction.Args.Count + ")", i);
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
            if (sortResults.HasCycle)
                throw new InvalidOperationException("Cycled functions found");

            var functionSolveOrder = new UserFunctionDefenitionSyntaxNode[sortResults.NodeNames.Length];
            for (int k = 0; k < sortResults.NodeNames.Length; k++)
                functionSolveOrder[k] = userFunctions[sortResults.NodeNames[k]];
            return functionSolveOrder;
        }

        
        
    }
}