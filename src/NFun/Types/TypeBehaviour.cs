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

    // real → integer per PRAGMATIC matrix §1.1: silent truncation (toward zero)
    // — 1.5 → 1, -1.5 → -1, NOT banker's round. Convert.ToInt32(double) does
    // banker's round (round-half-to-even), which is .NET's outlier — most
    // languages (C/C++/Java/Go/Python int()/C# cast `(int)x`) truncate. NaN
    // and out-of-range values throw OverflowException, which SoftFailureConverter
    // catches as `none` for `:T?` targets.
    private static long TruncateToInt64(object o) {
        var d = o is decimal m ? (double)m : (double)o;
        if (double.IsNaN(d) || double.IsInfinity(d) || d >= 9.2233720368547758E+18 || d < -9.2233720368547758E+18)
            throw new OverflowException($"real {d} cannot be truncated to int64");
        return (long)d;
    }
    protected static readonly Func<object, object> RealToInt8 = o => {
        var v = TruncateToInt64(o);
        if (v < sbyte.MinValue || v > sbyte.MaxValue) throw new OverflowException($"real truncates to {v}, out of int8 range");
        return (sbyte)v;
    };
    protected static readonly Func<object, object> RealToInt16 = o => {
        var v = TruncateToInt64(o);
        if (v < short.MinValue || v > short.MaxValue) throw new OverflowException($"real truncates to {v}, out of int16 range");
        return (short)v;
    };
    protected static readonly Func<object, object> RealToInt32 = o => {
        var v = TruncateToInt64(o);
        if (v < int.MinValue || v > int.MaxValue) throw new OverflowException($"real truncates to {v}, out of int32 range");
        return (int)v;
    };
    protected static readonly Func<object, object> RealToInt64 = o => TruncateToInt64(o);
    protected static readonly Func<object, object> RealToUInt8 = o => {
        var v = TruncateToInt64(o);
        if (v < 0 || v > byte.MaxValue) throw new OverflowException($"real truncates to {v}, out of uint8 range");
        return (byte)v;
    };
    protected static readonly Func<object, object> RealToUInt16 = o => {
        var v = TruncateToInt64(o);
        if (v < 0 || v > ushort.MaxValue) throw new OverflowException($"real truncates to {v}, out of uint16 range");
        return (ushort)v;
    };
    protected static readonly Func<object, object> RealToUInt32 = o => {
        var v = TruncateToInt64(o);
        if (v < 0 || v > uint.MaxValue) throw new OverflowException($"real truncates to {v}, out of uint32 range");
        return (uint)v;
    };
    protected static readonly Func<object, object> RealToUInt64 = o => {
        // uint64 range exceeds int64 — special-case via decimal/double check directly.
        var d = o is decimal m ? (double)m : (double)o;
        if (double.IsNaN(d) || double.IsInfinity(d) || d < 0 || d >= 1.8446744073709552E+19)
            throw new OverflowException($"real {d} out of uint64 range");
        return (ulong)d;
    };

    /// <summary>
    /// Numeric converter selector for real source — uses truncating converters per spec
    /// (versus banker's-rounding Convert.ToInt32). Returns null if the target is not an
    /// integer type the caller should fall back to the regular `GetNumericConverterOrNull`.
    /// </summary>
    public static Func<object, object> GetRealToIntConverterOrNull(BaseFunnyType to) =>
        to switch {
            BaseFunnyType.UInt8  => RealToUInt8,
            BaseFunnyType.UInt16 => RealToUInt16,
            BaseFunnyType.UInt32 => RealToUInt32,
            BaseFunnyType.UInt64 => RealToUInt64,
            BaseFunnyType.Int8   => RealToInt8,
            BaseFunnyType.Int16  => RealToInt16,
            BaseFunnyType.Int32  => RealToInt32,
            BaseFunnyType.Int64  => RealToInt64,
            _ => null
        };
    protected static readonly Func<object, object> ToBool = o => Convert.ToBoolean(o);
    protected static readonly Func<object, object> ToChar = o =>
        o switch {
            double dou  => Convert.ToChar((long)dou),
            decimal dec => Convert.ToChar((long)dec),
            _           => Convert.ToChar(o)
        };
    
    protected  static readonly Type[] FunToClrTypesMap = {
        null,              //  0 Empty
        typeof(char),      //  1 Char
        typeof(bool),      //  2 Bool
        typeof(byte),      //  3 UInt8
        typeof(ushort),    //  4 UInt16
        typeof(uint),      //  5 UInt32
        typeof(ulong),     //  6 UInt64
        typeof(short),     //  7 Int16
        typeof(int),       //  8 Int32
        typeof(long),      //  9 Int64
        typeof(double),    // 10 Real
        typeof(IPAddress), // 11 Ip
        null,              // 12 ArrayOf
        null,              // 13 Fun
        null,              // 14 Generic
        typeof(object),    // 15 Any
        null,              // 16 Struct
        null,              // 17 Optional
        null,              // 18 None
        null,              // 19 Custom
        null,              // 20 NamedStruct
        typeof(sbyte),     // 21 Int8
    };

    protected static readonly object[] DefaultPrimitiveValues = {
        null,                                //  0 Empty
        default(char),                       //  1 Char
        default(bool),                       //  2 Bool
        default(byte),                       //  3 UInt8
        default(ushort),                     //  4 UInt16
        default(uint),                       //  5 UInt32
        default(ulong),                      //  6 UInt64
        default(short),                      //  7 Int16
        default(int),                        //  8 Int32
        default(long),                       //  9 Int64
        default(double),                     // 10 Real
        new IPAddress(new byte[]{127,0,0,1}),// 11 Ip
        null,                                // 12 ArrayOf
        null,                                // 13 Fun
        null,                                // 14 Generic
        new(),                               // 15 Any
        null,                                // 16 Struct
        null,                                // 17 Optional
        null,                                // 18 None
        null,                                // 19 Custom
        null,                                // 20 NamedStruct
        default(sbyte),                      // 21 Int8
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
            { BaseFunnyType.Int8, new PrimitiveTypeInputFunnyConverter(FunnyType.Int8) },
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
    private static readonly IOutputFunnyConverter Int8Converter   = new PrimitiveTypeOutputFunnyConverter(FunnyType.Int8, typeof(sbyte));
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
            BaseFunnyType.Int8   => Int8Converter,
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
            BaseFunnyType.Int8  => o => {
                var b = (char)o;
                if (b > sbyte.MaxValue) throw new OverflowException($"Cannot convert char '{o}' to int8");
                return (sbyte)b;
            },
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
            { typeof(sbyte), new PrimitiveTypeOutputFunnyConverter(FunnyType.Int8, typeof(sbyte)) },
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
            { typeof(sbyte), new PrimitiveTypeInputFunnyConverter(FunnyType.Int8) },
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
            BaseFunnyType.Int8   => ToInt8,
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
        NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign|NumberStyles.AllowExponent,
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
            { typeof(sbyte),   new PrimitiveTypeOutputFunnyConverter(FunnyType.Int8, typeof(sbyte)) },
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
            { typeof(sbyte), new PrimitiveTypeInputFunnyConverter(FunnyType.Int8) },
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
            BaseFunnyType.Int8   => ToInt8,
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
        NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign|NumberStyles.AllowExponent,
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