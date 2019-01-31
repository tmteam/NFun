namespace Funny.Parsing
{
    public class LexEquatation
    {
        public LexEquatation(string id, LexNode expression)
        {
            Id = id;
            Expression = expression;
        }

        public string Id { get; }
        public LexNode Expression { get; }
    }
}