using System;
using System.Collections.Generic;

namespace Funny.Interpritation
{
    public  class OpExpressionNode : IExpressionNode
    {
        private readonly IExpressionNode _a;
        private readonly IExpressionNode _b;
        private readonly Func<double, double, double> _op;
        public OpExpressionNode(IExpressionNode a, IExpressionNode b, Func<double,double,double> op)
        {
            _a = a;
            _b = b;
            _op = op;
        }
        public IEnumerable<IExpressionNode> Children {
            get { 
                yield return _a;
                yield return _b;
            }
        }
        public double Calc() => _op(_a.Calc(), _b.Calc());
    }
}