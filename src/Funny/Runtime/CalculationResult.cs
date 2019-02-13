using System;

namespace Funny.Runtime
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
            foreach (var equatationResult in Results)
            {
                if (String.Equals(equatationResult.Name, name, 
                    StringComparison.CurrentCultureIgnoreCase))
                    return equatationResult;
            }
            throw new ArgumentException(name);
        }
        public object GetResultOf(string name)
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