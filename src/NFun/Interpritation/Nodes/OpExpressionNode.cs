using System;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public  class OpExpressionNodeOfT<TLeft,TRight, TOut> : IExpressionNode
    {
        private readonly IExpressionNode _a;
        private readonly IExpressionNode _b;
        private readonly Func<TLeft,TRight,TOut> _op;
        public OpExpressionNodeOfT(
            IExpressionNode a, 
            IExpressionNode b, 
            
            Func<TLeft,TRight,TOut> op, Interval interval)
        {
            if (typeof(TOut) == typeof(bool))
                Type = VarType.Bool;
            else if (typeof(TOut) == typeof(Int32))
                Type = VarType.Int32;
            else if (typeof(TOut) == typeof(Int64))
                Type = VarType.Int64;
            else if (typeof(TOut) == typeof(double))
                Type = VarType.Real;
            else if (typeof(TOut) == typeof(string))
                Type = VarType.Text;
            else
                throw new ArgumentException();
            _a = a;
            _b = b;
            _op = op;
            Interval = interval;
        }

        public VarType Type { get; }
        public Interval Interval { get; }
        public object Calc()
        {
            try
            {
                return _op((TLeft) _a.Calc(), (TRight) _b.Calc());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}