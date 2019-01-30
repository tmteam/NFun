using System;
using System.Collections.Generic;
using System.Linq;

namespace Funny.Take2
{
    public class Equatation
    {
        public string Id;
        public IExpressionNode Expression;
    }
    public class ExpressionReader
    {
        private readonly IEnumerable<LexEquatation> _lexEquatations;
        
        private Dictionary<string, VariableExpressionNode> _variables 
            = new Dictionary<string, VariableExpressionNode>();
        private Dictionary<string, Equatation> _equatations 
            = new Dictionary<string, Equatation>();
        
        public static Runtime Interpritate(IEnumerable<LexEquatation> equatations)
        {
            var ans = new ExpressionReader(equatations);
            ans.Interpritate();
            return new Runtime(ans._equatations.Values.ToArray(),  ans._variables);
        }
        private ExpressionReader(IEnumerable<LexEquatation> lexEquatations)
        {
            _lexEquatations = lexEquatations;
        }

        private void Interpritate()
        {
            foreach (var equatation in _lexEquatations)
            {
                var expression = InterpritateNode(equatation.Expression);
                _equatations.Add(equatation.Id.ToLower(), new Equatation
                {
                    Expression = expression,
                    Id = equatation.Id,
                });
            }
        }
        private IExpressionNode InterpritateNode(LexNode node)
        {
            if (node.Op.Is((TokType.Id)))
                return GetOrAddVariableNode(node.Op.Value);

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

        private IExpressionNode GetOrAddVariableNode(string varName)
        {
            var lower = varName.ToLower();
            if (_variables.ContainsKey(lower))
                return _variables[lower];
            var res = new VariableExpressionNode(lower);
            _variables.Add(lower, res);
            return res;
        }
    }
}