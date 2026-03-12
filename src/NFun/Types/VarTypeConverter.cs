using System;
using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tokenization;

namespace NFun.Types; 

public static class VarTypeConverter {
    private static readonly bool[,] PrimitiveConvertMap;
    private const int PrimitiveTypeCount = 16;
    static VarTypeConverter() {
        PrimitiveConvertMap = new bool [PrimitiveTypeCount, PrimitiveTypeCount];
        //every type can be converted to itself
        for (int i = 1; i < PrimitiveTypeCount; i++)
            PrimitiveConvertMap[i, i] = true;
        //except arrays and funs
        PrimitiveConvertMap[(int)BaseFunnyType.ArrayOf, (int)BaseFunnyType.ArrayOf] = false;
        PrimitiveConvertMap[(int)BaseFunnyType.Fun, (int)BaseFunnyType.Fun] = false;

        //every type can be converted to any
        for (int i = 1; i < PrimitiveTypeCount; i++)
            PrimitiveConvertMap[i, (int)BaseFunnyType.Any] = true;
        for (int i = (int)BaseFunnyType.UInt8; i < (int)BaseFunnyType.Real; i++)
        {
            //every number can be converted to real
            PrimitiveConvertMap[i, (int)BaseFunnyType.Real] = true;
            //every number can be converted from u8
            PrimitiveConvertMap[(int)BaseFunnyType.UInt8, i] = true;
        }

        PrimitiveConvertMap[(int)BaseFunnyType.UInt16, (int)BaseFunnyType.UInt32] = true;
        PrimitiveConvertMap[(int)BaseFunnyType.UInt16, (int)BaseFunnyType.UInt64] = true;
        PrimitiveConvertMap[(int)BaseFunnyType.UInt16, (int)BaseFunnyType.Int32] = true;
        PrimitiveConvertMap[(int)BaseFunnyType.UInt16, (int)BaseFunnyType.Int64] = true;

        PrimitiveConvertMap[(int)BaseFunnyType.UInt32, (int)BaseFunnyType.UInt64] = true;
        PrimitiveConvertMap[(int)BaseFunnyType.UInt32, (int)BaseFunnyType.Int64] = true;

        PrimitiveConvertMap[(int)BaseFunnyType.Int16, (int)BaseFunnyType.Int32] = true;
        PrimitiveConvertMap[(int)BaseFunnyType.Int16, (int)BaseFunnyType.Int64] = true;

        PrimitiveConvertMap[(int)BaseFunnyType.Int32, (int)BaseFunnyType.Int64] = true;

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
    Ip = 11,
    ArrayOf = 12,
    Fun = 13,
    Generic = 14,
    Any  = 15,
         * 
         */
    }

    private static readonly Func<object, object> ToText = o => new TextFunnyArray(o?.ToString() ?? "");
    private static readonly Func<object, object> NoConvertion = o => o;
    
    public static Func<object, object> GetConverterOrNull(TypeBehaviour typeBehaviour, FunnyType @from, FunnyType to) {
        //todo coverage
        if (to.IsText)
            return ToText;
        if (to.BaseType == BaseFunnyType.Any)
            return NoConvertion;

        // None → Optional(T): FunnyNone stays FunnyNone
        if (from.BaseType == BaseFunnyType.None && to.BaseType == BaseFunnyType.Optional)
            return NoConvertion;

        // T → Optional(T): implicit wrapping (boxed value is already valid)
        // At runtime, value might be FunnyNone (from coalesce chains); pass it through.
        if (to.BaseType == BaseFunnyType.Optional && from.BaseType != BaseFunnyType.Optional)
        {
            var inner = GetConverterOrNull(typeBehaviour, from, to.OptionalTypeSpecification.ElementType);
            if (inner == null || inner == NoConvertion) return NoConvertion;
            return o => o is FunnyNone ? o : inner(o);
        }

        // opt(T) → T: optional pass-through for implicit unwrap contexts (e.g., coalesce chains).
        // TIC verified type compatibility; at runtime the value is either FunnyNone (handled by caller)
        // or the actual T value. Convert element type if needed, but pass FunnyNone through.
        if (from.BaseType == BaseFunnyType.Optional && to.BaseType != BaseFunnyType.Optional)
        {
            var elemType = from.OptionalTypeSpecification.ElementType;
            if (elemType == to) return NoConvertion;
            var elementConverter = GetConverterOrNull(typeBehaviour, elemType, to);
            if (elementConverter == null) return null;
            if (elementConverter == NoConvertion) return NoConvertion;
            return o => o is FunnyNone ? o : elementConverter(o);
        }

        if (from.BaseType == BaseFunnyType.Char)
            return typeBehaviour.GetFromCharToNumberConverterOrNull(to.BaseType);
        if (from.IsNumeric())
            return  typeBehaviour.GetNumericConverterOrNull(to.BaseType);
        if (from.BaseType != to.BaseType)
            return null;
        
        if (from.BaseType == BaseFunnyType.Optional)
        {
            var elementConverter = GetConverterOrNull(typeBehaviour, @from.OptionalTypeSpecification.ElementType, to.OptionalTypeSpecification.ElementType);
            if (elementConverter == null)
                return null;
            if (elementConverter == NoConvertion)
                return NoConvertion;
            return o => o is FunnyNone ? o : elementConverter(o);
        }

        if (from.BaseType == BaseFunnyType.ArrayOf)
        {
            if (to == FunnyType.ArrayOf(FunnyType.Any))
                return o => o;

            var elementConverter = GetConverterOrNull(typeBehaviour, @from.ArrayTypeSpecification.FunnyType, to.ArrayTypeSpecification.FunnyType);
            if (elementConverter == null)
                return null;

            return o => {
                var origin = (IFunnyArray)o;
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
                var inputConverter = GetConverterOrNull(typeBehaviour, toInput, fromInput);
                if (inputConverter == null)
                    return null;
                inputConverters[i] = inputConverter;
            }

            var outputConverter =
                GetConverterOrNull(typeBehaviour, @from.FunTypeSpecification.Output, to.FunTypeSpecification.Output);
            if (outputConverter == null)
                return null;

            object Converter(object input) => new ConcreteFunctionWithConvertion(
                origin: (IConcreteFunction)input,
                resultType: to.FunTypeSpecification,
                inputConverters: inputConverters,
                outputConverter: outputConverter);

            return Converter;
        }

        if (from.BaseType == BaseFunnyType.Struct)
        {
            var fieldConverters = new Dictionary<string, Func<object, object>>(StringComparer.InvariantCultureIgnoreCase);
            bool needsConversion = false;
            foreach (var (key, toFieldType) in to.StructTypeSpecification)
            {
                if (!from.StructTypeSpecification.TryGetValue(key, out var fromFieldType))
                    return null;
                var fieldConverter = GetConverterOrNull(typeBehaviour, fromFieldType, toFieldType);
                if (fieldConverter == null)
                    return null;
                fieldConverters[key] = fieldConverter;
                if (!fromFieldType.Equals(toFieldType))
                    needsConversion = true;
            }
            if (!needsConversion)
                return NoConvertion;
            return o => {
                var origin = (FunnyStruct)o;
                var fields = new FunnyStruct.FieldsDictionary(origin.Count);
                foreach (var (key, value) in origin)
                {
                    if (fieldConverters.TryGetValue(key, out var converter))
                        fields[key] = converter(value);
                    else
                        fields[key] = value;
                }
                return new FunnyStruct(fields);
            };
        }
        return null;
    }
    
