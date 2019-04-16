using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Nodes;
using NFun.Parsing;
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
            
        public VariableSource GetSource(string id)
        {
            if (!_variables.TryGetValue(id.ToLower(), out var v)) 
                return null;
            
            if (v.Source.Name != id)
                return null;
            
            return v.Source;
        }
        
        private readonly Dictionary<string,VariableUsages> _variables 
            = new Dictionary<string, VariableUsages>();
        
        public VariableExpressionNode CreateVarNode(LexNode varName)
        {
            var name = varName.Value.ToLower();
            if (!_variables.ContainsKey(name))
            {
                var source = new VariableSource(name, VarType.Real);
                _variables.Add(name, new VariableUsages(source));
            }
            var node = new VariableExpressionNode(_variables[name].Source,varName.Interval);
            _variables[name].Nodes.AddLast(node);
            return node;
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