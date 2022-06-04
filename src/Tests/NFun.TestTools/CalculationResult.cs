using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun.TestTools; 

public sealed class CalculationResult {
    internal CalculationResult(VariableTypeAndValue[] rawResults, TypeBehaviour typeBehaviour) {
        _rawResults = rawResults;
        TypeBehaviour = typeBehaviour;
    }

    public int Count => _rawResults.Length;

    public IEnumerable<(string, object)> Results => _rawResults.Select(
        r =>
            (r.Name, TypeBehaviour.GetOutputConverterFor(r.Type).ToClrObject(r.Value)));

    public IEnumerable<string> ResultNames => _rawResults.Select(r => r.Name);

    private readonly VariableTypeAndValue[] _rawResults;
    public TypeBehaviour TypeBehaviour { get; }

    public object Get(string name) {
        foreach (var equationResult in _rawResults)
        {
            if (String.Equals(
                equationResult.Name, name,
                StringComparison.CurrentCultureIgnoreCase))
            {
                var converter = TypeBehaviour.GetOutputConverterFor(equationResult.Type);
                return converter.ToClrObject(equationResult.Value);
            }
        }

        throw new KeyNotFoundException($"value {name} is not found in calculation results");
    }

    public override string ToString() => string.Join("\r\n", _rawResults);
}