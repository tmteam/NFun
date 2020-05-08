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
    public struct VarVal {

        public static VarVal New<T>(string name, T[] value)
        {
            var baseType = ToVarType(typeof(T));
            var vartype = VarType.ArrayOf(baseType);
            return new VarVal(name, new ImmutableFunArray(value), vartype);
        }
        public static VarVal New<T>(string name, IEnumerable<T> value)
        {
            var baseType = ToVarType(typeof(T));
            var vartype = VarType.ArrayOf(baseType);
            if (value is IFunArray a)
                return  new VarVal(name, a, vartype);
            else
                return new VarVal(name, new ImmutableFunArray(value.ToArray()), vartype);
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
                case IEnumerable<double> arrDbl:
                    return New(name, arrDbl);
                //place signed first because ex: int[] fits to signed and unsigned   
                //case IEnumerable<sbyte> arrI8:
                //    return New(name, arrI8);
                case IEnumerable<short> arrI16:
                    return New(name, arrI16);
                case IEnumerable<int> arrI32:
                    return New(name, arrI32);
                case IEnumerable<long> arrI64:
                    return New(name, arrI64);
                //place unsigned after signed
                case IEnumerable<byte> arrUI8:
                    return New(name, arrUI8);
                case IEnumerable<ushort> arrUi16:
                    return New(name, arrUi16);
                case IEnumerable<uint> arrUi32:
                    return New(name, arrUi32);
                case IEnumerable<ulong> arrUi64:
                    return New(name, arrUi64);
                
                case IEnumerable<string> arrStr:
                    return New(name, arrStr);
                case IEnumerable<bool> arrBool:
                    return New(name, arrBool);
                case IEnumerable<object> arrObj:
                    return New(name, arrObj);
                default:
                    return new VarVal(name, value, VarType.Anything);
            }
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
            => new VarVal(name, value, VarType.Text);
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
        
        public VarVal(string name, object value, VarType type)
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