using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.Runtime
{
    public class FunRuntime
    {
        public IEnumerable<VariableUsages> GetInputVariableUsages() => _variables.GetAllUsages().Where(u=>!u.Source.IsOutput);
        public IEnumerable<VariableSource> GetAllVariableSources()  => _variables.GetAllSources();
        public VarInfo[] Inputs => _variables.GetAllSources()
            .Where(v => !v.IsOutput)
            .Select(s => new VarInfo(false,  s.Type,s.Name, s.IsStrictTyped, s.Attributes)).ToArray();

        public VarInfo[] Outputs => _variables.GetAllSources()
            .Where(v => v.IsOutput)
            .Select(s => new VarInfo(true,  s.Type,s.Name, s.IsStrictTyped, s.Attributes)).ToArray();

        public bool HasDefaultOutput => _variables
            .GetAllSources()
            .Any(s =>
                s.IsOutput && string.Equals(s.Name, Parser.AnonymousEquationId, StringComparison.OrdinalIgnoreCase));
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

                _variables.GetSourceOrNull(key).SetClrValue(v);
            }
            //get => _variables.GetSourceOrNull(key).Value;
        }

        public void Update()
        {
            for (int i = 0; i < _equations.Count; i++) 
                _equations[i].UpdateExpression();
        }

        public CalculationResult Calc(string id, object clrValue) => Calc((id, clrValue));
    
        public CalculationResult Calc(params (string id, object clrValue)[] values)
        {
            //todo value convertion or error in such a case: Input: int, expected: double 
            foreach (var value in values)
            {
                if (value.clrValue == null)
                    throw new ArgumentException($"Value for '{value.id}' cannot be null");
                
                var source = _variables.GetSourceOrNull(value.id);
                if(source==null)
                    throw new ArgumentException($"unexpected input '{value.id}'");
                var converter =FunnyTypeConverters.GetInputConverter(value.clrValue.GetType());
                //todo what to do in such a case: Input: int, expected: double ?
                //if(converter.FunnyType!=source.Type)
                //    throw new ArgumentException($"Input '{value.id}' has wrong type. " +
                //                                $"Expected {source.Type} but was {converter.FunnyType}");
                
                source.FunnyValue = converter.ToFunObject(value.clrValue);
            }
            var ans = new VarVal[_equations.Count];
            for (int i = 0; i < _equations.Count; i++) 
                ans[i] = _equations[i].CalcExpression();
            
            return new CalculationResult(ans);
        }
        public CalculationResult Calculate(params VarVal[] vars)
        {
            foreach (var value in vars)
            {
                var source = _variables.GetSourceOrNull(value.Name);
                if(source==null)
                    throw new ArgumentException($"unexpected input '{value.Name}'");
                source.SetClrValue(value.Value);
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
                source?.SetClrValue(value.Value);
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
                source?.SetClrValue(value.Value);
            }
            
            var ans = new VarVal[_equations.Count];
            for (int i = 0; i < _equations.Count; i++) 
                ans[i] = _equations[i].CalcExpression();
            
            return new CalculationResult(ans);
        }
        
        public FunRuntime Fork()
        {
            var scope =new ForkScope(_variables);
            var newEquations =_equations.SelectToArray(e => e.Fork(scope));
            return new FunRuntime(newEquations, scope.GetVariables() , UserFunctions);
            
            //UserFunctions
            //_equations = equations;
            //_variables = variables;
            //UserFunctions = userFunctions;
        }
    }
}