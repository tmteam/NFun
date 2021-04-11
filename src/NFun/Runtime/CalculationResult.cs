using System;
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

        public VarVal Get(string name)
        {
            foreach (var equationResult in Results)
            {
                if (String.Equals(equationResult.Name, name, 
                    StringComparison.CurrentCultureIgnoreCase))
                    return equationResult;
            }
            throw new ArgumentException(name);
        }
        public object GetValueOf(string name)
        {
            foreach (var equationResult in Results)
            {
                if (String.Equals(equationResult.Name, name, 
                    StringComparison.CurrentCultureIgnoreCase))
                    return equationResult.Value;
            }
            throw new ArgumentException(name);
        }

        public override string ToString()
        {
            return string.Join("\r\n",Results);
        }
    }
}