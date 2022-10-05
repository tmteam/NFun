using System.Collections.Generic;

namespace NFun.Runtime;

internal interface IReadonlyVariableDictionary {
    int Count { get; }
    VariableSource GetOrNull(string id);
    VariableSource[] GetAllAsArray();
    IEnumerable<VariableSource> GetAll();
    IReadonlyVariableDictionary Clone();
}