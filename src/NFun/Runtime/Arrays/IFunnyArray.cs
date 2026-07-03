using System;
using System.Collections.Generic;
using NFun.Runtime.Lists;

namespace NFun.Runtime.Arrays;

public interface IFunnyArray : IFunnyEnumerable {
    FunnyType ElementType { get; }
    IFunnyArray Slice(int? startIndex, int? endIndex, int? step);
    object GetElementOrNull(int index);
    IEnumerable<T> As<T>();
    Array ClrArray { get; }
    string ToText();
}
