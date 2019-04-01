using System;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
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
                Type = VarType.Bool;
            else if (typeof(TOut) == typeof(Int32))
                Type = VarType.Int;
            else if (typeof(TOut) == typeof(double))
                Type = VarType.Real;
            else if (typeof(TOut) == typeof(string))
                Type = VarType.Text;
            else
                throw new ArgumentException();
            _a = a;
            _b = b;
            _op = op;
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