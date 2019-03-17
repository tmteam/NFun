using System;
using System.Collections;
using System.Linq;
using Funny.Types;

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
            if (to == VarType.AnyType)
                return o => o;
            if (from.BaseType == BaseVarType.ArrayOf && to.BaseType== BaseVarType.ArrayOf)
            {
                if (to ==  VarType.ArrayOf(VarType.AnyType))
                    return o => o;
                
                var elementConverter = GetConverterOrThrow(from.ArrayTypeSpecification.VarType, to.ArrayTypeSpecification.VarType);
                return o => ((IEnumerable) o).Cast<object>().Select(elementConverter);
            }    
            throw  new ParseException($"Cast {from}->{to} is unavailable");
        }
    }
}