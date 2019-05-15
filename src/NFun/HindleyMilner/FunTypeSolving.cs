using NFun.HindleyMilner.Tyso;
using NFun.Types;

namespace NFun.HindleyMilner
{
    public class FunTypeSolving
    {
        private readonly NsResult _result;

        public FunTypeSolving(NsResult result)
        {
            _result = result;
        }

        public int GenericsCount => _result.GenericsCount;
        public bool IsSolved => _result.IsSolved;

        public VarType GetVarType(string varId, ISolvedTypeConverter converter)
            => converter.ToSimpleType( _result.GetVarType(varId));

        
        public VarType GetNodeTypeOrEmpty(int nodeId, ISolvedTypeConverter converter)
        {
            var hmType = _result.GetNodeTypeOrNull(nodeId);
            if (hmType == null)
                return VarType.Empty;
            return converter.ToSimpleType(hmType);
        }
    }
}