using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Types;

namespace NFun.TestTools
{
    public sealed class CalculationResult
    {
        public CalculationResult(VarVal[] rawResults)
        {
            RawResults = rawResults;
        }

        public int Count => RawResults.Length;

        public IEnumerable<(string, object)> Results => RawResults.Select(r =>
            (r.Name,
                FunnyTypeConverters.GetOutputConverter(r.Type).ToClrObject(r.Value)));

        private VarVal[] RawResults { get; } 
        
        public object Get(string name)
        {
            foreach (var equationResult in RawResults)
            {
                if (String.Equals(equationResult.Name, name,
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    var converter = FunnyTypeConverters.GetOutputConverter(equationResult.Type);
                    return converter.ToClrObject(equationResult.Value);
                }
            }
            throw new KeyNotFoundException($"value {name} is not found in calculation results");
        }

        public override string ToString() => string.Join("\r\n",RawResults);
    }
}