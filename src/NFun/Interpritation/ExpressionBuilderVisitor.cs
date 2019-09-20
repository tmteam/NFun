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
    public class ExpressionBuilderVisitor: ISyntaxNodeVisitor<IExpressionNode> {
        private readonly FunctionsDictionary _functions;
        private readonly VariableDictionary _variables;

        public static IExpressionNode BuildExpression(
            ISyntaxNode node, 
            FunctionsDictionary functions,
            VariableDictionary variables) =>
            node.Visit(new ExpressionBuilderVisitor(functions, variables));

        public static IExpressionNode BuildExpression(
            ISyntaxNode node, 
            FunctionsDictionary functions,
            VarType outputType,
            VariableDictionary variables)
        {
            var result =  node.Visit(new ExpressionBuilderVisitor(functions, variables));
            if (result.Type == outputType)
                return result;
            var converter = VarTypeConverter.GetConverterOrThrow(result.Type, outputType, node.Interval);
            return new CastExpressionNode(result, outputType, converter, node.Interval);
        }

        public ExpressionBuilderVisitor(FunctionsDictionary functions, VariableDictionary variables)
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
                name: "anonymous", 
                variables: arguments.ToArray(),
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
            var id = node.Id;//.ToLower();
            
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
                    name: id,
                    returnType: signature.ReturnType,
                    args: signature.ArgTypes);
            }
            else
            {
                //todo
                //Ti algorithm had not calculate concrete overload
                function = _functions.GetOrNullWithOverloadSearch(
                    name: id, 
                    returnType: node.OutputType,
                    args: childrenTypes.ToArray());
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
            var ifNodes = new List<IfCaseExpressionNode>();
            foreach (var ifNode in node.Ifs)
            {
                var condition = ReadNode(ifNode.Condition);
                var expr = ReadNode(ifNode.Expression);
                ifNodes.Add(new IfCaseExpressionNode(condition, expr,node.Interval));
            }

            var elseNode = ReadNode(node.ElseExpr);
            return new IfThenElseExpressionNode(
                ifNodes.ToArray(), 
                elseNode,
                elseNode.Interval, 
                node.OutputType);        
        }

        
        public IExpressionNode Visit(ConstantSyntaxNode node) => new ValueExpressionNode(node.Value, node.OutputType, node.Interval);

        public IExpressionNode Visit(ProcArrayInit node)
        {
            var start = ReadNode(node.From);
            
            var end = ReadNode(node.To);
            
            if (node.Step == null)
                return new RangeIntFunction().CreateWithConvertionOrThrow(new[] {start, end}, node.Interval);

            var step = ReadNode(node.Step);
            if(step.Type== VarType.Real)
                return new RangeWithStepRealFunction().CreateWithConvertionOrThrow(new[] {start, end, step},node.Interval);
            
            if (step.Type!= VarType.Int32)
                throw ErrorFactory.ArrayInitializerTypeMismatch(step.Type, node);

            
            return new RangeWithStepIntFunction().CreateWithConvertionOrThrow(new[] {start, end, step},node.Interval);        }

       
      
        public IExpressionNode Visit(VariableSyntaxNode node)
            => GetOrAddVariableNode(node);

        #region not an expression
        public IExpressionNode Visit(EquationSyntaxNode node) => Bad(node);

        public IExpressionNode Visit(IfCaseSyntaxNode node) => Bad(node);
        
        public IExpressionNode Visit(ListOfExpressionsSyntaxNode node)=> Bad(node);

        public IExpressionNode Visit(SyntaxTree node)=> Bad(node);

        public IExpressionNode Visit(TypedVarDefSyntaxNode node)=> Bad(node);

        public IExpressionNode Visit(UserFunctionDefenitionSyntaxNode node)=> Bad(node);

        public IExpressionNode Visit(VarDefenitionSyntaxNode node)=> Bad(node);

        #endregion
        private IExpressionNode Bad(ISyntaxNode node)=> throw ErrorFactory.NotAnExpression(node);

        private IExpressionNode ReadNode(ISyntaxNode node) => node.Visit(this);
        private IExpressionNode GetOrAddVariableNode(VariableSyntaxNode varNode)
        {
            var lower = varNode.Id;
            if (_variables.GetSourceOrNull(lower) == null)
            {
                //if it is not a variable it might be a functional-variable
                var funVars = _functions.GetNonGeneric(lower);
                if (funVars.Count > 1)
                    throw ErrorFactory.AmbiguousFunctionChoise(funVars, varNode);
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