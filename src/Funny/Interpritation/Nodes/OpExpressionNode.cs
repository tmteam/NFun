using System;
using System.Collections.Generic;
using Funny.Types;

namespace Funny.Interpritation.Nodes
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
                Type = VarType.BoolType;
            else if (typeof(TOut) == typeof(Int32))
                Type = VarType.IntType;
            else if (typeof(TOut) == typeof(double))
                Type = VarType.RealType;
            else if (typeof(TOut) == typeof(string))
                Type = VarType.TextType;
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