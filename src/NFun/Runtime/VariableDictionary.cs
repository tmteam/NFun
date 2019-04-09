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
                _variables.Add(variableSource.Name, new VariableUsages(variableSource));
            }
        }
        
        public bool TryAdd(VariableSource source)
        {
            if (_variables.ContainsKey(source.Name))
                return false;
            _variables.Add(source.Name, new VariableUsages(source));
            return true;
        }
            
        public VariableSource GetSource(string id)
        {
            if (_variables.TryGetValue(id, out var v))
                return v.Source;
            return null;
        }
        
        private Dictionary<string,VariableUsages> _variables = new Dictionary<string, VariableUsages>();
        
        public IExpressionNode CreateVarNode(LexNode varName)
        {
            var name = varName.Value;
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