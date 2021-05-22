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
            var converter =FunnyTypeConverters.GetInputConverter(typeof(T[]));
            var funValue = converter.ToFunObject(value);
            var funType = converter.FunnyType;
            return new VarVal(name,funValue,funType);
        }

        public static VarVal New(string name, object value)
        {
            var clrType = value.GetType();

            var converter = FunnyTypeConverters.GetInputConverter(clrType);
            return new VarVal(name, converter.ToFunObject(value), converter.FunnyType);
        }
        public static VarVal New(string name, bool value) 
            => new(name, value, VarType.Bool);

        public static VarVal New(string name, int value) 
            => new(name, value, VarType.Int32);

        public static VarVal New(string name, byte value) 
            => new(name, value, VarType.UInt8);
        public static VarVal New(string name, ushort value) 
            => new(name, value, VarType.UInt16);
        public static VarVal New(string name, uint value) 
            => new(name, value, VarType.UInt32);
        public static VarVal New(string name, ulong value) 
            => new(name, value, VarType.UInt64);
        public static VarVal New(string name, double value) 
            => new(name, value, VarType.Real);
        public static VarVal New(string name, string value) 
            => new(name, new TextFunArray(value), VarType.Text);
        public static VarVal New(string name, char value)
            => new(name, value, VarType.Char);
        public static VarVal New(string name, FunnyStruct values, VarType type)
            => new(name, values, type);
        
        public readonly string Name;
        public readonly object Value;
        public readonly VarType Type;

        public VarVal(string name, object value, VarType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        public override string ToString() => $"{Name}: {Value} of type {Type}";
    }
}