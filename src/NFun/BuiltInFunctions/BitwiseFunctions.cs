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

        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
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

        public override object Calc(object[] args) => throw new System.NotImplementedException();
        public class Int16Function : FunctionBase {
            public Int16Function() : base(CoreFunNames.BitOr, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args) => ((short)args[0]) | ((short)args[1]);
        }
        public class Int32Function : FunctionBase {
            public Int32Function() : base(CoreFunNames.BitOr, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object[] args) => ((int)args[0]) | ((int)args[1]);
        }
        public class Int64Function : FunctionBase {
            public Int64Function() : base(CoreFunNames.BitOr, VarType.Int64, VarType.Int64, VarType.Int64) { }
            public override object Calc(object[] args) => ((long)args[0]) | ((long)args[1]);
        }
        public class UInt8Function : FunctionBase {
            public UInt8Function() : base(CoreFunNames.BitOr, VarType.UInt8, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object[] args) => (byte)(((byte)args[0]) | ((byte)args[1]));
        }
        public class UInt16Function : FunctionBase {
            public UInt16Function() : base(CoreFunNames.BitOr, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object[] args) => (ushort)(((ushort)args[0]) | ((ushort)args[1]));
        }

        public class UInt32Function : FunctionBase {
            public UInt32Function() : base(CoreFunNames.BitOr, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object[] args) => (uint)(((uint)args[0]) | ((uint)args[1]));
        }
        public class UInt64Function : FunctionBase {
            public UInt64Function() : base(CoreFunNames.BitOr, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object[] args) => (ulong)((ulong)args[0] | (ulong)args[1]);
        }
    }
    public class BitXorFunction : GenericFunctionBase
    {
        public BitXorFunction() : base(CoreFunNames.BitXor, GenericConstrains.Integers, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
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

        public override object Calc(object[] args) => throw new System.NotImplementedException();
        public class Int16Function : FunctionBase
        {
            public Int16Function() : base(CoreFunNames.BitOr, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args) => ((short)args[0]) ^ ((short)args[1]);
        }
        public class Int32Function : FunctionBase
        {
            public Int32Function() : base(CoreFunNames.BitOr, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object[] args) => ((int)args[0]) ^ ((int)args[1]);
        }
        public class Int64Function : FunctionBase
        {
            public Int64Function() : base(CoreFunNames.BitOr, VarType.Int64, VarType.Int64, VarType.Int64) { }
            public override object Calc(object[] args) => ((long)args[0]) ^ ((long)args[1]);
        }
        public class UInt8Function : FunctionBase
        {
            public UInt8Function() : base(CoreFunNames.BitOr, VarType.UInt8, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object[] args) => (byte)(((byte)args[0]) ^ ((byte)args[1]));
        }
        public class UInt16Function : FunctionBase
        {
            public UInt16Function() : base(CoreFunNames.BitOr, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object[] args) => (ushort)(((ushort)args[0]) ^ ((ushort)args[1]));
        }

        public class UInt32Function : FunctionBase
        {
            public UInt32Function() : base(CoreFunNames.BitOr, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object[] args) => (uint)(((uint)args[0]) ^ ((uint)args[1]));
        }
        public class UInt64Function : FunctionBase
        {
            public UInt64Function() : base(CoreFunNames.BitOr, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object[] args) => (ulong)(((ulong)args[0]) ^ (ulong)args[1]);
        }
    }
    public class BitAndFunction : GenericFunctionBase
    {
        public BitAndFunction() : base(CoreFunNames.BitAnd, GenericConstrains.Integers, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
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

        public override object Calc(object[] args) => throw new System.NotImplementedException();
        public class Int16Function : FunctionBase{
            public Int16Function() : base(CoreFunNames.BitAnd, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args) => ((short)args[0]) & ((short)args[1]);
        }
        public class Int32Function : FunctionBase{
            public Int32Function() : base(CoreFunNames.BitAnd, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object[] args) => ((int)args[0]) & ((int)args[1]);
        }
        public class Int64Function : FunctionBase{
            public Int64Function() : base(CoreFunNames.BitAnd, VarType.Int64, VarType.Int64, VarType.Int64) { }
            public override object Calc(object[] args) => ((long)args[0]) & ((long)args[1]);
        }
        public class UInt8Function : FunctionBase{
            public UInt8Function() : base(CoreFunNames.BitAnd, VarType.UInt8, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object[] args) => (byte)(((byte)args[0]) & ((byte)args[1]));
        }
        public class UInt16Function : FunctionBase{
            public UInt16Function() : base(CoreFunNames.BitAnd, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object[] args) => (ushort)(((ushort)args[0]) & ((ushort)args[1]));
        }

        public class UInt32Function : FunctionBase {
            public UInt32Function() : base(CoreFunNames.BitAnd, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object[] args) => (uint)(((uint)args[0]) & ((uint)args[1]));
        }
        public class UInt64Function : FunctionBase {
            public UInt64Function() : base(CoreFunNames.BitAnd, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object[] args) => (ulong)(((ulong)args[0]) & (ulong)args[1]);
        }
    }
    public class BitInverseFunction : GenericFunctionBase
    {
        public BitInverseFunction() : base(CoreFunNames.BitInverse, GenericConstrains.Integers, VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
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

        public override object Calc(object[] args) => throw new System.NotImplementedException();
        public class Int16Function : FunctionBase
        {
            public Int16Function() : base(CoreFunNames.BitInverse, VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args) => (short)~((short)args[0]);
        }
        public class Int32Function : FunctionBase
        {
            public Int32Function() : base(CoreFunNames.BitInverse, VarType.Int32, VarType.Int32) { }
            public override object Calc(object[] args) => (int)~((int)args[0]);
        }
        public class Int64Function : FunctionBase
        {
            public Int64Function() : base(CoreFunNames.BitInverse, VarType.Int64, VarType.Int64) { }
            public override object Calc(object[] args) => (long)~((long)args[0]);
        }
        public class UInt8Function : FunctionBase
        {
            public UInt8Function() : base(CoreFunNames.BitInverse, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object[] args) => (byte)~((byte)args[0]);
        }
        public class UInt16Function : FunctionBase
        {
            public UInt16Function() : base(CoreFunNames.BitInverse, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object[] args) => (ushort)~((ushort)args[0]);
        }
        public class UInt32Function : FunctionBase
        {
            public UInt32Function() : base(CoreFunNames.BitInverse, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object[] args) => (uint)~((uint)args[0]);
        }
        public class UInt64Function : FunctionBase
        {
            public UInt64Function() : base(CoreFunNames.BitInverse, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object[] args) => (ulong)~(ulong)args[0];
        }
    }

    public class BitShiftLeftFunction : GenericFunctionBase
    {
        public BitShiftLeftFunction() : base(CoreFunNames.BitShiftLeft, 
            GenericConstrains.Integers3264, 
            VarType.Generic(0), 
            VarType.Generic(0), 
            VarType.UInt8) { }
        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
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
        public override object Calc(object[] args) => throw new InvalidOperationException();

        public class Int32Function : FunctionBase {
            public Int32Function() : base(CoreFunNames.BitShiftLeft, VarType.Int32, VarType.Int32, VarType.UInt8) { }
            public override object Calc(object[] args) => ((int)args[0]) << ((byte)args[1]);
        }
        public class Int64Function : FunctionBase {
            public Int64Function() : base(CoreFunNames.BitShiftLeft, VarType.Int64, VarType.Int64, VarType.UInt8) { }
            public override object Calc(object[] args) => ((long)args[0]) << ((byte)args[1]);
        }

        public class UInt32Function : FunctionBase {
            public UInt32Function() : base(CoreFunNames.BitShiftLeft, VarType.UInt32, VarType.UInt32, VarType.UInt8) { }
            public override object Calc(object[] args) => (uint)(((uint)args[0]) << ((byte)args[1]));
        }
        public class UInt64Function : FunctionBase {
            public UInt64Function() : base(CoreFunNames.BitShiftLeft, VarType.UInt64, VarType.UInt64, VarType.UInt8) { }
            public override object Calc(object[] args) => (ulong)(((ulong)args[0]) << ((byte)args[1]));
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
        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
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
        public override object Calc(object[] args) => throw new InvalidOperationException();

        public class Int32Function : FunctionBase {
            public Int32Function() : base(CoreFunNames.BitShiftRight, VarType.Int32, VarType.Int32, VarType.UInt8) { }
            public override object Calc(object[] args) => ((int)args[0]) >> ((byte)args[1]);
        }
        public class Int64Function : FunctionBase {
            public Int64Function() : base(CoreFunNames.BitShiftRight, VarType.Int64, VarType.Int64, VarType.UInt8) { }
            public override object Calc(object[] args) => ((long)args[0]) >> ((byte)args[1]);
        }

        public class UInt32Function : FunctionBase {
            public UInt32Function() : base(CoreFunNames.BitShiftRight, VarType.UInt32, VarType.UInt32, VarType.UInt8) { }
            public override object Calc(object[] args) => (uint)((uint)args[0] >> (byte)args[1]);
        }
        public class UInt64Function : FunctionBase {
            public UInt64Function() : base(CoreFunNames.BitShiftRight, VarType.UInt64, VarType.UInt64, VarType.UInt8) { }
            public override object Calc(object[] args) => (ulong)((ulong)args[0] >> (byte)args[1]);
        }
    }
}
 