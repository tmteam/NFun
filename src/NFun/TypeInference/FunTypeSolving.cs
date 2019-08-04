using System.Linq;
using NFun.TypeInference.Solving;
using NFun.Types;

namespace NFun.HindleyMilner
{
    public class FunTypeSolving
    {
        private readonly HmResult _result;
        public FunTypeSolving(HmResult result)
        {
            _result = result;
        }

        public int GenericsCount => _result.GenericsCount;
        public bool IsSolved => _result.IsSolved;

        public VarType GetVarType(string varId, SolvedTypeConverter converter)
            => converter.ToSimpleType( _result.GetVarType(varId));

        public FunFunctionSignature GetFunctionOverload(int nodeId,SolvedTypeConverter converter)
        {
            var overloadHmSignature = _result.GetFunctionOverload(nodeId);
            if (overloadHmSignature == null)
                return null;
            return new FunFunctionSignature(
                converter.ToSimpleType(overloadHmSignature.ReturnType), 
                overloadHmSignature.ArgTypes.Select(o=>converter.ToSimpleType(o)).ToArray());
        }
        
        public VarType GetNodeTypeOrEmpty(int nodeId, SolvedTypeConverter converter)
        {
            var hmType = _result.GetNodeTypeOrNull(nodeId);
            if (hmType == null)
                return VarType.Empty;
            return converter.ToSimpleType(hmType);
        }
    }
    public class FunFunctionSignature{
               public readonly VarType ReturnType;
               public readonly VarType[] ArgTypes;
               public FunFunctionSignature(VarType output, VarType[] inputs)
               {
                   ReturnType = output;
                   ArgTypes = inputs;
               }
       }
}