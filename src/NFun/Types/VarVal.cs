namespace NFun.Types
{
    /// <summary>
    /// Name type and value of concrete variable 
    /// </summary>
    public struct VarVal
    {
        public readonly string Name;
        public readonly object Value;
        public readonly FunnyType Type;
        
        internal VarVal(string name, object value, FunnyType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }
        public override string ToString() => $"{Name}:{Type} = {Value}";
    }
}