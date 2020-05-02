using System.Collections.Generic;
using NFun.Interpritation.Functions;
using NFun.Tic;

namespace NFun.TypeInferenceAdapter
{
    /// <summary>
    /// State of enter setup ti visitor
    /// </summary>
    public class SetupTiState
    {
        private readonly AliasTable _aliasTable;

        private Stack<IFunctionSignature> _functionsCall = new Stack<IFunctionSignature>();
        public void EnterFunction(IFunctionSignature signature) 
            => _functionsCall.Push(signature);

        public IFunctionSignature GetCurrentFunctionSignatureOrNull()
        {
            if (_functionsCall.Count == 0)
                return null;
            return _functionsCall.Peek();
        }
        public IFunctionSignature ExitFunction() => _functionsCall.Pop();

        public SetupTiState(GraphBuilder globalSolver)
        {
            CurrentSolver = globalSolver;
            _aliasTable = new AliasTable();
        }

        public GraphBuilder CurrentSolver { get; }

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