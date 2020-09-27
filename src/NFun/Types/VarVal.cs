using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types
{
    /// <summary>
    /// Name type and value of concrete variable 
    /// </summary>
    public struct VarVal
    {
        public static VarVal New<T>(string name, T[] value)
        {
            var baseType = ToVarType(typeof(T));
            var vartype = VarType.ArrayOf(baseType);
            return new VarVal(name, new ImmutableFunArray(value, baseType), vartype);
        }
        public static VarVal New<T>(string name, IEnumerable<T> value)
        {
            var baseType = ToVarType(typeof(T));
            var vartype = VarType.ArrayOf(baseType);
            if (value is IFunArray a)
                return  new VarVal(name, a, vartype);
            else
                return new VarVal(name, new ImmutableFunArray(value.ToArray(),baseType), vartype);
        }

        public static VarVal New(string name, object value)
        {
            switch (value)
            {
                case byte ui8:
                    return New(name, ui8);
                case ushort ui16:
                    return New(name, ui16);
                case uint ui32:
                    return New(name, ui32);
                case ulong ui64:
                    return New(name, ui64);
                case sbyte i8:
                    return New(name, i8);
                case short i16:
                    return New(name, i16);
                case int i32:
                    return New(name, i32);
                case long i64:
                    return New(name, i64);
                case double d:
                    return New(name, d);
                case bool b:
                    return New(name, b);
                case string s:
                    return New(name, s);
                case char c:
                    return New(name, c);
                case double[] arrDbl:
                    return New(name, arrDbl);
                case string[] arrStr:
                    return New(name, arrStr);
                case bool[]  arrBool:
                    return New(name, arrBool);
                case IFunArray funArray:
                    return New(name, funArray);
            }

            var type = value.GetType();
            if(type== typeof(short[]))
                return New(name, (short[])value);
            if (type == typeof(int[]))
                return New(name, (int[])value);
            if (type == typeof(long[]))
                return New(name, (long[])value);
            if (type == typeof(byte[]))
                return New(name, (byte[])value);
            if (type == typeof(ushort[]))
                return New(name, (ushort[])value);
            if (type == typeof(uint[]))
                return New(name, (uint[])value);
            if (type == typeof(ulong[]))
                return New(name, (uint[])value);
            if (type == typeof(object[]))
                return New(name, (object[])value);
            return new VarVal(name, value, VarType.Anything);
        }
        public static VarVal New(string name, bool value) 
            => new VarVal(name, value, VarType.Bool);
        public static VarVal New(string name, short value) 
            => new VarVal(name, value, VarType.Int16);
        public static VarVal New(string name, int value) 
            => new VarVal(name, value, VarType.Int32);
        public static VarVal New(string name, long value) 
            => new VarVal(name, value, VarType.Int64);
        
        public static VarVal New(string name, byte value) 
            => new VarVal(name, value, VarType.UInt8);
        public static VarVal New(string name, ushort value) 
            => new VarVal(name, value, VarType.UInt16);
        public static VarVal New(string name, uint value) 
            => new VarVal(name, value, VarType.UInt32);
        public static VarVal New(string name, ulong value) 
            => new VarVal(name, value, VarType.UInt64);
        public static VarVal New(string name, double value) 
            => new VarVal(name, value, VarType.Real);
        public static VarVal New(string name, string value) 
            => new VarVal(name, new TextFunArray(value), VarType.Text);
        public static VarVal New(string name, char value)
            => new VarVal(name, value, VarType.Char);
        public  static VarType ToVarType(Type t)
        {
            if (t == typeof(object))
                return VarType.Anything;
            if(t== typeof(byte))
                return VarType.UInt8;
            if(t== typeof(ushort))
                return VarType.UInt16;
            if (t == typeof(uint))
                return VarType.UInt32;
            if (t== typeof(ulong))    
                return VarType.UInt64;
            if(t== typeof(short))
                return VarType.Int16;
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
            if (t== typeof(char))
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
        public bool IsEmpty => this.Name == null && this.Type == VarType.Empty;

        public VarVal(string name, object value, VarType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        public override string ToString() => $"{Name}: {Value} of type {Type}";
    }
}