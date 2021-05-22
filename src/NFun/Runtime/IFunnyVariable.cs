using System;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.Runtime
{
    public interface IFunnyVariable
    {
        string Name { get; }
        bool IsOutput { get; }
        VarAttribute[] Attributes { get; }
        VarType Type { get; }
    }

    public interface IFunnyInput : IFunnyVariable
    {
        /// <summary>
        /// Setup clr value to funny input.
        /// Value type has to be exact as expected
        /// </summary>
        ///<exception cref="InvalidCastException">value cannot be casted to corresponding nfun type</exception>
        void SetValue(object value);
    }

    public interface IFunnyOutput : IFunnyVariable
    {
        /// <summary>
        /// Get current clr value of funny output
        /// </summary>
        object GetValue();
        Type ClrType { get; }
    }

    class FunnyOutput : IFunnyOutput
    {
        private readonly VariableSource _source;
        private readonly IOutputFunnyConverter _converter;

        public FunnyOutput(VariableSource source, IOutputFunnyConverter converter)
        {
            _source = source;
            _converter = converter;
        }
        public string Name => _source.Name;
        public bool IsOutput => _source.IsOutput;
        public VarAttribute[] Attributes => _source.Attributes;
        public VarType Type => _converter.FunnyType;
        public Type ClrType => _converter.ClrType;
        public object GetValue() => _converter.ToClrObject(_source.InternalFunnyValue);
    }

    class FunnyInput:IFunnyInput
    {
        private readonly VariableSource _source;
        private readonly IinputFunnyConverter _converter;

        public FunnyInput(VariableSource source, IinputFunnyConverter converter)
        {
            _source = source;
            _converter = converter;
        }

        public string Name => _source.Name;
        public bool IsOutput => _source.IsOutput;
        public VarAttribute[] Attributes { get; }
        public VarType Type => _converter.FunnyType;
        
        public void SetValue(object value) => 
            _source.InternalFunnyValue = _converter.ToFunObject(value);
    }
}