using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation.Functions;
using Funny.Interpritation.Nodes;
using Funny.Parsing;
using Funny.Types;

namespace Funny.Interpritation
{
    class SingleExpressionReader
    {
        private readonly FunctionsDictionary _functions;
        private readonly Dictionary<string, VariableExpressionNode> _variables;
        public SingleExpressionReader(
            FunctionsDictionary functions, 
            Dictionary<string, VariableExpressionNode> variables)
        {
            _functions = functions;
            _variables = variables;
        }

        public  IExpressionNode ReadNode(LexNode node)
        {
            if(node.Is(LexNodeType.Var))
                return GetOrAddVariableNode(node);
            if(node.Is(LexNodeType.Fun))
                return GetFunNode(node);
            if(node.Is(LexNodeType.IfThanElse))
                return GetIfThanElseNode(node);
            if(node.Is(LexNodeType.Number))
                return GetValueNode(node);
            if(node.Is(LexNodeType.Text))
                return GetTextValueNode(node);
            if(node.Is(LexNodeType.ArrayInit))
                return GetArrayNode(node);
            if(node.Is(LexNodeType.ArrayUnite))
                return GetUniteArrayNode(node);
            if(node.Is(LexNodeType.AnonymFun))
                return GetAnonymFun(node);
            if(StandartOperations.IsDefaultOp(node.Type))
                return GetOpNode(node);
            
            throw new ParseException($"{node} is not an expression");
        }

        private IExpressionNode GetAnonymFun(LexNode node)
        {
            var defenition = node.Children.ElementAtOrDefault(0);
            if(defenition==null)
                throw new ParseException("Anonymous fun defenition is missing");

            var expression = node.Children.ElementAtOrDefault(1);
            if(expression== null)
                throw new ParseException("Anonymous fun body is missing");
            
            var variablesDictionary = new Dictionary<string, VariableExpressionNode>();
            
            if (defenition.Type == LexNodeType.ListOfExpressions)
            {
                foreach (var arg in defenition.Children)
                {
                    var varNode =  ConvertToVarNodeOrThrow(arg);
                    variablesDictionary.Add(varNode.Name, varNode);
                }
            }
            else
            {
                var varNode =  ConvertToVarNodeOrThrow(defenition);
                variablesDictionary.Add(varNode.Name, varNode);
            }

            var originVariables = variablesDictionary.Keys.ToArray();
            var scope = new SingleExpressionReader(_functions, variablesDictionary);
            var expr = scope.ReadNode(expression);

            ExpressionHelper.CheckForUnknownVariables(originVariables, variablesDictionary);
     
            var fun = new UserFunction("anonymous", variablesDictionary.Values.ToArray(), expr);
            return new FunVariableExpressionNode(fun);
        }

        private VariableExpressionNode ConvertToVarNodeOrThrow(LexNode defenition)
        {
            if (defenition.Type == LexNodeType.Var)
                return new VariableExpressionNode(defenition.Value, VarType.Real);
            else if(defenition.Type== LexNodeType.Argument)
                return new VariableExpressionNode(defenition.Value, (VarType)defenition.AdditionalContent);
            else
                throw new ParseException(defenition + " is  not valid fun arg");
        }
        
        private IExpressionNode GetUniteArrayNode(LexNode node)
        {
            var left = node.Children.ElementAtOrDefault(0);
            if (left == null)
                throw new ParseException("\"a\" node is missing");

            var right = node.Children.ElementAtOrDefault(1);
            if (right == null)
                throw new ParseException("\"b\" node is missing");
                
            var leftExpr = ReadNode(left);
            var rightExpr = ReadNode(right);
            return new UniteArraysExpressionNode(leftExpr,rightExpr);
        }

