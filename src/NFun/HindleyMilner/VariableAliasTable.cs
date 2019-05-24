using System.Collections.Generic;

namespace NFun.HindleyMilner
{
    public class VariableAliasTable
    {
        public VariableAliasTable()
        {
            _variableAliasesStack = new List<Dictionary<string, string>>();
            _variableAliasesStack.Add(new Dictionary<string, string>());
        }
        
        private readonly List<Dictionary<string, string>> _variableAliasesStack;
        private string MakeAlias(int nodeLayerId, string varName) => nodeLayerId + "::" + varName;

        public bool HasVariable(string variableName)
        {
            for (int i = _variableAliasesStack.Count  - 1; i >= 0; i--)
            {
                if (_variableAliasesStack[i].ContainsKey(variableName))
                    return true;
            }

            return false;
        }

        public string AddVariableAlias(int node, string variableName)
        {
            var currentFrame = _variableAliasesStack[_variableAliasesStack.Count - 1];
            var alias = MakeAlias(node, variableName);
            if (currentFrame.ContainsKey(variableName))
            {
                
            }
            currentFrame.Add(variableName, alias);
            return alias;
        }
        public string GetVariableAlias(string variableName)
        {
            for (int i = _variableAliasesStack.Count  - 1; i >= 0; i--)
            {
                if (_variableAliasesStack[i].ContainsKey(variableName))
                    return _variableAliasesStack[i][variableName];
            }
            return variableName;
        }
        public void InitVariableScope(int nodeNumber, List<string> scopeVariables)
        {
            var dictionary = new Dictionary<string,string>();
            foreach (var scopeVariable in scopeVariables)
            {
                dictionary.Add(scopeVariable, MakeAlias(nodeNumber, scopeVariable));
            }
            _variableAliasesStack.Add(dictionary);
        }
        public void ExitVariableScope(int nodeNumber)
        {
            _variableAliasesStack.RemoveAt(_variableAliasesStack.Count-1);
        }
    }
}