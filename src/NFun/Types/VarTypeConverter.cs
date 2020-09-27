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
        private static readonly bool[,] PrimitiveConvertMap;
        static VarTypeConverter()
        {
            PrimitiveConvertMap = new bool [15, 15];
            //every type can be converted to itself
            for (int i = 1; i < 15; i++)
                PrimitiveConvertMap[i, i] = true;
            //except arrays and funs
            PrimitiveConvertMap[(int)BaseVarType.ArrayOf,(int)BaseVarType.ArrayOf] = false;
            PrimitiveConvertMap[(int)BaseVarType.Fun,(int)BaseVarType.Fun] = false;

            //every type can be converted to any
            for (int i = 1; i < 15; i++)
                PrimitiveConvertMap[i, (int)BaseVarType.Any] = true;
            for (int i = (int) BaseVarType.UInt8; i < (int) BaseVarType.Real; i++)
            {
                //every number can be converted to real
                PrimitiveConvertMap[i, (int)BaseVarType.Real] = true;
                //every number can be converted from u8
                PrimitiveConvertMap[(int)BaseVarType.UInt8,i] = true;
            }
            
            PrimitiveConvertMap[(int)BaseVarType.UInt16,(int)BaseVarType.UInt32] = true;
            PrimitiveConvertMap[(int)BaseVarType.UInt16,(int)BaseVarType.UInt64] = true;
            PrimitiveConvertMap[(int)BaseVarType.UInt16,(int)BaseVarType.Int32] = true;
            PrimitiveConvertMap[(int)BaseVarType.UInt16,(int)BaseVarType.Int64] = true;

            PrimitiveConvertMap[(int)BaseVarType.UInt32,(int)BaseVarType.UInt64] = true;
            PrimitiveConvertMap[(int)BaseVarType.UInt32,(int)BaseVarType.Int64] = true;

            PrimitiveConvertMap[(int)BaseVarType.Int16,(int)BaseVarType.Int32] = true;
            PrimitiveConvertMap[(int)BaseVarType.Int16,(int)BaseVarType.Int64] = true;

            PrimitiveConvertMap[(int)BaseVarType.Int32,(int)BaseVarType.Int64] = true;

            /*
             * Empty = 0,
        Char =  1,
        Bool  = 2,
        UInt8 = 3,
        UInt16 = 4,
        UInt32 = 5,
        UInt64 = 6,
        Int16  =7,
        Int32 = 8,
        Int64 = 9,
        Real = 10,
        ArrayOf = 11,
        Fun = 12,
        Generic = 13,
        Any  = 14,
             * 
             */
        }
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
                
                
                return o =>
                {
                    var origin = (IFunArray) o;
                    var array = new object[origin.Count];
                    int index = 0;
                    foreach (var e in origin)
                    {
                        array[index] = elementConverter(e);
                        index++;
                    }
                    return new ImmutableFunArray(array, to.ArrayTypeSpecification.VarType);
                };
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
            if (to.IsText)
                return true;
            if (to.BaseType == BaseVarType.ArrayOf && from.BaseType== BaseVarType.ArrayOf)
            {
                return CanBeConverted(
                    @from: @from.ArrayTypeSpecification.VarType,
                    to: to.ArrayTypeSpecification.VarType);
            }
            //todo fun-convertion
            return PrimitiveConvertMap[(int)from.BaseType,(int)to.BaseType];
            
            /*var fromBase = from.BaseType;
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
        }*/
        }
    }
}