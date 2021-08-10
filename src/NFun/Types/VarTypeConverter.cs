using System;
using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
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
            PrimitiveConvertMap[(int)BaseFunnyType.ArrayOf,(int)BaseFunnyType.ArrayOf] = false;
            PrimitiveConvertMap[(int)BaseFunnyType.Fun,(int)BaseFunnyType.Fun] = false;

            //every type can be converted to any
            for (int i = 1; i < 15; i++)
                PrimitiveConvertMap[i, (int)BaseFunnyType.Any] = true;
            for (int i = (int) BaseFunnyType.UInt8; i < (int) BaseFunnyType.Real; i++)
            {
                //every number can be converted to real
                PrimitiveConvertMap[i, (int)BaseFunnyType.Real] = true;
                //every number can be converted from u8
                PrimitiveConvertMap[(int)BaseFunnyType.UInt8,i] = true;
            }
            
            PrimitiveConvertMap[(int)BaseFunnyType.UInt16,(int)BaseFunnyType.UInt32] = true;
            PrimitiveConvertMap[(int)BaseFunnyType.UInt16,(int)BaseFunnyType.UInt64] = true;
            PrimitiveConvertMap[(int)BaseFunnyType.UInt16,(int)BaseFunnyType.Int32] = true;
            PrimitiveConvertMap[(int)BaseFunnyType.UInt16,(int)BaseFunnyType.Int64] = true;

            PrimitiveConvertMap[(int)BaseFunnyType.UInt32,(int)BaseFunnyType.UInt64] = true;
            PrimitiveConvertMap[(int)BaseFunnyType.UInt32,(int)BaseFunnyType.Int64] = true;

            PrimitiveConvertMap[(int)BaseFunnyType.Int16,(int)BaseFunnyType.Int32] = true;
            PrimitiveConvertMap[(int)BaseFunnyType.Int16,(int)BaseFunnyType.Int64] = true;

            PrimitiveConvertMap[(int)BaseFunnyType.Int32,(int)BaseFunnyType.Int64] = true;

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
        private static readonly Func<object, object> ToBool = o => Convert.ToBoolean(o);
        private static readonly Func<object, object> ToChar = o => Convert.ToChar(o);
        private static readonly Func<object, object> ToText   = o => new TextFunnyArray(o?.ToString() ?? "");
        private static readonly Func<object, object> NoConvertion    = o => o;

        
        public static Func<object, object> GetConverterOrNull(FunnyType from, FunnyType to)
        {
            //todo coverage
            if (to.IsText)
                return ToText;
            if (to.BaseType == BaseFunnyType.Any)
                return NoConvertion;

            if (from.IsNumeric())
            {
                switch (to.BaseType)
                {
                    case BaseFunnyType.UInt8:  return ToUInt8;
                    case BaseFunnyType.UInt16: return ToUInt16;
                    case BaseFunnyType.UInt32: return ToUInt32;
                    case BaseFunnyType.UInt64: return ToUInt64;
                    case BaseFunnyType.Int16:  return ToInt16;
                    case BaseFunnyType.Int32:  return ToInt32;
                    case BaseFunnyType.Int64:  return ToInt64;
                    case BaseFunnyType.Real:   return ToReal;
                    case BaseFunnyType.Bool:   return ToBool;
                    case BaseFunnyType.Char:   return ToChar;
                    case BaseFunnyType.ArrayOf:
                    case BaseFunnyType.Fun:     
                    case BaseFunnyType.Generic:
                    case BaseFunnyType.Any: break;
                    //todo other primitive types
                    default:
                        throw new ArgumentOutOfRangeException(to.BaseType.ToString());
                }
            }

            if (from.BaseType != to.BaseType)
                return null;
            if (from.BaseType == BaseFunnyType.ArrayOf)
            {
                if (to == FunnyType.ArrayOf(FunnyType.Any))
                    return o => o;

                var elementConverter = GetConverterOrNull(
                    from.ArrayTypeSpecification.FunnyType,
                    to.ArrayTypeSpecification.FunnyType);
                if (elementConverter == null)
                    return null;

                return o =>
                {
                    var origin = (IFunnyArray) o;
                    var array = new object[origin.Count];
                    int index = 0;
                    foreach (var e in origin)
                    {
                        array[index] = elementConverter(e);
                        index++;
                    }
                    return new ImmutableFunnyArray(array, to.ArrayTypeSpecification.FunnyType);
                };
            }
            
            if (from.BaseType == BaseFunnyType.Fun)
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

            if (from.BaseType == BaseFunnyType.Struct)
            {
                foreach (var (key, value) in to.StructTypeSpecification)
                {
                    if (!from.StructTypeSpecification.TryGetValue(key, out var fromFieldType))
                        return null;
                    if (!value.Equals(fromFieldType))
                        return null;
                }
                return NoConvertion;
            }
            return null;
        }

        
        public static Func<object, object> GetConverterOrThrow(FunnyType from, FunnyType to, Interval interval)
        {
            var res = GetConverterOrNull(from, to);
            if(res==null)
                throw ErrorFactory.ImpossibleCast(from, to, interval);
            return res;
        }

        public static bool CanBeConverted(FunnyType from, FunnyType to)
        {
            while (true)
            {
                if (to.IsText) return true;
                if (to.BaseType == from.BaseType)
                {
                    switch (to.BaseType)
                    {
                        case BaseFunnyType.ArrayOf:
                            @from = @from.ArrayTypeSpecification.FunnyType;
                            to = to.ArrayTypeSpecification.FunnyType;
                            continue;
                        //Check for Fun and struct types is quite expensive, so there is no big reason to write optimized code  
                        case BaseFunnyType.Fun:
                            return GetConverterOrNull(@from, to) != null;
                        case BaseFunnyType.Struct:
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
            public FunnyType[] ArgTypes => _resultType.Inputs;
            public FunnyType ReturnType => _resultType.Output;
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