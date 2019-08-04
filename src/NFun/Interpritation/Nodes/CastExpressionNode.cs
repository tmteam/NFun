using System;
using System.Linq;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class CastExpressionNode: IExpressionNode
    {
        private readonly IExpressionNode _origin;
        private Func<object, object> _converter;
        public CastExpressionNode(IExpressionNode origin, VarType targetType, Func<object, object> converter, Interval interval)
        {
            _origin = origin;
            Type = targetType;
            _converter = converter;
            Interval = interval;
        }
        public Interval Interval { get; }
        public VarType Type { get; }
        public object Calc()
        {
            var res = _origin.Calc();
            if (res is IFunConvertable c && Type!= VarType.Anything)
                return _converter(c.GetValue());
            return _converter(res);
        }
    }
}