using System;

namespace NFun.Types
{
    public interface IFunConvertable
    {
        object GetValue();
        T GetOrThrowValue<T>();
    }
    
}