using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class ConvertFunction : GenericFunctionBase
    {
        public ConvertFunction() : base(
            "convert",
            new[] {GenericConstrains.Any, GenericConstrains.Any},
            VarType.Generic(0), VarType.Generic(1))
        {
        }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            var from = concreteTypes[1];
            var to = concreteTypes[0];
            if (to== VarType.Anything || from == to)
                return new ConcreteConverter(o=>o, from, to);
            if(to == VarType.Text) 
                return new ConcreteConverter(o => o.ToString(), from, to);
            var safeConverter = VarTypeConverter.GetConverterOrNull(from, to);
            if (safeConverter != null)
                return new ConcreteConverter(safeConverter, from, to);
            
            if (to.ArrayTypeSpecification?.VarType == VarType.UInt8)
            {
                var serializer = CreateSerializerOrNull(from);
                if(serializer!=null)
                    return new ConcreteConverter(serializer, from, to);
            }
            else if (to.ArrayTypeSpecification?.VarType == VarType.Bool)
            {
                var serializer = CreateBinarizerOrNull(from);
                
                if (serializer != null)
                    return new ConcreteConverter(serializer, from, to);
            }
            
            if (from.ArrayTypeSpecification?.VarType == VarType.UInt8)
            {
                var deserializer = CreateDeserializerOrNull(to);
                if(deserializer!=null)
                    return new ConcreteConverter(deserializer, from, to);
            }
            else if (from.IsText)
            {
                var parser =CreateParserOrNull(to);
                if(parser!=null)
                    return new ConcreteConverter(parser, from, to);
            }

            throw FunParseException.ErrorStubToDo($"Impossible explicit convertation {from}->{to}");

        }
        private Func<object, object> CreateBinarizerOrNull(VarType from)
        {
            switch (from.BaseType)
            {
                case BaseVarType.Char:   return o => ToBoolArray(new[] { (byte)(char)o });
                case BaseVarType.Bool:   return o => new ImmutableFunArray(new []{(bool)o });
                case BaseVarType.UInt8:  return o => ToBoolArray(new[] { (byte)o });
                case BaseVarType.UInt16: return o => ToBoolArray(BitConverter.GetBytes((ushort)o));
                case BaseVarType.UInt32: return o => ToBoolArray(BitConverter.GetBytes((uint)o));
                case BaseVarType.UInt64: return o => ToBoolArray(BitConverter.GetBytes((long)o));
                case BaseVarType.Int16:  return o => ToBoolArray(BitConverter.GetBytes((short)o));
                case BaseVarType.Int32:  return o => ToBoolArray(BitConverter.GetBytes((int)o));
                case BaseVarType.Int64:  return o => ToBoolArray(BitConverter.GetBytes((long)o));
                case BaseVarType.Real:   return o => ToBoolArray(BitConverter.GetBytes((double)o));
            }

            if (from.IsText)
                return o => ToBoolArray(Encoding.Unicode.GetBytes(TypeHelper.GetTextOrThrow(o)));
            return null;
        }

        private ImmutableFunArray ToBoolArray(byte[] array)
        {
            var bitArray = new BitArray(array);
            var arr = new bool[bitArray.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = bitArray[i];
            }
            return new ImmutableFunArray(arr);
        }


        private Func<object, object> CreateSerializerOrNull(VarType from)
        {
            switch (from.BaseType)
            {
                case BaseVarType.Char: return o => new ImmutableFunArray(new[] {(byte) (char) o});
                case BaseVarType.Bool: return o => new ImmutableFunArray(new[] {(byte) ((bool) o ? 1 : 0)});
                case BaseVarType.UInt8: return o => new ImmutableFunArray(new[] {(byte) o});
                case BaseVarType.UInt16: return o => new ImmutableFunArray(BitConverter.GetBytes((ushort) o));
                case BaseVarType.UInt32: return o => new ImmutableFunArray(BitConverter.GetBytes((uint) o));
                case BaseVarType.UInt64: return o => new ImmutableFunArray(BitConverter.GetBytes((long) o));
                case BaseVarType.Int16: return o => new ImmutableFunArray(BitConverter.GetBytes((short) o));
                case BaseVarType.Int32: return o => new ImmutableFunArray(BitConverter.GetBytes((int) o));
                case BaseVarType.Int64: return o => new ImmutableFunArray(BitConverter.GetBytes((long) o));
                case BaseVarType.Real: return o => new ImmutableFunArray(BitConverter.GetBytes((double) o));
            }

            if (from.IsText)
                return o => new ImmutableFunArray(Encoding.Unicode.GetBytes(TypeHelper.GetTextOrThrow(o)));
            return null;
        }

        private Func<object, object> CreateParserOrNull(VarType to)
        {
            switch (to.BaseType)
            {
                case BaseVarType.Char: return o =>  char.Parse(TypeHelper.GetTextOrThrow(0));
                case BaseVarType.Bool: return o =>
                {
                    var str =TypeHelper.GetTextOrThrow(o);
                    if (string.Equals(str, "true", StringComparison.OrdinalIgnoreCase))  return true;
                    if (string.Equals(str, "1", StringComparison.OrdinalIgnoreCase))     return true;
                    if (string.Equals(str, "false", StringComparison.OrdinalIgnoreCase)) return false;
                    if (string.Equals(str, "0", StringComparison.Ordinal))               return false;
                    return null;
                };
                case BaseVarType.UInt8: return o => byte.Parse(TypeHelper.GetTextOrThrow(o));
                case BaseVarType.UInt16: return o => ushort.Parse(TypeHelper.GetTextOrThrow(o));
                case BaseVarType.UInt32: return o => UInt32.Parse(TypeHelper.GetTextOrThrow(o));
                case BaseVarType.UInt64: return o => UInt64.Parse(TypeHelper.GetTextOrThrow(o));
                case BaseVarType.Int16: return o => UInt16.Parse(TypeHelper.GetTextOrThrow(o));
                case BaseVarType.Int32: return o => Int32.Parse(TypeHelper.GetTextOrThrow(o));
                case BaseVarType.Int64: return o => Int64.Parse(TypeHelper.GetTextOrThrow(o));
                case BaseVarType.Real: return o => double.Parse(TypeHelper.GetTextOrThrow(o), CultureInfo.InvariantCulture);
            }

            return null;
        }
        private Func<object, object> CreateDeserializerOrNull(VarType to)
        {
            switch (to.BaseType)
            {
                case BaseVarType.Char:   return o =>  (char)GetArrayOfSize(o,1)[0];
                case BaseVarType.Bool:   return o => GetArrayOfSize(o, 1)[0]==1;
                case BaseVarType.UInt8:  return o => GetArrayOfSize(o, 1)[0];
                case BaseVarType.UInt16: return o => BitConverter.ToUInt16(GetArrayOfSize(o,2),0);
                case BaseVarType.UInt32: return o => BitConverter.ToUInt32(GetArrayOfSize(o, 4), 0);
                case BaseVarType.UInt64: return o => BitConverter.ToUInt64(GetArrayOfSize(o, 8), 0);
                case BaseVarType.Int16:  return o => BitConverter.ToInt16(GetArrayOfSize(o, 2), 0);
                case BaseVarType.Int32:  return o => BitConverter.ToInt32(GetArrayOfSize(o, 4), 0);
                case BaseVarType.Int64:  return o => BitConverter.ToInt64(GetArrayOfSize(o, 8), 0);
                case BaseVarType.Real:   return o => BitConverter.ToDouble(GetArrayOfSize(o, 8), 0);
            }

            if (to.IsText)
                return o => new ImmutableFunArray(Encoding.Unicode.GetBytes(TypeHelper.GetTextOrThrow(o)));
            
            return null;
        }
        class ConcreteConverter:FunctionWithManyArguments
        {
            private readonly Func<object, object> _converter;

            public ConcreteConverter(Func<object, object> converter, VarType from, VarType to): base("convert", to, from)
            {
                _converter = converter;
            }
            public override object Calc(object[] args)
            {
                try
                {
                    return _converter(args[0]);
                }
                catch (Exception e)
                {
                    throw new FunRuntimeException($"Cannot convert {args[0]} to type {this.ReturnType}", e);
                }
            }
        }

        private static byte[] GetArrayOfSize(object array, int size)
        {
            var val = (IFunArray) array;
            try
            {
                if (val.Count > 4)
                    throw new FunRuntimeException("Array is too long");
                if (val.Count == 4)
                {
                    return val.Select(Convert.ToByte).ToArray();
                }
                
                return val.Concat(new int[4 - val.Count].Cast<object>()).Select(Convert.ToByte).ToArray();
            }
            catch (Exception e)
            {
                throw new FunRuntimeException($"Array '{val}' cannot be converted into int", e);
            }
        }
    }
}
