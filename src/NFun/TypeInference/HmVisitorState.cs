using System;
using System.Collections.Generic;
using NFun.TypeInference.Solving;
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

        private readonly AliasTable _aliasTable;
        public HmHumanizerSolver CurrentSolver { get; }

        public SolvingNode CreateTypeNode(VarType type)
        {
            if (type.BaseType == BaseVarType.Empty)
                return CurrentSolver.MakeGeneric();
            return SolvingNode.CreateStrict(type.ConvertToHmType());
        }
        
        public string GetActualName(string varName) 
            => _aliasTable.GetVariableAlias(varName);

        public void EnterScope(int nodeId) 
            => _aliasTable.InitVariableScope(nodeId, new List<string>());

        public void ExitScope() 
            => _aliasTable.ExitVariableScope();

        public void AddVariableAliase(string originName, string alias)
            => _aliasTable.AddVariableAlias(originName, alias); 
        
        public bool HasAlias(string inputAlias) 
            => _aliasTable.HasVariable(inputAlias);
    }
}