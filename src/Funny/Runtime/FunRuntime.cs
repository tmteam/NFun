using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation;

namespace Funny.Runtime
{
    public class FunRuntime
    {
        public string[] Variables => _variables
            .Where(v=>!v.Value.IsOutput)
            .Select(v=>v.Key)
            .ToArray();

        private readonly Equatation[] _equatations;
        private readonly Dictionary<string, VariableExpressionNode> _variables;
        
        public FunRuntime(Equatation[] equatations, Dictionary<string, VariableExpressionNode> variables)
        {
            _equatations = equatations;
            _variables = variables;
        }

        public CalculationResult Calculate(params Var[] vars)
        {
            foreach (var value in vars)
            {
                var varName = value.Name;
                if (_variables.TryGetValue(varName, out var varNode))
                    varNode.SetValue(Convert.ToDouble(value.Value));
                else
                    throw new ArgumentException(value.Name);
            }
            
            var ans = new Var[_equatations.Length];
            for (int i = 0; i < _equatations.Length; i++)
            {
                var e = _equatations[i];
                ans[i] = new Var(e.Id, e.Expression.Calc(), e.Expression.Type);
                if (e.ReusingWithOtherEquatations)
                    _variables[e.Id].SetValue(Convert.ToDouble(ans[i].Value));
            }
            return new CalculationResult(ans);
        }
    }
}