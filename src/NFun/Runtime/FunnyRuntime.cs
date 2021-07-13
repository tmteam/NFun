using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.Runtime
{
    public class FunnyRuntime
    {
        internal IEnumerable<VariableUsages> GetInputVariableUsages() =>
            _variables.GetAllUsages().Where(u => !u.Source.IsOutput);

        public bool HasDefaultOutput => _variables
            .GetAllSources()
            .Any(s =>
                s.IsOutput && string.Equals(s.Name, Parser.AnonymousEquationId, StringComparison.OrdinalIgnoreCase));

        private readonly IList<Equation> _equations;
        private readonly VariableDictionary _variables;

        internal FunnyRuntime(IList<Equation> equations, VariableDictionary variables)
        {
            _equations = equations;
            _variables = variables;
        }

        public IFunnyVar this[string key] =>
            GetVariable(key);

        public IReadOnlyList<IFunnyVar> Variables => _variables.GetAllSources();

        public IFunnyVar GetVariable(string name) =>
            _variables.GetUsages(name)?.Source ?? throw new KeyNotFoundException($"Variable {name} not found");

        public bool TryGetVariable(string name, out IFunnyVar variable)
        {
            _variables.TryGetUsages(name, out var usage);
            variable = usage?.Source;
            return variable != null;
        }

        public void Run()
        {
            foreach (var equation in _equations)
                equation.Run();
        }
    }
}