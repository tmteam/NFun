using System;
using System.Collections.Generic;
using System.Globalization;
using NFun.Exceptions;
using NFun.Tokenization;

namespace NFun.Types {

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


    public abstract bool DoubleIsReal { get; }
    protected static readonly Func<object, object> ToInt8 = o => Convert.ToSByte(o);
    protected static readonly Func<object, object> ToInt16 = o => Convert.ToInt16(o);
    protected static readonly Func<object, object> ToInt32 = o => Convert.ToInt32(o);
    protected static readonly Func<object, object> ToInt64 = o => Convert.ToInt64(o);
    protected static readonly Func<object, object> ToUInt8 = o => Convert.ToByte(o);
    protected static readonly Func<object, object> ToUInt16 = o => Convert.ToUInt16(o);
    protected static readonly Func<object, object> ToUInt32 = o => Convert.ToUInt32(o);
    protected static readonly Func<object, object> ToUInt64 = o => Convert.ToUInt64(o);
    protected static readonly Func<object, object> ToBool = o => Convert.ToBoolean(o);
    protected static readonly Func<object, object> ToChar = o => Convert.ToChar(o);
    
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
        null,
        null,
        null,
        new(),
        null
    };
    
    private static readonly IReadOnlyDictionary<BaseFunnyType, IInputFunnyConverter> PrimitiveInputConvertersByName
        = new Dictionary<BaseFunnyType, IInputFunnyConverter> {
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
    
    protected static readonly IReadOnlyDictionary<BaseFunnyType, IOutputFunnyConverter>
        PrimitiveOutputConvertersByName
            = new Dictionary<BaseFunnyType, IOutputFunnyConverter> {
                {BaseFunnyType.Any, DynamicTypeOutputFunnyConverter.AnyConverter},
                { BaseFunnyType.Bool, new PrimitiveTypeOutputFunnyConverter(FunnyType.Bool, typeof(bool)) },
                { BaseFunnyType.Char, new PrimitiveTypeOutputFunnyConverter(FunnyType.Char, typeof(Char)) },
                { BaseFunnyType.UInt8, new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt8, typeof(byte)) },
                { BaseFunnyType.UInt16, new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt16, typeof(UInt16)) },
                { BaseFunnyType.UInt32, new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt32, typeof(UInt32)) },
                { BaseFunnyType.UInt64, new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt64, typeof(UInt64)) },
                { BaseFunnyType.Int16, new PrimitiveTypeOutputFunnyConverter(FunnyType.Int16, typeof(Int16)) },
                { BaseFunnyType.Int32, new PrimitiveTypeOutputFunnyConverter(FunnyType.Int32, typeof(Int32)) },
                { BaseFunnyType.Int64, new PrimitiveTypeOutputFunnyConverter(FunnyType.Int64, typeof(Int64)) },
            };
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
            { typeof(Decimal), new DoubleToDecimalTypeOutputFunnyConverter() }
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
        if (PrimitiveOutputConvertersByName.TryGetValue(funnyType.BaseType, out var converter))
            return converter;
        return null;
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

    public override bool DoubleIsReal => true;
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
            { typeof(double), new DoubleToDecimalInputFunnyConverter() },
            { typeof(float), new FloatToDecimalInputFunnyConverter() },
        };
    
    public override IInputFunnyConverter GetPrimitiveInputConverterOrNull(Type clrType) =>
        PrimitiveInputConvertersByType.TryGetValue(clrType, out var res) 
            ? res : null;    
    public override IOutputFunnyConverter GetPrimitiveOutputConverterOrNull(Type clrType) => 
        PrimitiveOutputConvertersByType.TryGetValue(clrType, out var res) 
            ? res : null;
   
    public override IOutputFunnyConverter GetPrimitiveOutputConverterOrNull(FunnyType funnyType) {
        if (funnyType.BaseType == BaseFunnyType.Real)
            return new PrimitiveTypeOutputFunnyConverter(FunnyType.Real, typeof(Decimal));
        if (PrimitiveOutputConvertersByName.TryGetValue(funnyType.BaseType, out var converter))
            return converter;
        return null;
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

    private static readonly Func<object, object> ToDecimalReal = o => Convert.ToDecimal(o);
    
    public override bool DoubleIsReal => false;
}


}