using System;
using NFun.Types;

namespace NFun.Runtime
{
    public class CalculationResult
    {
        public CalculationResult(Var[] results)
        {
            Results = results;
        }

        public Var[] Results { get; }

        public Var Get(string name)
        {
            foreach (var equationResult in Results)
            {
                if (String.Equals(equationResult.Name, name, 
                    StringComparison.CurrentCultureIgnoreCase))
                    return equationResult;
            }
            throw new ArgumentException(name);
        }
        public object GetResultOf(string name)
        {
            foreach (var equationResult in Results)
            {
                if (String.Equals(equationResult.Name, name, 
                    StringComparison.CurrentCultureIgnoreCase))
                    return equationResult.Value;
            }
            throw new ArgumentException(name);
        }
    }
}