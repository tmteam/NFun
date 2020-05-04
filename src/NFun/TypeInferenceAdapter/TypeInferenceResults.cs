using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
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
        readonly Dictionary<string, Fun> userFunctionSignatures = new Dictionary<string, Fun>();

        private Dictionary<string, IState> _namedNodes = null;
        private IState[] _syntaxNodeTypes = null;

        public void SetGenericTypes(int id, RefTo[] types)
        {
            while (_genericFunctionTypes.Count<=id) 
                _genericFunctionTypes.Add(null);

            _genericFunctionTypes[id] = types;
        }

        public Fun GetUserFunctionSignature(string id, int argsCount)
        {
            string name = id + "'" + argsCount;
            userFunctionSignatures.TryGetValue(name, out var res);
            return res;
;        }
        public IFunctionSignature GetSignatureOrNull(int id)
        {
            if (_functionSignatures.Count <= id)
                return null;
            return _functionSignatures[id];
        }
        public void RememberUserFunctionSignature(string name, Fun signature) 
            => userFunctionSignatures.Add(name+"'"+signature.ArgsCount, signature);

        public void RememberFunctionalVariable(int id, IFunctionSignature signature)
        {
            while (_functionalVariable.Count <= id)
                _functionalVariable.Add(null);
            _functionalVariable[id] = signature;
        }
        public void RememberFunctionCall(int id, IFunctionSignature signature)
        {
            while (_functionSignatures.Count <= id)
                _functionSignatures.Add(null);
            _functionSignatures[id] = signature;
        }
        public void SetResults(FinalizationResults bodyTypeSolving)
        {
            SetSyntaxNodeTypes(bodyTypeSolving.GetSyntaxNodes());
            foreach (var generic in bodyTypeSolving.GetAllGenerics)
            {
                AddGenericType(generic);
            }

            SetNamedNodes(bodyTypeSolving.GetAllNamedNodes());
        }
        public TypeInferenceResults Build()
        {
            return new TypeInferenceResults(
                genericFunctionTypes: _genericFunctionTypes.ToArray(), 
                syntaxNodeTypes: _syntaxNodeTypes, 
                generics: _constrainses.ToArray(),
                namedNodes: _namedNodes,
                functionSignatures: _functionSignatures.ToArray(),
                functionalVariables: _functionalVariable.ToArray()
                );
        }

        private void SetSyntaxNodeTypes(IState[] syntaxNodeTypes) 
            => _syntaxNodeTypes = syntaxNodeTypes;

        private void AddGenericType(Constrains constrains) 
            => _constrainses.Add(constrains);

        private void SetNamedNodes(Dictionary<string, IState> namedNodes) 
            => _namedNodes = namedNodes;


      
    }
    public class TypeInferenceResults
    {
        private Dictionary<string, IState> _namedNodes;
        private readonly IFunctionSignature[] _functions;
        private readonly IFunctionSignature[] _functionalVariables;

        public TypeInferenceResults(
            RefTo[][] genericFunctionTypes, 
            IState[] syntaxNodeTypes, 
            Constrains[] generics,
            Dictionary<string, IState> namedNodes,
            IFunctionSignature[] functionSignatures,
            IFunctionSignature[] functionalVariables
            )
        {
            GenericFunctionTypes = genericFunctionTypes;
            SyntaxNodeTypes = syntaxNodeTypes;
            Generics = generics;
            _namedNodes = namedNodes;
            _functions = functionSignatures;
            _functionalVariables = functionalVariables;
        }
        public IFunctionSignature GetSignatureOrNull(int id)
        {
            if (_functions.Length <= id)
                return null;
            return _functions[id];
        }
        public IFunctionSignature GetFunctionalVariableOrNull(int id)
        {
            if (_functionalVariables.Length <= id)
                return null;
            return _functionalVariables[id];
        }
        public IState[] GetGenericFunctionTypes(int id)
        {
            if (GenericFunctionTypes.Length <= id)
                return null;
            return GenericFunctionTypes[id];
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
