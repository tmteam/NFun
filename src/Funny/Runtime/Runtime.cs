using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation;

namespace Funny.Runtime
{
    public class Runtime
    {
        public string[] Variables => _variables
            .Where(v=>!v.Value.IsOutput)
            .Select(v=>v.Key)
            .ToArray();

        private readonly Equatation[] _equatations;
        private readonly Dictionary<string, VariableExpressionNode> _variables;
        
        public Runtime(Equatation[] equatations, Dictionary<string, VariableExpressionNode> variables)
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
                    varNode.SetValue(value.Value);
                else
                    throw new ArgumentException(value.Name);
            }
            
            var ans = new Var[_equatations.Length];
            for (int i = 0; i < _equatations.Length; i++)
            {
                var e = _equatations[i];
                ans[i] = Var.New(e.Id, e.Expression.Calc());
                if (e.ReusingWithOtherEquatations)
                    _variables[e.Id.ToLower()].SetValue(ans[i].Value);
            }
            return new CalculationResult(ans);
        }
    }
}