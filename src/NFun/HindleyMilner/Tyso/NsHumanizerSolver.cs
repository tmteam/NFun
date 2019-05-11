namespace NFun.HindleyMilner.Tyso
{
    public class NsHumanizerSolver
    {
        private readonly FSolver _solver;

        public NsHumanizerSolver()
        {
            _solver = new FSolver();
        }

        public NsResult Solve() => _solver.Solve();
        
        public bool ApplyIfClause(int nodeId, int[] testNodeIds, int[] thenNodeIds, int elseNodeId)
        {
            //conditions
            foreach (var testNodeId in testNodeIds)
            {
                if (!_solver.SetStrict(testNodeId, FType.Bool))
                    return false;
            }
            
            foreach (var thenNodeId in thenNodeIds)
            {
                if (!_solver.Unite(thenNodeId, elseNodeId))
                    return false;
            }
            
            
            return _solver.Unite(nodeId, elseNodeId);
        }
        public bool ApplyLcaIf(int nodeId, int[] testNodeIds, int[] thenElseNodeIds)
        {
            //conditions
            foreach (var testNodeId in testNodeIds)
            {
                if (!_solver.SetStrict(testNodeId, FType.Bool))
                    return false;
            }

            //var valNodes = thenElseNodeIds.Select(_solver.GetNode) .ToArray();
            return _solver.SetLca(nodeId, thenElseNodeIds);
        }
        /// <summary>
        /// Set call, that limit its args to concrete types
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public bool SetCall(CallDef call) => _solver.SetLimArgCall(call);

        /// <summary>
        /// Set call, that limit its args to concrete types
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public bool SetOverloadCall(FunSignature[] candidates, int returnNodeId, params int[] argIds)
        {
            if (candidates.Length == 1)
                return SetCall(candidates[0].ToCallDefenition(returnNodeId, argIds));

            if (candidates.Length == 0)
                return false;
            //if output type is same for all overloads and not generic => set it as lca to the type
            bool allOutpusEqual = true;
            for (int i = 1; i < candidates.Length; i++)
            {
                var type = candidates[i].ReturnType;
                if (type.IsNotConcrete|| !type.Equals(candidates[i - 1].ReturnType))
                {
                    allOutpusEqual = false;
                    break;
                }
            }

            if (allOutpusEqual) {
                if (!_solver.SetStrict(returnNodeId, candidates[0].ReturnType))
                    return false;
            }
            //if input type is same for all overloads and not generic => set it as limit to the type
            for (int argNum = 0; argNum < argIds.Length; argNum++)
            {
                bool allEqual = true;
                for (int i = 1; i < candidates.Length; i++)
                {
                    var type = candidates[i].ArgTypes[argNum];
                    if (type.IsNotConcrete|| !type.Equals(candidates[i - 1].ArgTypes[argNum]))
                    {
                        allEqual = false;
                        break;
                    }
                }

                if (allEqual) {
                    if (!_solver.SetNonGenericLimit(argIds[argNum], candidates[0].ArgTypes[argNum]))
                        return false;
                }
            }
            //AllOther cases shoud calculates at the end of solving
            _solver.SetLazyOverloadsCall(new OverloadCall(candidates, returnNodeId, argIds));
            return true;
            
        }

        public bool SetStrict(int node, FType type) => _solver.SetStrict(node, type);
        public bool SetVar(int nodeId, string varName) => _solver.SetVar(nodeId, varName);
        public bool SetVarType(string varName, FType type) => _solver.SetVarType(varName, type);
        public bool SetDefenition(string variableName, int varId, int exprId)
        {
            return
                _solver.SetVar(varId, variableName) && _solver.SetLca(varId, new[]{exprId});
        }

        public SolvingNode GetByVar(string variableName) => _solver.GetOrCreate(variableName);

        public bool SetBitwiseOperator(int nodeId, int leftId, int rightId)
        {
            return 
                _solver.SetLimit(leftId, NTypeName.SomeInteger)
                && _solver.SetLimit(rightId, NTypeName.SomeInteger)
                && _solver.SetLca(nodeId, new[] {leftId, rightId});
        }
        
        
        
        public bool SetBitShiftOperator(int nodeId, int leftId, int rightId)
        {
            return 
                _solver.SetLimit(leftId, NTypeName.SomeInteger)
                && _solver.SetLimit(rightId, NTypeName.SomeInteger)
                && _solver.SetLca(nodeId, new[] {leftId, rightId});
        }
        
        public bool SetArithmeticalOp(int nodeId, int leftId, int rightId)
        {
            return 
               _solver.SetLimit(leftId, NTypeName.Real)
            && _solver.SetLimit(rightId, NTypeName.Real)
            && _solver.SetLca(nodeId, new[] {leftId, rightId});
        }
        
        
        public bool SetConst(int nodeId, FType type) 
        {
            return _solver.SetStrict(nodeId, type);
        }

        public bool SetNode(int nodeId, SolvingNode closured) 
            => _solver.SetNode(nodeId, closured);
    }

    public class OverloadCall
    {
        public readonly FunSignature[] Candidates;
        public readonly int ReturnNodeId;
        public readonly int[] ArgIds;

        public OverloadCall(FunSignature[] candidates, int returnNodeId, int[] argIds)
        {
            Candidates = candidates;
            ReturnNodeId = returnNodeId;
            ArgIds = argIds;
        }

    }
}