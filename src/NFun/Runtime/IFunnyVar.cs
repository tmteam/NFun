using System;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Runtime
{
    public interface IFunnyVar
    {
        /// <summary>
        /// Variable name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Variable attributes
        /// </summary>
        FunnyAttribute[] Attributes { get; }

        /// <summary>
        /// Nfun type of variable
        /// </summary>
        FunnyType Type { get; }

        /// <summary>
        /// internal representation of value
        /// </summary>
        object FunnyValue { get; }

        /// <summary>
        /// The variable is calculated in the script and can be used as one of the results of the script
        /// </summary>
        bool IsOutput { get; }

        /// <summary>
        /// Represents current CLR value of the funny variable. 
        /// </summary>
        object Value { get; set; }
    }

    internal class VariableSource : IFunnyVar
    {
        private object _funnyValue;

        private readonly FunnyVarAccess _access;

        internal static VariableSource CreateWithStrictTypeLabel(
            string name,
            FunnyType type,
            Interval typeSpecificationIntervalOrNull,
            FunnyVarAccess access,
            FunnyAttribute[] attributes = null)
            => new(name, type, typeSpecificationIntervalOrNull, access, attributes);

        internal static VariableSource CreateWithoutStrictTypeLabel(
            string name, FunnyType type, FunnyVarAccess access, FunnyAttribute[] attributes = null)
            => new(name, type, access, attributes);

        private VariableSource(
            string name,
            FunnyType type,
            Interval typeSpecificationIntervalOrNull,
            FunnyVarAccess access,
            FunnyAttribute[] attributes = null)
        {
            _access = access;
            _funnyValue = type.GetDefaultValueOrNull();
            TypeSpecificationIntervalOrNull = typeSpecificationIntervalOrNull;
            Attributes = attributes ?? Array.Empty<FunnyAttribute>();
            Name = name;
            Type = type;
        }

        public void SetFunnyValueUnsafe(object funnyValue) => _funnyValue = funnyValue;

        public bool IsOutput => _access.HasFlag(FunnyVarAccess.Output);


        private VariableSource(string name, FunnyType type, FunnyVarAccess access, FunnyAttribute[] attributes = null)
        {
            _access = access;
            _funnyValue = type.GetDefaultValueOrNull();
            Attributes = attributes ?? Array.Empty<FunnyAttribute>();
            Name = name;
            Type = type;
        }

        public FunnyAttribute[] Attributes { get; }
        public string Name { get; }
        internal Interval? TypeSpecificationIntervalOrNull { get; }
        public FunnyType Type { get; }
        public object FunnyValue => _funnyValue;

        private bool _outputConverterLoaded;
        private IOutputFunnyConverter _outputConverter;
        
        public object Value
        {
            get
            {
                if (!_outputConverterLoaded)
                {
                    _outputConverterLoaded = true;
                    _outputConverter = FunnyTypeConverters.GetOutputConverter(Type);
                }

                return _outputConverter.ToClrObject(_funnyValue);
            }
            set => _funnyValue = FunnyTypeConverters.ConvertInputOrThrow(value, Type);
        }
    }

    internal enum FunnyVarAccess
    {
        NoInfo = 0,

        /// <summary>
        /// Funny variable is input, so can be modified from the outside before calculation
        /// </summary>
        Input = 1 << 0,

        /// <summary>
        /// Funny variable is output so it can be considered as the result of the calculation
        /// </summary>
        Output = 1 << 1,
    }
}