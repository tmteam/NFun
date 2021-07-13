namespace NFun.SyntaxParsing
{
    public class FunnyAttribute
    {
        public FunnyAttribute(string name,object value)
        {
            Value = value;
            Name = name;
        }

        public object Value { get;}
        public string Name { get; }
    }
}