using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.Runtime
{
    public class FunRuntime
    {
        public IEnumerable<VariableUsages> GetInputVariableUsages() => _variables.GetAllUsages().Where(u=>!u.Source.IsOutput);
        public IFunnyVariable[] GetAllVariables()  => _variables.GetAllVariables();

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
                var usage =_variables.GetUsages(key);
                if(usage==null)
                    throw new KeyNotFoundException($"Variable '{key}' not found in scope");
                
                var converter = FunnyTypeConverters.GetInputConverter(value.GetType());
                usage.Source.InternalFunnyValue = converter.ToFunObject(value);
            }
            get
            {
                var usage =_variables.GetUsages(key);
                if(usage==null)
                    throw new KeyNotFoundException($"Variable '{key}' not found in scope");
                var output =usage.GetVariable() as IFunnyOutput;
                if (output == null)
                    throw new KeyNotFoundException($"Variable '{key}' is input and cannot be read");
                return output.GetValue();
            }
        }

        public IFunnyVariable GetVariable(string name) => 
            _variables.GetUsages(name)?.GetVariable()??throw new KeyNotFoundException($"Variable {name} not found");

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
                this[value.id] = value.clrValue;
                //
                // if (value.clrValue == null)
                //     throw new ArgumentException($"Value for '{value.id}' cannot be null");
                //
                // var source = _variables.GetSourceOrNull(value.id);
                // if(source==null)
                //     throw new ArgumentException($"unexpected input '{value.id}'");
                // v;
                // //todo what to do in such a case: Input: int, expected: double ?
                // //if(converter.FunnyType!=source.Type)
                // //    throw new ArgumentException($"Input '{value.id}' has wrong type. " +
                // //                                $"Expected {source.Type} but was {converter.FunnyType}");
                //
                // source.FunnyValue = converter.ToFunObject(value.clrValue);
            }
            var ans = new VarVal[_equations.Count];
            for (int i = 0; i < _equations.Count; i++) 
                ans[i] = _equations[i].CalcExpression();
            
            return new CalculationResult(ans);
        }

        internal CalculationResult CalculateSafe(params VarVal[] vars)
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
    }
}