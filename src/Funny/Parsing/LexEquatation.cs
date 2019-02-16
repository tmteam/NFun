namespace Funny.Parsing
{
    public class LexEquation
    {
        public LexEquation(string id, LexNode expression)
        {
            Id = id;
            Expression = expression;
        }
        public string Id { get; }
        public LexNode Expression { get; }
    }
}