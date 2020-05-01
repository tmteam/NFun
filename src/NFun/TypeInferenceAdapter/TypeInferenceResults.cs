using System;
using System.Collections.Generic;
using System.Text;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;

namespace NFun.TypeInferenceAdapter
{
    public class TypeInferenceResultsBuilder
    {
        readonly List<RefTo[]> _genericFunctionTypes = new List<RefTo[]>();
        readonly List<Constrains> _constrainses = new List<Constrains>();
        
        private Dictionary<string, IState> _namedNodes = null;
        private IState[] _syntaxNodeTypes = null;

        public void SetGenericFunctionTypes(int id, RefTo[] types)
        {
            while (_genericFunctionTypes.Count<=id) 
                _genericFunctionTypes.Add(null);

            _genericFunctionTypes[id] = types;
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
            return new TypeInferenceResults(_genericFunctionTypes.ToArray(), _syntaxNodeTypes, _constrainses.ToArray(),
                _namedNodes);
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

        public TypeInferenceResults(RefTo[][] genericFunctionTypes, IState[] syntaxNodeTypes, Constrains[] generics,
            Dictionary<string, IState> namedNodes)
        {
            GenericFunctionTypes = genericFunctionTypes;
            SyntaxNodeTypes = syntaxNodeTypes;
            Generics = generics;
            _namedNodes = namedNodes;
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
