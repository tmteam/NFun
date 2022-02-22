
using System;
using NFun.Interpretation.Functions;
using NFun.Types;
namespace NFun.Functions {
public class DivideIntFunction : PureGenericFunctionBase {
    public DivideIntFunction() : base(CoreFunNames.DivideInt, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => new UInt8Function(),            
                                           BaseFunnyType.UInt16 => new UInt16Function(),            
                                           BaseFunnyType.UInt32 => new UInt32Function(),            
                                           BaseFunnyType.UInt64 => new UInt64Function(),            
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
        _                   => throw new ArgumentOutOfRangeException()
    };
    private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.DivideInt, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a / (byte)b);
        }
    private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.DivideInt, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a / (UInt16)b);
        }
    private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.DivideInt, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a / (UInt32)b);
        }
    private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.DivideInt, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a / (UInt64)b);
        }
    private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.DivideInt, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a / (Int16)b);
        }
    private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.DivideInt, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a / (Int32)b);
        }
    private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.DivideInt, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a / (Int64)b);
        }
}
public class RemainderFunction : PureGenericFunctionBase {
    public RemainderFunction() : base(CoreFunNames.Remainder, GenericConstrains.Numbers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => new UInt8Function(),            
                                           BaseFunnyType.UInt16 => new UInt16Function(),            
                                           BaseFunnyType.UInt32 => new UInt32Function(),            
                                           BaseFunnyType.UInt64 => new UInt64Function(),            
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<IConcreteFunction>(new DoubleFunction(),new DecimalFunction()),
        _                   => throw new ArgumentOutOfRangeException()
    };
    private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.Remainder, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a % (byte)b);
        }
    private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.Remainder, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a % (UInt16)b);
        }
    private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.Remainder, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a % (UInt32)b);
        }
    private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.Remainder, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a % (UInt64)b);
        }
    private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.Remainder, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a % (Int16)b);
        }
    private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.Remainder, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a % (Int32)b);
        }
    private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.Remainder, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a % (Int64)b);
        }
    private class DoubleFunction : FunctionWithTwoArgs {
             public DoubleFunction() : base(CoreFunNames.Remainder, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Double)((Double)a % (Double)b);
        }
    private class DecimalFunction : FunctionWithTwoArgs {
             public DecimalFunction() : base(CoreFunNames.Remainder, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Decimal)((Decimal)a % (Decimal)b);
        }
}
public class AddFunction : PureGenericFunctionBase {
    public AddFunction() : base(CoreFunNames.Add, GenericConstrains.Arithmetical, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => new UInt8Function(),            
                                           BaseFunnyType.UInt16 => new UInt16Function(),            
                                           BaseFunnyType.UInt32 => new UInt32Function(),            
                                           BaseFunnyType.UInt64 => new UInt64Function(),            
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<IConcreteFunction>(new DoubleFunction(),new DecimalFunction()),
        _                   => throw new ArgumentOutOfRangeException()
    };
    private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.Add, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a + (byte)b);
        }
    private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.Add, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a + (UInt16)b);
        }
    private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.Add, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a + (UInt32)b);
        }
    private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.Add, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a + (UInt64)b);
        }
    private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.Add, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a + (Int16)b);
        }
    private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.Add, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a + (Int32)b);
        }
    private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.Add, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a + (Int64)b);
        }
    private class DoubleFunction : FunctionWithTwoArgs {
             public DoubleFunction() : base(CoreFunNames.Add, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Double)((Double)a + (Double)b);
        }
    private class DecimalFunction : FunctionWithTwoArgs {
             public DecimalFunction() : base(CoreFunNames.Add, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Decimal)((Decimal)a + (Decimal)b);
        }
}
public class SubstractFunction : PureGenericFunctionBase {
    public SubstractFunction() : base(CoreFunNames.Substract, GenericConstrains.Arithmetical, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => new UInt8Function(),            
                                           BaseFunnyType.UInt16 => new UInt16Function(),            
                                           BaseFunnyType.UInt32 => new UInt32Function(),            
                                           BaseFunnyType.UInt64 => new UInt64Function(),            
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<IConcreteFunction>(new DoubleFunction(),new DecimalFunction()),
        _                   => throw new ArgumentOutOfRangeException()
    };
    private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.Substract, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a - (byte)b);
        }
    private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.Substract, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a - (UInt16)b);
        }
    private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.Substract, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a - (UInt32)b);
        }
    private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.Substract, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a - (UInt64)b);
        }
    private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.Substract, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a - (Int16)b);
        }
    private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.Substract, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a - (Int32)b);
        }
    private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.Substract, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a - (Int64)b);
        }
    private class DoubleFunction : FunctionWithTwoArgs {
             public DoubleFunction() : base(CoreFunNames.Substract, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Double)((Double)a - (Double)b);
        }
    private class DecimalFunction : FunctionWithTwoArgs {
             public DecimalFunction() : base(CoreFunNames.Substract, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Decimal)((Decimal)a - (Decimal)b);
        }
}
public class MultiplyFunction : PureGenericFunctionBase {
    public MultiplyFunction() : base(CoreFunNames.Multiply, GenericConstrains.Arithmetical, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => new UInt8Function(),            
                                           BaseFunnyType.UInt16 => new UInt16Function(),            
                                           BaseFunnyType.UInt32 => new UInt32Function(),            
                                           BaseFunnyType.UInt64 => new UInt64Function(),            
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<IConcreteFunction>(new DoubleFunction(),new DecimalFunction()),
        _                   => throw new ArgumentOutOfRangeException()
    };
    private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.Multiply, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a * (byte)b);
        }
    private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.Multiply, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a * (UInt16)b);
        }
    private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.Multiply, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a * (UInt32)b);
        }
    private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.Multiply, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a * (UInt64)b);
        }
    private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.Multiply, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a * (Int16)b);
        }
    private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.Multiply, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a * (Int32)b);
        }
    private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.Multiply, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a * (Int64)b);
        }
    private class DoubleFunction : FunctionWithTwoArgs {
             public DoubleFunction() : base(CoreFunNames.Multiply, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Double)((Double)a * (Double)b);
        }
    private class DecimalFunction : FunctionWithTwoArgs {
             public DecimalFunction() : base(CoreFunNames.Multiply, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Decimal)((Decimal)a * (Decimal)b);
        }
}
public class BitXorFunction : PureGenericFunctionBase {
    public BitXorFunction() : base(CoreFunNames.BitXor, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => new UInt8Function(),            
                                           BaseFunnyType.UInt16 => new UInt16Function(),            
                                           BaseFunnyType.UInt32 => new UInt32Function(),            
                                           BaseFunnyType.UInt64 => new UInt64Function(),            
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
        _                   => throw new ArgumentOutOfRangeException()
    };
    private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.BitXor, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a ^ (byte)b);
        }
    private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.BitXor, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a ^ (UInt16)b);
        }
    private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.BitXor, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a ^ (UInt32)b);
        }
    private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.BitXor, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a ^ (UInt64)b);
        }
    private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.BitXor, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a ^ (Int16)b);
        }
    private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.BitXor, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a ^ (Int32)b);
        }
    private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.BitXor, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a ^ (Int64)b);
        }
}
public class BitAndFunction : PureGenericFunctionBase {
    public BitAndFunction() : base(CoreFunNames.BitAnd, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => new UInt8Function(),            
                                           BaseFunnyType.UInt16 => new UInt16Function(),            
                                           BaseFunnyType.UInt32 => new UInt32Function(),            
                                           BaseFunnyType.UInt64 => new UInt64Function(),            
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
        _                   => throw new ArgumentOutOfRangeException()
    };
    private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.BitAnd, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a & (byte)b);
        }
    private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.BitAnd, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a & (UInt16)b);
        }
    private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.BitAnd, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a & (UInt32)b);
        }
    private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.BitAnd, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a & (UInt64)b);
        }
    private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.BitAnd, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a & (Int16)b);
        }
    private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.BitAnd, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a & (Int32)b);
        }
    private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.BitAnd, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a & (Int64)b);
        }
}
public class BitOrFunction : PureGenericFunctionBase {
    public BitOrFunction() : base(CoreFunNames.BitOr, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => new UInt8Function(),            
                                           BaseFunnyType.UInt16 => new UInt16Function(),            
                                           BaseFunnyType.UInt32 => new UInt32Function(),            
                                           BaseFunnyType.UInt64 => new UInt64Function(),            
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
        _                   => throw new ArgumentOutOfRangeException()
    };
    private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.BitOr, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a | (byte)b);
        }
    private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.BitOr, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a | (UInt16)b);
        }
    private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.BitOr, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a | (UInt32)b);
        }
    private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.BitOr, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a | (UInt64)b);
        }
    private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.BitOr, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a | (Int16)b);
        }
    private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.BitOr, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a | (Int32)b);
        }
    private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.BitOr, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a | (Int64)b);
        }
}


