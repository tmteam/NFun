using System;
using System.Collections.Generic;
using System.Linq;

namespace Funny.Take2
{
    public class ExpressionReader
    {
        private readonly LexNode _node;
        private IExpressionNode _expressionNode; 
        
        private Dictionary<string, VariableExpressionNode> _variables 
            = new Dictionary<string, VariableExpressionNode>();
        
        public static Runtime Interpritate(LexEquatation equatation)
        {
            var ans = new ExpressionReader(equatation.Expression);
            ans.Interpritate();
            return new Runtime(equatation.Id, ans._expressionNode, ans._variables);
        }
        private ExpressionReader(LexNode node)
        {
            _node = node;
        }

        private void Interpritate()
        {
            this._expressionNode = InterpritateNode(_node);
        }
        private IExpressionNode InterpritateNode(LexNode node)
        {
            if (node.Op.Is((TokType.Id)))
            {
                var varName = node.Op.Value.ToLower();
                if (_variables.ContainsKey(varName))
                    return _variables[varName];
                var res =  new VariableExpressionNode(node.Op.Value);
                _variables.Add(varName, res);
                return res;
            }

            if(node.Op.Is(TokType.Uint))
                return new ValueExpressionNode(int.Parse(node.Op.Value));
            Func<double, double, double> op = null;
            switch (node.Op.Type)
            {
                case TokType.Plus:
                    op = (a, b) => a + b;
                    break;
                case TokType.Minus:
                    op = (a, b) => a - b;
                     break;
                case TokType.Div:
                    op = (a, b) => a / b;
                    break;
                case TokType.Mult:
                    op = (a, b) => a * b;
                    break;
                case TokType.Pow:
                    op = Math.Pow; 
                    break;
                case TokType.Equal:
                    op = (a, b) => a == b? 1:0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var left =node.ChildrenNode.ElementAtOrDefault(0);
            if(left==null)
                throw new ParseException("a node is missing");

            var right = node.ChildrenNode.ElementAtOrDefault(1);
            if(right==null)
                throw new ParseException("b node is missing");

            var leftExpr = InterpritateNode(left);
            var rightExpr = InterpritateNode(right);
            
            return new OpExpressionNode(leftExpr, rightExpr, op);
        }
        
        
        
    }
    
}