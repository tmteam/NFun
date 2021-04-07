using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Runtime
{
    public class FunRuntime
    {
        public IEnumerable<VariableSource> GetAllVariableSources() => _variables.GetAllSources();
        public VarInfo[] Inputs => _variables.GetAllSources()
            .Where(v => !v.IsOutput)
            .Select(s => new VarInfo(false,  s.Type,s.Name, s.IsStrictTyped, s.Attributes)).ToArray();

        public VarInfo[] Outputs => _variables.GetAllSources()
            .Where(v => v.IsOutput)
            .Select(s => new VarInfo(true,  s.Type,s.Name, s.IsStrictTyped, s.Attributes)).ToArray();

        public IEnumerable<IFunctionSignature> UserFunctions { get; }

        private readonly IList<Equation> _equations;
        private readonly VariableDictionary _variables;
      
        public FunRuntime(IList<Equation> equations, VariableDictionary variables, List<IFunctionSignature> userFunctions)
        {
            _equations = equations;
            _variables = variables;
            UserFunctions = userFunctions;
        }
        public object this[string key]
        {
            set
            {
                object v;
                if (value is string str)
                    v = new TextFunArray(str);
                else if (value is Array arr)
                {
                    throw new NotImplementedException();
                    v = new ImmutableFunArray(arr,VarType.Anything);
                }
                else
                    v = value;

                _variables.GetSourceOrNull(key).SetConvertedValue(v);
            }
            //get => _variables.GetSourceOrNull(key).Value;
        }

        public void Update()
        {
            for (int i = 0; i < _equations.Count; i++) 
                _equations[i].UpdateExpression();
        }
        public CalculationResult Calculate(params VarVal[] vars)
        {
            foreach (var value in vars)
            {
                var source = _variables.GetSourceOrNull(value.Name);
                if(source==null)
                    throw new ArgumentException($"unexpected input '{value.Name}'");
                source.SetConvertedValue(value.Value);
            }
            
            var ans = new VarVal[_equations.Count];
            for (int i = 0; i < _equations.Count; i++) 
                ans[i] = _equations[i].CalcExpression();
            
            return new CalculationResult(ans);
        }
        
        public CalculationResult CalculateSafe(params VarVal[] vars)
        {
            foreach (var value in vars)
            {
                var source = _variables.GetSourceOrNull(value.Name);
                source?.SetConvertedValue(value.Value);
            }
            
            var ans = new VarVal[_equations.Count];
            for (int i = 0; i < _equations.Count; i++) 
                ans[i] = _equations[i].CalcExpression();
            
            return new CalculationResult(ans);
        }
        internal CalculationResult CalculateSafe(Span<VarVal> vars)
        {
            foreach (var value in vars)
            {
                var source = _variables.GetSourceOrNull(value.Name);
                source?.SetConvertedValue(value.Value);
            }
            
            var ans = new VarVal[_equations.Count];
            for (int i = 0; i < _equations.Count; i++) 
                ans[i] = _equations[i].CalcExpression();
            
            return new CalculationResult(ans);
        }
    }
}