    public static Func<object, object> GetConverterOrThrow(TypeBehaviour typeBehaviour, FunnyType from, FunnyType to,  Interval interval) {
        var res = GetConverterOrNull(typeBehaviour, @from, to);
        if (res == null)
            throw Errors.ImpossibleCast(from, to, interval);
        return res;
    }

    public static bool CanBeConverted(FunnyType from, FunnyType to) {
        while (true)
        {
            if (to.IsText) return true;

            // None → Optional(T) is always valid
            if (from.BaseType == BaseFunnyType.None && to.BaseType == BaseFunnyType.Optional)
                return true;

            // T → Optional(T) - implicit wrapping
            if (to.BaseType == BaseFunnyType.Optional && from.BaseType != BaseFunnyType.Optional)
                return CanBeConverted(from, to.OptionalTypeSpecification.ElementType);

            // opt(T) → T: optional pass-through (TIC verified compatibility)
            if (from.BaseType == BaseFunnyType.Optional && to.BaseType != BaseFunnyType.Optional)
                return CanBeConverted(from.OptionalTypeSpecification.ElementType, to);

            if (to.BaseType == from.BaseType)
            {
                switch (to.BaseType)
                {
                    case BaseFunnyType.ArrayOf:
                        @from = @from.ArrayTypeSpecification.FunnyType;
                        to = to.ArrayTypeSpecification.FunnyType;
                        continue;
                    case BaseFunnyType.Optional:
                        @from = @from.OptionalTypeSpecification.ElementType;
                        to = to.OptionalTypeSpecification.ElementType;
                        continue;
                    //Check for Fun and struct types is quite expensive, so there is no big reason to write optimized code
                    case BaseFunnyType.Fun:
                        return GetConverterOrNull(Dialects.Origin.Converter.TypeBehaviour, @from, to) != null;
                    case BaseFunnyType.Struct:
                        return GetConverterOrNull(Dialects.Origin.Converter.TypeBehaviour, @from, to) != null;
                }
            }

            if ((int)from.BaseType >= PrimitiveTypeCount || (int)to.BaseType >= PrimitiveTypeCount)
                return false;

            return PrimitiveConvertMap[(int)from.BaseType, (int)to.BaseType];
        }
    }

    private class ConcreteFunctionWithConvertion : IConcreteFunction {
        private readonly IConcreteFunction _origin;
        private readonly FunTypeSpecification _resultType;
        private readonly Func<object, object>[] _inputConverters;
        private readonly Func<object, object> _outputConverter;

        public ConcreteFunctionWithConvertion(
            IConcreteFunction origin,
            FunTypeSpecification resultType,
            Func<object, object>[] inputConverters,
            Func<object, object> outputConverter) {
            _origin = origin;
            _resultType = resultType;
            _inputConverters = inputConverters;
            _outputConverter = outputConverter;
        }

        public string Name => _origin.Name;
        public FunnyType[] ArgTypes => _resultType.Inputs;
        public FunnyType ReturnType => _resultType.Output;

        public object Calc(object[] parameters) {
            var convertedParameters = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) {
                convertedParameters[i] = _inputConverters[i](parameters[i]);
            }

            var result = _origin.Calc(convertedParameters);
            var convertedResult = _outputConverter(result);
            return convertedResult;
        }

        public IConcreteFunction Clone(ICloneContext context) 
            => new ConcreteFunctionWithConvertion(_origin.Clone(context), _resultType, _inputConverters, _outputConverter);

        public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, TypeBehaviour typeBehaviour, Interval interval)
            => throw new NotSupportedException("Function convertation is not supported for expression building");
    }
}