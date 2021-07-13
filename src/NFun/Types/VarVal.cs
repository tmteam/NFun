namespace NFun.Types
{
    /// <summary>
    /// Name type and value of concrete variable 
    /// </summary>
    public readonly struct VarVal
    {
        public readonly string Name;
        public readonly object Value;
        public readonly FunnyType Type;
        
        public VarVal(string name, object value, FunnyType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }
        public override string ToString() => $"{Name}:{Type} = {Value}";
    }
}