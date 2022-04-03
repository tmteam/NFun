
using System;
using NFun.Interpretation.Functions;
using NFun.Types;
namespace NFun.Functions {
public class DivideIntFunction : PureGenericFunctionBase {
    // GENERATED
    public DivideIntFunction() : base(CoreFunNames.DivideInt, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => UInt8Instance,            
                                           BaseFunnyType.UInt16 => UInt16Instance,            
                                           BaseFunnyType.UInt32 => UInt32Instance,            
                                           BaseFunnyType.UInt64 => UInt64Instance,            
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
        _                   => throw new ArgumentOutOfRangeException()
    };
      
        private static UInt8Function UInt8Instance = new UInt8Function();
        private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.DivideInt, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a / (byte)b);
        }
      
        private static UInt16Function UInt16Instance = new UInt16Function();
        private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.DivideInt, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a / (UInt16)b);
        }
      
        private static UInt32Function UInt32Instance = new UInt32Function();
        private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.DivideInt, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a / (UInt32)b);
        }
      
        private static UInt64Function UInt64Instance = new UInt64Function();
        private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.DivideInt, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a / (UInt64)b);
        }
      
        private static Int16Function Int16Instance = new Int16Function();
        private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.DivideInt, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a / (Int16)b);
        }
      
        private static Int32Function Int32Instance = new Int32Function();
        private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.DivideInt, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a / (Int32)b);
        }
      
        private static Int64Function Int64Instance = new Int64Function();
        private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.DivideInt, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a / (Int64)b);
        }
}
public class RemainderFunction : PureGenericFunctionBase {
    // GENERATED
    public RemainderFunction() : base(CoreFunNames.Remainder, GenericConstrains.Numbers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => UInt8Instance,            
                                           BaseFunnyType.UInt16 => UInt16Instance,            
                                           BaseFunnyType.UInt32 => UInt32Instance,            
                                           BaseFunnyType.UInt64 => UInt64Instance,            
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<IConcreteFunction>(DoubleInstance,DecimalInstance),
        _                   => throw new ArgumentOutOfRangeException()
    };
      
        private static UInt8Function UInt8Instance = new UInt8Function();
        private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.Remainder, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a % (byte)b);
        }
      
        private static UInt16Function UInt16Instance = new UInt16Function();
        private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.Remainder, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a % (UInt16)b);
        }
      
        private static UInt32Function UInt32Instance = new UInt32Function();
        private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.Remainder, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a % (UInt32)b);
        }
      
        private static UInt64Function UInt64Instance = new UInt64Function();
        private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.Remainder, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a % (UInt64)b);
        }
      
        private static Int16Function Int16Instance = new Int16Function();
        private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.Remainder, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a % (Int16)b);
        }
      
        private static Int32Function Int32Instance = new Int32Function();
        private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.Remainder, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a % (Int32)b);
        }
      
        private static Int64Function Int64Instance = new Int64Function();
        private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.Remainder, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a % (Int64)b);
        }
      
        private static DoubleFunction DoubleInstance = new DoubleFunction();
        private class DoubleFunction : FunctionWithTwoArgs {
             public DoubleFunction() : base(CoreFunNames.Remainder, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Double)((Double)a % (Double)b);
        }
      
        private static DecimalFunction DecimalInstance = new DecimalFunction();
        private class DecimalFunction : FunctionWithTwoArgs {
             public DecimalFunction() : base(CoreFunNames.Remainder, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Decimal)((Decimal)a % (Decimal)b);
        }
}
public class AddFunction : PureGenericFunctionBase {
    // GENERATED
    public AddFunction() : base(CoreFunNames.Add, GenericConstrains.Arithmetical, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => UInt8Instance,            
                                           BaseFunnyType.UInt16 => UInt16Instance,            
                                           BaseFunnyType.UInt32 => UInt32Instance,            
                                           BaseFunnyType.UInt64 => UInt64Instance,            
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<IConcreteFunction>(DoubleInstance,DecimalInstance),
        _                   => throw new ArgumentOutOfRangeException()
    };
      