public class BitInverseFunction : PureGenericFunctionBase {
    public BitInverseFunction() : base(CoreFunNames.BitInverse, GenericConstrains.Integers, 1) { }
    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) {
        FunctionWithSingleArg result = concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => new UInt8Function(),            
                                           BaseFunnyType.UInt16 => new UInt16Function(),            
                                           BaseFunnyType.UInt32 => new UInt32Function(),            
                                           BaseFunnyType.UInt64 => new UInt64Function(),            
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
        _                   => throw new ArgumentOutOfRangeException()
    };
        result.Name = CoreFunNames.Negate;
        result.ArgTypes = concreteTypes;
        result.ReturnType = concreteTypes[0];
        return result;
    }
    private class UInt8Function : FunctionWithSingleArg {  public override object Calc(object a) => (byte)(~((byte)a)); }
    private class UInt16Function : FunctionWithSingleArg {  public override object Calc(object a) => (UInt16)(~((UInt16)a)); }
    private class UInt32Function : FunctionWithSingleArg {  public override object Calc(object a) => (UInt32)(~((UInt32)a)); }
    private class UInt64Function : FunctionWithSingleArg {  public override object Calc(object a) => (UInt64)(~((UInt64)a)); }
    private class Int16Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int16)(~((Int16)a)); }
    private class Int32Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int32)(~((Int32)a)); }
    private class Int64Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int64)(~((Int64)a)); }
}



