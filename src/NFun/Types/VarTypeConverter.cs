using System;
using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tokenization;

namespace NFun.Types
{
    public static class VarTypeConverter
    {
        public static readonly Func<object, object> ToInt8   = (o => Convert.ToSByte(o));
        public static readonly Func<object, object> ToInt16  = (o => Convert.ToInt16(o));
        public static readonly Func<object, object> ToInt32  = (o => Convert.ToInt32(o));
        public static readonly Func<object, object> ToInt64  = (o => Convert.ToInt64(o));
        public static readonly Func<object, object> ToUInt8  = (o => Convert.ToByte(o));
        public static readonly Func<object, object> ToUInt16 = (o => Convert.ToUInt16(o));
        public static readonly Func<object, object> ToUInt32 = (o => Convert.ToUInt32(o));
        public static readonly Func<object, object> ToUInt64 = (o => Convert.ToUInt64(o));
        public static readonly Func<object, object> ToReal   = (o => Convert.ToDouble(o));
        public static readonly Func<object, object> ToText   = (o => o?.ToString() ?? "");
        public static readonly Func<object, object> ToAny    = (o => o);

        public static Func<object, object> GetConverterOrNull(VarType from, VarType to)
        {
            if (to.IsText)
                return ToText;
            if (to.BaseType == BaseVarType.Any)
                return ToAny;

            if (from.BaseType.IsNumeric())
            {
                switch (to.BaseType)
                {
                    case BaseVarType.UInt8:  return ToUInt8;
                    case BaseVarType.UInt16: return ToUInt16;
                    case BaseVarType.UInt32: return ToUInt32;
                    case BaseVarType.UInt64: return ToUInt64;
                    case BaseVarType.Int16:  return ToInt16;
                    case BaseVarType.Int32:  return ToInt32;
                    case BaseVarType.Int64:  return ToInt64;
                    case BaseVarType.Real:   return ToReal;
                    case BaseVarType.ArrayOf:
                    case BaseVarType.Fun:     
                    case BaseVarType.Generic:
                    case BaseVarType.Any: break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            if (from.BaseType == BaseVarType.ArrayOf && to.BaseType == BaseVarType.ArrayOf)
            {
                if (to == VarType.ArrayOf(VarType.Anything))
                    return o => o;

                var elementConverter = GetConverterOrNull(
                    from.ArrayTypeSpecification.VarType,
                    to.ArrayTypeSpecification.VarType);
                if (elementConverter == null)
                    return null;

                return o => ImmutableFunArray.By(((IFunArray)o).Select(elementConverter));
            }
            return null;
        }
        
        public static Func<object, object> GetConverterOrThrow(VarType from, VarType to, Interval interval)
        {
            var res = GetConverterOrNull(from, to);
            if(res==null)
                throw ErrorFactory.ImpossibleCast(from, to, interval);
            return res;
        }


        public static bool IsNumeric(this BaseVarType varType) 
            => varType >= BaseVarType.UInt8 && varType <= BaseVarType.Real;

        public static bool CanBeConverted(VarType from, VarType to)
        {
            var fromBase = from.BaseType;
            if (fromBase == BaseVarType.Empty)
                return false;
            if (to.IsText)
                return true;
            switch (to.BaseType)
            {
                case BaseVarType.Any:
                case BaseVarType.Char:
                    return true;
                case BaseVarType.Fun:
                case BaseVarType.ArrayOf when fromBase != BaseVarType.ArrayOf:
                    return false;
                case BaseVarType.ArrayOf:
                    return CanBeConverted(
                        @from: @from.ArrayTypeSpecification.VarType, 
                        to: to.ArrayTypeSpecification.VarType);
            }
            
            if (fromBase == to.BaseType)
                return true;

            switch (to.BaseType)
            {
                case BaseVarType.UInt16:
                    return fromBase == BaseVarType.UInt8;
                case BaseVarType.UInt32:
                    return fromBase == BaseVarType.UInt8 || fromBase == BaseVarType.UInt16;
                case BaseVarType.UInt64:
                    return fromBase == BaseVarType.UInt8
                           || fromBase == BaseVarType.UInt16
                           || fromBase == BaseVarType.UInt32;
                case BaseVarType.Int16:
                    return  fromBase == BaseVarType.UInt8;
                case BaseVarType.Int32:
                    return 
                           fromBase == BaseVarType.UInt8
                           || fromBase == BaseVarType.Int16
                           || fromBase == BaseVarType.UInt16;
                case BaseVarType.Int64:
                    return 
                              fromBase == BaseVarType.UInt8
                           || fromBase == BaseVarType.Int16
                           || fromBase == BaseVarType.UInt16
                           || fromBase == BaseVarType.Int32
                           || fromBase == BaseVarType.UInt32;

                case BaseVarType.Real:
                    return fromBase.IsNumeric();
                case BaseVarType.ArrayOf:
                    if (fromBase != BaseVarType.ArrayOf)
                        return false;
                    return CanBeConverted(
                        @from: from.ArrayTypeSpecification.VarType, 
                        to: to.ArrayTypeSpecification.VarType);
                case BaseVarType.Empty:
                    return false;
                case BaseVarType.Bool:
                    return false;
                case BaseVarType.UInt8:
                    return false;
                case BaseVarType.Any:
                    return true;
                case BaseVarType.Fun:
                    return false;    
                case BaseVarType.Generic:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}