using System;
using System.Linq;

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

        public bool SetFunDefenition(string funId, int funNodeId, int expressionId)
        {
            if (!_solver.SetVar(funNodeId, funId))
                return false;
            var node = _solver.GetOrNull(funId);
            var funType = node.Behavior.MakeType(SolvingNode.MaxTypeDepth);
            if (funType.Name.Id != NTypeName.FunId)
                return false;
            
            if (!_solver.Unite(expressionId, funType.Arguments[0]))
                return false;
            return true;
        }
        

        public bool SetInvoke(int nodeId, string funId,int[] argIds)
        {
            var funDef = _solver.GetOrNull(funId);
            if(funDef==null)
                throw new InvalidOperationException("Fun "+ funId+" not found");
            var funType = funDef.MakeType(SolvingNode.MaxTypeDepth);
            if (!funType.Name.Equals(NTypeName.Function))
                return false;

            if (!_solver.SetNode(nodeId, funType.Arguments[0]))
                return false;
            for (int i = 0; i < argIds.Length; i++) {
                if (!_solver.Unite(argIds[i], funType.Arguments[i+1]))
                    return false;
            }
            return true;
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
            for (int argNum = 0; argNum < argIds.Length; argNum++)
            {
                if (!TrySpecifyArgumentType(candidates, argIds, argNum)) return false;
            }
            //AllOther cases shoud calculates at the end of solving
            _solver.SetLazyOverloadsCall(new OverloadCall(candidates, returnNodeId, argIds));
            return true;
            
        }

        private bool TrySpecifyArgumentType(FunSignature[] candidates, int[] argIds, int argNum)
        {
            bool allEqual = true;
            
            for (int i = 1; i < candidates.Length; i++)
            {
                var type = candidates[i].ArgTypes[argNum];
                if (type.IsNotConcrete || !type.Equals(candidates[i - 1].ArgTypes[argNum]))
                {
                    allEqual = false;
                    break;
                }
            }
            //if input type is same for all overloads and not generic => set it as limit to the type
            if (allEqual)
            {
                if (!_solver.SetNonGenericLimit(argIds[argNum], candidates[0].ArgTypes[argNum]))
                    return false;
            }
            
            //try found base type of all type candidates. 
            int minEnterId = Int32.MaxValue;
            int maxEnterId =0;
            FType winner = null;
            bool hasBaseType = true;
            foreach (var candidate in candidates)
            {
                var type = candidate.ArgTypes[argNum];
                
                if (!type.IsPrimitive || type.IsNotConcrete)
                {
                    hasBaseType = false;
                    break;
                }
                if (type.Name.Start < minEnterId)
                {
                    minEnterId = type.Name.Start;
                    winner = null;
                }
                if (type.Name.Finish > maxEnterId)
                {
                    maxEnterId = type.Name.Finish;
                    winner = null;
                }

                if (type.Name.Start == minEnterId && type.Name.Finish == maxEnterId)
                {
                    winner = type;
                }
            }

            if (hasBaseType && winner != null)
            {
                return _solver.SetNonGenericLimit(argIds[argNum], winner);
            }
            return true;
        }

        public bool InitLambda(int nodeId, int exprId, SolvingNode[] args)
        {
            var outputType= _solver.GetOrCreate(exprId);
            var funType =  SolvingNode.CreateStrict(NTypeName.Function, new[]{outputType}.Concat(args).ToArray());
            if (!_solver.SetNode(nodeId, funType))
                return false;
            if (!_solver.Unite(exprId, outputType))
                return false;
            return true;
        }

        public bool SetStrict(int node, FType type) => _solver.SetStrict(node, type);
        public SolvingNode SetNewVar(string varName)
        {
            var genericType = new SolvingNode();
            if(!_solver.SetVarType(varName, genericType))
                throw new InvalidOperationException("var already declared");
            return genericType;
        }

        public bool SetVar(int nodeId, string varName) => _solver.SetVar(nodeId, varName);
        public bool SetVarType(string varName, FType type) => _solver.SetVarType(varName, type);
        public bool SetDefenition(string variableName, int varId, int exprId)
        {
            return
                _solver.SetVar(varId, variableName) && _solver.SetLca(varId, new[]{exprId});
        }

        public bool HasVariable(string variableName) => _solver.GetOrNull(variableName) != null;
        public SolvingNode GetOrCreate(string variableName) => _solver.GetOrCreate(variableName);

        public bool SetComparationOperator(int nodeId, int leftId, int rightId)
        {
            return 
                _solver.SetLimit(leftId, NTypeName.Real)
                && _solver.SetLimit(rightId, NTypeName.Real)
                && _solver.SetStrict(nodeId, FType.Bool);
        }
        
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
            var leftSet = _solver.SetLimit(leftId, NTypeName.Real);
            var rightSet = _solver.SetLimit(rightId, NTypeName.Real);
            var lcaSet =  _solver.SetLca(nodeId, new[] {leftId, rightId});
            return leftSet && rightSet && lcaSet;
        }
        
        
        public bool SetConst(int nodeId, FType type) 
        {
            return _solver.SetStrict(nodeId, type);
        }

        public bool SetNode(int nodeId, SolvingNode closured) 
            => _solver.SetNode(nodeId, closured);

        public bool SetArrayInit(int arrayNode, params int[] nodes)
        {
            if (nodes.Length == 0)
                return _solver.SetStrict(arrayNode, FType.ArrayOf(FType.Generic(0)));

            for (int i = 1; i < nodes.Length; i++)
            {
                if (!_solver.Unite(nodes[i - 1], nodes[i]))
                    return false;
            }

            var genericType = _solver.GetOrNull(nodes[0]);
            return _solver.SetStrict(arrayNode, FType.ArrayOf(genericType));
        }

        public bool SetProcArrayInit(int nodeId, int fromId, int toId)
        {
            return _solver.SetLimArgCall(new CallDef(
                new[] {FType.ArrayOf(FType.Int32), FType.Int32, FType.Int32}, new[] {nodeId, fromId, toId}));
        }

        public bool SetProcArrayInit(int nodeId, int fromId, int toId, int stepId)
        {

            var limits = _solver.SetLimit(fromId, NTypeName.Real)
                         && _solver.SetLimit(toId, NTypeName.Real)
                         && _solver.SetLimit(stepId, NTypeName.Real);
            if (!limits)
                return false;
            //T0 = Lca(from, to, step)
            var lcaType = SolvingNode.CreateLca(_solver.GetOrNull(fromId), _solver.GetOrNull(toId),
                _solver.GetOrNull(stepId));
            _solver.AddAdditionalType(lcaType);
            //set nodeId as T0[]
            var returnType = SolvingNode.CreateStrict(NTypeName.Array, lcaType);
            _solver.AddAdditionalType(returnType);
            return _solver.Unite(nodeId, returnType);
        }

        public SolvingNode MakeGeneric()
        {
            var res = new SolvingNode();
            _solver.AddAdditionalType(res);
            return res;
        }
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