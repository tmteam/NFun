using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Runtime;

namespace NFun.Types
{
    public struct Var {

        public static Var New<T>(string name, T[] value)
        {
            var baseType = ToVarType(typeof(T));
            var vartype = VarType.ArrayOf(baseType);
            return new Var(name, new FunArray(value), vartype);
        }
        public static Var New<T>(string name, IEnumerable<T> value)
        {
            var baseType = ToVarType(typeof(T));
            var vartype = VarType.ArrayOf(baseType);
            if (value is IFunArray a)
                return  new Var(name, a, vartype);
            else
                return new Var(name, new FunArray(value.ToArray()), vartype);
        }
        public static Var New(string name, object value)
        {
            if (value is int i)
                return New(name, i);
            if(value is long l)
                return New(name, l);
            if (value is double d)
                return New(name, d);
            if (value is bool b)
                return New(name, b);
            if (value is string s)
                return New(name, s);
            if (value is char c)
                return New(name, c);
            if (value is IEnumerable<double> arrDbl)
                return New(name, arrDbl);
            if (value is IEnumerable<long> arrLong)
                return New(name, arrLong);
            if (value is IEnumerable<int> arrInt)
                return New(name, arrInt);
            if (value is IEnumerable<string> arrStr)
                return New(name, arrStr);
            if (value is IEnumerable<bool> arrBool)
                return New(name, arrBool);
            if (value is IEnumerable<object> arrObj)
                return New(name, arrObj);
            return new Var(name, value, VarType.Anything);
        }
        public static Var New(string name, bool value) 
            => new Var(name, value, VarType.Bool);
        public static Var New(string name, int value) 
            => new Var(name, value, VarType.Int32);
        public static Var New(string name, long value) 
            => new Var(name, value, VarType.Int64);
        public static Var New(string name, double value) 
            => new Var(name, value, VarType.Real);
        public static Var New(string name, string value) 
            => new Var(name, value, VarType.Text);
        public static Var New(string name, char value)
            => new Var(name, value, VarType.Char);
        public  static VarType ToVarType(Type t)
        {
            if (t == typeof(object))
                return VarType.Anything;
            if (t == typeof(int))
                return VarType.Int32;
            if (t== typeof(long))    
                return VarType.Int64;
            if (t == typeof(double))
                return VarType.Real;
            if (t == typeof(string))
                return VarType.Text;
            if (t == typeof(bool))
                return VarType.Bool;
            if(t== typeof(char))
                return VarType.Char;
            if (t.IsArray)
            {
                var eType = t.GetElementType();
                return VarType.ArrayOf(ToVarType(eType));
            }
            throw new ArgumentException($"Type {t.Name} is not supported");
        }
        public readonly string Name;
        public readonly object Value;
        public readonly VarType Type;
        
        public Var(string name, object value, VarType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        public override string ToString()
        {
            return $"{Name}: {Value} of type {Type}";
        }
    }
}