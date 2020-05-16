using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Interpritation
{
    public sealed class ExpressionBuilderVisitor: ISyntaxNodeVisitor<IExpressionNode> {
        
        private readonly IFunctionDicitionary _functions;
        private readonly VariableDictionary _variables;
        private readonly TypeInferenceResults _typeInferenceResults;
        private readonly TicTypesConverter _typesConverter;
        public void OnEnterNode(ISyntaxNode parent, int childNum) { }

        public static IExpressionNode BuildExpression(
            ISyntaxNode node,
            IFunctionDicitionary functions,
            VariableDictionary variables, 
            TypeInferenceResults typeInferenceResults, 
            TicTypesConverter typesConverter) =>
            node.Accept(new ExpressionBuilderVisitor(functions, variables, typeInferenceResults, typesConverter));

        public static IExpressionNode BuildExpression(
            ISyntaxNode node,
            IFunctionDicitionary functions,
            VarType outputType,
            VariableDictionary variables, 
            TypeInferenceResults typeInferenceResults, 
            TicTypesConverter typesConverter)
        {
            var result =  node.Accept(
                new ExpressionBuilderVisitor(functions, variables, typeInferenceResults, typesConverter));
            if (result.Type == outputType)
                return result;
            var converter = VarTypeConverter.GetConverterOrThrow(result.Type, outputType, node.Interval);
            return new CastExpressionNode(result, outputType, converter, node.Interval);
        }

        private ExpressionBuilderVisitor(
            IFunctionDicitionary functions, 
            VariableDictionary variables,
            TypeInferenceResults typeInferenceResults, 
            TicTypesConverter typesConverter)
        {
            _functions = functions;
            _variables = variables;
            _typeInferenceResults = typeInferenceResults;
            _typesConverter = typesConverter;
        }

        public IExpressionNode Visit(ArrowAnonymFunctionSyntaxNode arrowAnonymFunNode)
        {
            if (arrowAnonymFunNode.Defenition==null)
                throw ErrorFactory.AnonymousFunDefenitionIsMissing(arrowAnonymFunNode);

            if(arrowAnonymFunNode.Body==null)
                throw ErrorFactory.AnonymousFunBodyIsMissing(arrowAnonymFunNode);
            
            //Anonym fun arguments list
            var argumentLexNodes = arrowAnonymFunNode.ArgumentsDefenition;
            
            //Prepare local variable scope
            //Capture all outerscope variables
            var localVariables = new VariableDictionary(_variables.GetAllSources());
            
            var arguments = new List<VariableSource>();
            foreach (var arg in argumentLexNodes)
            {
                //Convert argument node
                var varNode = FunArgumentExpressionNode.CreateWith(arg);
                var source = VariableSource.CreateWithStrictTypeLabel(varNode.Name, varNode.Type, arg.Interval);
                //collect argument
                arguments.Add(source);
                //add argument to local scope
                if (!localVariables.TryAdd(source))
                {   //Check for duplicated arg-names

                    //If outer-scope contains the conflict variable name
                    if (_variables.GetSourceOrNull(varNode.Name) != null)
                        throw ErrorFactory.AnonymousFunctionArgumentConflictsWithOuterScope(varNode, arrowAnonymFunNode.Defenition);
                    else //else it is duplicated arg name
                        throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, arrowAnonymFunNode.Defenition);
                }
            }

            var sources = localVariables.GetAllSources();
            var originVariables = new string[sources.Length];
            for (int i = 0; i < originVariables.Length; i++) originVariables[i] = sources[i].Name;
            var expr = BuildExpression(arrowAnonymFunNode.Body, _functions, localVariables, _typeInferenceResults,_typesConverter);
            
            //New variables are new closured
            var closured =  localVariables.GetAllUsages()
                .Where(s => !originVariables.Contains(s.Source.Name))
                .ToList();

            //Add closured vars to outer-scope dictionary
            foreach (var newVar in closured)
                _variables.TryAdd(newVar); //add full usage info to allow analyze outer errors
            
            var fun = new ConcreteUserFunction(
                name:       "anonymous", 
                variables:  arguments.ToArray(),
                isReturnTypeStrictlyTyped: arrowAnonymFunNode.Defenition.OutputType!= VarType.Empty, 
                expression: expr );
            return new FunVariableExpressionNode(fun, arrowAnonymFunNode.Interval);
        }

        public IExpressionNode Visit(ArraySyntaxNode node)
        {
            var nodes = new IExpressionNode[node.Expressions.Length];
            for (int i = 0; i< node.Expressions.Length; i++)
                nodes[i] = ReadNode(node.Expressions[i]);

            return new ArrayExpressionNode(nodes,node.Interval, node.OutputType);
        }

        public IExpressionNode Visit(FunCallSyntaxNode node)
        {
            var id = node.Id;
            
            var someFunc = _functions.GetOrNull(id, node.Args.Length);
            if (someFunc is null)
            {
                //todo move to variable syntax node

                //hi order function
                 var functionalVariableSource = _variables.GetSourceOrNull(id);
                 if (functionalVariableSource?.Type.FunTypeSpecification == null)
                     throw ErrorFactory.FunctionOverloadNotFound(node, _functions);
                 return CreateFunctionCall(node, ConcreteHiOrderFunction.Create(functionalVariableSource));
            }

            if (someFunc is FunctionBase f) //concrete function
                return CreateFunctionCall(node, f);

            if (someFunc is GenericFunctionBase genericFunction) //generic function
            {
                VarType[] genericArgs;

                var genericTypes = _typeInferenceResults.GetGenericCallArguments(node.OrderNumber);
                if (genericTypes == null)
                {
                    // Generuc call arguments are unknown  in case of  generic recursion function . 
                    // Take it from type inference results in this case
                    var recCallSignature =  _typeInferenceResults.GetRecursiveCallOrNull(node.OrderNumber);
                    //if generic call arguments not exist in type inference result - it is NFUN core error
                    if(recCallSignature==null)
                        throw new ImpossibleException($"MJ78. Function {id}`{node.Args.Length} was not found");

                    var varTypeCallSignature = _typesConverter.Convert(recCallSignature);
                    //Calculate generic call arguments by concrete function signature
                    genericArgs = genericFunction.CalcGenericArgTypeList(varTypeCallSignature.FunTypeSpecification);
                }
                else
                {
                    genericArgs = new VarType[genericTypes.Length];
                    for (int i = 0; i < genericTypes.Length; i++) 
                        genericArgs[i] = _typesConverter.Convert(genericTypes[i]);
                }

                var function = genericFunction.CreateConcrete(genericArgs);
                return CreateFunctionCall(node, function);
            }

            throw new ImpossibleException($"MJ101. Function {id}`{node.Args.Length} type is unknown");
        }

        public IExpressionNode Visit(ResultFunCallSyntaxNode node)
        {
            var functionGenerator = ReadNode(node.ResultExpression);
            var function          = ConcreteHiOrderFunctionWithSyntaxNode.Create(functionGenerator);
            return CreateFunctionCall(node, function);
        }

        public IExpressionNode Visit(MetaInfoSyntaxNode node)
        {
            var id = node.VariableSyntaxNode.Id;
            if (!_typeInferenceResults.HasVariable(id))
                throw FunParseException.ErrorStubToDo($"Variable {id} not exist in the scope");
            return new MetaInfoExpressionNode(_variables, id , node.Interval);
        }

        public IExpressionNode Visit(SuperAnonymFunctionSyntaxNode arrowAnonymFunNode)
        {
            throw new NotImplementedException();
        }

        public IExpressionNode Visit(IfThenElseSyntaxNode node)
        {
            //expressions
            //if (...) {here} 
            var expressionNodes = new IExpressionNode[node.Ifs.Length];
            //conditions
            // if ( {here} ) ...
            var conditionNodes = new IExpressionNode[node.Ifs.Length];

            for (int i = 0; i < expressionNodes.Length; i++)
            {
                conditionNodes[i] = ReadNode(node.Ifs[i].Condition);
                var exprNode = ReadNode(node.Ifs[i].Expression);
                expressionNodes[i] = CastExpressionNode.GetConvertedOrOriginOrThrow(exprNode, node.OutputType);
            }

            var elseNode = CastExpressionNode.GetConvertedOrOriginOrThrow(ReadNode(node.ElseExpr), node.OutputType);

            return new IfElseExpressionNode(
                ifExpressionNodes: expressionNodes,
                conditionNodes:    conditionNodes,
                elseNode:          elseNode,
                interval:          node.Interval, 
                type:              node.OutputType);
        }

        public IExpressionNode Visit(ConstantSyntaxNode node)
        {
            var type = _typesConverter.Convert(_typeInferenceResults.SyntaxNodeTypes[node.OrderNumber]);
            //All integer values are encoded by ulong (if it is ulong) or long otherwise
            if(node.Value is long l)
                return ValueExpressionNode.CreateConcrete(type, l, node.Interval);
            else if (node.Value is ulong u)
                return ValueExpressionNode.CreateConcrete(type, u, node.Interval);
            else //other types have their own clr-types
                return new ValueExpressionNode(node.Value, type, node.Interval);
        }

        public IExpressionNode Visit(GenericIntSyntaxNode node)
        {
            var type = _typesConverter.Convert(_typeInferenceResults.SyntaxNodeTypes[node.OrderNumber]);

            if (node.Value is long l) 
                return ValueExpressionNode.CreateConcrete(type, l, node.Interval);
            else if (node.Value is ulong u)
                return ValueExpressionNode.CreateConcrete(type, u, node.Interval);
            else if (node.Value is double d)
                return new ValueExpressionNode(node.Value, type, node.Interval);                
            else
                throw new ImpossibleException($"Generic syntax node has wrong value type: {node.Value.GetType().Name}");
            
        }
        
        public IExpressionNode Visit(VariableSyntaxNode node)
            => GetOrAddVariableNode(node);

        #region not an expression
        public IExpressionNode Visit(EquationSyntaxNode node) 
            => ThrowNotAnExpression(node);

        public IExpressionNode Visit(IfCaseSyntaxNode node) 
            => ThrowNotAnExpression(node);
        
        public IExpressionNode Visit(ListOfExpressionsSyntaxNode node)
            => ThrowNotAnExpression(node);

        public IExpressionNode Visit(SyntaxTree node)
            => ThrowNotAnExpression(node);

        public IExpressionNode Visit(TypedVarDefSyntaxNode node)
            => ThrowNotAnExpression(node);

        public IExpressionNode Visit(UserFunctionDefenitionSyntaxNode node)
            => ThrowNotAnExpression(node);

        public IExpressionNode Visit(VarDefenitionSyntaxNode node)
            => ThrowNotAnExpression(node);

        #endregion

        private IExpressionNode CreateFunctionCall(IFunCallSyntaxNode node, FunctionBase function)
        {
            var children = new List<IExpressionNode>();
            foreach (var argLexNode in node.Args)
            {
                var argNode = ReadNode(argLexNode);
                children.Add(argNode);
            }
            var converted = function.CreateWithConvertionOrThrow(children, node.Interval);
            if (converted.Type != node.OutputType)
            {
                var converter = VarTypeConverter.GetConverterOrThrow(converted.Type, node.OutputType, node.Interval);
                return new CastExpressionNode(converted, node.OutputType, converter, node.Interval);
            }
            else
                return converted;
        }


        private IExpressionNode ThrowNotAnExpression(ISyntaxNode node)
            => throw ErrorFactory.NotAnExpression(node);

        private IExpressionNode ReadNode(ISyntaxNode node) 
            => node.Accept(this);
        
        private IExpressionNode GetOrAddVariableNode(VariableSyntaxNode varNode)
        {
           
            var funVariable = _typeInferenceResults.GetFunctionalVariableOrNull(varNode.OrderNumber);
            if (funVariable != null)
            {
                if (funVariable is GenericFunctionBase genericFunction)
                {
                    var genericTypes = _typeInferenceResults.GetGenericCallArguments(varNode.OrderNumber);
                    if (genericTypes == null)
                        throw new ImpossibleException($"MJ79. Generic function is missed at {varNode.OrderNumber}:  {varNode.Id}`{genericFunction.Name} ");

                    var genericArgs = new VarType[genericTypes.Length];
                    for (int i = 0; i < genericTypes.Length; i++)
                        genericArgs[i] = _typesConverter.Convert(genericTypes[i]);

                    var function = genericFunction.CreateConcrete(genericArgs); 
                    return new FunVariableExpressionNode(function, varNode.Interval);

                }
                else if (funVariable is FunctionBase concrete)
                    return new FunVariableExpressionNode(concrete, varNode.Interval);
            }
            
            var lower = varNode.Id;
            if (_variables.GetSourceOrNull(lower) == null)
            {
                //if it is not a variable it might be a functional-variable
                var funVars = _functions.GetOverloads(lower);
                if (funVars.Count > 0)
                {
                    var specification = varNode.OutputType.FunTypeSpecification;
                    if (specification == null)
                        throw ErrorFactory.FunctionNameAndVariableNameConflict(varNode);

                    if (funVars.Count > 1)
                    {
                        //several function with such name are appliable
                        var result = funVars.Where(f =>
                                f.ReturnType == specification.Output && f.ArgTypes.SequenceEqual(specification.Inputs))
                            .ToList();
                        if (result.Count == 0)
                            throw ErrorFactory.FunctionIsNotExists(varNode);
                        if (result.Count > 1)
                            throw ErrorFactory.AmbiguousFunctionChoise(varNode);
                        if (result[0] is FunctionBase ff)
                            return new FunVariableExpressionNode(ff, varNode.Interval);
                        else
                            throw new NotImplementedException("GenericsAreNotSupp");
                    }

                    if (funVars.Count == 1)
                    {
                        if (funVars[0] is FunctionBase ff)
                            return new FunVariableExpressionNode(ff, varNode.Interval);
                    }
                }
            }
            var node = _variables.CreateVarNode(varNode.Id, varNode.Interval, varNode.OutputType);
            if(node.Source.Name!= varNode.Id)
                throw ErrorFactory.InputNameWithDifferentCase(varNode.Id, node.Source.Name, varNode.Interval);
            return node;
        }
    }
}