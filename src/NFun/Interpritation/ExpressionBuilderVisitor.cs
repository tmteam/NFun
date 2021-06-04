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

        private static IExpressionNode BuildExpression(
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
            var outputTypeFunDefinition = arrowAnonymFunNode.OutputType.FunTypeSpecification;
            if(outputTypeFunDefinition==null)
                throw new ImpossibleException("Fun definition expected");
            string[] argNames = null;
            if (outputTypeFunDefinition.Inputs.Length == 1)
                argNames = new[] {"it"};
            else
            {
                argNames = new string[outputTypeFunDefinition.Inputs.Length];
                for (int i = 0; i < outputTypeFunDefinition.Inputs.Length; i++)
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
                var type = outputTypeFunDefinition.Inputs[i];
                var source = VariableSource.CreateWithoutStrictTypeLabel(arg, type, false);
                //collect argument
                arguments[i] = source;
                //add argument to local scope
                //if argument with it* name already exist - replace it
                localVariables.AddOrReplace(source);
            }

            var body = arrowAnonymFunNode.Body;
            return BuildAnonymousFunction(arrowAnonymFunNode.Interval, body, localVariables, arguments);
        }

        public IExpressionNode Visit(StructFieldAccessSyntaxNode node)
        {
            var structNode = ReadNode(node.Source);
            //Funtic allows default values for not specified types 
            // so call:
            //  y = the{}.missingField
            // is allowed, but it semantically incorrect
            
            if (!structNode.Type.StructTypeSpecification.ContainsKey(node.FieldName))
                throw FunParseException.ErrorStubToDo($"Access to non exist field {node.FieldName}");
            return new StructFieldAccessExpressionNode(node.FieldName, structNode, node.Interval);
        }

        public IExpressionNode Visit(StructInitSyntaxNode node)
        {
            var types = new Dictionary<string,VarType>(node.Fields.Count);
            var names = new string[node.Fields.Count];
            var nodes = new IExpressionNode[node.Fields.Count];
            
            for (int i = 0; i < node.Fields.Count; i++)
            {
                var field = node.Fields[i];
                nodes[i] = ReadNode(field.Node);
                names[i] = field.Name;
                types.Add(field.Name,field.Node.OutputType);
            }

            foreach (var field in node.OutputType.StructTypeSpecification)
            {
                if (!types.ContainsKey(field.Key))
                    throw FunParseException.ErrorStubToDo($"Field {field.Key} is missed in struct");
            }
            return new StructInitExpressionNode(names,nodes,node.Interval,VarType.StructOf(types));
        }

        public IExpressionNode Visit(ArrowAnonymFunctionSyntaxNode arrowAnonymFunNode)
        {
            if (arrowAnonymFunNode.Definition==null)
                throw ErrorFactory.AnonymousFunDefinitionIsMissing(arrowAnonymFunNode);

            if(arrowAnonymFunNode.Body==null)
                throw ErrorFactory.AnonymousFunBodyIsMissing(arrowAnonymFunNode);
            
            //Anonym fun arguments list
            var argumentLexNodes = arrowAnonymFunNode.ArgumentsDefinition;
            
            //Prepare local variable scope
            //Capture all outerscope variables
            var localVariables = new VariableDictionary(_variables.GetAllSources());
            
            var arguments = new VariableSource[argumentLexNodes.Length];
            var argIndex = 0;
            foreach (var arg in argumentLexNodes)
            {
                //Convert argument node
                var varNode = FunArgumentExpressionNode.CreateWith(arg);
                var source = VariableSource.CreateWithStrictTypeLabel(varNode.Name, varNode.Type, arg.Interval, isOutput:false);
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
                        throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, arrowAnonymFunNode.Definition);
                }
            }
            var body = arrowAnonymFunNode.Body;
            return BuildAnonymousFunction(arrowAnonymFunNode.Interval, body, localVariables, arguments);
        }

        public IExpressionNode Visit(ArraySyntaxNode node)
        {
            var elements = new IExpressionNode[node.Expressions.Count];
            var expectedElementType = node.OutputType.ArrayTypeSpecification.VarType;
            for (int i = 0; i < node.Expressions.Count; i++)
            {
                var elementNode = ReadNode(node.Expressions[i]);
                elements[i] = CastExpressionNode.GetConvertedOrOriginOrThrow(elementNode, expectedElementType);
            }

            return new ArrayExpressionNode(elements,node.Interval, node.OutputType);
        }

        public IExpressionNode Visit(FunCallSyntaxNode node)
        {
            var id = node.Id;
            
            var someFunc = node.FunctionSignature ?? _functions.GetOrNull(id, node.Args.Length);
            
            if (someFunc is null)
            {
                //todo move to variable syntax node
                //hi order function
                 var functionalVariableSource = _variables.GetSourceOrNull(id);
                 if (functionalVariableSource?.Type.FunTypeSpecification == null)
                     throw ErrorFactory.FunctionOverloadNotFound(node, _functions);
                 return CreateFunctionCall(node, ConcreteHiOrderFunction.Create(functionalVariableSource));
            }

            if (someFunc is IConcreteFunction f) //concrete function
                return CreateFunctionCall(node, f);

            if (someFunc is IGenericFunction genericFunction) //generic function
            {
                VarType[] genericArgs;
                // Generic function type arguments usually stored in tic results
                var genericTypes = _typeInferenceResults.GetGenericCallArguments(node.OrderNumber);
                if (genericTypes == null)
                {
                    // Generic call arguments are unknown  in case of generic recursion function . 
                    // Take them from type inference results
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
            var type = _typesConverter.Convert(_typeInferenceResults.GetSyntaxNodeTypeOrNull(node.OrderNumber));
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
            var type = _typesConverter.Convert(_typeInferenceResults.GetSyntaxNodeTypeOrNull(node.OrderNumber));

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

            var funVariable = _typeInferenceResults.GetFunctionalVariableOrNull(node.OrderNumber);
            if (funVariable != null)
            {
                if (funVariable is IGenericFunction genericFunction)
                {
                    var genericTypes = _typeInferenceResults.GetGenericCallArguments(node.OrderNumber);
                    if (genericTypes == null)
                        throw new ImpossibleException($"MJ79. Generic function is missed at {node.OrderNumber}:  {node.Id}`{genericFunction.Name} ");

                    var genericArgs = new VarType[genericTypes.Length];
                    for (int i = 0; i < genericTypes.Length; i++)
                        genericArgs[i] = _typesConverter.Convert(genericTypes[i]);

                    var function = genericFunction.CreateConcrete(genericArgs); 
                    return new FunVariableExpressionNode(function, node.Interval);

                }
                else if (funVariable is IConcreteFunction concrete)
                    return new FunVariableExpressionNode(concrete, node.Interval);
            }
            
            var lower = node.Id;
            if (_variables.GetSourceOrNull(lower) == null)
            {
                //if it is not a variable it might be a functional-variable
                var funVars = _functions.GetOverloads(lower);
                if (funVars.Count > 0)
                {
                    var specification = node.OutputType.FunTypeSpecification;
                    if (specification == null)
                        throw ErrorFactory.FunctionNameAndVariableNameConflict(node);

                    if (funVars.Count > 1)
                    {
                        //several function with such name are appliable
                        var result = funVars.Where(f =>
                                f.ReturnType == specification.Output && f.ArgTypes.SequenceEqual(specification.Inputs))
                            .ToList();
                        if (result.Count == 0)
                            throw ErrorFactory.FunctionIsNotExists(node);
                        if (result.Count > 1)
                            throw ErrorFactory.AmbiguousFunctionChoise(node);
                        if (result[0] is IConcreteFunction ff)
                            return new FunVariableExpressionNode(ff, node.Interval);
                        else
                            throw new NotImplementedException("GenericsAreNotSupp");
                    }

                    if (funVars.Count == 1)
                    {
                        if (funVars[0] is IConcreteFunction ff)
                            return new FunVariableExpressionNode(ff, node.Interval);
                    }
                }
            }
            var node1 = _variables.CreateVarNode(node.Id, node.Interval, node.OutputType);
            if(node1.Source.Name!= node.Id)
                throw ErrorFactory.InputNameWithDifferentCase(node.Id, node1.Source.Name, node.Interval);
            return node1;
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

        public IExpressionNode Visit(UserFunctionDefinitionSyntaxNode node)
            => ThrowNotAnExpression(node);

        public IExpressionNode Visit(VarDefinitionSyntaxNode node)
            => ThrowNotAnExpression(node);

        #endregion

        private IExpressionNode BuildAnonymousFunction(Interval interval, ISyntaxNode body,
            VariableDictionary localVariables, VariableSource[] arguments)
        {
            var sources = localVariables.GetAllSources().ToArray();
            var originVariables = new string[sources.Length];
            for (int i = 0; i < originVariables.Length; i++) originVariables[i] = sources[i].Name;

            var expr = BuildExpression(body, _functions, localVariables, _typeInferenceResults, _typesConverter);

            //New variables are new closured
            var closured = localVariables.GetAllUsages()
                .Where(s => !originVariables.Contains(s.Source.Name))
                .ToList();
            
            if(closured.Any(c => Helper.DoesItLooksLikeSuperAnonymousVariable(c.Source.Name)))
                throw FunParseException.ErrorStubToDo("Unexpected it* variable");
            
            //Add closured vars to outer-scope dictionary
            foreach (var newVar in closured)
                _variables.TryAdd(newVar); //add full usage info to allow analyze outer errors

            var fun = ConcreteUserFunction.Create(
                isRecursive: false,
                name: "anonymous",
                variables: arguments,
                isReturnTypeStrictlyTyped: false,
                expression: expr);
            return new FunVariableExpressionNode(fun, interval);
        }
        
        private IExpressionNode CreateFunctionCall(IFunCallSyntaxNode node, IConcreteFunction function)
        {
            var children = node.Args.SelectToArray(ReadNode);
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
    }
}