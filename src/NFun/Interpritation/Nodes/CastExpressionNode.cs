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
        
        
        /*
        public static Func<object, object> GetConverterOrThrow(VarType from, VarType to, Interval interval)
        {
            if (from == to)
                return o => o;
            if (@from == VarType.Int32)
            {
                if (to == VarType.Real)  return o => Convert.ToDouble(o);
                if (to == VarType.Int64) return o => Convert.ToInt64(o);
            }

            if (to == VarType.Text)
                return o => o?.ToString() ?? "";
            if (to == VarType.Anything)
                return o => o;
            if (from.BaseType == BaseVarType.ArrayOf && to.BaseType== BaseVarType.ArrayOf)
            {
                if (to ==  VarType.ArrayOf(VarType.Anything))
                    return o => o;
                
                var elementConverter = VarTypeConverter.GetConverterOrThrow(
                    from.ArrayTypeSpecification.VarType, 
                    to.ArrayTypeSpecification.VarType, 
                    interval);
                return o => FunArray.By(((IFunArray) o).Select(elementConverter));
            }

            throw ErrorFactory.ImpossibleCast(from, to, interval);
        }
        */
    }
}