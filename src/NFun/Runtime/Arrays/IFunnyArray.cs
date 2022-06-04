using System;
using System.Collections.Generic;

namespace NFun.Runtime.Arrays; 

public interface IFunnyArray : IEnumerable<object> {
    FunnyType ElementType { get; }
    int Count { get; }
    IFunnyArray Slice(int? startIndex, int? endIndex, int? step);
    object GetElementOrNull(int index);
    IEnumerable<T> As<T>();
    Array ClrArray { get; }
    string ToText();
}