using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation.Nodes;
using Funny.Parsing;

namespace Funny.Interpritation
{
    class SingleExpressionReader
    {
        private readonly Dictionary<string, FunctionBase> _predefinedfunctions;
        private readonly Dictionary<string, VariableExpressionNode> _variables;
        public SingleExpressionReader(
            Dictionary<string, FunctionBase> predefinedfunctions, 
            Dictionary<string, VariableExpressionNode> variables)
        {
            _predefinedfunctions = predefinedfunctions;
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
            if (StandartOperations.IsDefaultOp(node.Type))
                return GetOpNode(node);
            
            throw new ArgumentException($"Unknown lexnode type {node.Type}");
        }
        
        private IExpressionNode GetOrAddVariableNode(LexNode varName)
        {
            var lower = varName.Value;
            VariableExpressionNode res;
            
            if (_variables.ContainsKey(lower))
                res= _variables[lower];
            else {
                res = new VariableExpressionNode(lower);
                _variables.Add(lower, res);            
            }
            return res;
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
            if(!_predefinedfunctions.ContainsKey(id))
                throw new ParseException($"Function \"{id}\" is not defined");
            var fun = _predefinedfunctions[id];
            var children = node.Children.Select(c => ReadNode(c)).ToArray();
            if(children.Length!= fun.ArgsCount)
                throw new ParseException($"Args count of function \"{id}\" is wrong. Expected: {fun.ArgsCount} but was {children.Length}");
            return new FunExpressionNode(fun, children);
        }

    }
}