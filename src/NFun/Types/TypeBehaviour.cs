using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace NFun.Types; 

public enum RealClrType {
    IsDecimal,
    IsDouble
}

public abstract class TypeBehaviour {

    public static readonly TypeBehaviour RealIsDouble =  new RealIsDoubleTypeBehaviour();
    public static readonly TypeBehaviour RealIsDecimal = new RealIsDecimalTypeBehaviour();

    public abstract IInputFunnyConverter GetPrimitiveInputConverterOrNull(Type clrType);
    public virtual IInputFunnyConverter GetPrimitiveInputConverterOrNull(FunnyType funnyType) =>
        PrimitiveInputConvertersByName.TryGetValue(funnyType.BaseType, out var converter) ? converter : null;
    public abstract IOutputFunnyConverter GetPrimitiveOutputConverterOrNull(Type clrType);
    public abstract IOutputFunnyConverter GetPrimitiveOutputConverterOrNull(FunnyType funnyType);
    public abstract object GetDefaultPrimitiveValueOrNull(BaseFunnyType typeName);
    public abstract Func<object, object> GetNumericConverterOrNull(BaseFunnyType to);
    public abstract object GetRealConstantValue(ulong d);
    public abstract object GetRealConstantValue(long d);
    public abstract object ParseOrNull(string text);
    public abstract Type GetClrTypeFor(BaseFunnyType funnyType);
    public abstract T RealTypeSelect<T>(T ifIsDouble, T ifIsDecimal);
    public abstract Type RealType { get; }

    protected static readonly Func<object, object> ToInt8 = o => Convert.ToSByte(o);
    protected static readonly Func<object, object> ToInt16 = o => Convert.ToInt16(o);
    protected static readonly Func<object, object> ToInt32 = o => Convert.ToInt32(o);
    protected static readonly Func<object, object> ToInt64 = o => Convert.ToInt64(o);
    protected static readonly Func<object, object> ToUInt8 = o => Convert.ToByte(o);
    protected static readonly Func<object, object> ToUInt16 = o => Convert.ToUInt16(o);
    protected static readonly Func<object, object> ToUInt32 = o => Convert.ToUInt32(o);
    protected static readonly Func<object, object> ToUInt64 = o => Convert.ToUInt64(o);
    protected static readonly Func<object, object> ToBool = o => Convert.ToBoolean(o);
    protected static readonly Func<object, object> ToChar = o =>
        o switch {
            double dou  => Convert.ToChar((long)dou),
            decimal dec => Convert.ToChar((long)dec),
            _           => Convert.ToChar(o)
        };
    
    protected  static readonly Type[] FunToClrTypesMap = {
        null,
        typeof(char),
        typeof(bool),
        typeof(byte),
        typeof(ushort),
        typeof(uint),
        typeof(ulong),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(double),
        typeof(IPAddress),
        null,
        null,
        null,
        typeof(object),
        null
    };
    
    protected static readonly object[] DefaultPrimitiveValues = {
        null,
        default(char),
        default(bool),
        default(byte),
        default(ushort),
        default(uint),
        default(ulong),
        default(short),
        default(int),
        default(long),
        default(double),
        new IPAddress(new byte[]{127,0,0,1}),
        null,
        null,
        null,
        new(),
        null
    };
    
