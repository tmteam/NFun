using NFun.Tokenization;

namespace NFun.Parsing
{
    public class EquationSyntaxNode : ISyntaxNode
    {
        public string Id { get; }
        public ISyntaxNode Expression { get; }
        public VarAttribute[] Attributes { get; }

        public EquationSyntaxNode(string id, int start, ISyntaxNode expression, VarAttribute[] attributes)
        {
            Id = id;
            Expression = expression;
            Attributes = attributes;
            IsBracket = false;
            Interval = Interval.New(start, expression.Interval.Finish);
        }
        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.Equation;
        public Interval Interval { get; set; }
    }
}