        private static UInt8Function UInt8Instance = new UInt8Function();
        private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.Add, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a + (byte)b);
        }
      
        private static UInt16Function UInt16Instance = new UInt16Function();
        private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.Add, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a + (UInt16)b);
        }
      
        private static UInt32Function UInt32Instance = new UInt32Function();
        private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.Add, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a + (UInt32)b);
        }
      
        private static UInt64Function UInt64Instance = new UInt64Function();
        private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.Add, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a + (UInt64)b);
        }
      
        private static Int16Function Int16Instance = new Int16Function();
        private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.Add, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a + (Int16)b);
        }
      
        private static Int32Function Int32Instance = new Int32Function();
        private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.Add, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a + (Int32)b);
        }
      
        private static Int64Function Int64Instance = new Int64Function();
        private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.Add, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a + (Int64)b);
        }
      
        private static DoubleFunction DoubleInstance = new DoubleFunction();
        private class DoubleFunction : FunctionWithTwoArgs {
             public DoubleFunction() : base(CoreFunNames.Add, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Double)((Double)a + (Double)b);
        }
      
        private static DecimalFunction DecimalInstance = new DecimalFunction();
        private class DecimalFunction : FunctionWithTwoArgs {
             public DecimalFunction() : base(CoreFunNames.Add, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Decimal)((Decimal)a + (Decimal)b);
        }
}
public class SubstractFunction : PureGenericFunctionBase {
    // GENERATED
    public SubstractFunction() : base(CoreFunNames.Substract, GenericConstrains.Arithmetical, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => UInt8Instance,            
                                           BaseFunnyType.UInt16 => UInt16Instance,            
                                           BaseFunnyType.UInt32 => UInt32Instance,            
                                           BaseFunnyType.UInt64 => UInt64Instance,            
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<IConcreteFunction>(DoubleInstance,DecimalInstance),
        _                   => throw new ArgumentOutOfRangeException()
    };
      
        private static UInt8Function UInt8Instance = new UInt8Function();
        private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.Substract, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a - (byte)b);
        }
      
        private static UInt16Function UInt16Instance = new UInt16Function();
        private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.Substract, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a - (UInt16)b);
        }
      
        private static UInt32Function UInt32Instance = new UInt32Function();
        private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.Substract, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a - (UInt32)b);
        }
      
        private static UInt64Function UInt64Instance = new UInt64Function();
        private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.Substract, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a - (UInt64)b);
        }
      
        private static Int16Function Int16Instance = new Int16Function();
        private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.Substract, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a - (Int16)b);
        }
      
        private static Int32Function Int32Instance = new Int32Function();
        private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.Substract, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a - (Int32)b);
        }
      
        private static Int64Function Int64Instance = new Int64Function();
        private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.Substract, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a - (Int64)b);
        }
      
        private static DoubleFunction DoubleInstance = new DoubleFunction();
        private class DoubleFunction : FunctionWithTwoArgs {
             public DoubleFunction() : base(CoreFunNames.Substract, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Double)((Double)a - (Double)b);
        }
      
        private static DecimalFunction DecimalInstance = new DecimalFunction();
        private class DecimalFunction : FunctionWithTwoArgs {
             public DecimalFunction() : base(CoreFunNames.Substract, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Decimal)((Decimal)a - (Decimal)b);
        }
}
public class MultiplyFunction : PureGenericFunctionBase {
    // GENERATED
    public MultiplyFunction() : base(CoreFunNames.Multiply, GenericConstrains.Arithmetical, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => UInt8Instance,            
                                           BaseFunnyType.UInt16 => UInt16Instance,            
                                           BaseFunnyType.UInt32 => UInt32Instance,            
                                           BaseFunnyType.UInt64 => UInt64Instance,            
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<IConcreteFunction>(DoubleInstance,DecimalInstance),
        _                   => throw new ArgumentOutOfRangeException()
    };
      
