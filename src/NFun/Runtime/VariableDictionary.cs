using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Runtime
{
    public class VariableDictionary
    {
        private readonly Dictionary<string, VariableUsages> _variables;

        public VariableDictionary()
        {
            _variables = new Dictionary<string, VariableUsages>(StringComparer.OrdinalIgnoreCase);
        }

        public VariableDictionary(int capacity) => 
            _variables = new Dictionary<string, VariableUsages>(capacity, StringComparer.OrdinalIgnoreCase);

        public VariableDictionary(IEnumerable<VariableSource> sources)
        {
            _variables = new Dictionary<string, VariableUsages>(StringComparer.OrdinalIgnoreCase);

            foreach (var variableSource in sources)
            {
                _variables.Add(variableSource.Name, new VariableUsages(variableSource));
            }
        }
        
        public void AddOrReplace(VariableSource source) => _variables[source.Name] = new VariableUsages(source);

        /// <summary>
        /// Returns false if variable is already registered
        /// </summary>
        public bool TryAdd(VariableSource source)
        {
            var name = source.Name;
            if (_variables.ContainsKey(name))
                return false;
            _variables.Add(name, new VariableUsages(source));
            return true;
        }
        
        /// <summary>
        /// Returns false if variable is already registered
        /// </summary>
        public bool TryAdd(VariableUsages usages)
        {
            var name = usages.Source.Name;
            if (_variables.ContainsKey(name))
                return false;
            _variables.Add(name, usages);
            return true;
        }
            
        public VariableSource GetSourceOrNull(string id) => 
            _variables.TryGetValue(id, out var v) ? v.Source:null;

        public VariableExpressionNode CreateVarNode(string id, Interval interval, VarType type)
        {
            if (!_variables.TryGetValue(id, out  var usage))
            {
                var source = VariableSource.CreateWithoutStrictTypeLabel(id, type,false);
                usage = new VariableUsages(source);
                _variables.Add(id, usage);
            }
            var node = new VariableExpressionNode(usage.Source,interval);
            usage.Usages.AddLast(node);
            return node;
        }

        public VariableUsages GetSuperAnonymousVariableOrNull()
        {
            foreach (var key in _variables.Keys)
            {
                if (Helper.DoesItLooksLikeSuperAnonymousVariable(key))
                    return _variables[key];
            }
            return null;
        }
        
        public VariableUsages GetUsages(string id) => _variables[id];
        public VariableUsages[] GetAllUsages()
        {
            var sources = new VariableUsages[_variables.Count];
            var i = 0;
            foreach (var variable in _variables)
            {
                sources[i] = variable.Value;
                i++;
            }
            return sources;
        }

        public VariableSource[] GetAllSources()
        {
            var sources = new VariableSource[_variables.Count];
            var i = 0;
            foreach (var variable in _variables)
            {
                sources[i] = variable.Value.Source;
                i++;
            }
            return sources;
        }

        public IFunnyVariable[] GetAllVariables()
        {
            var variables = new IFunnyVariable[_variables.Count];
            var i = 0;
            foreach (var variable in _variables)
            {
                variables[i] = variable.Value.GetVariable();
                i++;
            }
            return variables;
        }
    }
}