using NFun.Types;

namespace NFun.Parsing
{
    public class VariableInfo
    {
        public readonly string Id;
        public readonly VarType Type;
        public readonly VarAttribute[] Attributes;


        public VariableInfo(string id, VarType type, VarAttribute[] attributes = null)
        {
            Id = id;
            Type = type;
            Attributes = attributes??new VarAttribute[0];
        }

        public override string ToString() => Id + ":" + Type;
    }
}