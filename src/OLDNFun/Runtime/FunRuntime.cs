using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation;
using NFun.Types;

namespace NFun.Runtime
{
    public class FunRuntime
    {
        public VarInfo[] Inputs => _variables.GetAllSources()
            .Where(v => !v.IsOutput)
            .Select(s => new VarInfo(false,  s.Type,s.Name,s.Attributes)).ToArray();

        public VarInfo[] Outputs => _variables.GetAllSources()
            .Where(v => v.IsOutput)
            .Select(s => new VarInfo(true,  s.Type,s.Name, s.Attributes)).ToArray();

        private readonly IList<Equation> _equations;
        private readonly VariableDictionary _variables;
        
        public FunRuntime(IList<Equation> equations, VariableDictionary variables)
        {
            _equations = equations;
            _variables = variables;
        }

        public CalculationResult Calculate(params Var[] vars)
        {
            foreach (var value in vars)
            {
                var varName = value.Name;
                var source = _variables.GetSourceOrNull(varName);
                if(source==null)
                    throw new ArgumentException($"unexpected input '{value.Name}'");
                source.SetConvertedValue(value.Value);
            }
            
            var ans = new Var[_equations.Count];
            for (int i = 0; i < _equations.Count; i++)
            { 
                var e = _equations[i];
                ans[i] = new Var(e.Id, e.Expression.Calc(), e.Expression.Type);
                _variables.GetSourceOrNull(e.Id).Value = ans[i].Value;
            }
            return new CalculationResult(ans);
        }
    }
}