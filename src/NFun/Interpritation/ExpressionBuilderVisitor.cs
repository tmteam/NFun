using System.Collections.Generic;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.Exceptions;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.Interpritation
{
    public sealed class ExpressionBuilderVisitor: ISyntaxNodeVisitor<IExpressionNode> {
        
        private readonly FunctionsDictionary _functions;
        private readonly IVariableDictionary _variables;

        public static IExpressionNode BuildExpression(
            ISyntaxNode node, 
            FunctionsDictionary functions,
            IVariableDictionary variables) =>
            node.Accept(new ExpressionBuilderVisitor(functions, variables));

        public static IExpressionNode BuildExpression(
            ISyntaxNode node, 
            FunctionsDictionary functions,
            VarType outputType,
            VariableDictionary variables)
        {
            var result =  node.Accept(new ExpressionBuilderVisitor(functions, variables));
            if (result.Type == outputType)
                return result;
            var converter = VarTypeConverter.GetConverterOrThrow(result.Type, outputType, node.Interval);
            return new CastExpressionNode(result, outputType, converter, node.Interval);
        }

        private ExpressionBuilderVisitor(FunctionsDictionary functions, IVariableDictionary variables)
        {
            _functions = functions;
            _variables = variables;
        }


        public IExpressionNode Visit(AnonymCallSyntaxNode anonymFunNode)
        {
            if (anonymFunNode.Defenition==null)
                throw ErrorFactory.AnonymousFunDefenitionIsMissing(anonymFunNode);

            if(anonymFunNode.Body==null)
                throw ErrorFactory.AnonymousFunBodyIsMissing(anonymFunNode);
            
            
            //Anonym fun arguments list
            var argumentLexNodes = anonymFunNode.ArgumentsDefenition;
            
             new VariableDictionary(_variables.GetAllSources());
            
            var arguments = new Dictionary<string, VariableSource>();
            foreach (var arg in argumentLexNodes)
            {
                //Convert argument node
                var argument = CallArgument.CreateWith(arg);
                
                //Check for duplicated arg-names
                if (arguments.ContainsKey(argument.Name))
                    throw ErrorFactory.AnonymousFunctionArgumentDuplicates(argument, anonymFunNode.Defenition);

                //If outer-scope contains the conflict variable name
                if (_variables.Contains(argument.Name))
                    throw ErrorFactory.AnonymousFunctionArgumentConflictsWithOuterScope(argument, anonymFunNode.Defenition);

                var source = VariableSource.CreateWithStrictTypeLabel(argument.Name, argument.Type, arg.Interval);
                arguments.Add(argument.Name, source);
            }

            //Prepare local variable scope
            //Captures all outerscope variables and put it to origin dictionary
            var localVariables = new LocalFunctionVariableDictionary(_variables, arguments);

            var originVariables = localVariables.GetAllSources().Select(s=>s.Name).ToArray();
            var expr = BuildExpression(anonymFunNode.Body, _functions, localVariables);
            
            var fun = new UserFunction(
                name:       Constants.AnonymousFunctionId, 
                variables:  arguments.Values.ToArray(),
                isReturnTypeStrictlyTyped: anonymFunNode.Defenition.OutputType!= VarType.Empty, 
                isGeneric: false,
                isRecursive: false,
                expression: expr );
            return new FunVariableExpressionNode(fun, anonymFunNode.Interval);
        }

        public IExpressionNode Visit(ArraySyntaxNode node)
        {
            var nodes = node.Expressions.Select(ReadNode).ToArray();
            return new ArrayExpressionNode(nodes,node.Interval, node.OutputType);
        }

        public IExpressionNode Visit(FunCallSyntaxNode node)
        {
            var id = node.Id;
            
            var children= new List<IExpressionNode>();
            var childrenTypes = new List<VarType>();
            foreach (var argLexNode in node.Args)
            {
                var argNode =  ReadNode(argLexNode);
                children.Add(argNode);
                childrenTypes.Add(argNode.Type);
            }
            var signature = node.SignatureOfOverload;
            FunctionBase function = null;
            if (signature != null)
            {
                //Signature was calculated by Ti algorithm.
                function = _functions.GetOrNullConcrete(
                    name:       id,
                    returnType: signature.ReturnType,
                    args:       signature.ArgTypes);
            }
            else
            {
                //todo
                //Ti algorithm had not calculate concrete overload
                function = _functions.GetOrNullWithOverloadSearch(
                    name:       id, 
                    returnType: node.OutputType,
                    args:       childrenTypes.ToArray());
            }

            if (function == null)
                throw new ImpossibleException("MJ78. Function overload was not found");
             
            var converted =  function.CreateWithConvertionOrThrow(children, node.Interval);
            if(converted.Type!= node.OutputType)
            {
                var converter = VarTypeConverter.GetConverterOrThrow(converted.Type, node.OutputType, node.Interval);
                return new CastExpressionNode(converted, node.OutputType, converter,node.Interval);
            }
            else
                return converted;
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
            => new ConstantExpressionNode(node.Value, node.OutputType, node.Interval);

        public IExpressionNode Visit(ProcArrayInit node)
        {
            var start = ReadNode(node.From);
            var end   = ReadNode(node.To);
            
            if (node.Step == null)
                return new RangeIntFunction().CreateWithConvertionOrThrow(new[] {start, end}, node.Interval);

            var step = ReadNode(node.Step);
            if(step.Type== VarType.Real)
                return new RangeWithStepRealFunction().CreateWithConvertionOrThrow(new[] {start, end, step},node.Interval);
            
            if (step.Type!= VarType.Int32)
                throw ErrorFactory.ArrayInitializerTypeMismatch(step.Type, node);
            
            return new RangeWithStepIntFunction().CreateWithConvertionOrThrow(new[] {start, end, step},node.Interval);        
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
        private IExpressionNode ThrowNotAnExpression(ISyntaxNode node)
            => throw ErrorFactory.NotAnExpression(node);

        private IExpressionNode ReadNode(ISyntaxNode node) 
            => node.Accept(this);
        private IExpressionNode GetOrAddVariableNode(VariableSyntaxNode varNode)
        {
            var lower = varNode.Id;
            if (_variables.GetSourceOrNull(lower) == null)
            {
                //if it is not a variable it might be a functional-variable
                var funVars = _functions.GetConcretes(lower);
                if (funVars.Count > 1)
                {
                    var specification = varNode.OutputType.FunTypeSpecification;
                    if (specification == null)
                        throw ErrorFactory.FunctionNameAndVariableNameConflict(varNode);

                    //several function with such name are appliable
                    var result = funVars.Where(f =>
                            f.ReturnType == specification.Output && f.ArgTypes.SequenceEqual(specification.Inputs))
                        .ToList();

                    if (result.Count > 1)
                        throw ErrorFactory.AmbiguousFunctionChoise(varNode);
                    return new FunVariableExpressionNode(result[0], varNode.Interval);
                }

                if (funVars.Count == 1)
                    return new FunVariableExpressionNode(funVars[0], varNode.Interval);
            }
            var node = _variables.CreateVarNode(varNode.Id, varNode.Interval, varNode.OutputType);
            if(node.Source.Name!= varNode.Id)
                throw ErrorFactory.InputNameWithDifferentCase(varNode.Id, node.Source.Name, varNode.Interval);
            return node;
        }
    }
}