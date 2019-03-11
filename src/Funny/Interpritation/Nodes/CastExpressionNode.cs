using System;

namespace Funny.Interpritation.Nodes
{
    public class CastExpressionNode: IExpressionNode
    {
        private readonly IExpressionNode _origin;
        private Func<object, object> _converter;
        public CastExpressionNode(IExpressionNode origin, VarType targetType, Func<object, object> converter)
        {
            _origin = origin;
            Type = targetType;
            _converter = converter;
        }
        
        public VarType Type { get; }
        public object Calc() 
            => _converter(_origin.Calc());

        public static Func<object, object> GetConverterOrThrow(VarType from, VarType to)
        {
            if (from == to)
                return o => o;
            if (from == VarType.IntType && to == VarType.RealType)
                return o=>Convert.ToDouble(o);
            
            if (to == VarType.TextType)
                return o => o?.ToString() ?? "";
                
            throw  new ParseException($"Cast {from}->{to} is unavailable");
        }
    }
}