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
            if (from == VarType.Int && to == VarType.Real)
                return o=>Convert.ToDouble(o);
            if (to == VarType.Text)
                return o => o?.ToString() ?? "";
            if (to == VarType.Any)
                return o => o;
            if (from.BaseType == BaseVarType.ArrayOf && to.BaseType== BaseVarType.ArrayOf)
            {
                if (to ==  VarType.ArrayOf(VarType.Any))
                    return o => o;
                
                var elementConverter = GetConverterOrThrow(from.ArrayTypeSpecification.VarType, to.ArrayTypeSpecification.VarType);
                return o => ((IEnumerable) o).Cast<object>().Select(elementConverter);
            }    
            throw  new ParseException($"Cast {from}->{to} is unavailable");
        }
    }
}