using System;
using System.Collections.Generic;
using System.Text;
using NFun.Tic.SolvingStates;

namespace NFun.TypeInferenceAdapter
{
    public class TypeInferenceResultsBuilder
    {
        List<RefTo[]> _genericFunctionTypes = new List<RefTo[]>();
        private IState[] _syntaxNodeTypes;
        List<Constrains> _constrainses = new List<Constrains>();
        private Dictionary<string, IState> _namedNodes = new Dictionary<string, IState>();

        public void SetGenericFunctionTypes(int id, RefTo[] types)
        {
            while (_genericFunctionTypes.Count<=id) 
                _genericFunctionTypes.Add(null);

            _genericFunctionTypes[id] = types;
        }

        public void SetSyntaxNodeTypes(IState[] syntaxNodeTypes)
        {
            _syntaxNodeTypes = syntaxNodeTypes;
        }

        public void AddGenericType(Constrains constrains)
        {
            _constrainses.Add(constrains);
        }

        public TypeInferenceResults Build()
        {
            return new TypeInferenceResults(_genericFunctionTypes.ToArray(), _syntaxNodeTypes, _constrainses.ToArray(), _namedNodes);
        }

        public void SetNamedNodes(Dictionary<string, IState> namedNodes)
        {
            _namedNodes = namedNodes;
        }
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
        public IState GetVariableType(string name)
        {
            return _namedNodes[name];
        }
    }

}
