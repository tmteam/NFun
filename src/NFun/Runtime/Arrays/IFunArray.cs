using System;
using System.Collections.Generic;
using NFun.Types;

namespace NFun.Runtime.Arrays
{
    public interface IFunArray : IEnumerable<object>
    {
        VarType ElementType { get; }
    int Count { get; }
    IFunArray Slice(int? startIndex, int? endIndex, int? step);
    object GetElementOrNull(int index);
    bool IsEquivalent(IFunArray array);
    IEnumerable<T> As<T>();
    Array ClrArray { get; }
    string ToText();
    }
}