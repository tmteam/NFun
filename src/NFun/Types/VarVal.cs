using System;
using System.Collections.Generic;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types
{
    /// <summary>
    /// Name type and value of concrete variable 
    /// </summary>
    public struct VarVal
    {
        public readonly string Name;
        public readonly object Value;
        public readonly VarType Type;
        public VarVal(string name, object value, VarType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }
        public override string ToString() => $"{Name}:{Type} = {Value}";
    }
}