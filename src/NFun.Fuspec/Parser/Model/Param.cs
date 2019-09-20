using NFun.Types;

namespace Nfun.Fuspec.Parser.Model
{
    public class Param
    {
        public string Value { get; }
        public VarType VarType { get; }
        

        public Param(string value, VarType varType)
        {
            Value = value;
            VarType = varType;
            
        }
    }
}