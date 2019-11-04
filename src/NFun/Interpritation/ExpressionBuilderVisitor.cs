using System;
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
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation
{
    public sealed class ExpressionBuilderVisitor: ISyntaxNodeVisitor<IExpressionNode> {
        
        private readonly FunctionsDictionary _functions;
        private readonly VariableDictionary _variables;

        public static IExpressionNode BuildExpression(
            ISyntaxNode node, 
            FunctionsDictionary functions,
            VariableDictionary variables) =>
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

        private ExpressionBuilderVisitor(FunctionsDictionary functions, VariableDictionary variables)
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
                        throw ErrorFactory.AnonymousFunctionArgumentConflictsWithOuterScope(varNode, anonymFunNode.Defenition);
                    else //else it is duplicated arg name
                        throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, anonymFunNode.Defenition);
                }
            }

            var originVariables = localVariables.GetAllSources().Select(s=>s.Name).ToArray();
            var expr = BuildExpression(anonymFunNode.Body, _functions, localVariables);
            
            //New variables are new closured
            var closured =  localVariables.GetAllUsages()
                .Where(s => !originVariables.Contains(s.Source.Name))
                .ToList();

            //Add closured vars to outer-scope dictionary
            foreach (var newVar in closured)
                _variables.TryAdd(newVar); //add full usage info to allow analyze outer errors
            
            var fun = new UserFunction(
                name:       "anonymous", 
                variables:  arguments.ToArray(),
                isReturnTypeStrictlyTyped: anonymFunNode.Defenition.OutputType!= VarType.Empty, 
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
            => new ValueExpressionNode(node.Value, node.OutputType, node.Interval);

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
                var funVars = _functions.GetNonGeneric(lower);
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