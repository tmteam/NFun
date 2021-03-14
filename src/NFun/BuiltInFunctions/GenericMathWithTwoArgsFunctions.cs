using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class ArithmeticalGenericFunctionOfTwoArgsBase : PureGenericFunctionBase
    {
        private readonly IConcreteFunction[] _functions;
        protected ArithmeticalGenericFunctionOfTwoArgsBase(
            string name,
            GenericConstrains constrains
        ) : base(name, constrains,  2)
        {
            _functions = new IConcreteFunction[15];
        }

        protected void Setup(VarType type, FunctionWithTwoArgs function)
        {
            _functions[(int) type.BaseType] = function;
            function.Setup(Name, type);
        }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypesMap)
            => _functions[(int) concreteTypesMap[0].BaseType];
    }

    public class RemainderFunction : ArithmeticalGenericFunctionOfTwoArgsBase
    {
        public RemainderFunction() : base(
            CoreFunNames.Remainder,
            GenericConstrains.Numbers)
        {
            Setup(VarType.UInt8, new UInt8Function());
            Setup(VarType.UInt16, new UInt16Function());
            Setup(VarType.UInt32, new UInt32Function());
            Setup(VarType.UInt64, new UInt64Function());
            Setup(VarType.Int16, new Int16Function());
            Setup(VarType.Int32, new Int32Function());
            Setup(VarType.Int64, new Int64Function());
            Setup(VarType.Real, new RealFunction());
        }

        private class RealFunction : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => ((double) a) % ((double) b);
        }

        private class Int16Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (short) ((short) a) % ((short) b);
        }

        private class Int32Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => ((int) a) % ((int) b);
        }

        private class Int64Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => ((long) a) % ((long) b);
        }

        private class UInt8Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (byte) (((byte) a) % ((byte) b));
        }

        private class UInt16Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (ushort) (((ushort) a) % ((ushort) b));
        }

        private class UInt32Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (uint) (((uint) a) % ((uint) b));
        }

        private class UInt64Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (ulong) (((ulong) a) % (ulong) b);
        }
    }


    public class SubstractFunction : ArithmeticalGenericFunctionOfTwoArgsBase
    {
        public SubstractFunction() : base(
            CoreFunNames.Substract,
            GenericConstrains.Arithmetical)
        {
            Setup(VarType.UInt8, new UInt8Function());
            Setup(VarType.UInt16, new UInt16Function());
            Setup(VarType.UInt32, new UInt32Function());
            Setup(VarType.UInt64, new UInt64Function());
            Setup(VarType.Int16, new Int16Function());
            Setup(VarType.Int32, new Int32Function());
            Setup(VarType.Int64, new Int64Function());
            Setup(VarType.Real, new RealFunction());

        }

        private class RealFunction : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => ((double) a) - ((double) b);
        }

        public class Int16Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (short) ((short) a) - ((short) b);
        }

        public class Int32Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => ((int) a) - ((int) b);
        }

        public class Int64Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => ((long) a) - ((long) b);
        }

        public class UInt8Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (byte) (((byte) a) - ((byte) b));
        }

        public class UInt16Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (ushort) (((ushort) a) - ((ushort) b));
        }

        public class UInt32Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (uint) (((uint) a) - ((uint) b));
        }

        public class UInt64Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (ulong) (((ulong) a) - (ulong) b);
        }
    }

    public class MultiplyFunction : ArithmeticalGenericFunctionOfTwoArgsBase
    {
        public MultiplyFunction() : base(
            CoreFunNames.Multiply,
            GenericConstrains.Arithmetical)
        {
            Setup(VarType.UInt32, new UInt32Function());
            Setup(VarType.UInt64, new UInt64Function());
            Setup(VarType.Int32, new Int32Function());
            Setup(VarType.Int64, new Int64Function());
            Setup(VarType.Real, new RealFunction());
        }

        private class RealFunction : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => ((double) a) * ((double) b);
        }

        public class Int32Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (int) a * (int) b;
        }

        public class Int64Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => ((long) a) * ((long) b);
        }

        public class UInt32Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (uint) (((uint) a) * ((uint) b));
        }

        public class UInt64Function : FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) => (ulong) (((ulong) a) * (ulong) b);
        }
    }
}