public class NegateFunction : PureGenericFunctionBase {
    public NegateFunction() : base(CoreFunNames.Negate, GenericConstrains.SignedNumber, 1) { }
    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) {
        FunctionWithSingleArg result = concreteTypes[0].BaseType switch {
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<FunctionWithSingleArg>(new DoubleFunction(),new DecimalFunction()),
        _                   => throw new ArgumentOutOfRangeException()
    };
        result.Name = CoreFunNames.Negate;
        result.ArgTypes = concreteTypes;
        result.ReturnType = concreteTypes[0];
        return result;
    }
    private class Int16Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int16)(-((Int16)a)); }
    private class Int32Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int32)(-((Int32)a)); }
    private class Int64Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int64)(-((Int64)a)); }
    private class DoubleFunction : FunctionWithSingleArg {  public override object Calc(object a) => (Double)(-((Double)a)); }
    private class DecimalFunction : FunctionWithSingleArg {  public override object Calc(object a) => (Decimal)(-((Decimal)a)); }
}



public class AbsFunction : PureGenericFunctionBase {
    public AbsFunction() : base("abs", GenericConstrains.SignedNumber, 1) { }
    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) {
        FunctionWithSingleArg result = concreteTypes[0].BaseType switch {
                                           BaseFunnyType.Int16 => new Int16Function(),            
                                           BaseFunnyType.Int32 => new Int32Function(),            
                                           BaseFunnyType.Int64 => new Int64Function(),            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<FunctionWithSingleArg>(new DoubleFunction(),new DecimalFunction()),
        _                   => throw new ArgumentOutOfRangeException()
    };
        result.Name = CoreFunNames.Negate;
        result.ArgTypes = concreteTypes;
        result.ReturnType = concreteTypes[0];
        return result;
    }
    private class Int16Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int16)(Math.Abs((Int16)a)); }
    private class Int32Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int32)(Math.Abs((Int32)a)); }
    private class Int64Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int64)(Math.Abs((Int64)a)); }
    private class DoubleFunction : FunctionWithSingleArg {  public override object Calc(object a) => (Double)(Math.Abs((Double)a)); }
    private class DecimalFunction : FunctionWithSingleArg {  public override object Calc(object a) => (Decimal)(Math.Abs((Decimal)a)); }
}

}






