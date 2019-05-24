using System;
using System.Collections.Generic;
using NFun.HindleyMilner.Tyso;
using NFun.Types;

namespace NFun.HindleyMilner
{
    public class HmVisitorState
    {
        public HmVisitorState(NsHumanizerSolver globalSolver)
        {
            CurrentSolver = globalSolver;
        }

        public NsHumanizerSolver CurrentSolver { get; }
        private readonly Dictionary<string, string> _anonymVariablesAliases = new Dictionary<string, string>();

        
        public SolvingNode CreateTypeNode(VarType type)
        {
            if (type.BaseType == BaseVarType.Empty)
                return CurrentSolver.MakeGeneric();
            return SolvingNode.CreateStrict(type.ConvertToHmType());
        }
        
        public string GetActualName(string varName)
        {
            if (_anonymVariablesAliases.TryGetValue(varName, out var realVarName))
                return realVarName;
            return varName;
        }

        public void EnterScope()
        {
            
        }

        public void ExitScope()
        {
            
        }
        public void AddVariableAliase(string originName, string anonymName) 
            => _anonymVariablesAliases.Add(originName,anonymName);

        public bool HasAlias(string inputAlias)
        {
            return _anonymVariablesAliases.ContainsKey(inputAlias);
        }
    }
}