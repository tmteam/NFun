using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Types;

namespace NFun.Runtime
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
        
        public bool TryGet(string name, out object result)
        {
            foreach (var equationResult in RawResults)
            {
                if (String.Equals(equationResult.Name, name,
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    result = FunnyTypeConverters.GetOutputConverter(equationResult.Type).ToClrObject(equationResult.Value);
                    return true;
                }
            }
            result = default;
            return false;
        }
        
        public bool TryGet(string name, IOutputFunnyConverter converter, out object result)
        {
            foreach (var equationResult in RawResults)
            {
                if (String.Equals(equationResult.Name, name,
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    result = converter.ToClrObject(equationResult.Value);
                    return true;
                }
            }
            result = default;
            return false;
        }

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