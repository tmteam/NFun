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
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Interpritation
{
    public sealed class ExpressionBuilderVisitor: ISyntaxNodeVisitor<IExpressionNode> {
        
        private readonly IFunctionDictionary _functions;
        private readonly VariableDictionary _variables;
        private readonly TypeInferenceResults _typeInferenceResults;
        private readonly TicTypesConverter _typesConverter;

        public static IExpressionNode BuildExpression(
            ISyntaxNode node,
            IFunctionDictionary functions,
            VariableDictionary variables, 
            TypeInferenceResults typeInferenceResults, 
            TicTypesConverter typesConverter) =>
            node.Accept(new ExpressionBuilderVisitor(functions, variables, typeInferenceResults, typesConverter));

        public static IExpressionNode BuildExpression(
            ISyntaxNode node,
            IFunctionDictionary functions,
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
            IFunctionDictionary functions, 
            VariableDictionary variables,
            TypeInferenceResults typeInferenceResults, 
            TicTypesConverter typesConverter)
        {
            _functions = functions;
            _variables = variables;
            _typeInferenceResults = typeInferenceResults;
            _typesConverter = typesConverter;
        }

        public IExpressionNode Visit(SuperAnonymFunctionSyntaxNode arrowAnonymFunNode)
        {
            var outputTypeFunDefenition = arrowAnonymFunNode.OutputType.FunTypeSpecification;
            if(outputTypeFunDefenition==null)
                throw new ImpossibleException("Fun defenition expected");
            string[] argNames = null;
            if (outputTypeFunDefenition.Inputs.Length == 1)
                argNames = new[] {"it"};
            else
            {
                argNames = new string[outputTypeFunDefenition.Inputs.Length];
                for (int i = 0; i < outputTypeFunDefenition.Inputs.Length; i++)
                {
                    argNames[i] = $"it{i + 1}";
                }
            }

            //Prepare local variable scope
            //Capture all outerscope variables
            var localVariables = new VariableDictionary(_variables.GetAllSources());
            
            var arguments = new VariableSource[argNames.Length];
            for (var i = 0; i < argNames.Length; i++)
            {
                var arg = argNames[i];
                var type = outputTypeFunDefenition.Inputs[i];
                var source = VariableSource.CreateWithoutStrictTypeLabel(arg, type);
                //collect argument
                arguments[i] = source;
                //add argument to local scope
                //if argument with it* name already exist - replace it
                localVariables.AddOrReplace(source);
            }

            var body = arrowAnonymFunNode.Body;
            return BuildAnonymousFunction(arrowAnonymFunNode.Interval, body, localVariables, arguments);
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
            
            var arguments = new VariableSource[argumentLexNodes.Length];
            var argIndex = 0;
            foreach (var arg in argumentLexNodes)
            {
                //Convert argument node
                var varNode = FunArgumentExpressionNode.CreateWith(arg);
                var source = VariableSource.CreateWithStrictTypeLabel(varNode.Name, varNode.Type, arg.Interval);
                //collect argument
                arguments[argIndex] = source;
                argIndex++;
                //add argument to local scope
                if (!localVariables.TryAdd(source))
                {   //Check for duplicated arg-names

                    //If outer-scope contains the conflict variable name
                    if (_variables.GetSourceOrNull(varNode.Name) != null)
                        throw ErrorFactory.AnonymousFunctionArgumentConflictsWithOuterScope(varNode.Name, arrowAnonymFunNode.Interval);
                    else //else it is duplicated arg name
                        throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, arrowAnonymFunNode.Defenition);
                }
            }
            var body = arrowAnonymFunNode.Body;
            return BuildAnonymousFunction(arrowAnonymFunNode.Interval, body, localVariables, arguments);
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
            if (node.NamedIdSyntaxNode.IdType != NamedIdNodeType.Variable)
                throw FunParseException.ErrorStubToDo("Only variables are allowed as argument of metafunctions");
            
            //registrate var node
            GetOrAddVariableNode(node.NamedIdSyntaxNode);
            var id = node.NamedIdSyntaxNode.Id;
            if (!_typeInferenceResults.HasVariable(id))
                throw FunParseException.ErrorStubToDo($"Variable {id} not exist in the scope");
            return new MetaInfoExpressionNode(_variables, id , node.Interval);
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
                return ConstantExpressionNode.CreateConcrete(type, l, node.Interval);
            else if (node.Value is ulong u)
                return ConstantExpressionNode.CreateConcrete(type, u, node.Interval);
            else //other types have their own clr-types
                return new ConstantExpressionNode(node.Value, type, node.Interval);
        }

        public IExpressionNode Visit(GenericIntSyntaxNode node)
        {
            var type = _typesConverter.Convert(_typeInferenceResults.SyntaxNodeTypes[node.OrderNumber]);

            if (node.Value is long l) 
                return ConstantExpressionNode.CreateConcrete(type, l, node.Interval);
            else if (node.Value is ulong u)
                return ConstantExpressionNode.CreateConcrete(type, u, node.Interval);
            else if (node.Value is double d)
                return new ConstantExpressionNode(node.Value, type, node.Interval);                
            else
                throw new ImpossibleException($"Generic syntax node has wrong value type: {node.Value.GetType().Name}");
            
        }
        
        public IExpressionNode Visit(NamedIdSyntaxNode node)
        {
            if (node.IdType == NamedIdNodeType.Constant)
            {
                var varVal = (VarVal)node.IdContent;
                return new ConstantExpressionNode(varVal.Value, varVal.Type, node.Interval);
            }
            return GetOrAddVariableNode(node);
        }

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

        private IExpressionNode BuildAnonymousFunction(Interval interval, ISyntaxNode body,
            VariableDictionary localVariables, VariableSource[] arguments)
        {
            var sources = localVariables.GetAllSources();
            var originVariables = new string[sources.Length];
            for (int i = 0; i < originVariables.Length; i++) originVariables[i] = sources[i].Name;

            var expr = BuildExpression(body, _functions, localVariables, _typeInferenceResults, _typesConverter);

            //New variables are new closured
            var closured = localVariables.GetAllUsages()
                .Where(s => !originVariables.Contains(s.Source.Name))
                .ToList();
            var itVar = closured.FirstOrDefault(c => c.Source.Name.StartsWith("it", StringComparison.OrdinalIgnoreCase));
            if (itVar != null) 
                throw FunParseException.ErrorStubToDo("Unexpected it* variable");
            //Add closured vars to outer-scope dictionary
            foreach (var newVar in closured)
                _variables.TryAdd(newVar); //add full usage info to allow analyze outer errors

            var fun = ConcreteUserFunction.Create(
                name: "anonymous",
                variables: arguments,
                isReturnTypeStrictlyTyped: false,
                expression: expr);
            return new FunVariableExpressionNode(fun, interval);
        }

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
        
        private IExpressionNode GetOrAddVariableNode(NamedIdSyntaxNode varNode)
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