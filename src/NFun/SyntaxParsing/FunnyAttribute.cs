namespace NFun.SyntaxParsing; 

public class FunnyAttribute {
    internal FunnyAttribute(string name, object value) {
        Value = value;
        Name = name;
    }

    public object Value { get; }
    public string Name { get; }
}