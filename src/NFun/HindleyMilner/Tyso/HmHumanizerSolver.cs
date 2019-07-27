using System;
using System.Linq;
using NFun.ParseErrors;

namespace NFun.HindleyMilner.Tyso
{
    public class HmHumanizerSolver
    {
        private readonly HmNodeSolver _solver;

        public HmHumanizerSolver()
        {
            _solver = new HmNodeSolver();
        }

        public HmResult Solve() => _solver.Solve();

        public SetTypeResult SetFunDefenition(string funId, int funNodeId, int expressionId)
        {
            if (!_solver.SetVar(funNodeId, funId))
                return SetTypeResult.Failed(funNodeId, SetTypeResultError.VariableDefenitionDuplicates);
            var node = _solver.GetOrNull(funId);
            var funType = node.Behavior.MakeType(SolvingNode.MaxTypeDepth);
            if (funType.Name.Id != HmTypeName.FunId)
                throw new InvalidOperationException("functional type mismatch");
            
            if (!_solver.Unite(expressionId, funType.Arguments[0]))
                return SetTypeResult.Failed(expressionId, SetTypeResultError.ExpressionTypeIsIncorrect);
            return SetTypeResult.Succesfully;
        }
        

        public bool SetInvoke(int nodeId, string funId,int[] argIds)
        {
            var funDef = _solver.GetOrNull(funId);
            if(funDef==null)
                throw new InvalidOperationException("Fun "+ funId+" not found");
            var funType = funDef.MakeType(SolvingNode.MaxTypeDepth);
            if (!funType.Name.Equals(HmTypeName.Function))
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

        public bool SetOverloadCall(FunSignature[] candidates, int returnNodeId,
            params int[] argIds) => SetOverloadCall(candidates, returnNodeId, argIds, true);
        /// <summary>
        /// Set call, that limit its args to concrete types
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public bool SetOverloadCall(FunSignature[] candidates, int returnNodeId, 
             int[] argIds, bool useForArgLimitation = true )
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
            _solver.AddLazyOverloadsCall(new OverloadCall(candidates, returnNodeId, argIds, useForArgLimitation));
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

        public SetTypeResult InitLambda(int nodeId, int exprId, SolvingNode[] args)
        {
            var outputType= _solver.GetOrCreate(exprId);
            var funType =  SolvingNode.CreateStrict(HmTypeName.Function, new[]{outputType}.Concat(args).ToArray());
            if (!_solver.SetNode(nodeId, funType))
                return SetTypeResult.Failed(nodeId);
            if (!_solver.Unite(exprId, outputType))
                return SetTypeResult.Failed(exprId, SetTypeResultError.ExpressionTypeIsIncorrect);
            return SetTypeResult.Succesfully;
        }

        public bool SetStrict(int node, FType type) => _solver.SetStrict(node, type);
        public SolvingNode SetNewVarOrThrow(string varName)
        {
            var genericType = new SolvingNode();
            if (!_solver.SetVarType(varName, genericType))
                throw FunParseException.ErrorStubToDo("var already declared");
            return genericType;
        }
        
        public SolvingNode SetNewVarOrNull(string varName)
        {
            var genericType = new SolvingNode();
            if (!_solver.SetVarType(varName, genericType))
                return null;
            return genericType;
        }

        public bool SetVar(int nodeId, string varName) => _solver.SetVar(nodeId, varName);
        public bool SetVarType(string varName, FType type) => _solver.SetVarType(varName, type);
        public SetTypeResult SetDefenition(string variableName, int varId, int exprId)
        {
            if(!_solver.SetVar(varId, variableName))
                return SetTypeResult.Failed(exprId, SetTypeResultError.VariableDefenitionDuplicates);
                
            if(!_solver.SetLca(varId, new[]{exprId}))
                return SetTypeResult.Failed(exprId, SetTypeResultError.ExpressionTypeIsIncorrect);
            
            return SetTypeResult.Succesfully;
        }

        public bool HasVariable(string variableName) => _solver.GetOrNull(variableName) != null;
        public SolvingNode GetOrCreate(string variableName) => _solver.GetOrCreate(variableName);

        public SetTypeResult SetComparationOperator(int nodeId, int leftId, int rightId)
        { /*
           if (SetOverloadCall(
                new[]
                {
                    new FunSignature(FType.Bool, FType.Int32, FType.Int32),
                    new FunSignature(FType.Bool, FType.Real, FType.Real),
                    new FunSignature(FType.Bool, FType.Int64, FType.Int64),
                }, nodeId, new []{leftId, rightId}))
               return SetTypeResult.Succesfully;
            return SetTypeResult.Failed(nodeId,SetTypeResultError.ArgumentIsNotANumber);
            */
            
            if(!_solver.SetLimit(leftId, HmTypeName.Real))
                return SetTypeResult.Failed(leftId, SetTypeResultError.ArgumentIsNotANumber);
            if(!_solver.SetLimit(rightId, HmTypeName.Real))
                return SetTypeResult.Failed(leftId, SetTypeResultError.ArgumentIsNotANumber);



            if(!_solver.SetStrict(nodeId, FType.Bool))
                return SetTypeResult.Failed(nodeId, SetTypeResultError.ExpressionTypeIsIncorrect);
                
            return SetTypeResult.Succesfully;
        }
        
        public SetTypeResult SetBitwiseOperator(int nodeId, int leftId, int rightId)
        {
            if(!_solver.SetLimit(leftId, HmTypeName.SomeInteger))
                return SetTypeResult.Failed(leftId, SetTypeResultError.ArgumentIsNotAnInt);
                       
            if(!_solver.SetLimit(rightId, HmTypeName.SomeInteger))
                return SetTypeResult.Failed(rightId, SetTypeResultError.ArgumentIsNotAnInt);
                
            if(!_solver.SetLca(nodeId, new[] {leftId, rightId}))
                return SetTypeResult.Failed(nodeId);
            return SetTypeResult.Succesfully;
        }
        
        public SetTypeResult SetNegateOp(int nodeId, int argNodeId)
        {
            if(!_solver.SetLimit(argNodeId, HmTypeName.Real))
                return SetTypeResult.Failed(argNodeId, SetTypeResultError.ArgumentIsNotANumber);
            if(! _solver.Unite(nodeId, argNodeId))
                return SetTypeResult.Failed(nodeId);
            return SetTypeResult.Succesfully;
        }
        
        public SetTypeResult SetBitShiftOperator(int nodeId, int leftId, int rightId)
        {
            
            if(!_solver.SetLimit(leftId, HmTypeName.SomeInteger))
                return SetTypeResult.Failed(leftId, SetTypeResultError.ArgumentIsNotAnInt);
                       
            if(!_solver.SetLimit(rightId, HmTypeName.SomeInteger))
                return SetTypeResult.Failed(rightId, SetTypeResultError.ArgumentIsNotAnInt);
                
            if(!_solver.SetLca(nodeId, new[] {leftId, rightId}))
                    return SetTypeResult.Failed(nodeId);
            return SetTypeResult.Succesfully;
                
        }
        
        
        
        public SetTypeResult SetArithmeticalOp(int nodeId, int leftId, int rightId)
        {
            if(!_solver.SetLimit(leftId, HmTypeName.Real))
                return SetTypeResult.Failed(leftId, SetTypeResultError.ArgumentIsNotANumber);
            if(!_solver.SetLimit(rightId, HmTypeName.Real))
                return SetTypeResult.Failed(leftId, SetTypeResultError.ArgumentIsNotANumber);
            _solver.SetLca(nodeId, new[]{ 
                _solver.GetOrCreate(leftId),
                _solver.GetOrCreate(rightId), 
                SolvingNode.CreateStrict(FType.UInt16) 
            });
            if(!_solver.SetLca(nodeId, new[] {leftId, rightId}))
                return SetTypeResult.Failed(nodeId, SetTypeResultError.ExpressionTypeIsIncorrect);
            return SetTypeResult.Succesfully;
        }
        
        public SetTypeResult SetArithmeticalWithOverloadsOp(int nodeId, int leftId, int rightId)
        {
            if(!_solver.SetLimit(leftId, HmTypeName.Real))
                return SetTypeResult.Failed(leftId, SetTypeResultError.ArgumentIsNotANumber);
            if(!_solver.SetLimit(rightId, HmTypeName.Real))
                return SetTypeResult.Failed(leftId, SetTypeResultError.ArgumentIsNotANumber);
            
            _solver.SetLca(nodeId, new[]{ 
                _solver.GetOrCreate(leftId),
                _solver.GetOrCreate(rightId), 
                SolvingNode.CreateStrict(FType.UInt16) 
            });
            
            
            //Save possible overloads.
          /*  _solver.AddLazyOverloadsCall(
                    new OverloadCall(
                        new []{
                        new FunSignature(FType.Real, FType.Real, FType.Real),
                        new FunSignature(FType.Int32, FType.Int32, FType.Int32),
                        new FunSignature(FType.Int64, FType.Int64, FType.Int64),
                        new FunSignature(FType.UInt32, FType.UInt32, FType.UInt32),
                        new FunSignature(FType.UInt64, FType.UInt64, FType.UInt64)
                        }, nodeId, new[] {leftId, rightId}, false));
            */
            return SetTypeResult.Succesfully;
        }
        
        
        public bool SetConst(int nodeId, FType type) 
        {
            return _solver.SetStrict(nodeId, type);
        }

        public bool SetNode(int nodeId, SolvingNode closured) 
            => _solver.SetNode(nodeId, closured);

        public SetTypeResult SetArrayInit(int arrayNode, params int[] nodes)
        {
            if (nodes.Length == 0)
            {
                if(_solver.SetStrict(arrayNode, FType.ArrayOf(FType.Generic(0))))
                    return SetTypeResult.Succesfully;
                
                return SetTypeResult.Failed(arrayNode, SetTypeResultError.ExpressionTypeIsIncorrect);
            }

            for (int i = 1; i < nodes.Length; i++)
            {
                if (!_solver.Unite(nodes[i - 1], nodes[i]))
                    return SetTypeResult.Failed(nodes[i], SetTypeResultError.IncorrectVariableType);
            }

            var genericType = _solver.GetOrNull(nodes[0]);
            if(_solver.SetStrict(arrayNode, FType.ArrayOf(genericType)))
                return  SetTypeResult.Succesfully;
            return SetTypeResult.Failed(arrayNode, SetTypeResultError.ExpressionTypeIsIncorrect);
        }
        
        public bool SetProcArrayInit(int nodeId, int fromId, int toId)
        {
            return _solver.SetLimArgCall(new CallDef(
                new[] {FType.ArrayOf(FType.Int32), FType.Int32, FType.Int32}, new[] {nodeId, fromId, toId}));
        }

        public bool SetProcArrayInit(int nodeId, int fromId, int toId, int stepId)
        {

            var limits = _solver.SetLimit(fromId, HmTypeName.Real)
                         && _solver.SetLimit(toId, HmTypeName.Real)
                         && _solver.SetLimit(stepId, HmTypeName.Real);
            if (!limits)
                return false;
            //T0 = Lca(from, to, step)
            var lcaType = SolvingNode.CreateLca(_solver.GetOrNull(fromId), _solver.GetOrNull(toId),
                _solver.GetOrNull(stepId));
            _solver.AddAdditionalType(lcaType);
            //set nodeId as T0[]
            var returnType = SolvingNode.CreateStrict(HmTypeName.Array, lcaType);
            _solver.AddAdditionalType(returnType);
            return _solver.Unite(nodeId, returnType);
        }

        public SolvingNode MakeGeneric()
        {
            var res = new SolvingNode();
            _solver.AddAdditionalType(res);
            return res;
        }


        public FType GetOrNull(string funAlias)
        {
            var type =  _solver.GetOrNull(funAlias);
            return type?.MakeType();
            
        }

        public bool SetLcaConst(int nodeOrderNumber, FType type)
        {
            return _solver.SetLca(nodeOrderNumber, new []{SolvingNode.CreateStrict(type)});
        }
    }

