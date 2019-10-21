using NFun.Types;

namespace Nfun.Fuspec.Parser.Model
{
    public class IdType
    {
        public string Id { get; }
        public VarType VarType { get; }
        

        public IdType(string id, VarType varType)
        {
            Id = id;
            VarType = varType;
        }

        public override string ToString()
        {
            return Id+":"+VarType;
        }
    }
}