using System;
using System.Collections.Generic;
using NFun.Types;

namespace NFun.Runtime
{
    public sealed class CalculationResult
    {
        public CalculationResult(VarVal[] results)
        {
            Results = results;
        }

        public VarVal[] Results { get; }

        public bool  TryGet(string name, out VarVal result)
        {
            foreach (var equationResult in Results)
            {
                if (String.Equals(equationResult.Name, name,
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    result = equationResult;
                    return true;
                }
            }
            result = default;
            return false;
        }
        public VarVal Get(string name)
        {
            foreach (var equationResult in Results)
            {
                if (String.Equals(equationResult.Name, name, 
                    StringComparison.CurrentCultureIgnoreCase))
                    return equationResult;
            }
            throw new KeyNotFoundException($"value {name} is not found in calculation results");
        }
        public object GetValueOf(string name)
        {
            foreach (var equationResult in Results)
            {
                if (String.Equals(equationResult.Name, name, 
                    StringComparison.CurrentCultureIgnoreCase))
                    return equationResult.Value;
            }
            throw new KeyNotFoundException($"value {name} is not found in calculation results");
        }

        public override string ToString() => string.Join("\r\n",Results);
    }
}