        private static UInt8Function UInt8Instance = new UInt8Function();
        private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.Multiply, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a * (byte)b);
        }
      
        private static UInt16Function UInt16Instance = new UInt16Function();
        private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.Multiply, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a * (UInt16)b);
        }
      
        private static UInt32Function UInt32Instance = new UInt32Function();
        private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.Multiply, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a * (UInt32)b);
        }
      
        private static UInt64Function UInt64Instance = new UInt64Function();
        private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.Multiply, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a * (UInt64)b);
        }
      
        private static Int16Function Int16Instance = new Int16Function();
        private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.Multiply, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a * (Int16)b);
        }
      
        private static Int32Function Int32Instance = new Int32Function();
        private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.Multiply, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a * (Int32)b);
        }
      
        private static Int64Function Int64Instance = new Int64Function();
        private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.Multiply, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a * (Int64)b);
        }
      
        private static DoubleFunction DoubleInstance = new DoubleFunction();
        private class DoubleFunction : FunctionWithTwoArgs {
             public DoubleFunction() : base(CoreFunNames.Multiply, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Double)((Double)a * (Double)b);
        }
      
        private static DecimalFunction DecimalInstance = new DecimalFunction();
        private class DecimalFunction : FunctionWithTwoArgs {
             public DecimalFunction() : base(CoreFunNames.Multiply, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
             public override object Calc(object a, object b) => (Decimal)((Decimal)a * (Decimal)b);
        }
}
public class BitXorFunction : PureGenericFunctionBase {
    // GENERATED
    public BitXorFunction() : base(CoreFunNames.BitXor, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => UInt8Instance,            
                                           BaseFunnyType.UInt16 => UInt16Instance,            
                                           BaseFunnyType.UInt32 => UInt32Instance,            
                                           BaseFunnyType.UInt64 => UInt64Instance,            
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
        _                   => throw new ArgumentOutOfRangeException()
    };
      
        private static UInt8Function UInt8Instance = new UInt8Function();
        private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.BitXor, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a ^ (byte)b);
        }
      
        private static UInt16Function UInt16Instance = new UInt16Function();
        private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.BitXor, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a ^ (UInt16)b);
        }
      
        private static UInt32Function UInt32Instance = new UInt32Function();
        private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.BitXor, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a ^ (UInt32)b);
        }
      
        private static UInt64Function UInt64Instance = new UInt64Function();
        private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.BitXor, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a ^ (UInt64)b);
        }
      
        private static Int16Function Int16Instance = new Int16Function();
        private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.BitXor, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a ^ (Int16)b);
        }
      
        private static Int32Function Int32Instance = new Int32Function();
        private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.BitXor, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a ^ (Int32)b);
        }
      
        private static Int64Function Int64Instance = new Int64Function();
        private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.BitXor, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a ^ (Int64)b);
        }
}
public class BitAndFunction : PureGenericFunctionBase {
    // GENERATED
    public BitAndFunction() : base(CoreFunNames.BitAnd, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => UInt8Instance,            
                                           BaseFunnyType.UInt16 => UInt16Instance,            
                                           BaseFunnyType.UInt32 => UInt32Instance,            
                                           BaseFunnyType.UInt64 => UInt64Instance,            
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
        _                   => throw new ArgumentOutOfRangeException()
    };
      
