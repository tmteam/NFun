using System;

namespace Funny.Runtime
{
    public class CalculationResult
    {
        public CalculationResult(Variable[] results)
        {
            Results = results;
        }

        public Variable[] Results { get; }

        public double GetResultOf(string name)
        {
            foreach (var equatationResult in Results)
            {
                if (String.Equals(equatationResult.Name, name, 
                    StringComparison.CurrentCultureIgnoreCase))
                    return equatationResult.Value;
            }
            throw new ArgumentException(name);
        }
    }
}