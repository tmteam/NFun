using System.Collections.Generic;
using System.Linq;

namespace NFun.HindleyMilner.Tyso
{
    public class FSolver
    {
        public FSolver()
        {
            _variables = new Dictionary<string, SolvingNode>();
            _originNodes = new List<SolvingNode>();
            _additionalNodes = new List<SolvingNode>();
        }
        private readonly Dictionary<string,SolvingNode> _variables;
        private readonly List<SolvingNode> _originNodes;
        
        private readonly List<SolvingNode> _additionalNodes;
        private readonly List<OverloadCall> _lazyOverloads = new List<OverloadCall>();

        public SolvingNode GetOrNull(int nodeId)
        {
            if (_originNodes.Count < nodeId)
                return null;
            return _originNodes[nodeId];
        }
        public SolvingNode GetOrCreate(string varId)
        {
            if (_variables.ContainsKey(varId)) 
                return _variables[varId];
            
            var node = new SolvingNode();
            _additionalNodes.Add(node);
            _variables.Add(varId, node);
            return node;

        }
        public SolvingNode GetOrCreate(int nodeid)
        {
            
            while (_originNodes.Count <= nodeid) 
                _originNodes.Add(null);

            if (_originNodes[nodeid] != null) 
                return _originNodes[nodeid];
            
            var solvingNode = new SolvingNode();
            _additionalNodes.Add(solvingNode);
            _originNodes[nodeid] = solvingNode;
            return solvingNode;

        }
        public bool SetVarType(string varId, FType type)
        {
            if (_variables.ContainsKey(varId)) 
                return _variables[varId].SetStrict(type);
            
            var node = SolvingNode.CreateStrict(type.Name, type.Arguments);
            _additionalNodes.Add(node);
            _variables.Add(varId, node);
            return true;
        }
        public bool SetVarType(string varId, SolvingNode node)
        {
            if (_variables.ContainsKey(varId)) 
                return false;
            
            _additionalNodes.Add(node);
            _variables.Add(varId, node);
            return true;
        }
        
        public bool SetVar(int nodeId, string varId)
        {
            var newNode = GetOrCreate(nodeId);

            if (!_variables.ContainsKey(varId))
            {
                _variables[varId] = newNode;
                return true;
            }
            
            var currentNode = _variables[varId];
            //if the variable already has its node
            //Just set equality from nodeId to varId
            return currentNode.SetEqualTo(newNode);
        }
        
        public bool SetCall(CallDef call)
        {
            var generics = new GenericMap();

            for (int i = 0; i < call.nodesId.Length; i++)
            {
                if (!SetStrict(call.nodesId[i], call.Types[i], generics))
                    return false;
            }
            _additionalNodes.AddRange(generics.Nodes);
            return true;
        }
        
        public bool SetLimArgCall(CallDef call)
        {
            var generics = new GenericMap();
            //Set strict to return type
            if (!SetStrict(call.nodesId[0], call.Types[0], generics))
                return false;
            
            //Set limit to arg types
            for (int i = 1; i < call.nodesId.Length; i++)
            {
                if (!SetLimit(call.nodesId[i], call.Types[i], generics))
                    return false;
            }
            //Save result generics
            _additionalNodes.AddRange(generics.Nodes);
            return true;
        }
        
        public bool SetStrict(int nodeId, FType type)
        {
            var generics = new GenericMap();
            var res = SetStrict(nodeId, type, generics);
            _additionalNodes.AddRange(generics.Nodes);
            return res;
        }