        private static UInt8Function UInt8Instance = new UInt8Function();
        private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.BitAnd, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a & (byte)b);
        }
      
        private static UInt16Function UInt16Instance = new UInt16Function();
        private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.BitAnd, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a & (UInt16)b);
        }
      
        private static UInt32Function UInt32Instance = new UInt32Function();
        private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.BitAnd, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a & (UInt32)b);
        }
      
        private static UInt64Function UInt64Instance = new UInt64Function();
        private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.BitAnd, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a & (UInt64)b);
        }
      
        private static Int16Function Int16Instance = new Int16Function();
        private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.BitAnd, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a & (Int16)b);
        }
      
        private static Int32Function Int32Instance = new Int32Function();
        private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.BitAnd, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a & (Int32)b);
        }
      
        private static Int64Function Int64Instance = new Int64Function();
        private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.BitAnd, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a & (Int64)b);
        }
}
public class BitOrFunction : PureGenericFunctionBase {
    // GENERATED
    public BitOrFunction() : base(CoreFunNames.BitOr, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => UInt8Instance,            
                                           BaseFunnyType.UInt16 => UInt16Instance,            
                                           BaseFunnyType.UInt32 => UInt32Instance,            
                                           BaseFunnyType.UInt64 => UInt64Instance,            
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
        _                   => throw new ArgumentOutOfRangeException()
    };
      
        private static UInt8Function UInt8Instance = new UInt8Function();
        private class UInt8Function : FunctionWithTwoArgs {
             public UInt8Function() : base(CoreFunNames.BitOr, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
             public override object Calc(object a, object b) => (byte)((byte)a | (byte)b);
        }
      
        private static UInt16Function UInt16Instance = new UInt16Function();
        private class UInt16Function : FunctionWithTwoArgs {
             public UInt16Function() : base(CoreFunNames.BitOr, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
             public override object Calc(object a, object b) => (UInt16)((UInt16)a | (UInt16)b);
        }
      
        private static UInt32Function UInt32Instance = new UInt32Function();
        private class UInt32Function : FunctionWithTwoArgs {
             public UInt32Function() : base(CoreFunNames.BitOr, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
             public override object Calc(object a, object b) => (UInt32)((UInt32)a | (UInt32)b);
        }
      
        private static UInt64Function UInt64Instance = new UInt64Function();
        private class UInt64Function : FunctionWithTwoArgs {
             public UInt64Function() : base(CoreFunNames.BitOr, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
             public override object Calc(object a, object b) => (UInt64)((UInt64)a | (UInt64)b);
        }
      
        private static Int16Function Int16Instance = new Int16Function();
        private class Int16Function : FunctionWithTwoArgs {
             public Int16Function() : base(CoreFunNames.BitOr, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
             public override object Calc(object a, object b) => (Int16)((Int16)a | (Int16)b);
        }
      
        private static Int32Function Int32Instance = new Int32Function();
        private class Int32Function : FunctionWithTwoArgs {
             public Int32Function() : base(CoreFunNames.BitOr, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
             public override object Calc(object a, object b) => (Int32)((Int32)a | (Int32)b);
        }
      
        private static Int64Function Int64Instance = new Int64Function();
        private class Int64Function : FunctionWithTwoArgs {
             public Int64Function() : base(CoreFunNames.BitOr, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
             public override object Calc(object a, object b) => (Int64)((Int64)a | (Int64)b);
        }
}


public class BitInverseFunction : PureGenericFunctionBase {
    public BitInverseFunction() : base(CoreFunNames.BitInverse, GenericConstrains.Integers, 1) { }
    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) {
        FunctionWithSingleArg result = concreteTypes[0].BaseType switch {
                                           BaseFunnyType.UInt8 => UInt8Instance,            
                                           BaseFunnyType.UInt16 => UInt16Instance,            
                                           BaseFunnyType.UInt32 => UInt32Instance,            
                                           BaseFunnyType.UInt64 => UInt64Instance,            
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
        _                   => throw new ArgumentOutOfRangeException()
    };
        result.Name = CoreFunNames.Negate;
        result.ArgTypes = concreteTypes;
        result.ReturnType = concreteTypes[0];
        return result;
    }
      
                private static UInt8Function UInt8Instance = new UInt8Function();
                private class UInt8Function : FunctionWithSingleArg {  public override object Calc(object a) => (byte)(~((byte)a)); }
      
                private static UInt16Function UInt16Instance = new UInt16Function();
                private class UInt16Function : FunctionWithSingleArg {  public override object Calc(object a) => (UInt16)(~((UInt16)a)); }
      