    public class OverloadCall
    {
        public readonly bool UseForArgLimitation;
        public readonly FunSignature[] Candidates;
        public readonly int ReturnNodeId;
        public readonly int[] ArgIds;

        public OverloadCall(FunSignature[] candidates, int returnNodeId, int[] argIds, bool useForArgLimitation = true)
        {
            UseForArgLimitation = useForArgLimitation;
            Candidates = candidates;
            ReturnNodeId = returnNodeId;
            ArgIds = argIds;
        }

    }

    public enum SetTypeResultError
    {
        NoError=0, 
        ExpressionTypeIsIncorrect = 1,
        VariableDefenitionDuplicates = 2,

        IncorrectVariableType,
        ArgumentIsNotANumber,
        ArgumentIsNotAnInt
    }
    public struct SetTypeResult
    {
        public static readonly SetTypeResult Succesfully = new SetTypeResult(isSuccesfully:true);
        public static SetTypeResult Failed(int nodeId,SetTypeResultError error = SetTypeResultError.ExpressionTypeIsIncorrect) => new SetTypeResult(nodeId,error);

        private SetTypeResult(bool isSuccesfully)
        {
            IsSuccesfully = isSuccesfully;
            FailedNodeId = -1;
            Error = SetTypeResultError.NoError;
        }
        public SetTypeResult(int failedNodeId, SetTypeResultError error)
        {
            IsSuccesfully = false;
            FailedNodeId = failedNodeId;
            Error = error;
        }
        public readonly bool IsSuccesfully;
        public readonly int FailedNodeId;
        public readonly SetTypeResultError Error;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(SetTypeResult other)
        {
            return IsSuccesfully == other.IsSuccesfully && FailedNodeId == other.FailedNodeId && Error == other.Error;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IsSuccesfully.GetHashCode();
                hashCode = (hashCode * 397) ^ FailedNodeId;
                hashCode = (hashCode * 397) ^ (int) Error;
                return hashCode;
            }
        }
    }
}