        private IExpressionNode GetOrAddVariableNode(LexNode varName)
        {
            var lower = varName.Value;
            var funVars = _functions.Get(lower);
            
            if(funVars.Count>1)
                throw new ParseException($"Ambiguous call of function with name: {lower}");
            if(funVars.Count==1)
                return new FunVariableExpressionNode(funVars[0]);   
            
            if (_variables.ContainsKey(lower))
                return _variables[lower];
            else {
                var res = new VariableExpressionNode(lower, VarType.Real);
                _variables.Add(lower, res);
                return res;
            }
        }
        
        private IExpressionNode GetOpNode(LexNode node)
        {
            var left = node.Children.ElementAtOrDefault(0);
            if (left == null)
                throw new ParseException("\"a\" node is missing");

            var right = node.Children.ElementAtOrDefault(1);
            if (right == null)
                throw new ParseException("\"b\" node is missing");

            var leftExpr = ReadNode(left);
            var rightExpr = ReadNode(right);
            
            return StandartOperations.GetOp(node.Type, leftExpr, rightExpr);            
        }
        
        private IExpressionNode GetArrayNode(LexNode node)
        {
            var nodes = node.Children.Select(ReadNode).ToArray();
            return new ArrayExpressionNode(nodes);
        }
        private IExpressionNode GetIfThanElseNode(LexNode node)
        {
            var ifNodes = new List<IfCaseExpressionNode>();
            foreach (var ifNode in node.Children.Where(c => c.Is(LexNodeType.IfThen)))
            {
                var condition = ReadNode(ifNode.Children.First());
                var expr = ReadNode(ifNode.Children.Last());
                ifNodes.Add(new IfCaseExpressionNode(condition, expr));
            }

            var elseNode = ReadNode(node.Children.Last());
            return new IfThanElseExpressionNode(ifNodes.ToArray(), elseNode);
        }

        private static IExpressionNode GetTextValueNode(LexNode node) 
            => new ValueExpressionNode(node.Value);

        private static IExpressionNode GetValueNode(LexNode node)
        {
            var val = node.Value;
            try
            {
                if (val.Length > 2)
                {
                    if(val == "true")
                        return new ValueExpressionNode(true);
                    if(val == "false")
                        return new ValueExpressionNode(false);
                    
                    val = val.Replace("_", null);

                    if (val[1] == 'b')
                        return new ValueExpressionNode(Convert.ToInt32(val.Substring(2), 2));
                    if (val[1] == 'x')
                        return new ValueExpressionNode(Convert.ToInt32(val, 16));
                }

                if (val.Contains('.'))
                {
                    if (val.EndsWith('.'))
                        throw new FormatException();
                    return new ValueExpressionNode(double.Parse(val));
                }

                return new ValueExpressionNode(int.Parse(val));
            }
            catch (FormatException e)
            {
                throw new ParseException("Cannot parse number \"" + node.Value + "\"");
            }
        }

        private IExpressionNode GetFunNode(LexNode node)
        {
            var id = node.Value.ToLower();
            
            var children= new List<IExpressionNode>();
            var childrenTypes = new List<VarType>();
            foreach (var argLexNode in node.Children)
            {
                var argNode = ReadNode(argLexNode);
                children.Add(argNode);
                childrenTypes.Add(argNode.Type);
            }

            var function = _functions.GetOrNull(id, childrenTypes.ToArray());
            if (function == null)
                throw new ParseException($"Function {id}({string.Join(", ", childrenTypes.ToArray())}) is not defined");
            var castedChildren = new List<IExpressionNode>();
            var i = 0;            
            foreach (var argNode in children)
            {
                var toType = function.ArgTypes[i];
                var fromType = argNode.Type;
                var castedNode = argNode;
                if (fromType != toType)
                {
                    var converter = CastExpressionNode.GetConverterOrThrow(fromType, toType);
                    castedNode = new CastExpressionNode(argNode, toType, converter);
                }
                castedChildren.Add(castedNode);
                i++;
            }

            return new FunExpressionNode(function, castedChildren.ToArray());
        }

    }
}