    private static readonly IReadOnlyDictionary<BaseFunnyType, IInputFunnyConverter> PrimitiveInputConvertersByName
        = new Dictionary<BaseFunnyType, IInputFunnyConverter> {
            { BaseFunnyType.Ip, new PrimitiveTypeInputFunnyConverter(FunnyType.Ip)},
            { BaseFunnyType.Bool, new PrimitiveTypeInputFunnyConverter(FunnyType.Bool) },
            { BaseFunnyType.Char, new PrimitiveTypeInputFunnyConverter(FunnyType.Char) },
            { BaseFunnyType.UInt8, new PrimitiveTypeInputFunnyConverter(FunnyType.UInt8) },
            { BaseFunnyType.UInt16, new PrimitiveTypeInputFunnyConverter(FunnyType.UInt16) },
            { BaseFunnyType.UInt32, new PrimitiveTypeInputFunnyConverter(FunnyType.UInt32) },
            { BaseFunnyType.UInt64, new PrimitiveTypeInputFunnyConverter(FunnyType.UInt64) },
            { BaseFunnyType.Int16, new PrimitiveTypeInputFunnyConverter(FunnyType.Int16) },
            { BaseFunnyType.Int32, new PrimitiveTypeInputFunnyConverter(FunnyType.Int32) },
            { BaseFunnyType.Int64, new PrimitiveTypeInputFunnyConverter(FunnyType.Int64) },
            { BaseFunnyType.Real, new PrimitiveTypeInputFunnyConverter(FunnyType.Real) },
        };
    

    private static readonly IOutputFunnyConverter BoolConverter   = new PrimitiveTypeOutputFunnyConverter(FunnyType.Bool, typeof(bool));
    private static readonly IOutputFunnyConverter CharConverter   = new PrimitiveTypeOutputFunnyConverter(FunnyType.Char, typeof(Char));
    private static readonly IOutputFunnyConverter IpConverter     = new PrimitiveTypeOutputFunnyConverter(FunnyType.Ip, typeof(IPAddress));
    private static readonly IOutputFunnyConverter Uint8Converter  = new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt8, typeof(byte));
    private static readonly IOutputFunnyConverter Uint16Converter = new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt16, typeof(UInt16));
    private static readonly IOutputFunnyConverter Uint32Converter = new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt32, typeof(UInt32));
    private static readonly IOutputFunnyConverter Uint64Converter = new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt64, typeof(UInt64));
    private static readonly IOutputFunnyConverter Int16Converter  = new PrimitiveTypeOutputFunnyConverter(FunnyType.Int16, typeof(Int16));
    private static readonly IOutputFunnyConverter Int32Converter  = new PrimitiveTypeOutputFunnyConverter(FunnyType.Int32, typeof(Int32));
    private static readonly IOutputFunnyConverter Int64Converter  = new PrimitiveTypeOutputFunnyConverter(FunnyType.Int64, typeof(Int64));

    protected static IOutputFunnyConverter GetPrimitiveOutputConverterOrNull(BaseFunnyType baseType) =>
        baseType switch
        {
            BaseFunnyType.Any    => DynamicTypeOutputFunnyConverter.AnyConverter,
            BaseFunnyType.Ip     => IpConverter,
            BaseFunnyType.Bool   => BoolConverter,
            BaseFunnyType.Char   => CharConverter,
            BaseFunnyType.UInt8  => Uint8Converter,
            BaseFunnyType.UInt16 => Uint16Converter,
            BaseFunnyType.UInt32 => Uint32Converter,
            BaseFunnyType.UInt64 => Uint64Converter,
            BaseFunnyType.Int16  => Int16Converter,
            BaseFunnyType.Int32  => Int32Converter,
            BaseFunnyType.Int64  => Int64Converter,
            _                    => null
        };
    
    
    protected static int GetUnicodeBytes(object o1, out byte[] bytes) {
        var chars = new[] { (char)o1 };
        bytes = new byte[8];
        return Encoding.Unicode.GetBytes(chars,0,1,bytes,0);
    }
    
    public Func<object, object> GetFromCharToNumberConverterOrNull(BaseFunnyType to) =>
        to switch {
            BaseFunnyType.UInt8 => o => Convert.ToByte((char)o),
            BaseFunnyType.UInt16 => o => GetUnicodeBytes(o, out var bytes) > 2
                ? throw new OverflowException($"Cannot convert char value '{o}' to unt16")
                : BitConverter.ToUInt16(bytes, 0),
            BaseFunnyType.Int16 => o => GetUnicodeBytes(o, out var bytes) > 2
                ? throw new OverflowException($"Cannot convert char value '{o}' to int16")
                : BitConverter.ToInt16(bytes, 0),
            BaseFunnyType.UInt32 => o => GetUnicodeBytes(o, out var bytes) > 4
                ? throw new OverflowException($"Cannot convert char value '{o}' to unt32")
                : BitConverter.ToUInt32(bytes, 0),
            BaseFunnyType.Int32 => o => GetUnicodeBytes(o, out var bytes) > 4
                ? throw new OverflowException($"Cannot convert char value '{o}' to int32")
                : BitConverter.ToInt32(bytes, 0),
            BaseFunnyType.UInt64 => o => {
                GetUnicodeBytes(o, out var bytes);
                return BitConverter.ToUInt64(bytes, 0);
            },
            BaseFunnyType.Int64 => o => {
                GetUnicodeBytes(o, out var bytes);
                return BitConverter.ToInt64(bytes, 0);
            },
            BaseFunnyType.Real => FromCharToRealConverter,
            _                  => null
        };

    protected abstract Func<object, object> FromCharToRealConverter { get; }
}

