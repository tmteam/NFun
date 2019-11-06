using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Runtime
{
    public class LocalFunctionVariableDictionary: IVariableDictionary
    {
        private readonly IVariableDictionary _origin;
        private readonly Dictionary<string, VariableSource> _localArguments;

        public LocalFunctionVariableDictionary(IVariableDictionary origin, Dictionary<string, VariableSource> localArguments)
        {
            _origin = origin;
            _localArguments = localArguments;
        }

        public bool Contains(string id) => _localArguments.ContainsKey(id) || _origin.Contains(id);

        public bool TryAdd(VariableSource source) => _origin.TryAdd(source);

        public bool TryAdd(VariableUsages usages) => _origin.TryAdd(usages);

        public VariableSource GetSourceOrNull(string id)
        {
            if (_localArguments.TryGetValue(id, out var value))
                return value;
            return _origin.GetSourceOrNull(id);
        }

        public VariableExpressionNode CreateVarNode(string id, Interval interval, VarType type)
        {
            if(!_localArguments.ContainsKey(id))
                return _origin.CreateVarNode(id, interval, type);
            return new VariableExpressionNode(_localArguments[id], interval);
        }

        public VariableUsages GetUsages(string id) 
            => _origin.GetUsages(id);

        public VariableUsages[] GetAllUsages() 
            => _origin.GetAllUsages();

        public VariableSource[] GetAllSources() 
            =>  _origin.GetAllSources().Concat(_localArguments.Values).ToArray();
    }
}