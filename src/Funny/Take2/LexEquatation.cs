namespace Funny.Take2
{
    public class LexEquatation
    {
        public LexEquatation(string id, LexNode expression)
        {
            Id = id;
            Expression = expression;
        }

        public string Id { get; set; }
        public LexNode Expression { get; set; }
    }
}