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
        public static VarVal New<T>(string name, T[] value)
        {
            FunTypesConverter.TryGetSpecificConverter(typeof(T[]), out var specificConverter);
            var funValue = specificConverter.ToFunObject(value);
            var funType = specificConverter.FunType;
            return new VarVal(name,funValue,funType);
        }

        public static VarVal New(string name, object value)
        {
            var clrType = value.GetType();
            if (FunTypesConverter.TryGetSpecificConverter(clrType, out var specificConverter))
            {
                var funValue = specificConverter.ToFunObject(value);
                var funType  = specificConverter.FunType;
                return new VarVal(name,funValue,funType);
            }
            else
            {
                var funType = ToPrimitiveFunType(clrType);
                return new VarVal(name, value, funType);
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
            => new VarVal(name, new TextFunArray(value), VarType.Text);
        public static VarVal New(string name, char value)
            => new VarVal(name, value, VarType.Char);
        public static VarVal New(string name, FunnyStruct values, VarType type)
            => new VarVal(name, values, type);

        public static VarVal New(string name, FunnyStruct values)
        {
            var subTypes = new Dictionary<string,VarType>();
            foreach (var field in values.Fields)
            {
                subTypes.Add(field.Key,New("", field.Value).Type);
            }

            return new VarVal(name, values, VarType.StructOf(subTypes));
        }

        public static VarType ToPrimitiveFunType(Type t)
        {
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
            return VarType.Anything;
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