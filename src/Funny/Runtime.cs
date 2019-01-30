using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Take2;

namespace Funny
{
    public class Runtime
    {
        public string[] Variables => _variables.Keys.ToArray();

        private readonly Equatation[] _equatations;
        private readonly Dictionary<string, VariableExpressionNode> _variables;
        
        public Runtime(Equatation[] equatations, Dictionary<string, VariableExpressionNode> variables)
        {
            _equatations = equatations;
            _variables = variables;
        }


        public CalculationResult Calculate(params Variable[] variables)
        {
            foreach (var value in variables)
            {
                var varName = value.Name;
                if (_variables.TryGetValue(varName, out var varNode))
                    varNode.SetValue(value.Value);
                else
                    throw new ArgumentException(value.Name);
            }
            
            var ans = new Variable[_equatations.Length];
            for (int i = 0; i < _equatations.Length; i++)
            {
                ans[i] = Variable.New(_equatations[i].Id, _equatations[i].Expression.Calc());
            }
            return new CalculationResult(ans);
        }
    }
}