public class RealIsDoubleTypeBehaviour : TypeBehaviour {

    private static readonly IReadOnlyDictionary<Type, IOutputFunnyConverter> PrimitiveOutputConvertersByType
        = new Dictionary<Type, IOutputFunnyConverter> {
            { typeof(bool), new PrimitiveTypeOutputFunnyConverter(FunnyType.Bool, typeof(bool)) },
            { typeof(Char), new PrimitiveTypeOutputFunnyConverter(FunnyType.Char, typeof(Char)) },
            { typeof(byte), new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt8, typeof(byte)) },
            { typeof(UInt16), new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt16, typeof(UInt16)) },
            { typeof(UInt32), new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt32, typeof(UInt32)) },
            { typeof(UInt64), new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt64, typeof(UInt64)) },
            { typeof(Int16), new PrimitiveTypeOutputFunnyConverter(FunnyType.Int16, typeof(Int16)) },
            { typeof(Int32), new PrimitiveTypeOutputFunnyConverter(FunnyType.Int32, typeof(Int32)) },
            { typeof(Int64), new PrimitiveTypeOutputFunnyConverter(FunnyType.Int64, typeof(Int64)) },
            { typeof(double), new PrimitiveTypeOutputFunnyConverter(FunnyType.Real, typeof(double)) },
            { typeof(float),  new DoubleToFloatTypeOutputFunnyConverter() },
            { typeof(Decimal), new DoubleToDecimalTypeOutputFunnyConverter() },
            { typeof(IPAddress), new PrimitiveTypeOutputFunnyConverter(FunnyType.Ip, typeof(IPAddress))}
        };
    
    private static readonly IReadOnlyDictionary<Type, IInputFunnyConverter> PrimitiveInputConvertersByType
        = new Dictionary<Type, IInputFunnyConverter> {
            { typeof(bool), new PrimitiveTypeInputFunnyConverter(FunnyType.Bool) },
            { typeof(Char), new PrimitiveTypeInputFunnyConverter(FunnyType.Char) },
            { typeof(byte), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt8) },
            { typeof(UInt16), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt16) },
            { typeof(UInt32), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt32) },
            { typeof(UInt64), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt64) },
            { typeof(Int16), new PrimitiveTypeInputFunnyConverter(FunnyType.Int16) },
            { typeof(Int32), new PrimitiveTypeInputFunnyConverter(FunnyType.Int32) },
            { typeof(Int64), new PrimitiveTypeInputFunnyConverter(FunnyType.Int64) },
            { typeof(double), new PrimitiveTypeInputFunnyConverter(FunnyType.Real) },
            { typeof(float), new FloatToDoubleInputFunnyConverter() },
            { typeof(Decimal), new DecimalToDoubleInputFunnyConverter() },
            { typeof(IPAddress), new PrimitiveTypeInputFunnyConverter(FunnyType.Ip) },
        };
    
    public override IInputFunnyConverter GetPrimitiveInputConverterOrNull(Type clrType) =>
        PrimitiveInputConvertersByType.TryGetValue(clrType, out var res) 
            ? res : null;    
    public override IOutputFunnyConverter GetPrimitiveOutputConverterOrNull(Type clrType) => 
        PrimitiveOutputConvertersByType.TryGetValue(clrType, out var res) 
            ? res : null;
    
    public override IOutputFunnyConverter GetPrimitiveOutputConverterOrNull(FunnyType funnyType) {
        if (funnyType.BaseType == BaseFunnyType.Real)
            return new PrimitiveTypeOutputFunnyConverter(FunnyType.Real, typeof(double));
        return GetPrimitiveOutputConverterOrNull(funnyType.BaseType);
    }

    public override object GetDefaultPrimitiveValueOrNull(BaseFunnyType typeName) => 
        typeName == BaseFunnyType.Real 
            ? 0.0 
            : DefaultPrimitiveValues[(int)typeName];
    
    public override Func<object, object> GetNumericConverterOrNull(BaseFunnyType to) =>
        to switch {
            BaseFunnyType.UInt8  => ToUInt8,
            BaseFunnyType.UInt16 => ToUInt16,
            BaseFunnyType.UInt32 => ToUInt32,
            BaseFunnyType.UInt64 => ToUInt64,
            BaseFunnyType.Int16  => ToInt16,
            BaseFunnyType.Int32  => ToInt32,
            BaseFunnyType.Int64  => ToInt64,
            BaseFunnyType.Real   => ToDoubleReal,
            BaseFunnyType.Bool   => ToBool,
            BaseFunnyType.Char   => ToChar,
            _                    => null
        };
    
    public override object GetRealConstantValue(ulong d) => (double)d;
    public override object GetRealConstantValue(long d) => (double)d;
    public override object ParseOrNull(string text) => double.TryParse(text,
        NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,
        CultureInfo.InvariantCulture, out var dbl) ? dbl : null;
    public override Type GetClrTypeFor(BaseFunnyType funnyType) =>
        funnyType != BaseFunnyType.Real ? FunToClrTypesMap[(int)funnyType] : typeof(double);
    public override T RealTypeSelect<T>(T ifIsDouble, T ifIsDecimal) => ifIsDouble;

    private static readonly Func<object, object> ToDoubleReal = o => Convert.ToDouble(o);

    public override Type RealType { get; } = typeof(Double);

    protected override Func<object, object> FromCharToRealConverter { get; } = o => {
        GetUnicodeBytes(o, out var bytes);
        return (double)BitConverter.ToInt64(bytes,0);
    };
}

