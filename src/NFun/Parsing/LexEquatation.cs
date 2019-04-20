namespace NFun.Parsing
{
    public class LexEquation
    {
        public LexEquation(string id, LexNode expression, VarAttribute[] attributes)
        {
            Id = id;
            Expression = expression;
            Attributes = attributes;
        }
        
        public string Id { get; }
        public LexNode Expression { get; }
        public VarAttribute[] Attributes { get; }
    }
}