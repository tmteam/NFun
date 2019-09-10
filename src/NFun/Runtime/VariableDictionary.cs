using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Runtime
{
    public class VariableDictionary
    {
        public VariableDictionary()
        {
            
        }
        
        public VariableDictionary(IEnumerable<VariableSource> sources)
        {
            foreach (var variableSource in sources)
            {
                _variables.Add(variableSource.Name.ToLower(), new VariableUsages(variableSource));
            }
        }
        
        /// <summary>
        /// Returns false if variable is already registered
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public bool TryAdd(VariableSource source)
        {
            var lower = source.Name.ToLower();
            if (_variables.ContainsKey(lower))
                return false;
            _variables.Add(lower, new VariableUsages(source));
            return true;
        }
        
        public bool TryAdd(VariableUsages usages)
        {
            var lower = usages.Source.Name.ToLower();

            if (_variables.ContainsKey(lower))
                return false;
            _variables.Add(lower, usages);
            return true;
        }
            
        public VariableSource GetSourceOrNull(string id)
        {
            if (!_variables.TryGetValue(id.ToLower(), out var v)) 
                return null;
            
            if (v.Source.Name != id)
                return null;
            
            return v.Source;
        }
        
        private readonly Dictionary<string,VariableUsages> _variables 
            = new Dictionary<string, VariableUsages>();
        
        public VariableExpressionNode CreateVarNode(string id, Interval interval, VarType type)
        {
            var name = id.ToLower();
            if (!_variables.ContainsKey(name))
            {
                var source = VariableSource.CreateWithoutStrictTypeLabel(id, type);
                _variables.Add(name, new VariableUsages(source));
            }
            var node = new VariableExpressionNode(_variables[name].Source,interval);
            _variables[name].Nodes.AddLast(node);
            return node;
        }
        public VariableUsages GetUsages(string id)
        {
            return _variables[id.ToLower()];
        }
        public VariableUsages[] GetAllUsages()
        {
            return _variables.Values.ToArray();
        }
        public VariableSource[] GetAllSources() {
            return _variables.Values.Select(v => v.Source).ToArray();
        }
    }
}