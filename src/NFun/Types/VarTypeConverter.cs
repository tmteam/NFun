using System;
using System.Collections.Generic;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.ParseErrors;
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
        private static readonly Func<object, object> ToInt8   = o => Convert.ToSByte(o);
        private static readonly Func<object, object> ToInt16  = o => Convert.ToInt16(o);
        private static readonly Func<object, object> ToInt32  = o => Convert.ToInt32(o);
        private static readonly Func<object, object> ToInt64  = o => Convert.ToInt64(o);
        private static readonly Func<object, object> ToUInt8  = o => Convert.ToByte(o);
        private static readonly Func<object, object> ToUInt16 = o => Convert.ToUInt16(o);
        private static readonly Func<object, object> ToUInt32 = o => Convert.ToUInt32(o);
        private static readonly Func<object, object> ToUInt64 = o => Convert.ToUInt64(o);
        private static readonly Func<object, object> ToReal   = o => Convert.ToDouble(o);
        private static readonly Func<object, object> ToText   = o => new TextFunArray(o?.ToString() ?? "");
        private static readonly Func<object, object> NoConvertion    = o => o;

        public static Func<object, object> GetConverterOrNull(VarType from, VarType to)
        {
            if (to.IsText)
                return ToText;
            if (to.BaseType == BaseVarType.Any)
                return NoConvertion;

            if (from.IsNumeric())
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

            if (from.BaseType != to.BaseType)
                return null;
            if (from.BaseType == BaseVarType.ArrayOf)
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
            
            if (from.BaseType == BaseVarType.Fun)
            {
                var fromInputs = from.FunTypeSpecification.Inputs;
                var toInputs = to.FunTypeSpecification.Inputs;
                if (fromInputs.Length != toInputs.Length)
                    return null;
                var inputConverters = new Func<object, object>[fromInputs.Length];
                for (int i = 0; i < fromInputs.Length; i++)
                {
                    var fromInput = fromInputs[i];
                    var toInput = toInputs[i];
                    var inputConverter = GetConverterOrNull(toInput,fromInput);
                    if (inputConverter == null)
                        return null;
                    inputConverters[i] = inputConverter;
                }

                var outputConverter =
                    GetConverterOrNull(from.FunTypeSpecification.Output, to.FunTypeSpecification.Output);
                if (outputConverter == null)
                    return null;

                object Converter(object input) => new ConcreteFunctionWithConvertation(
                    origin:          (IConcreteFunction) input, 
                    resultType:      to.FunTypeSpecification, 
                    inputConverters: inputConverters, 
                    outputConverter: outputConverter);
                return Converter;
            }

            if (from.BaseType == BaseVarType.Struct)
            {
                foreach (var field in to.StructTypeSpecification)
                {
                    if (!from.StructTypeSpecification.TryGetValue(field.Key, out var fromFieldType))
                        return null;
                    if (!field.Value.Equals(fromFieldType))
                        return null;
                }
                return NoConvertion;
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

        public static bool CanBeConverted(VarType from, VarType to)
        {
            while (true)
            {
                if (to.IsText) return true;
                if (to.BaseType == from.BaseType)
                {
                    switch (to.BaseType)
                    {
                        case BaseVarType.ArrayOf:
                            @from = @from.ArrayTypeSpecification.VarType;
                            to = to.ArrayTypeSpecification.VarType;
                            continue;
                        //Check for Fun and struct types is quite expensive, so there is no big reason to write optimized code  
                        case BaseVarType.Fun:
                            return GetConverterOrNull(@from, to) != null;
                        case BaseVarType.Struct:
                            return GetConverterOrNull(@from, to) != null;
                    }
                }

                return PrimitiveConvertMap[(int) from.BaseType, (int) to.BaseType];
            }
        }

        class ConcreteFunctionWithConvertation : IConcreteFunction
        {
            private readonly IConcreteFunction _origin;
            private readonly FunTypeSpecification _resultType;
            private readonly Func<object, object>[] _inputConverters;
            private readonly Func<object, object> _outputConverter;

            public ConcreteFunctionWithConvertation(
                IConcreteFunction origin, 
                FunTypeSpecification resultType, 
                Func<object,object>[] inputConverters, 
                Func<object,object> outputConverter)
            {
                _origin = origin;
                _resultType = resultType;
                _inputConverters = inputConverters;
                _outputConverter = outputConverter;
            }

            public string Name => _origin.Name;
            public VarType[] ArgTypes => _resultType.Inputs;
            public VarType ReturnType => _resultType.Output;
            public object Calc(object[] parameters)
            {
                var convertedParameters = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    convertedParameters[i] = _inputConverters[i](parameters[i]);
                }

                var result = _origin.Calc(convertedParameters);
                var convertedResult = _outputConverter(result);
                return convertedResult;
            }

            public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, Interval interval) 
                => throw new NotSupportedException("Function convertation is not supported for expression building");
        }

    }
}