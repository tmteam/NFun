using System;
using Funny.Types;

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
            foreach (var EquationResult in Results)
            {
                if (String.Equals(EquationResult.Name, name, 
                    StringComparison.CurrentCultureIgnoreCase))
                    return EquationResult;
            }
            throw new ArgumentException(name);
        }
        public object GetResultOf(string name)
        {
            foreach (var EquationResult in Results)
            {
                if (String.Equals(EquationResult.Name, name, 
                    StringComparison.CurrentCultureIgnoreCase))
                    return EquationResult.Value;
            }
            throw new ArgumentException(name);
        }
    }
}