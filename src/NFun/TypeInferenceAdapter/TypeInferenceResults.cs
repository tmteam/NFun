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
        readonly List<RefTo[]> _genericFunctionTypes = new List<RefTo[]>();
        readonly List<Constrains> _constrainses = new List<Constrains>();
        readonly List<IFunctionSignature> _functionSignatures = new List<IFunctionSignature>();
        readonly List<IFunctionSignature> _functionalVariable = new List<IFunctionSignature>();
        readonly List<Fun> _recursiveCalls = new List<Fun>();

        readonly Dictionary<string, Fun> _userFunctionSignatures = new Dictionary<string, Fun>();

        private Dictionary<string, IState> _namedNodes = null;
        private IState[] _syntaxNodeTypes = null;

        public void RememberGenericCallArguments(int id, RefTo[] types) 
            => _genericFunctionTypes.EnlargeAndSet(id, types);

        public Fun GetUserFunctionSignature(string id, int argsCount)
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
        public void RememberUserFunctionSignature(string name, Fun signature) 
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

        public void RememberRecursiveCall(int id, Fun userFunction) 
            => _recursiveCalls.EnlargeAndSet(id, userFunction);
    }
    public class TypeInferenceResults
    {
        private readonly Dictionary<string, IState> _namedNodes;
        private readonly IList<IFunctionSignature> _functions;
        private readonly IList<IFunctionSignature> _functionalVariables;
        private readonly IList<Fun> _recursiveCalls;

        public TypeInferenceResults(
            RefTo[][] genericFunctionTypes, 
            IState[] syntaxNodeTypes, 
            Constrains[] generics,
            Dictionary<string, IState> namedNodes,
            IList<IFunctionSignature> functionSignatures,
            IList<IFunctionSignature> functionalVariables, 
            IList<Fun> recursiveCalls
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
        public IState[] GetGenericCallArguments(int id)
        {
            if (GenericFunctionTypes.Length <= id)
                return null;
            return GenericFunctionTypes[id];
        }
        public Fun GetRecursiveCallOrNull(int id)
        {
            if (_recursiveCalls.Count <= id)
                return null;
            return _recursiveCalls[id];
        }
        public IState[][] GenericFunctionTypes { get; }
        public IState[] SyntaxNodeTypes{ get; }
        public Constrains[] Generics { get; }

        public IState GetSyntaxNodeTypeOrNull(int id)
        {
            if (SyntaxNodeTypes.Length <= id)
                return null;
            return SyntaxNodeTypes[id];
        }

        public IState GetVariableType(string name) => _namedNodes[name];
    }
}