        public bool SetLca(int nodeId, int[] dependentNodes)
        {
            var children = dependentNodes.Select(GetOrCreate).ToArray();
            return GetOrCreate(nodeId).SetLca(children);
        }
        public bool SetLimit(int nodeId, NTypeName name)
        {
            return GetOrCreate(nodeId).SetLimit(new FType(name));
        }
        public bool SetNonGenericLimit(int nodeId, FType nonGenericType)
        {
            return GetOrCreate(nodeId).SetLimit(nonGenericType);
        }
        private bool SetStrict(int nodeId, FType type, GenericMap genericsContext)
        {
            if (type is GenericType t)
            {
                var generic = genericsContext.Get(t.GenericId);
                return GetOrCreate(nodeId).SetEqualTo(generic);
            }
            var solvingNode = genericsContext.CreateSolvingNode(type);
            return GetOrCreate(nodeId).SetStrict(solvingNode.MakeType(SolvingNode.MaxTypeDepth));
        }
        private bool SetLimit(int nodeId, FType type, GenericMap genericsContext)
        {
            if (type is GenericType t)
            {
                var generic = genericsContext.Get(t.GenericId);
                return GetOrCreate(nodeId).SetEqualTo(generic);
            }
            var solvingNode = genericsContext.CreateSolvingNode(type);
            return GetOrCreate(nodeId).SetLimit(solvingNode.MakeType(SolvingNode.MaxTypeDepth));
        }
        public bool Unite(int nodeAid, int nodeBid)
        {
            var solvingA = GetOrCreate(nodeAid);
            var solvingB = GetOrCreate(nodeBid);
            return solvingA.SetEqualTo(solvingB);
        }
        public bool Unite(int nodeAid, SolvingNode returnType)
        {
            var solvingA = GetOrCreate(nodeAid);
            return solvingA.SetEqualTo(returnType);
        }
        public NsResult Solve()
        {
            //First: Optimize type graph
            if(!Optimize())
                return NsResult.NotSolvedResult();
            
            //Second: Try append overload calls
            foreach (var lazyOverload in _lazyOverloads)
            {
                var bestCandidate = FindBestCandidate(lazyOverload, out var severalNonStrictFits);
                if(severalNonStrictFits)
                    //More than one candidate fits.
                    return NsResult.NotSolvedResult();

                if(bestCandidate==null)
                    //no candidates fits
                    return NsResult.NotSolvedResult();
                
                //Single function fits. That's good
                SetLimArgCall(bestCandidate.ToCallDefenition(lazyOverload.ReturnNodeId, lazyOverload.ArgIds));
            }
            
            //Third: Job finished!
            return new NsResult(
                _originNodes,
                _additionalNodes,
                _variables);
        }

        private FunSignature FindBestCandidate(OverloadCall lazyOverload, out bool severalNonStrictFits)
        {
            FunSignature bestCandidate = null;
            severalNonStrictFits = false;
            int fitScore = 0;
            foreach (var candidate in lazyOverload.Candidates)
            {
                FitResults candidateFit;
                int candidateScore = 0;
                var returnTypeFit = GetOrCreate(lazyOverload.ReturnNodeId)
                    .Fits(candidate.ReturnType, SolvingNode.MaxTypeDepth);
                if (returnTypeFit == FitResults.Not)
                    continue;
                candidateScore += (int) returnTypeFit;
                candidateFit = returnTypeFit;

                for (int argNum = 0; argNum < lazyOverload.ArgIds.Length; argNum++)
                {
                    var argFit = GetOrCreate(lazyOverload.ArgIds[argNum]).Fits(
                        candidate.ArgTypes[argNum], SolvingNode.MaxTypeDepth);
                    if (argFit == FitResults.Not)
                        continue;
                    if (argFit < candidateFit)
                        candidateFit = argFit;
                    candidateScore += (int) argFit;
                }

                if (candidateFit == FitResults.Strict)
                {
                    severalNonStrictFits = false;
                    return candidate;
                }

                if (candidateFit == FitResults.Converable)
                {
                    if (bestCandidate != null && candidateScore == fitScore)
                        severalNonStrictFits = true;
                    else
                    {
                        if (candidateScore > fitScore)
                        {
                            fitScore = candidateScore;
                            severalNonStrictFits = false;
                            bestCandidate = candidate;
                        }
                    }
                }
            }

            return bestCandidate;
        }


        private bool Optimize()
        {
            
            for (int i = 0; i < 100; i++)
            {
                bool somethingChanged = false;
                foreach (var originNode in _originNodes)
                {
                    if (originNode == null) 
                        continue;
                    if (!originNode.Optimize(out var changed))
                        return false;
                    somethingChanged = somethingChanged || changed;
                }

                if (!somethingChanged)
                    return true;
            }
            return false;
        }
        

        

        public bool SetNode(int nodeId, SolvingNode closured)
        {
            if (_originNodes.Count > nodeId && _originNodes[nodeId] != null)
                return false;
            
            while (_originNodes.Count <= nodeId) 
                _originNodes.Add(null);

            _originNodes[nodeId] = closured;
            return true;
        }

        public void SetLazyOverloadsCall(OverloadCall overloadCall)
        {
            _lazyOverloads.Add(overloadCall);
        }

        public void AddAdditionalType(SolvingNode node)
        {
            _additionalNodes.Add(node);
        }



    }
}