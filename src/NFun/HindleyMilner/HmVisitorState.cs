using System;
using System.Collections.Generic;
using NFun.HindleyMilner.Tyso;

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

        public string GetActualName(string varName)
        {
            if (AnonymVariablesAliases.TryGetValue(varName, out var realVarName))
                return realVarName;
            return varName;
        }
        public void AddAnonymVariablesAliase(string originName, string anonymName) 
            => AnonymVariablesAliases.Add(anonymName, originName);
    }
}