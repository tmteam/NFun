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
            _globalSolver = globalSolver;
            CurrentSolver = _globalSolver;
        }
        private NsHumanizerSolver _globalSolver;
        private UserFunctionHmSolving _currentFunctionSolving = null;

        public NsHumanizerSolver CurrentSolver { get; private set; }
        public void EnterUserFunction(string name, int argsCount)
        { 
            if(_currentFunctionSolving!=null)
                throw new InvalidOperationException($"re enter into '{name}' function");
            _currentFunctionSolving = new UserFunctionHmSolving(name, argsCount, new NsHumanizerSolver());
            CurrentSolver = _currentFunctionSolving.Solver;
        }

        public UserFunctionHmSolving ExitFunction()
        {
            if(_currentFunctionSolving==null)
                throw new InvalidOperationException($"No analyzing function");
            CurrentSolver = _globalSolver;

            var fun = _currentFunctionSolving;
            _currentFunctionSolving = null;
            return fun;
        }
        private readonly Dictionary<string, string> AnonymVariablesAliases = new Dictionary<string, string>();

        
        public SolvingNode CreateTypeNode(VarType type)
        {
            if (type.BaseType == BaseVarType.Empty)
                return CurrentSolver.MakeGeneric();
            return SolvingNode.CreateStrict(type.ConvertToHmType());
        }
        
        public string GetActualName(string varName)
        {
            if (AnonymVariablesAliases.TryGetValue(varName, out var realVarName))
                return realVarName;
            return varName;
        }
        public void AddVariableAliase(string originName, string anonymName) 
            => AnonymVariablesAliases.Add(originName,anonymName);

        public bool HasAlias(string inputAlias)
        {
            return AnonymVariablesAliases.ContainsKey(inputAlias);
        }
    }
}