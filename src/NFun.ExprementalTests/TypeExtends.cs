using System;
using NFun.Runtime;
using NFun.Types;

namespace NFun.ExprementalTests
{
    public class VQTArray : FunArray, IVQT
    {
        public VQTArray(Array val):base(val)
        {
            
        }

        public object V => this;
        public int Q { get; set; }
        public long T { get; set; }    
    }

    public interface IVQT
    {
        object V { get; }
        int Q { get; set; }
        long T { get; set;}
    }
    public class PrimitiveVQT: IFunConvertable, IVQT
    {
        private readonly object _value;

        public PrimitiveVQT(object value)
        {
            _value = value;
        }

        public object GetValue() => _value;

        public T GetOrThrowValue<T>() => (T) _value;
        public object V => _value;
        public int Q { get; set; }
        public long T { get; set; }
    }
    
    
}