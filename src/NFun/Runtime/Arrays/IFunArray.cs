using System.Collections.Generic;

namespace NFun.Runtime.Arrays
{
    public interface IFunArray: IEnumerable<object>
    {
        int Count { get; }
        IFunArray Slice(int? startIndex, int? endIndex, int? step);

        object GetElementOrNull(int index);
        bool IsEquivalent(IFunArray array);
        IEnumerable<T> As<T>();
    }
}