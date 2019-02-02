using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Parsing;
using Funny.Tokenization;

namespace Funny.Interpritation
{
    public class ExpressionReader
    {
        private readonly IEnumerable<LexEquatation> _lexEquatations;
        
        private readonly Dictionary<string, VariableExpressionNode> _variables 
            = new Dictionary<string, VariableExpressionNode>();
        
        private Dictionary<string, Equatation> _equatations 
            = new Dictionary<string, Equatation>();
        
        public static Runtime.Runtime Interpritate(IEnumerable<LexEquatation> lexEquatations)
        {
            var ans = new ExpressionReader(lexEquatations);
            ans.Interpritate();
            //now we need to build map of dependencies
            var variables = ans._variables.Values;

            //some of the variables are input, and some are inputs reusing
           // GraphTools.SortTopology(variables.ToArray());
            
            var equatations = ans._equatations.Values.ToArray();

            return new Runtime.Runtime(equatations,  ans._variables);
        }
        private ExpressionReader(IEnumerable<LexEquatation> lexEquatations)
        {
            _lexEquatations = lexEquatations;
        }

        private void Interpritate()
        {
            foreach (var equatation in _lexEquatations)
            {
                var expression = InterpritateNode(equatation.Expression, equatation);
                _equatations.Add(equatation.Id.ToLower(), new Equatation
                {
                    Expression = expression,
                    Id = equatation.Id,
                });
            }
        }
        private IExpressionNode InterpritateNode(LexNode node, LexEquatation equatation)
        {
            if (node.Op.Is((TokType.Id)))
                return GetOrAddVariableNode(node.Op.Value, equatation);

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

            var leftExpr = InterpritateNode(left,equatation);
            var rightExpr = InterpritateNode(right,equatation);
            
            return new OpExpressionNode(leftExpr, rightExpr, op);
        }

        private IExpressionNode GetOrAddVariableNode(string varName, LexEquatation equatation)
        {
            var lower = varName.ToLower();
            VariableExpressionNode res;
            
            if (_variables.ContainsKey(lower))
                res= _variables[lower];
            else {
                res = new VariableExpressionNode(lower);
                _variables.Add(lower, res);            
            }
            res.AddEquatationName(equatation.Id);
            return res;
        }


        private void DFSRecursionChecker(Equatation[] equatations, VariableExpressionNode[] variables)
        {
            
        }
    }
}