public class RealIsDecimalTypeBehaviour : TypeBehaviour {

    private static readonly IReadOnlyDictionary<Type, IOutputFunnyConverter> PrimitiveOutputConvertersByType
        = new Dictionary<Type, IOutputFunnyConverter> {
            { typeof(bool), new PrimitiveTypeOutputFunnyConverter(FunnyType.Bool, typeof(bool)) },
            { typeof(Char), new PrimitiveTypeOutputFunnyConverter(FunnyType.Char, typeof(Char)) },
            { typeof(byte), new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt8, typeof(byte)) },
            { typeof(UInt16),  new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt16, typeof(UInt16)) },
            { typeof(UInt32),  new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt32, typeof(UInt32)) },
            { typeof(UInt64),  new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt64, typeof(UInt64)) },
            { typeof(Int16),   new PrimitiveTypeOutputFunnyConverter(FunnyType.Int16, typeof(Int16)) },
            { typeof(Int32),   new PrimitiveTypeOutputFunnyConverter(FunnyType.Int32, typeof(Int32)) },
            { typeof(Int64),   new PrimitiveTypeOutputFunnyConverter(FunnyType.Int64, typeof(Int64)) },
            { typeof(IPAddress), new PrimitiveTypeOutputFunnyConverter(FunnyType.Ip, typeof(IPAddress)) },
            { typeof(Decimal), new PrimitiveTypeOutputFunnyConverter(FunnyType.Real, typeof(Decimal))},
            { typeof(float),   new DecimalToFloatTypeOutputFunnyConverter() },
            { typeof(double),  new DecimalToDoubleTypeOutputFunnyConverter() },
        };
    
    private static readonly IReadOnlyDictionary<Type, IInputFunnyConverter> PrimitiveInputConvertersByType
        = new Dictionary<Type, IInputFunnyConverter> {
            { typeof(bool), new PrimitiveTypeInputFunnyConverter(FunnyType.Bool) },
            { typeof(Char), new PrimitiveTypeInputFunnyConverter(FunnyType.Char) },
            { typeof(byte), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt8) },
            { typeof(UInt16), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt16) },
            { typeof(UInt32), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt32) },
            { typeof(UInt64), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt64) },
            { typeof(Int16), new PrimitiveTypeInputFunnyConverter(FunnyType.Int16) },
            { typeof(Int32), new PrimitiveTypeInputFunnyConverter(FunnyType.Int32) },
            { typeof(Int64), new PrimitiveTypeInputFunnyConverter(FunnyType.Int64) },
            { typeof(Decimal), new PrimitiveTypeInputFunnyConverter(FunnyType.Real) },
            { typeof(IPAddress), new PrimitiveTypeInputFunnyConverter(FunnyType.Ip) },
            { typeof(double), new DoubleToDecimalInputFunnyConverter() },
            { typeof(float), new FloatToDecimalInputFunnyConverter() },
        };
    
    private static readonly Func<object, object> ToDecimalReal = o => Convert.ToDecimal(o);

    public override IInputFunnyConverter GetPrimitiveInputConverterOrNull(Type clrType) =>
        PrimitiveInputConvertersByType.TryGetValue(clrType, out var res) 
            ? res : null;    
    public override IOutputFunnyConverter GetPrimitiveOutputConverterOrNull(Type clrType) => 
        PrimitiveOutputConvertersByType.TryGetValue(clrType, out var res) 
            ? res : null;
   
    public override IOutputFunnyConverter GetPrimitiveOutputConverterOrNull(FunnyType funnyType) {
        if (funnyType.BaseType == BaseFunnyType.Real)
            return new PrimitiveTypeOutputFunnyConverter(FunnyType.Real, typeof(Decimal));
        return GetPrimitiveOutputConverterOrNull(funnyType.BaseType);
    }
    
    public override object GetDefaultPrimitiveValueOrNull(BaseFunnyType typeName) => 
        typeName == BaseFunnyType.Real 
            ? decimal.Zero 
            : DefaultPrimitiveValues[(int)typeName];
    
    public override Func<object, object> GetNumericConverterOrNull(BaseFunnyType to) =>
        to switch {
            BaseFunnyType.UInt8  => ToUInt8,
            BaseFunnyType.UInt16 => ToUInt16,
            BaseFunnyType.UInt32 => ToUInt32,
            BaseFunnyType.UInt64 => ToUInt64,
            BaseFunnyType.Int16  => ToInt16,
            BaseFunnyType.Int32  => ToInt32,
            BaseFunnyType.Int64  => ToInt64,
            BaseFunnyType.Real   => ToDecimalReal,
            BaseFunnyType.Bool   => ToBool,
            BaseFunnyType.Char   => ToChar,
            _                    => null
        };
    
    public override object GetRealConstantValue(ulong d) => new decimal(d);
    public override object GetRealConstantValue(long d) => new decimal(d);
    public override object ParseOrNull(string text) => decimal.TryParse(text, 
        NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,
        CultureInfo.InvariantCulture, out var dbl) ? dbl : null;
    
    public override Type GetClrTypeFor(BaseFunnyType funnyType) =>
        funnyType != BaseFunnyType.Real ? FunToClrTypesMap[(int)funnyType] : typeof(Decimal);
    
    public override T RealTypeSelect<T>(T ifIsDouble, T ifIsDecimal) => ifIsDecimal;
    
    protected override Func<object, object> FromCharToRealConverter { get; } = o => {
        GetUnicodeBytes(o, out var bytes);
        return new decimal(BitConverter.ToInt64(bytes, 0));
    };

    public override Type RealType { get; } = typeof(Decimal);

}