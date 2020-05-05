using System;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class RemainderFunction : GenericFunctionBase
    {
        public RemainderFunction(string name) : base(
            name,
            GenericConstrains.Numbers,
            VarType.Generic(0), VarType.Generic(0), VarType.Generic(0))
        { }

        public RemainderFunction() : this(CoreFunNames.Remainder) { }

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
                case BaseVarType.Real: return new RealFunction();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override object Calc(object[] args) => args.Get<double>(0) + args.Get<double>(1);
        public class RealFunction : FunctionBase
        {
            public RealFunction() : base(CoreFunNames.Add, VarType.Real, VarType.Real, VarType.Real) { }

            public override object Calc(object[] args) => args.Get<double>(0) % args.Get<double>(1);
        }
        public class Int16Function : FunctionBase
        {
            public Int16Function() : base(CoreFunNames.Add, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args) => (short)args.Get<short>(0) % args.Get<short>(1);
        }
        public class Int32Function : FunctionBase
        {
            public Int32Function() : base(CoreFunNames.Add, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object[] args) => args.Get<int>(0) % args.Get<int>(1);
        }
        public class Int64Function : FunctionBase
        {
            public Int64Function() : base(CoreFunNames.Add, VarType.Int64, VarType.Int64, VarType.Int64) { }
            public override object Calc(object[] args) => args.Get<long>(0) % args.Get<long>(1);
        }
        public class UInt8Function : FunctionBase
        {
            public UInt8Function() : base(CoreFunNames.Add, VarType.UInt8, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object[] args) => (byte)(args.Get<byte>(0) % args.Get<byte>(1));
        }
        public class UInt16Function : FunctionBase
        {
            public UInt16Function() : base(CoreFunNames.Add, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object[] args) => (ushort)(args.Get<ushort>(0) % args.Get<ushort>(1));
        }
        public class UInt32Function : FunctionBase
        {
            public UInt32Function() : base(CoreFunNames.Add, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object[] args) => (uint)(args.Get<uint>(0) % args.Get<uint>(1));
        }
        public class UInt64Function : FunctionBase
        {
            public UInt64Function() : base(CoreFunNames.Add, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object[] args) => (ulong)(args.Get<ulong>(0) % args.Get<ulong>(1));
        }
    }
    public class AbsFunction : GenericFunctionBase
    {
        public AbsFunction() : base(
            id,
            GenericConstrains.SignedNumber,
            VarType.Generic(0), VarType.Generic(0))
        { }
        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.Int16: return new Int16Function();
                case BaseVarType.Int32: return new Int32Function();
                case BaseVarType.Int64: return new Int64Function();
                case BaseVarType.Real: return new RealFunction();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private const string id = "abs";
        public override object Calc(object[] args) => throw new InvalidOperationException();
        public class RealFunction : FunctionBase
        {
            public RealFunction() : base(id, VarType.Real, VarType.Real) { }

            public override object Calc(object[] args) => Math.Abs(args.Get<double>(0));
        }
        public class Int16Function : FunctionBase
        {
            public Int16Function() : base(id, VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args) => (short)Math.Abs(args.Get<short>(0));
        }
        public class Int32Function : FunctionBase
        {
            public Int32Function() : base(id, VarType.Int32, VarType.Int32) { }
            public override object Calc(object[] args) => Math.Abs(args.Get<int>(0));
        }
        public class Int64Function : FunctionBase
        {
            public Int64Function() : base(id, VarType.Int64, VarType.Int64) { }
            public override object Calc(object[] args) => Math.Abs(args.Get<long>(0));
        }

    }
    public class InvertFunction : GenericFunctionBase
    {
        public InvertFunction() : base(
            CoreFunNames.Negate,
            GenericConstrains.SignedNumber,
            VarType.Generic(0), VarType.Generic(0))
        { }
        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.Int16: return new Int16Function();
                case BaseVarType.Int32: return new Int32Function();
                case BaseVarType.Int64: return new Int64Function();
                case BaseVarType.Real: return new RealFunction();
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return base.CreateConcrete(concreteTypes);
        }

        public override object Calc(object[] args) => args.Get<double>(0) + args.Get<double>(1);
        public class RealFunction : FunctionBase
        {
            public RealFunction() : base(CoreFunNames.Negate, VarType.Real, VarType.Real) { }

            public override object Calc(object[] args) => -args.Get<double>(0);
        }
        public class Int16Function : FunctionBase
        {
            public Int16Function() : base(CoreFunNames.Negate, VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args) => (short)-args.Get<short>(1);
        }
        public class Int32Function : FunctionBase
        {
            public Int32Function() : base(CoreFunNames.Negate, VarType.Int32, VarType.Int32) { }
            public override object Calc(object[] args) => -args.Get<int>(1);
        }
        public class Int64Function : FunctionBase
        {
            public Int64Function() : base(CoreFunNames.Negate, VarType.Int64, VarType.Int64) { }
            public override object Calc(object[] args) => -args.Get<long>(1);
        }
       
    }
    public class AddFunction : GenericFunctionBase
    {
        public AddFunction(string name) : base(
            name, 
            GenericConstrains.Arithmetical, 
            VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }

        public AddFunction() : this(CoreFunNames.Add) { }

        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt16: return new UInt16Function();
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int16:  return new Int16Function();
                case BaseVarType.Int32:  return new Int32Function();
                case BaseVarType.Int64:  return new Int64Function();
                case BaseVarType.Real:   return new RealFunction();
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return base.CreateConcrete(concreteTypes);
        }

        public override object Calc(object[] args) => args.Get<double>(0) + args.Get<double>(1);
        public class RealFunction : FunctionBase
        {
            public RealFunction() : base(CoreFunNames.Add, VarType.Real, VarType.Real, VarType.Real) { }

            public override object Calc(object[] args) => args.Get<double>(0) + args.Get<double>(1);
        }
        public class Int16Function : FunctionBase {
            public Int16Function() : base(CoreFunNames.Add, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args) => (short)args.Get<short>(0) + args.Get<short>(1);
        }
        public class Int32Function : FunctionBase {
            public Int32Function() : base(CoreFunNames.Add, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object[] args) => args.Get<int>(0) + args.Get<int>(1);
        }
        public class Int64Function : FunctionBase {
            public Int64Function() : base(CoreFunNames.Add, VarType.Int64, VarType.Int64, VarType.Int64) { }
            public override object Calc(object[] args) => args.Get<long>(0) + args.Get<long>(1);
        }
        public class UInt16Function : FunctionBase {
            public UInt16Function() : base(CoreFunNames.Add, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object[] args) => (ushort)(args.Get<ushort>(0) + args.Get<ushort>(1));
        }
        public class UInt32Function : FunctionBase {
            public  UInt32Function() : base(CoreFunNames.Add, VarType.UInt32, VarType.UInt32, VarType.UInt32){}
            public override object Calc(object[] args) => (uint)(args.Get<uint>(0) + args.Get<uint>(1));
        }
        public class UInt64Function : FunctionBase {
            public  UInt64Function() : base(CoreFunNames.Add, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object[] args) => (ulong)(args.Get<ulong>(0) + args.Get<ulong>(1));
        }
    }

    public class SubstractFunction : GenericFunctionBase
    {
        public SubstractFunction(string name) : base(name, GenericConstrains.Arithmetical, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }
        public SubstractFunction() : this(CoreFunNames.Substract) { }

        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt8: return new  UInt8Function();
                case BaseVarType.UInt16: return new UInt16Function();
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int16:  return new Int16Function();
                case BaseVarType.Int32:  return new Int32Function();
                case BaseVarType.Int64:  return new Int64Function();
                case BaseVarType.Real:   return new RealFunction();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override object Calc(object[] args) => args.Get<double>(0) - args.Get<double>(1);
        public class RealFunction : FunctionBase
        {
            public RealFunction() : base(CoreFunNames.Substract, VarType.Real, VarType.Real, VarType.Real) { }

            public override object Calc(object[] args) => args.Get<double>(0) - args.Get<double>(1);
        }
        public class Int16Function : FunctionBase
        {
            public Int16Function() : base(CoreFunNames.Substract, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args) => (short)args.Get<short>(0) - args.Get<short>(1);
        }
        public class Int32Function : FunctionBase
        {
            public Int32Function() : base(CoreFunNames.Substract, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object[] args) => args.Get<int>(0) - args.Get<int>(1);
        }
        public class Int64Function : FunctionBase
        {
            public Int64Function() : base(CoreFunNames.Substract, VarType.Int64, VarType.Int64, VarType.Int64) { }

            public override object Calc(object[] args) => args.Get<long>(0) - args.Get<long>(1);
        }
        public class UInt8Function : FunctionBase
        {
            public UInt8Function() : base(CoreFunNames.Substract, VarType.UInt16, VarType.UInt8, VarType.UInt8) { }

            public override object Calc(object[] args) => (byte)(args.Get<byte>(0) - args.Get<byte>(1));
        }
        public class UInt16Function : FunctionBase
        {
            public UInt16Function() : base(CoreFunNames.Substract, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object[] args) => (ushort)(args.Get<ushort>(0) - args.Get<ushort>(1));
        }
        public class UInt32Function : FunctionBase
        {
            public UInt32Function() : base(CoreFunNames.Substract, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object[] args) => (uint)(args.Get<uint>(0) - args.Get<uint>(1));
        }
        public class UInt64Function : FunctionBase
        {
            public UInt64Function() : base(CoreFunNames.Substract, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }

            public override object Calc(object[] args) => (ulong)(args.Get<ulong>(0) - args.Get<ulong>(1));
        }
    }

    public class MultiplyFunction : GenericFunctionBase
    {
        public MultiplyFunction(string name) : base(name, GenericConstrains.Arithmetical, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }
        public MultiplyFunction() : this(CoreFunNames.Multiply) { }

        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int32:  return new Int32Function();
                case BaseVarType.Int64:  return new Int64Function();
                case BaseVarType.Real:   return new RealFunction();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override object Calc(object[] args) => args.Get<double>(0) * args.Get<double>(1);
        public class RealFunction : FunctionBase
        {
            public RealFunction() : base(CoreFunNames.Substract, VarType.Real, VarType.Real, VarType.Real) { }

            public override object Calc(object[] args) => args.Get<double>(0) * args.Get<double>(1);
        }
     
        public class Int32Function : FunctionBase
        {
            public Int32Function() : base(CoreFunNames.Substract, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object[] args) => args.Get<int>(0) * args.Get<int>(1);
        }
        public class Int64Function : FunctionBase
        {
            public Int64Function() : base(CoreFunNames.Substract, VarType.Int64, VarType.Int64, VarType.Int64) { }

            public override object Calc(object[] args) => args.Get<long>(0) * args.Get<long>(1);
        }
      
        public class UInt32Function : FunctionBase
        {
            public UInt32Function() : base(CoreFunNames.Substract, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object[] args) => (uint)(args.Get<uint>(0) * args.Get<uint>(1));
        }
        public class UInt64Function : FunctionBase
        {
            public UInt64Function() : base(CoreFunNames.Substract, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }

            public override object Calc(object[] args) => (ulong)(args.Get<ulong>(0) * args.Get<ulong>(1));
        }
    }

}
