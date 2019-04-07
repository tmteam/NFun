using NFun.Types;

namespace NFun.Parsing
{
    public class VariableInfo
    {
        public readonly string Id;
        public readonly VarType Type;

        public VariableInfo(string id, VarType type)
        {
            Id = id;
            Type = type;
        }

        public override string ToString() => Id + ":" + Type;
    }
}