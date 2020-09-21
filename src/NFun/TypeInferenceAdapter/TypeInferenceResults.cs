using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;

namespace NFun.TypeInferenceAdapter
{
    public class TypeInferenceResultsBuilder
    {
        readonly List<StateRefTo[]> _genericFunctionTypes = new List<StateRefTo[]>();
        readonly List<ConstrainsState> _constrainses = new List<ConstrainsState>();
        readonly List<IFunctionSignature> _functionSignatures = new List<IFunctionSignature>();
        readonly List<IFunctionSignature> _functionalVariable = new List<IFunctionSignature>();
        readonly List<StateFun> _recursiveCalls = new List<StateFun>();

        readonly Dictionary<string, StateFun> _userFunctionSignatures = new Dictionary<string, StateFun>();

        private Dictionary<string, ITicNodeState> _namedNodes = null;
        private ITicNodeState[] _syntaxNodeTypes = null;

        public void RememberGenericCallArguments(int id, StateRefTo[] types) 
            => _genericFunctionTypes.EnlargeAndSet(id, types);

        public StateFun GetUserFunctionSignature(string id, int argsCount)
        {
            string name = id + "'" + argsCount;
            _userFunctionSignatures.TryGetValue(name, out var res);
            return res;
;        }
        public IFunctionSignature GetSignatureOrNull(int id)
        {
            if (_functionSignatures.Count <= id)
                return null;
            return _functionSignatures[id];
        }
        public void RememberUserFunctionSignature(string name, StateFun signature) 
            => _userFunctionSignatures.Add(name+"'"+signature.ArgsCount, signature);

        public void RememberFunctionalVariable(int id, IFunctionSignature signature) 
            => _functionalVariable.EnlargeAndSet(id, signature);

        public void RememberFunctionCall(int id, IFunctionSignature signature) 
            => _functionSignatures.EnlargeAndSet(id, signature);

        public void SetResults(FinalizationResults bodyTypeSolving)
        {
            _syntaxNodeTypes = bodyTypeSolving.GetSyntaxNodeStates();
            _namedNodes      = bodyTypeSolving.GetAllNamedNodeStates();
            _constrainses.AddRange(bodyTypeSolving.GenericsStates);
        }
        public TypeInferenceResults Build()
        {
            return new TypeInferenceResults(
                genericFunctionTypes: _genericFunctionTypes.ToArray(), 
                syntaxNodeTypes: _syntaxNodeTypes, 
                generics: _constrainses.ToArray(),
                namedNodes: _namedNodes,
                functionSignatures: _functionSignatures,
                functionalVariables: _functionalVariable,
                recursiveCalls: _recursiveCalls
                );
        }

        public void RememberRecursiveCall(int id, StateFun userFunction) 
            => _recursiveCalls.EnlargeAndSet(id, userFunction);
    }
    public class TypeInferenceResults
    {
        private readonly Dictionary<string, ITicNodeState> _namedNodes;
        private readonly IList<IFunctionSignature> _functions;
        private readonly IList<IFunctionSignature> _functionalVariables;
        private readonly IList<StateFun> _recursiveCalls;

        public TypeInferenceResults(
            StateRefTo[][] genericFunctionTypes, 
            ITicNodeState[] syntaxNodeTypes, 
            ConstrainsState[] generics,
            Dictionary<string, ITicNodeState> namedNodes,
            IList<IFunctionSignature> functionSignatures,
            IList<IFunctionSignature> functionalVariables, 
            IList<StateFun> recursiveCalls
            )
        {
            GenericFunctionTypes = genericFunctionTypes;
            SyntaxNodeTypes = syntaxNodeTypes;
            Generics = generics;
            _namedNodes = namedNodes;
            _functions = functionSignatures;
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
        public ITicNodeState[] SyntaxNodeTypes{ get; }
        public ConstrainsState[] Generics { get; }

        public ITicNodeState GetSyntaxNodeTypeOrNull(int id)
        {
            if (SyntaxNodeTypes.Length <= id)
                return null;
            return SyntaxNodeTypes[id];
        }

        public ITicNodeState GetVariableType(string name) => _namedNodes[name];
    }
}
