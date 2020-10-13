using System.Collections.Generic;
using NFun.Interpritation.Functions;
using NFun.Tic;
using NFun.Tic.SolvingStates;

namespace NFun.TypeInferenceAdapter
{
    public class TypeInferenceResultsBuilder
    {
        readonly List<StateRefTo[]> _genericFunctionTypes = new List<StateRefTo[]>();
        readonly List<IFunctionSignature> _functionalVariable = new List<IFunctionSignature>();
        readonly List<StateFun> _recursiveCalls = new List<StateFun>();
        readonly SmallStringDictionary<StateFun> _userFunctionSignatures = new SmallStringDictionary<StateFun>();

        private ITicResults _bodyTypeSolving;

        public void RememberGenericCallArguments(int id, StateRefTo[] types) 
            => _genericFunctionTypes.EnlargeAndSet(id, types);

        public StateFun GetUserFunctionSignature(string id, int argsCount)
        {
            if (_userFunctionSignatures.Count == 0)
                return null;
            string name = id + "'" + argsCount;
            _userFunctionSignatures.TryGetValue(name, out var res);
            return res;
;        }
      
        public void RememberUserFunctionSignature(string name, StateFun signature) 
            => _userFunctionSignatures.Add(name+"'"+signature.ArgsCount, signature);

        public void RememberFunctionalVariable(int id, IFunctionSignature signature) 
            => _functionalVariable.EnlargeAndSet(id, signature);

        public void SetResults(ITicResults bodyTypeSolving) => _bodyTypeSolving = bodyTypeSolving;

        public TypeInferenceResults Build()
        {
            return new TypeInferenceResults(
                bodyTypeSolving:_bodyTypeSolving,
                genericFunctionTypes: _genericFunctionTypes.ToArray(), 
                functionalVariables: _functionalVariable,
                recursiveCalls: _recursiveCalls
                );
        }

        public void RememberRecursiveCall(int id, StateFun userFunction) 
            => _recursiveCalls.EnlargeAndSet(id, userFunction);
    }
    public class TypeInferenceResults
    {
        private readonly ITicResults _bodyTypeSolving;
        private readonly IList<IFunctionSignature> _functionalVariables;
        private readonly IList<StateFun> _recursiveCalls;

        public TypeInferenceResults(
            ITicResults bodyTypeSolving, 
            StateRefTo[][] genericFunctionTypes,
            IList<IFunctionSignature> functionalVariables,
            IList<StateFun> recursiveCalls)
        {
            GenericFunctionTypes = genericFunctionTypes;
            _bodyTypeSolving = bodyTypeSolving;
            _functionalVariables = functionalVariables;
            _recursiveCalls = recursiveCalls;
        }
        public IFunctionSignature GetFunctionalVariableOrNull(int id)
        {
            if (_functionalVariables.Count <= id)
                return null;
            return _functionalVariables[id];
        }
        public ITicNodeState[] GetGenericCallArguments(int id)
        {
            if (GenericFunctionTypes.Length <= id)
                return null;
            return GenericFunctionTypes[id];
        }
        public StateFun GetRecursiveCallOrNull(int id)
        {
            if (_recursiveCalls.Count <= id)
                return null;
            return _recursiveCalls[id];
        }
        public ITicNodeState[][] GenericFunctionTypes { get; }
        public ConstrainsState[] Generics => _bodyTypeSolving.GenericsStates;

        public ITicNodeState GetSyntaxNodeTypeOrNull(int id)
        {
            var node = _bodyTypeSolving.GetSyntaxNodeOrNull(id);
            return node?.State;
        }

        public ITicNodeState GetVariableType(string name) 
            => _bodyTypeSolving.GetVariableNode(name).State;
    }
}
