using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Funny.Types
{
    public struct Var {

        public static Var New<T>(string name, T[] value)
        {
            var baseType = ToVarType(typeof(T));
            var vartype = VarType.ArrayOf(baseType);
            return new Var(name, value, vartype);
        }
        public static Var New<T>(string name, IEnumerable<T> value)
        {
            var baseType = ToVarType(typeof(T));
            var vartype = VarType.ArrayOf(baseType);
            return new Var(name, value.ToArray(), vartype);
        }
        public static Var NewAny(string name, object value)
            => new Var(name, value, VarType.AnyType);
        public static Var New(string name, object value)
        {
            if (value is int i)
                return New(name, i);
            if (value is double d)
                return New(name, d);
            if (value is bool b)
                return New(name, b);
            if (value is string s)
                return New(name, s);
            
            if (value is IEnumerable<double> arrDbl)
                return New(name, arrDbl);
            if (value is IEnumerable<int> arrInt)
                return New(name, arrInt);
            if (value is IEnumerable<string> arrStr)
                return New(name, arrStr);
            if (value is IEnumerable<bool> arrBool)
                return New(name, arrBool);
            if (value is IEnumerable<object> arrObj)
                return New(name, arrObj);
            return new Var(name, value, VarType.AnyType);
        }
        public static Var New(string name, bool value) 
            => new Var(name, value, VarType.BoolType);
        public static Var New(string name, int value) 
            => new Var(name, value, VarType.IntType);
        public static Var New(string name, double value) 
            => new Var(name, value, VarType.RealType);
        public static Var New(string name, string value) 
            => new Var(name, value, VarType.TextType);

        public  static VarType ToVarType(Type t)
        {
            if (t == typeof(object))
                return VarType.AnyType;
            if (t == typeof(int))
                return VarType.IntType;
            if (t == typeof(double))
                return VarType.RealType;
            if (t == typeof(string))
                return VarType.TextType;
            if (t == typeof(bool))
                return VarType.BoolType;
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