using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation;
using NFun.Interpritation.Nodes;
using NFun.Types;

namespace NFun.Runtime
{
    public class FunRuntime
    {
        public VarInfo[] Inputs => _variables
            .Where(v => !v.Value.IsOutput)
            .Select(s => new VarInfo(false,  s.Value.Type,s.Key)).ToArray();

        public VarInfo[] Outputs => _variables
            .Where(v => v.Value.IsOutput)
            .Select(s => new VarInfo(true,  s.Value.Type,s.Key)).ToArray();

        private readonly IList<Equation> _equations;
        private readonly Dictionary<string, VariableExpressionNode> _variables;
        
        public FunRuntime(IList<Equation> equations, Dictionary<string, VariableExpressionNode> variables)
        {
            _equations = equations;
            _variables = variables;
        }

        public CalculationResult Calculate(params Var[] vars)
        {
            foreach (var value in vars)
            {
                var varName = value.Name;
                if (_variables.TryGetValue(varName, out var varNode))
                    varNode.SetConvertedValue(value.Value);
                else
                    throw new ArgumentException($"unexpected input '{value.Name}'");
            }
            
            var ans = new Var[_equations.Count];
            for (int i = 0; i < _equations.Count; i++)
            { 
                var e = _equations[i];
                ans[i] = new Var(e.Id, e.Expression.Calc(), e.Expression.Type);
                if (e.ReusingWithOtherEquations)
                    _variables[e.Id].SetValue(ans[i].Value);
            }
            return new CalculationResult(ans);
        }
    }
}