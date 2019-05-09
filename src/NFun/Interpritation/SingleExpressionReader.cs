using System;
using System.Collections.Generic;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.ParseErrors;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation
{
    class SingleExpressionReader
    {
        private readonly FunctionsDictionary _functions;
        private readonly VariableDictionary _variables;
        public SingleExpressionReader(
            FunctionsDictionary functions, 
            VariableDictionary variables)
        {
            _functions = functions;
            _variables = variables;
        }

        public  IExpressionNode ReadNode(ISyntaxNode node)
        {
            //todo Visitor
            if (node is VariableSyntaxNode vNode)
                return GetOrAddVariableNode(vNode);
            if(node is FunCallSyntaxNode fNode)
                return GetFunNode(fNode);
            if(node is IfThenElseSyntaxNode ifNode)
                return GetIfThanElseNode(ifNode);
            if(node is NumberSyntaxNode numNode)
                return GetValueNode(numNode);
            if(node is TextSyntaxNode textNode)
                return GetTextValueNode(textNode);
            if(node is ArraySyntaxNode arrNode)
                return GetArrayNode(arrNode);
            if(node is ProcArrayInit arrInitNode)
                return GetProcedureArrayNode(arrInitNode);
            if(node is AnonymCallSyntaxNode anonNode)
                return GetAnonymFun(anonNode);
            throw ErrorFactory.NotAnExpression(node);
        }

        private IExpressionNode GetAnonymFun(AnonymCallSyntaxNode node)
        {
            if (node.Defenition==null)
                throw ErrorFactory.AnonymousFunDefenitionIsMissing(node);

            if(node.Body==null)
                throw ErrorFactory.AnonymousFunBodyIsMissing(node);
            
            
            //Anonym fun arguments list

            ISyntaxNode[] argumentLexNodes;
            if (node.Defenition is ListOfExpressionsSyntaxNode list)
                //it can be comlex: (x1,x2,x3)=>...
                argumentLexNodes = list.Expressions;
            else
                //or primitive: x1 => ...
                argumentLexNodes = new[] {node.Defenition};
            
            //Prepare local variable scope
            //Capture all outerscope variables
            var localVariables = new VariableDictionary(_variables.GetAllSources());
            
            var arguments = new List<VariableSource>();
            foreach (var arg in argumentLexNodes)
            {
                //Convert argument node
                var varNode = ConvertToArgumentNodeOrThrow(arg);
                var source = new VariableSource(varNode.Name, varNode.Type);
                //collect argument
                arguments.Add(source);
                //add argument to local scope
                if (!localVariables.TryAdd(source))
                {   //Check for duplicated arg-names

                    //If outer-scope contains the conflict variable name
                    if (_variables.GetSourceOrNull(varNode.Name) != null)
                        throw ErrorFactory.AnonymousFunctionArgumentConflictsWithOuterScope(varNode, node.Defenition);
                    else //else it is duplicated arg name
                        throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, node.Defenition);
                }
            }

            var originVariables = localVariables.GetAllSources().Select(s=>s.Name).ToArray();
            var scope = new SingleExpressionReader(_functions, localVariables);
            var expr = scope.ReadNode(node.Body);

            //New variables are new closured
            var closured =  localVariables.GetAllUsages()
                .Where(s => !originVariables.Contains(s.Source.Name))
                .ToList();

            //Add closured vars to outer-scope dictionary
            foreach (var newVar in closured)
                _variables.TryAdd(newVar); //add full usage info to allow analyze outer errors
            
            var fun = new UserFunction("anonymous", arguments.ToArray(), expr);
            return new FunVariableExpressionNode(fun, node.Interval);
        }
        
        private FunArgumentExpressionNode ConvertToArgumentNodeOrThrow(ISyntaxNode node)
        {
            if(node is VariableSyntaxNode varNode)
                return new FunArgumentExpressionNode(varNode.Value, VarType.Real, node.Interval);
            if(node is TypedVarDefSyntaxNode typeVarNode)
                return new FunArgumentExpressionNode(typeVarNode.Name, typeVarNode.VarType, node.Interval);
            
            throw ErrorFactory.InvalidArgTypeDefenition(node);
        }
        
        private IExpressionNode GetOrAddVariableNode(VariableSyntaxNode varNode)
        {
            var lower = varNode.Value;
            if (_variables.GetSourceOrNull(lower) == null)
            {
                var funVars = _functions.Get(lower);
                if (funVars.Count > 1)
                    throw ErrorFactory.AmbiguousFunctionChoise(funVars, varNode);
                if (funVars.Count == 1)
                    return new FunVariableExpressionNode(funVars[0], varNode.Interval);
            }
            var node = _variables.CreateVarNode(varNode.Value, varNode.Interval);
            if(node.Source.Name!= varNode.Value)
                throw ErrorFactory.InputNameWithDifferentCase(varNode.Value, node.Source.Name, varNode.Interval);
            return node;
        }
        
        private IExpressionNode GetProcedureArrayNode(ProcArrayInit node)
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

            
            return new RangeWithStepIntFunction().CreateWithConvertionOrThrow(new[] {start, end, step},node.Interval);
        }
        private IExpressionNode GetArrayNode(ArraySyntaxNode node)
        {
            var nodes = node.Expressions.Select(ReadNode).ToArray();
            return new ArrayExpressionNode(nodes,node.Interval);
        }
        private IExpressionNode GetIfThanElseNode(IfThenElseSyntaxNode node)
        {
            var ifNodes = new List<IfCaseExpressionNode>();
            foreach (var ifNode in node.Ifs)
            {
                var condition = ReadNode(ifNode.Condition);
                var expr = ReadNode(ifNode.Expr);
                ifNodes.Add(new IfCaseExpressionNode(condition, expr,node.Interval));
            }

            var elseNode = ReadNode(node.ElseExpr);
            return new IfThanElseExpressionNode(ifNodes.ToArray(), elseNode,elseNode.Interval);
        }

        private static IExpressionNode GetTextValueNode(TextSyntaxNode node) 
            => new ValueExpressionNode(node.Value, node.Interval);

        private static IExpressionNode GetValueNode(NumberSyntaxNode node)
        {
            var val = node.Value;
            try
            {
                if (val.Length > 2) {
                    if (val == "true")
                        return new ValueExpressionNode(true, node.Interval);
                    if (val == "false")
                        return new ValueExpressionNode(false, node.Interval);
                }
                var number = TokenHelper.ToNumber(val);
                if(number is int inum)
                    return new ValueExpressionNode(inum, node.Interval);
                else 
                    return new ValueExpressionNode((double)number, node.Interval);
            }
            catch (FormatException)
            {
                throw ErrorFactory.CannotParseNumber(node);
            }
        }

        private IExpressionNode GetFunNode(FunCallSyntaxNode node)
        {
            var id = node.Value;//.ToLower();
            
            var children= new List<IExpressionNode>();
            var childrenTypes = new List<VarType>();
            foreach (var argLexNode in node.Args)
            {
                var argNode = ReadNode(argLexNode);
                children.Add(argNode);
                childrenTypes.Add(argNode.Type);
            }

            var function = _functions.GetOrNull(id, childrenTypes.ToArray());
            if (function == null)
                throw ErrorFactory.FunctionNotFound( node.Value, node.Interval, children, _functions);
            return function.CreateWithConvertionOrThrow(children, node.Interval);
        }
    }
}