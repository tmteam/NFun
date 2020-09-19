using System;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class BitOrFunction : GenericFunctionBase
    {
        public BitOrFunction() : base(CoreFunNames.BitOr, GenericConstrains.Integers, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt8: return new UInt8Function();
                case BaseVarType.UInt16: return new UInt16Function();
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int16: return new Int16Function();
                case BaseVarType.Int32: return new Int32Function();
                case BaseVarType.Int64: return new Int64Function();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private class Int16Function : FunctionWithTwoArgs {
            public Int16Function() : base(CoreFunNames.BitOr, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object a, object b) =>  (short) a | (short) b;
        }

        private class Int32Function : FunctionWithTwoArgs {
            public Int32Function() : base(CoreFunNames.BitOr, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object a, object b) => (int) a | (int) b;

        }

        private class Int64Function : FunctionWithTwoArgs {
            public Int64Function() : base(CoreFunNames.BitOr, VarType.Int64, VarType.Int64, VarType.Int64) { }
            public override object Calc(object a, object b) => (long) a | (long) b;
        }

        private class UInt8Function : FunctionWithTwoArgs {
            public UInt8Function() : base(CoreFunNames.BitOr, VarType.UInt8, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object a, object b) => (byte)( (byte) a | (byte) b);
        }

        private class UInt16Function : FunctionWithTwoArgs {
            public UInt16Function() : base(CoreFunNames.BitOr, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object a, object b) => (ushort)( (ushort) a | (ushort) b);
        }

        private class UInt32Function : FunctionWithTwoArgs {
            public UInt32Function() : base(CoreFunNames.BitOr, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object a, object b) => ( (uint) a | (uint) b);
        }

        private class UInt64Function : FunctionWithTwoArgs {
            public UInt64Function() : base(CoreFunNames.BitOr, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object a, object b) => ( (ulong) a | (ulong) b);
        }
    }
    public class BitXorFunction : GenericFunctionBase
    {
        public BitXorFunction() : base(CoreFunNames.BitXor, GenericConstrains.Integers, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt8: return new UInt8Function();
                case BaseVarType.UInt16: return new UInt16Function();
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int16: return new Int16Function();
                case BaseVarType.Int32: return new Int32Function();
                case BaseVarType.Int64: return new Int64Function();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private class Int16Function : FunctionWithTwoArgs {
            public Int16Function() : base(CoreFunNames.BitXor, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object a, object b) =>  (short) a ^ (short) b;
        }

        private class Int32Function : FunctionWithTwoArgs {
            public Int32Function() : base(CoreFunNames.BitXor, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object a, object b) => (int) a ^ (int) b;

        }

        private class Int64Function : FunctionWithTwoArgs {
            public Int64Function() : base(CoreFunNames.BitXor, VarType.Int64, VarType.Int64, VarType.Int64) { }
            public override object Calc(object a, object b) => (long) a ^ (long) b;
        }

        private class UInt8Function : FunctionWithTwoArgs {
            public UInt8Function() : base(CoreFunNames.BitXor, VarType.UInt8, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object a, object b) => (byte)( (byte) a ^ (byte) b);
        }

        private class UInt16Function : FunctionWithTwoArgs {
            public UInt16Function() : base(CoreFunNames.BitXor, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object a, object b) => (ushort)( (ushort) a ^ (ushort) b);
        }

        private class UInt32Function : FunctionWithTwoArgs {
            public UInt32Function() : base(CoreFunNames.BitXor, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object a, object b) => ( (uint) a ^ (uint) b);
        }

        private class UInt64Function : FunctionWithTwoArgs {
            public UInt64Function() : base(CoreFunNames.BitXor, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object a, object b) => ( (ulong) a ^ (ulong) b);
        }
    }
    public class BitAndFunction : GenericFunctionBase
    {
        public BitAndFunction() : base(CoreFunNames.BitAnd, GenericConstrains.Integers, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt8: return new UInt8Function();
                case BaseVarType.UInt16: return new UInt16Function();
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int16: return new Int16Function();
                case BaseVarType.Int32: return new Int32Function();
                case BaseVarType.Int64: return new Int64Function();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private class Int16Function : FunctionWithTwoArgs {
            public Int16Function() : base(CoreFunNames.BitAnd, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object a, object b) =>  (short) a & (short) b;
        }

        private class Int32Function : FunctionWithTwoArgs {
            public Int32Function() : base(CoreFunNames.BitAnd, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object a, object b) => (int) a & (int) b;

        }

        private class Int64Function : FunctionWithTwoArgs {
            public Int64Function() : base(CoreFunNames.BitAnd, VarType.Int64, VarType.Int64, VarType.Int64) { }
            public override object Calc(object a, object b) => (long) a & (long) b;
        }

        private class UInt8Function : FunctionWithTwoArgs {
            public UInt8Function() : base(CoreFunNames.BitAnd, VarType.UInt8, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object a, object b) => (byte)( (byte) a & (byte) b);
        }

        private class UInt16Function : FunctionWithTwoArgs {
            public UInt16Function() : base(CoreFunNames.BitAnd, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object a, object b) => (ushort)( (ushort) a & (ushort) b);
        }

        private class UInt32Function : FunctionWithTwoArgs {
            public UInt32Function() : base(CoreFunNames.BitAnd, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object a, object b) => ( (uint) a & (uint) b);
        }

        private class UInt64Function : FunctionWithTwoArgs {
            public UInt64Function() : base(CoreFunNames.BitAnd, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object a, object b) => ( (ulong) a & (ulong) b);
        }
    }
    public class BitInverseFunction : GenericFunctionBase
    {
        public BitInverseFunction() : base(CoreFunNames.BitInverse, GenericConstrains.Integers, VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt8: return new UInt8Function();
                case BaseVarType.UInt16: return new UInt16Function();
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int16: return new Int16Function();
                case BaseVarType.Int32: return new Int32Function();
                case BaseVarType.Int64: return new Int64Function();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public class Int16Function : FunctionWithSingleArg
        {
            public Int16Function() : base(CoreFunNames.BitInverse, VarType.Int16, VarType.Int16) { }
            public override object Calc(object a) => (short)~((short)a);
        }
        public class Int32Function : FunctionWithSingleArg
        {
            public Int32Function() : base(CoreFunNames.BitInverse, VarType.Int32, VarType.Int32) { }
            public override object Calc(object a) => (int)~((int)a);
        }
        public class Int64Function : FunctionWithSingleArg
        {
            public Int64Function() : base(CoreFunNames.BitInverse, VarType.Int64, VarType.Int64) { }
            public override object Calc(object a) => (long)~((long)a);
        }
        public class UInt8Function : FunctionWithSingleArg
        {
            public UInt8Function() : base(CoreFunNames.BitInverse, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object a) => (byte)~((byte)a);
        }
        public class UInt16Function : FunctionWithSingleArg
        {
            public UInt16Function() : base(CoreFunNames.BitInverse, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object a) => (ushort)~((ushort)a);
        }
        public class UInt32Function : FunctionWithSingleArg
        {
            public UInt32Function() : base(CoreFunNames.BitInverse, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object a) => (uint)~((uint)a);
        }
        public class UInt64Function : FunctionWithSingleArg
        {
            public UInt64Function() : base(CoreFunNames.BitInverse, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object a) => (ulong)~(ulong)a;
        }
    }

    public class BitShiftLeftFunction : GenericFunctionBase
    {
        public BitShiftLeftFunction() : base(CoreFunNames.BitShiftLeft, 
            GenericConstrains.Integers3264, 
            VarType.Generic(0), 
            VarType.Generic(0), 
            VarType.UInt8) { }
        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int32: return new Int32Function();
                case BaseVarType.Int64: return new Int64Function();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public class Int32Function : FunctionWithTwoArgs {
            public Int32Function() : base(CoreFunNames.BitShiftLeft, VarType.Int32, VarType.Int32, VarType.UInt8) { }
            public override object Calc(object a, object b) => ((int)a) << ((byte)b);
        }
        public class Int64Function : FunctionWithTwoArgs {
            public Int64Function() : base(CoreFunNames.BitShiftLeft, VarType.Int64, VarType.Int64, VarType.UInt8) { }
            public override object Calc(object a, object b) => ((long)a) << ((byte)b);
        }

        public class UInt32Function : FunctionWithTwoArgs {
            public UInt32Function() : base(CoreFunNames.BitShiftLeft, VarType.UInt32, VarType.UInt32, VarType.UInt8) { }
            public override object Calc(object a, object b) => (uint)(((uint)a) << ((byte)b));
        }
        public class UInt64Function : FunctionWithTwoArgs {
            public UInt64Function() : base(CoreFunNames.BitShiftLeft, VarType.UInt64, VarType.UInt64, VarType.UInt8) { }
            public override object Calc(object a, object b) => (ulong)(((ulong)a) << ((byte)b));
        }
    }
    public class BitShiftRightFunction : GenericFunctionBase
    {
        public BitShiftRightFunction() : base(CoreFunNames.BitShiftRight,
            GenericConstrains.Integers3264,
            VarType.Generic(0),
            VarType.Generic(0),
            VarType.UInt8)
        { }
        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int32: return new Int32Function();
                case BaseVarType.Int64: return new Int64Function();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public class Int32Function : FunctionWithTwoArgs {
            public Int32Function() : base(CoreFunNames.BitShiftRight, VarType.Int32, VarType.Int32, VarType.UInt8) { }
            public override object Calc(object a, object b) => ((int)a) >> ((byte)b);
        }
        public class Int64Function : FunctionWithTwoArgs {
            public Int64Function() : base(CoreFunNames.BitShiftRight, VarType.Int64, VarType.Int64, VarType.UInt8) { }
            public override object Calc(object a, object b) => ((long)a) >> ((byte)b);
        }

        public class UInt32Function : FunctionWithTwoArgs {
            public UInt32Function() : base(CoreFunNames.BitShiftRight, VarType.UInt32, VarType.UInt32, VarType.UInt8) { }
            public override object Calc(object a, object b) => (uint)((uint)a >> (byte)b);
        }
        public class UInt64Function : FunctionWithTwoArgs {
            public UInt64Function() : base(CoreFunNames.BitShiftRight, VarType.UInt64, VarType.UInt64, VarType.UInt8) { }
            public override object Calc(object a, object b) => (ulong)((ulong)a >> (byte)b);
        }
    }
}
 