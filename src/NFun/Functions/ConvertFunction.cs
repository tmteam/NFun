using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions
{
    public class ConvertFunction : GenericFunctionBase
    {
        public ConvertFunction() : base(
            "convert",
            new[] {GenericConstrains.Any, GenericConstrains.Any},
            FunnyType.Generic(0), FunnyType.Generic(1))
        {
        }

        public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes)
        {
            var from = concreteTypes[1];
            var to = concreteTypes[0];
            if (to== FunnyType.Any || from == to)
                return new ConcreteConverter(o=>o, from, to);
            if(to == FunnyType.Text) 
                return new ConcreteConverter(o => o.ToString(), from, to);
            var safeConverter = VarTypeConverter.GetConverterOrNull(from, to);
            if (safeConverter != null)
                return new ConcreteConverter(safeConverter, from, to);
            
            if (to.ArrayTypeSpecification?.FunnyType == FunnyType.UInt8)
            {
                var serializer = CreateSerializerOrNull(from);
                if(serializer!=null)
                    return new ConcreteConverter(serializer, from, to);
            }
            else if (to.ArrayTypeSpecification?.FunnyType == FunnyType.Bool)
            {
                var serializer = CreateBinarizerOrNull(from);
                
                if (serializer != null)
                    return new ConcreteConverter(serializer, from, to);
            }
            
            if (from.ArrayTypeSpecification?.FunnyType == FunnyType.UInt8)
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

            throw FunnyParseException.ErrorStubToDo($"Impossible explicit convertation {from}->{to}");

        }
        private Func<object, object> CreateBinarizerOrNull(FunnyType from)
        {
            switch (from.BaseType)
            {
                case BaseFunnyType.Char:   return o => ToBoolArray(new[] { (byte)(char)o });
                case BaseFunnyType.Bool:   return o => new ImmutableFunnyArray(new []{(bool)o });
                case BaseFunnyType.UInt8:  return o => ToBoolArray(new[] { (byte)o });
                case BaseFunnyType.UInt16: return o => ToBoolArray(BitConverter.GetBytes((ushort)o));
                case BaseFunnyType.UInt32: return o => ToBoolArray(BitConverter.GetBytes((uint)o));
                case BaseFunnyType.UInt64: return o => ToBoolArray(BitConverter.GetBytes((long)o));
                case BaseFunnyType.Int16:  return o => ToBoolArray(BitConverter.GetBytes((short)o));
                case BaseFunnyType.Int32:  return o => ToBoolArray(BitConverter.GetBytes((int)o));
                case BaseFunnyType.Int64:  return o => ToBoolArray(BitConverter.GetBytes((long)o));
                case BaseFunnyType.Real:   return o => ToBoolArray(BitConverter.GetBytes((double)o));
            }

            if (from.IsText)
                return o => ToBoolArray(Encoding.Unicode.GetBytes(((IFunnyArray)o).ToText()));
            return null;
        }

        private static ImmutableFunnyArray ToBoolArray(byte[] array)
        {
            var bitArray = new BitArray(array);
            var arr = new bool[bitArray.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = bitArray[i];
            }
            return new ImmutableFunnyArray(arr);
        }


        private static Func<object, object> CreateSerializerOrNull(FunnyType from)
        {
            switch (from.BaseType)
            {
                case BaseFunnyType.Char: return o => new ImmutableFunnyArray(new[] {(byte) (char) o});
                case BaseFunnyType.Bool: return o => new ImmutableFunnyArray(new[] {(byte) ((bool) o ? 1 : 0)});
                case BaseFunnyType.UInt8: return o => new ImmutableFunnyArray(new[] {(byte) o});
                case BaseFunnyType.UInt16: return o => new ImmutableFunnyArray(BitConverter.GetBytes((ushort) o));
                case BaseFunnyType.UInt32: return o => new ImmutableFunnyArray(BitConverter.GetBytes((uint) o));
                case BaseFunnyType.UInt64: return o => new ImmutableFunnyArray(BitConverter.GetBytes((long) o));
                case BaseFunnyType.Int16: return o => new ImmutableFunnyArray(BitConverter.GetBytes((short) o));
                case BaseFunnyType.Int32: return o => new ImmutableFunnyArray(BitConverter.GetBytes((int) o));
                case BaseFunnyType.Int64: return o => new ImmutableFunnyArray(BitConverter.GetBytes((long) o));
                case BaseFunnyType.Real: return o => new ImmutableFunnyArray(BitConverter.GetBytes((double) o));
            }

            if (from.IsText)
                return o => new ImmutableFunnyArray(Encoding.Unicode.GetBytes(((IFunnyArray)o).ToText()));
            return null;
        }

        private static Func<object, object> CreateParserOrNull(FunnyType to) =>
            to.BaseType switch
            {
                BaseFunnyType.Bool => o =>
                {
                    var str = ((IFunnyArray) o).ToText();
                    if (string.Equals(str, "true", StringComparison.OrdinalIgnoreCase)) return true;
                    if (string.Equals(str, "1", StringComparison.OrdinalIgnoreCase)) return true;
                    if (string.Equals(str, "false", StringComparison.OrdinalIgnoreCase)) return false;
                    if (string.Equals(str, "0", StringComparison.Ordinal)) return false;
                    return null;
                },
                BaseFunnyType.UInt8  => o => byte.Parse(((IFunnyArray) o).ToText()),
                BaseFunnyType.UInt16 => o => ushort.Parse(((IFunnyArray) o).ToText()),
                BaseFunnyType.UInt32 => o => UInt32.Parse(((IFunnyArray) o).ToText()),
                BaseFunnyType.UInt64 => o => UInt64.Parse(((IFunnyArray) o).ToText()),
                BaseFunnyType.Int16  => o => UInt16.Parse(((IFunnyArray) o).ToText()),
                BaseFunnyType.Int32  => o => Int32.Parse(((IFunnyArray) o).ToText()),
                BaseFunnyType.Int64  => o => Int64.Parse(((IFunnyArray) o).ToText()),
                BaseFunnyType.Real   => o => double.Parse(((IFunnyArray) o).ToText(), CultureInfo.InvariantCulture),
                _ => null
            };

        private static Func<object, object> CreateDeserializerOrNull(FunnyType to)
        {
            switch (to.BaseType)
            {
                case BaseFunnyType.Char:   return o =>  (char)GetArrayOfSize(o,1)[0];
                case BaseFunnyType.Bool:   return o => GetArrayOfSize(o, 1)[0]==1;
                case BaseFunnyType.UInt8:  return o => GetArrayOfSize(o, 1)[0];
                case BaseFunnyType.UInt16: return o => BitConverter.ToUInt16(GetArrayOfSize(o,2),0);
                case BaseFunnyType.UInt32: return o => BitConverter.ToUInt32(GetArrayOfSize(o, 4), 0);
                case BaseFunnyType.UInt64: return o => BitConverter.ToUInt64(GetArrayOfSize(o, 8), 0);
                case BaseFunnyType.Int16:  return o => BitConverter.ToInt16(GetArrayOfSize(o, 2), 0);
                case BaseFunnyType.Int32:  return o => BitConverter.ToInt32(GetArrayOfSize(o, 4), 0);
                case BaseFunnyType.Int64:  return o => BitConverter.ToInt64(GetArrayOfSize(o, 8), 0);
                case BaseFunnyType.Real:   return o => BitConverter.ToDouble(GetArrayOfSize(o, 8), 0);
            }

            if (to.IsText)
                return o => new ImmutableFunnyArray(Encoding.Unicode.GetBytes(((IFunnyArray)o).ToText()));
            
            return null;
        }
        class ConcreteConverter:FunctionWithSingleArg
        {
            private readonly Func<object, object> _converter;

            public ConcreteConverter(Func<object, object> converter, FunnyType from, FunnyType to): base("convert", to, from)
            {
                _converter = converter;
            }
            public override object Calc(object a)
            {
                try
                {
                    return _converter(a);
                }
                catch (Exception e)
                {
                    throw new FunnyRuntimeException($"Cannot convert {a} to type {this.ReturnType}", e);
                }
            }
        }
        //todo wtf?
        private static byte[] GetArrayOfSize(object array, int size)
        {
            var val = (IFunnyArray) array;
            try
            {
                if (val.Count > 4)
                    throw new FunnyRuntimeException("Array is too long");
                if (val.Count == 4)
                {
                    return val.Select(Convert.ToByte).ToArray();
                }
                
                return val.Concat(new int[4 - val.Count].Cast<object>()).Select(Convert.ToByte).ToArray();
            }
            catch (Exception e)
            {
                throw new FunnyRuntimeException($"Array '{val}' cannot be converted into int", e);
            }
        }
    }
}
