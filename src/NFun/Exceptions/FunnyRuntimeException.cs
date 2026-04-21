using System;

namespace NFun.Exceptions; 

public class FunnyRuntimeException : Exception {
    /// <summary>Optional data payload from oops("msg", data).</summary>
    public object OopsData { get; }

    public FunnyRuntimeException(string message, Exception e) : base(message, e) { }

    public FunnyRuntimeException(string message) : base(message) { }

    public FunnyRuntimeException(string message, object oopsData) : base(message) {
        OopsData = oopsData;
    }
}