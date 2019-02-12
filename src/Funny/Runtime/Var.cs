using System;

namespace Funny.Runtime
{
    public struct Var {
        
        public static Var Number(string name, double value) 
            => new Var(name, value, VarType.NumberType);
      
        public readonly string Name;
        public readonly double Value;
        public readonly VarType Type;
        public Var(string name, object value, VarType type)
        {
            Name = name;
            Value = Convert.ToDouble(value);
            Type = type;
        }
    }
}