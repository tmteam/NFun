using System;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.Functions; 

public class BitShiftLeftFunction : GenericFunctionBase {
    public BitShiftLeftFunction() : base(
        CoreFunNames.BitShiftLeft,
        GenericConstrains.Integers,
        FunnyType.Generic(0),
        FunnyType.Generic(0),
        FunnyType.UInt8) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context)
        => concreteTypes[0].BaseType switch {
               BaseFunnyType.UInt64 => new UInt64Function(),
               BaseFunnyType.UInt32 => new UInt32Function(),
               BaseFunnyType.UInt16 => new UInt16Function(),
               BaseFunnyType.UInt8  => new UInt8Function(),
               BaseFunnyType.Int32  => new Int32Function(),
               BaseFunnyType.Int64  => new Int64Function(),
               BaseFunnyType.Int16  => new Int16Function(),
               _                    => throw new ArgumentOutOfRangeException()
           };

    private class Int16Function : FunctionWithTwoArgs {
        public Int16Function() : base(CoreFunNames.BitShiftLeft, FunnyType.Int16, FunnyType.Int16, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (Int16)((Int16)a << (byte)b);
    }

    private class Int32Function : FunctionWithTwoArgs {
        public Int32Function() : base(CoreFunNames.BitShiftLeft, FunnyType.Int32, FunnyType.Int32, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (int)a << (byte)b;
    }

    private class Int64Function : FunctionWithTwoArgs {
        public Int64Function() : base(CoreFunNames.BitShiftLeft, FunnyType.Int64, FunnyType.Int64, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (long)a << (byte)b;
    }

    private class UInt8Function : FunctionWithTwoArgs {
        public UInt8Function() : base(
            CoreFunNames.BitShiftLeft, FunnyType.UInt8, FunnyType.UInt8,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (byte)((byte)a << (byte)b);
    }

    private class UInt16Function : FunctionWithTwoArgs {
        public UInt16Function() : base(
            CoreFunNames.BitShiftLeft, FunnyType.UInt16, FunnyType.UInt16,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (UInt16)((UInt16)a << (byte)b);
    }
    
    private class UInt32Function : FunctionWithTwoArgs {
        public UInt32Function() : base(
            CoreFunNames.BitShiftLeft, FunnyType.UInt32, FunnyType.UInt32,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (uint)((uint)a << (byte)b);
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public UInt64Function() : base(
            CoreFunNames.BitShiftLeft, FunnyType.UInt64, FunnyType.UInt64,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (ulong)((ulong)a << (byte)b);
    }
}

public class BitShiftRightFunction : GenericFunctionBase {
    public BitShiftRightFunction() : base(
        CoreFunNames.BitShiftRight,
        GenericConstrains.Integers,
        FunnyType.Generic(0),
        FunnyType.Generic(0),
        FunnyType.UInt8) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context)
        => concreteTypes[0].BaseType switch {
               BaseFunnyType.UInt64 => new UInt64Function(),
               BaseFunnyType.UInt32 => new UInt32Function(),
               BaseFunnyType.UInt16 => new UInt16Function(),
               BaseFunnyType.UInt8  => new UInt8Function(),
               BaseFunnyType.Int32  => new Int32Function(),
               BaseFunnyType.Int64  => new Int64Function(),
               BaseFunnyType.Int16  => new Int16Function(),
               _                    => throw new ArgumentOutOfRangeException()
           };

    private class Int16Function : FunctionWithTwoArgs {
        public Int16Function() : base(CoreFunNames.BitShiftLeft, FunnyType.Int16, FunnyType.Int16, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (Int16)((Int16)a >> (byte)b);
    }

    private class Int32Function : FunctionWithTwoArgs {
        public Int32Function() : base(CoreFunNames.BitShiftRight, FunnyType.Int32, FunnyType.Int32, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (int)a >> (byte)b;
    }

    private class Int64Function : FunctionWithTwoArgs {
        public Int64Function() : base(CoreFunNames.BitShiftRight, FunnyType.Int64, FunnyType.Int64, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (long)a >> (byte)b;
    }

    private class UInt8Function : FunctionWithTwoArgs {
        public UInt8Function() : base(
            CoreFunNames.BitShiftLeft, FunnyType.UInt8, FunnyType.UInt8,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (byte)((byte)a >> (byte)b);
    }

    private class UInt16Function : FunctionWithTwoArgs {
        public UInt16Function() : base(
            CoreFunNames.BitShiftLeft, FunnyType.UInt16, FunnyType.UInt16,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (UInt16)((UInt16)a >> (byte)b);
    }

    private class UInt32Function : FunctionWithTwoArgs {
        public UInt32Function() : base(
            CoreFunNames.BitShiftRight, FunnyType.UInt32, FunnyType.UInt32,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (uint)((uint)a >> (byte)b);
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public UInt64Function() : base(
            CoreFunNames.BitShiftRight, FunnyType.UInt64, FunnyType.UInt64,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (ulong)((ulong)a >> (byte)b);
    }
}