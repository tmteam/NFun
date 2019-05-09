using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class VarDefenitionSyntaxNode : ISyntaxNode
    {
        public string Id { get; }
        public VarType VarType { get; }
        public VarAttribute[] Attributes { get; }

        public VarDefenitionSyntaxNode(string id, VarType varType, VarAttribute[] attributes = null)
        {
            Id = id;
            VarType = varType;
            Attributes = attributes??new VarAttribute[0];

        }
        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.GlobalVarDefenition;
        public Interval Interval { get; set; }
        public override string ToString() => Id + ":" + Type;

    }
}