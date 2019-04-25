using NFun.Types;

namespace NFun.Parsing
{
    public class LexVarDefenition: ILexRoot
    {
        public string Id { get; }
        public readonly VarType Type;
        public readonly VarAttribute[] Attributes;
        

        public LexVarDefenition(string id, VarType type, VarAttribute[] attributes = null)
        {
            Id = id;
            Type = type;
            Attributes = attributes??new VarAttribute[0];
        }

        public override string ToString() => Id + ":" + Type;
    }
}