using System;

namespace Funny.Runtime
{
    public struct Var {
        
        
        public static Var New(string name, bool value) 
            => new Var(name, value, VarType.BoolType);
        public static Var New(string name, int value) 
            => new Var(name, value, VarType.IntType);
        public static Var New(string name, double value) 
            => new Var(name, value, VarType.NumberType);
      
        public readonly string Name;
        public readonly object Value;
        public readonly VarType Type;
        public Var(string name, object value, VarType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }
    }
}