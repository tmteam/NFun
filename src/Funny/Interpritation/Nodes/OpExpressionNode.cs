using System;
using System.Collections.Generic;
using Funny.Interpritation.Nodes;
using Funny.Runtime;

namespace Funny.Interpritation
{
    public  class OpExpressionNode : IExpressionNode
    {
        private readonly IExpressionNode _a;
        private readonly IExpressionNode _b;
        private readonly Func<double, double, double> _op;
        public OpExpressionNode(
            IExpressionNode a, 
            IExpressionNode b, 
            Func<double,double,double> op)
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
        public VarType Type => VarType.NumberType;

        public object Calc() => _op((double)_a.Calc(), (double)_b.Calc());
    }
    public  class OpExpressionNodeOfT<Tleft,TRight, TOut> : IExpressionNode
    {
        private readonly IExpressionNode _a;
        private readonly IExpressionNode _b;
        private readonly Func<Tleft,TRight,TOut> _op;
        public OpExpressionNodeOfT(
            IExpressionNode a, 
            IExpressionNode b, 
            
            Func<Tleft,TRight,TOut> op)
        {
            if (typeof(TOut) == typeof(bool))
                Type = VarType.BoolType;
            else if (typeof(TOut) == typeof(Int32))
                Type = VarType.IntType;
            else if (typeof(TOut) == typeof(double))
                Type = VarType.NumberType;
            else
                throw new ArgumentException();
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
        public VarType Type { get; }

        public object Calc()
        {
            try
            {
                return _op((Tleft) _a.Calc(), (TRight) _b.Calc());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}