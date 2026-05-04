namespace NFun.SyntaxParsing;

public class FunnyAttribute {
    internal FunnyAttribute(string name, object value) {
        Value = value;
        Values = System.Array.Empty<object>();
        Name = name;
    }

    internal FunnyAttribute(string name, object[] values) {
        Value = values.Length > 0 ? values[0] : null;
        Values = values;
        Name = name;
    }

    public object Value { get; }
    /// <summary>All argument values for parameterized attributes like @Test(1,2,3).</summary>
    public object[] Values { get; }
    public string Name { get; }
}
