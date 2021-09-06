using System;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.Functions {

public class BitOrFunction : PureGenericFunctionBase {
    public BitOrFunction() : base(CoreFunNames.BitOr, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) =>
        concreteTypes[0].BaseType switch {
            BaseFunnyType.UInt8 => new UInt8Function(),
            BaseFunnyType.UInt16 => new UInt16Function(),
            BaseFunnyType.UInt32 => new UInt32Function(),
            BaseFunnyType.UInt64 => new UInt64Function(),
            BaseFunnyType.Int16 => new Int16Function(),
            BaseFunnyType.Int32 => new Int32Function(),
            BaseFunnyType.Int64 => new Int64Function(),
            _ => throw new ArgumentOutOfRangeException()
        };

    private class Int16Function : FunctionWithTwoArgs {
        public Int16Function() : base(CoreFunNames.BitOr, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
        public override object Calc(object a, object b) => (short)((short)a | (short)b);
    }

    private class Int32Function : FunctionWithTwoArgs {
        public Int32Function() : base(CoreFunNames.BitOr, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
        public override object Calc(object a, object b) => (int)a | (int)b;
    }

    private class Int64Function : FunctionWithTwoArgs {
        public Int64Function() : base(CoreFunNames.BitOr, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
        public override object Calc(object a, object b) => (long)a | (long)b;
    }

    private class UInt8Function : FunctionWithTwoArgs {
        public UInt8Function() : base(CoreFunNames.BitOr, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (byte)((byte)a | (byte)b);
    }

    private class UInt16Function : FunctionWithTwoArgs {
        public UInt16Function() : base(CoreFunNames.BitOr, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
        public override object Calc(object a, object b) => (ushort)((ushort)a | (ushort)b);
    }

    private class UInt32Function : FunctionWithTwoArgs {
        public UInt32Function() : base(CoreFunNames.BitOr, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
        public override object Calc(object a, object b) => ((uint)a | (uint)b);
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public UInt64Function() : base(CoreFunNames.BitOr, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
        public override object Calc(object a, object b) => ((ulong)a | (ulong)b);
    }
}

public class BitXorFunction : PureGenericFunctionBase {
    public BitXorFunction() : base(CoreFunNames.BitXor, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) =>
        concreteTypes[0].BaseType switch {
            BaseFunnyType.UInt8 => new UInt8Function(),
            BaseFunnyType.UInt16 => new UInt16Function(),
            BaseFunnyType.UInt32 => new UInt32Function(),
            BaseFunnyType.UInt64 => new UInt64Function(),
            BaseFunnyType.Int16 => new Int16Function(),
            BaseFunnyType.Int32 => new Int32Function(),
            BaseFunnyType.Int64 => new Int64Function(),
            _ => throw new ArgumentOutOfRangeException()
        };

    private class Int16Function : FunctionWithTwoArgs {
        public Int16Function() : base(CoreFunNames.BitXor, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
        public override object Calc(object a, object b) => (short)((short)a ^ (short)b);
    }

    private class Int32Function : FunctionWithTwoArgs {
        public Int32Function() : base(CoreFunNames.BitXor, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
        public override object Calc(object a, object b) => (int)a ^ (int)b;
    }

    private class Int64Function : FunctionWithTwoArgs {
        public Int64Function() : base(CoreFunNames.BitXor, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
        public override object Calc(object a, object b) => (long)a ^ (long)b;
    }

    private class UInt8Function : FunctionWithTwoArgs {
        public UInt8Function() : base(CoreFunNames.BitXor, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (byte)((byte)a ^ (byte)b);
    }

    private class UInt16Function : FunctionWithTwoArgs {
        public UInt16Function() : base(CoreFunNames.BitXor, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
        public override object Calc(object a, object b) => (ushort)((ushort)a ^ (ushort)b);
    }

    private class UInt32Function : FunctionWithTwoArgs {
        public UInt32Function() : base(CoreFunNames.BitXor, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
        public override object Calc(object a, object b) => ((uint)a ^ (uint)b);
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public UInt64Function() : base(CoreFunNames.BitXor, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
        public override object Calc(object a, object b) => ((ulong)a ^ (ulong)b);
    }
}

public class BitAndFunction : PureGenericFunctionBase {
    public BitAndFunction() : base(CoreFunNames.BitAnd, GenericConstrains.Integers, 2) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) =>
        concreteTypes[0].BaseType switch {
            BaseFunnyType.UInt8 => new UInt8Function(),
            BaseFunnyType.UInt16 => new UInt16Function(),
            BaseFunnyType.UInt32 => new UInt32Function(),
            BaseFunnyType.UInt64 => new UInt64Function(),
            BaseFunnyType.Int16 => new Int16Function(),
            BaseFunnyType.Int32 => new Int32Function(),
            BaseFunnyType.Int64 => new Int64Function(),
            _ => throw new ArgumentOutOfRangeException()
        };

    private class Int16Function : FunctionWithTwoArgs {
        public Int16Function() : base(CoreFunNames.BitAnd, FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
        public override object Calc(object a, object b) => (short)((short)a & (short)b);
    }

    private class Int32Function : FunctionWithTwoArgs {
        public Int32Function() : base(CoreFunNames.BitAnd, FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }
        public override object Calc(object a, object b) => (int)a & (int)b;
    }

    private class Int64Function : FunctionWithTwoArgs {
        public Int64Function() : base(CoreFunNames.BitAnd, FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }
        public override object Calc(object a, object b) => (long)a & (long)b;
    }

    private class UInt8Function : FunctionWithTwoArgs {
        public UInt8Function() : base(CoreFunNames.BitAnd, FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (byte)((byte)a & (byte)b);
    }

    private class UInt16Function : FunctionWithTwoArgs {
        public UInt16Function() : base(CoreFunNames.BitAnd, FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
        public override object Calc(object a, object b) => (ushort)((ushort)a & (ushort)b);
    }

    private class UInt32Function : FunctionWithTwoArgs {
        public UInt32Function() : base(CoreFunNames.BitAnd, FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
        public override object Calc(object a, object b) => ((uint)a & (uint)b);
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public UInt64Function() : base(CoreFunNames.BitAnd, FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
        public override object Calc(object a, object b) => ((ulong)a & (ulong)b);
    }
}

public class BitInverseFunction : PureGenericFunctionBase {
    public BitInverseFunction() : base(CoreFunNames.BitInverse, GenericConstrains.Integers, 1) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) =>
        concreteTypes[0].BaseType switch {
            BaseFunnyType.UInt8 => new UInt8Function(),
            BaseFunnyType.UInt16 => new UInt16Function(),
            BaseFunnyType.UInt32 => new UInt32Function(),
            BaseFunnyType.UInt64 => new UInt64Function(),
            BaseFunnyType.Int16 => new Int16Function(),
            BaseFunnyType.Int32 => new Int32Function(),
            BaseFunnyType.Int64 => new Int64Function(),
            _ => throw new ArgumentOutOfRangeException()
        };

    public class Int16Function : FunctionWithSingleArg {
        public Int16Function() : base(CoreFunNames.BitInverse, FunnyType.Int16, FunnyType.Int16) { }
        public override object Calc(object a) => (short)~((short)a);
    }

    public class Int32Function : FunctionWithSingleArg {
        public Int32Function() : base(CoreFunNames.BitInverse, FunnyType.Int32, FunnyType.Int32) { }
        public override object Calc(object a) => (int)~((int)a);
    }

    public class Int64Function : FunctionWithSingleArg {
        public Int64Function() : base(CoreFunNames.BitInverse, FunnyType.Int64, FunnyType.Int64) { }
        public override object Calc(object a) => (long)~((long)a);
    }

    public class UInt8Function : FunctionWithSingleArg {
        public UInt8Function() : base(CoreFunNames.BitInverse, FunnyType.UInt8, FunnyType.UInt8) { }
        public override object Calc(object a) => (byte)~((byte)a);
    }

    public class UInt16Function : FunctionWithSingleArg {
        public UInt16Function() : base(CoreFunNames.BitInverse, FunnyType.UInt16, FunnyType.UInt16) { }
        public override object Calc(object a) => (ushort)~((ushort)a);
    }

    public class UInt32Function : FunctionWithSingleArg {
        public UInt32Function() : base(CoreFunNames.BitInverse, FunnyType.UInt32, FunnyType.UInt32) { }
        public override object Calc(object a) => (uint)~((uint)a);
    }

    public class UInt64Function : FunctionWithSingleArg {
        public UInt64Function() : base(CoreFunNames.BitInverse, FunnyType.UInt64, FunnyType.UInt64) { }
        public override object Calc(object a) => (ulong)~(ulong)a;
    }
}

public class BitShiftLeftFunction : GenericFunctionBase {
    public BitShiftLeftFunction() : base(CoreFunNames.BitShiftLeft,
        GenericConstrains.Integers3264,
        FunnyType.Generic(0),
        FunnyType.Generic(0),
        FunnyType.UInt8) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) {
        return concreteTypes[0].BaseType switch {
            BaseFunnyType.UInt32 => new UInt32Function(),
            BaseFunnyType.UInt64 => new UInt64Function(),
            BaseFunnyType.Int32 => new Int32Function(),
            BaseFunnyType.Int64 => new Int64Function(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }


    public class Int32Function : FunctionWithTwoArgs {
        public Int32Function() : base(CoreFunNames.BitShiftLeft, FunnyType.Int32, FunnyType.Int32, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => ((int)a) << ((byte)b);
    }

    public class Int64Function : FunctionWithTwoArgs {
        public Int64Function() : base(CoreFunNames.BitShiftLeft, FunnyType.Int64, FunnyType.Int64, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => ((long)a) << ((byte)b);
    }

    public class UInt32Function : FunctionWithTwoArgs {
        public UInt32Function() : base(CoreFunNames.BitShiftLeft, FunnyType.UInt32, FunnyType.UInt32,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (uint)(((uint)a) << ((byte)b));
    }

    public class UInt64Function : FunctionWithTwoArgs {
        public UInt64Function() : base(CoreFunNames.BitShiftLeft, FunnyType.UInt64, FunnyType.UInt64,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (ulong)(((ulong)a) << ((byte)b));
    }
}

public class BitShiftRightFunction : GenericFunctionBase {
    public BitShiftRightFunction() : base(CoreFunNames.BitShiftRight,
        GenericConstrains.Integers3264,
        FunnyType.Generic(0),
        FunnyType.Generic(0),
        FunnyType.UInt8) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) {
        return concreteTypes[0].BaseType switch {
            BaseFunnyType.UInt32 => new UInt32Function(),
            BaseFunnyType.UInt64 => new UInt64Function(),
            BaseFunnyType.Int32 => new Int32Function(),
            BaseFunnyType.Int64 => new Int64Function(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }


    public class Int32Function : FunctionWithTwoArgs {
        public Int32Function() : base(CoreFunNames.BitShiftRight, FunnyType.Int32, FunnyType.Int32, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => ((int)a) >> ((byte)b);
    }

    public class Int64Function : FunctionWithTwoArgs {
        public Int64Function() : base(CoreFunNames.BitShiftRight, FunnyType.Int64, FunnyType.Int64, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => ((long)a) >> ((byte)b);
    }

    public class UInt32Function : FunctionWithTwoArgs {
        public UInt32Function() : base(CoreFunNames.BitShiftRight, FunnyType.UInt32, FunnyType.UInt32,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (uint)((uint)a >> (byte)b);
    }

    public class UInt64Function : FunctionWithTwoArgs {
        public UInt64Function() : base(CoreFunNames.BitShiftRight, FunnyType.UInt64, FunnyType.UInt64,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (ulong)((ulong)a >> (byte)b);
    }
}

}