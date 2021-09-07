using System;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.Functions {

public class ArithmeticalGenericFunctionOfTwoArgsBase : PureGenericFunctionBase {
    private readonly IConcreteFunction[] _functions;

    protected ArithmeticalGenericFunctionOfTwoArgsBase(
        string name,
        GenericConstrains constrains
    ) : base(name, constrains, 2) {
        _functions = new IConcreteFunction[15];
    }

    protected void Setup(FunnyType type, FunctionWithTwoArgs function) {
        _functions[(int)type.BaseType] = function;
        function.Setup(Name, type);
    }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap)
        => _functions[(int)concreteTypesMap[0].BaseType];
}

public class DivideIntFunction : ArithmeticalGenericFunctionOfTwoArgsBase {
    public DivideIntFunction() : base(CoreFunNames.DivideInt, GenericConstrains.Integers) {
        Setup(FunnyType.UInt8, new UInt8Function());
        Setup(FunnyType.UInt16, new UInt16Function());
        Setup(FunnyType.UInt32, new UInt32Function());
        Setup(FunnyType.UInt64, new UInt64Function());
        Setup(FunnyType.Int16, new Int16Function());
        Setup(FunnyType.Int32, new Int32Function());
        Setup(FunnyType.Int64, new Int64Function());
    }

    private class Int16Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (Int16)((Int16)a / (Int16)b);
    }

    private class Int32Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (int)((int)a / (int)b);
    }

    private class Int64Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (long)((long)a / (long)b);
    }

    private class UInt8Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (byte)((byte)a / (byte)b);
    }

    private class UInt16Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (ushort)((ushort)a / (ushort)b);
    }

    private class UInt32Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (uint)((uint)a / (uint)b);
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (ulong)((ulong)a / (ulong)b);
    }
}

public class RemainderFunction : ArithmeticalGenericFunctionOfTwoArgsBase {
    public RemainderFunction() : base(
        CoreFunNames.Remainder,
        GenericConstrains.Numbers) {
        Setup(FunnyType.UInt8, new UInt8Function());
        Setup(FunnyType.UInt16, new UInt16Function());
        Setup(FunnyType.UInt32, new UInt32Function());
        Setup(FunnyType.UInt64, new UInt64Function());
        Setup(FunnyType.Int16, new Int16Function());
        Setup(FunnyType.Int32, new Int32Function());
        Setup(FunnyType.Int64, new Int64Function());
        Setup(FunnyType.Real, new RealFunction());
    }

    private class RealFunction : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (double)a % (double)b;
    }

    private class Int16Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (short)((short)a % (short)b);
    }

    private class Int32Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (int)a % (int)b;
    }

    private class Int64Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (long)a % (long)b;
    }

    private class UInt8Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (byte)((byte)a % (byte)b);
    }

    private class UInt16Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (ushort)((ushort)a % (ushort)b);
    }

    private class UInt32Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (uint)((uint)a % (uint)b);
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (ulong)((ulong)a % (ulong)b);
    }
}


public class SubstractFunction : ArithmeticalGenericFunctionOfTwoArgsBase {
    public SubstractFunction() : base(
        CoreFunNames.Substract,
        GenericConstrains.Arithmetical) {
        Setup(FunnyType.UInt32, new UInt32Function());
        Setup(FunnyType.UInt64, new UInt64Function());
        Setup(FunnyType.Int32, new Int32Function());
        Setup(FunnyType.Int64, new Int64Function());
        Setup(FunnyType.Real, new RealFunction());
    }

    private class RealFunction : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (double)a - (double)b;
    }


    private class Int32Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (int)a - (int)b;
    }

    private class Int64Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (long)a - (long)b;
    }

    private class UInt32Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (uint)a - (uint)b;
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (ulong)a - (ulong)b;
    }
}

public class MultiplyFunction : ArithmeticalGenericFunctionOfTwoArgsBase {
    public MultiplyFunction() : base(
        CoreFunNames.Multiply,
        GenericConstrains.Arithmetical) {
        Setup(FunnyType.UInt32, new UInt32Function());
        Setup(FunnyType.UInt64, new UInt64Function());
        Setup(FunnyType.Int32, new Int32Function());
        Setup(FunnyType.Int64, new Int64Function());
        Setup(FunnyType.Real, new RealFunction());
    }

    private class RealFunction : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (double)a * (double)b;
    }

    private class Int32Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (int)a * (int)b;
    }

    private class Int64Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (long)a * (long)b;
    }

    private class UInt32Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (uint)a * (uint)b;
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public override object Calc(object a, object b) => (ulong)a * (ulong)b;
    }
}

}