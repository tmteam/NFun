namespace NFun.Parsing
{
    public class VarAttribute
    {
        public VarAttribute(string name,object value)
        {
            Value = value;
            Name = name;
        }

        public object Value { get;}
        public string Name { get; }
    }
}