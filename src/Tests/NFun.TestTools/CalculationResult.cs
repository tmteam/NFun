using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Types;

namespace NFun.TestTools;

public sealed class CalculationResult {
    internal CalculationResult(VariableTypeAndValue[] rawResults, FunnyConverter converter) {
        _rawResults = rawResults;
        Converter = converter;
    }

    public int Count => _rawResults.Length;

    public IEnumerable<(string, object)> Results => _rawResults.Select(
        r =>
            (r.Name, Converter.GetOutputConverterFor(r.Type).ToClrObject(r.Value)));

    public IEnumerable<string> ResultNames => _rawResults.Select(r => r.Name);

    private readonly VariableTypeAndValue[] _rawResults;
    public FunnyConverter Converter { get; }

    public object Get(string name) {
        foreach (var equationResult in _rawResults)
        {
            if (String.Equals(
                    equationResult.Name, name,
                    StringComparison.CurrentCultureIgnoreCase))
            {
                var converter = Converter.GetOutputConverterFor(equationResult.Type);
                return converter.ToClrObject(equationResult.Value);
            }
        }

        throw new KeyNotFoundException($"value {name} is not found in calculation results");
    }

    public override string ToString() => string.Join("\r\n", _rawResults);
}