                private static UInt32Function UInt32Instance = new UInt32Function();
                private class UInt32Function : FunctionWithSingleArg {  public override object Calc(object a) => (UInt32)(~((UInt32)a)); }
      
                private static UInt64Function UInt64Instance = new UInt64Function();
                private class UInt64Function : FunctionWithSingleArg {  public override object Calc(object a) => (UInt64)(~((UInt64)a)); }
      
                private static Int16Function Int16Instance = new Int16Function();
                private class Int16Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int16)(~((Int16)a)); }
      
                private static Int32Function Int32Instance = new Int32Function();
                private class Int32Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int32)(~((Int32)a)); }
      
                private static Int64Function Int64Instance = new Int64Function();
                private class Int64Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int64)(~((Int64)a)); }
}



public class NegateFunction : PureGenericFunctionBase {
    public NegateFunction() : base(CoreFunNames.Negate, GenericConstrains.SignedNumber, 1) { }
    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) {
        FunctionWithSingleArg result = concreteTypes[0].BaseType switch {
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<FunctionWithSingleArg>(DoubleInstance,DecimalInstance),
        _                   => throw new ArgumentOutOfRangeException()
    };
        result.Name = CoreFunNames.Negate;
        result.ArgTypes = concreteTypes;
        result.ReturnType = concreteTypes[0];
        return result;
    }
      
                private static Int16Function Int16Instance = new Int16Function();
                private class Int16Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int16)(-((Int16)a)); }
      
                private static Int32Function Int32Instance = new Int32Function();
                private class Int32Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int32)(-((Int32)a)); }
      
                private static Int64Function Int64Instance = new Int64Function();
                private class Int64Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int64)(-((Int64)a)); }
      
                private static DoubleFunction DoubleInstance = new DoubleFunction();
                private class DoubleFunction : FunctionWithSingleArg {  public override object Calc(object a) => (Double)(-((Double)a)); }
      
                private static DecimalFunction DecimalInstance = new DecimalFunction();
                private class DecimalFunction : FunctionWithSingleArg {  public override object Calc(object a) => (Decimal)(-((Decimal)a)); }
}



public class AbsFunction : PureGenericFunctionBase {
    public AbsFunction() : base("abs", GenericConstrains.SignedNumber, 1) { }
    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) {
        FunctionWithSingleArg result = concreteTypes[0].BaseType switch {
                                           BaseFunnyType.Int16 => Int16Instance,            
                                           BaseFunnyType.Int32 => Int32Instance,            
                                           BaseFunnyType.Int64 => Int64Instance,            
                                           BaseFunnyType.Real => typeBehaviour.RealTypeSelect<FunctionWithSingleArg>(DoubleInstance,DecimalInstance),
        _                   => throw new ArgumentOutOfRangeException()
    };
        result.Name = CoreFunNames.Negate;
        result.ArgTypes = concreteTypes;
        result.ReturnType = concreteTypes[0];
        return result;
    }
      
                private static Int16Function Int16Instance = new Int16Function();
                private class Int16Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int16)(Math.Abs((Int16)a)); }
      
                private static Int32Function Int32Instance = new Int32Function();
                private class Int32Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int32)(Math.Abs((Int32)a)); }
      
                private static Int64Function Int64Instance = new Int64Function();
                private class Int64Function : FunctionWithSingleArg {  public override object Calc(object a) => (Int64)(Math.Abs((Int64)a)); }
      
                private static DoubleFunction DoubleInstance = new DoubleFunction();
                private class DoubleFunction : FunctionWithSingleArg {  public override object Calc(object a) => (Double)(Math.Abs((Double)a)); }
      
                private static DecimalFunction DecimalInstance = new DecimalFunction();
                private class DecimalFunction : FunctionWithSingleArg {  public override object Calc(object a) => (Decimal)(Math.Abs((Decimal)a)); }
}

}






