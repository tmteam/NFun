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

        public VarType GetVarType(string varId)
            => AdpterHelper.ConvertToSimpleTypes( _result.GetVarType(varId));

        public VarType GetNodeType(int nodeId) =>
            AdpterHelper.ConvertToSimpleTypes(_result.GetNodeType(nodeId));
        
        public VarType GetNodeTypeOrEmpty(int nodeId)
        {
            var hmType = _result.GetNodeTypeOrNull(nodeId);
            if (hmType == null)
                return VarType.Empty;
            return AdpterHelper.ConvertToSimpleTypes(hmType);
        }
    }
}