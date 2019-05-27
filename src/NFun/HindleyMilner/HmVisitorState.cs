using System;
using System.Collections.Generic;
using NFun.HindleyMilner.Tyso;
using NFun.Types;

namespace NFun.HindleyMilner
{
    public class HmVisitorState
    {
        public HmVisitorState(HmHumanizerSolver globalSolver)
        {
            CurrentSolver = globalSolver;
            _aliasTable = new AliasTable();
        }

        public HmHumanizerSolver CurrentSolver { get; }
        private readonly Dictionary<string, string> _anonymVariablesAliases = new Dictionary<string, string>();
        private AliasTable _aliasTable;


        public SolvingNode CreateTypeNode(VarType type)
        {
            if (type.BaseType == BaseVarType.Empty)
                return CurrentSolver.MakeGeneric();
            return SolvingNode.CreateStrict(type.ConvertToHmType());
        }
        
        public string GetActualName(string varName)
        {
            return _aliasTable.GetVariableAlias(varName);
            /*
            if (_anonymVariablesAliases.TryGetValue(varName, out var realVarName))
                return realVarName;
            return varName;*/
        }

        public void EnterScope(int nodeId)
        {
            _aliasTable.InitVariableScope(nodeId, new List<string>());
        }

        public void ExitScope()
        {
            _aliasTable.ExitVariableScope();
        }


        public void AddVariableAliase(string originName, string alias)
            => _aliasTable.AddVariableAlias(originName, alias); // _anonymVariablesAliases.Add(originName,anonymName);

        public bool HasAlias(string inputAlias)
        {
            return _aliasTable.HasVariable(inputAlias);
            //return _anonymVariablesAliases.ContainsKey(inputAlias);
        }
    }
}