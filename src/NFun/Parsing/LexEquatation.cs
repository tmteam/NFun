namespace NFun.Parsing
{
    public interface ILexRoot{
        string Id { get; }

    }
    public class LexEquation: ILexRoot
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