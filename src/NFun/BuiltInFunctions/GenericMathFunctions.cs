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
                case BaseVarType.Real: return new RealFunction();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public class RealFunction : FunctionWithTwoArgs
        {
            public RealFunction() : base(CoreFunNames.Add, VarType.Real, VarType.Real, VarType.Real) { }

            public override object Calc(object a, object b) => ((double)a) % ((double)b);
        }
        public class Int16Function : FunctionWithTwoArgs
        {
            public Int16Function() : base(CoreFunNames.Add, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object a, object b) => (short)((short)a) % ((short)b);
        }
        public class Int32Function : FunctionWithTwoArgs
        {
            public Int32Function() : base(CoreFunNames.Add, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object a, object b) => ((int)a) % ((int)b);
        }
        public class Int64Function : FunctionWithTwoArgs
        {
            public Int64Function() : base(CoreFunNames.Add, VarType.Int64, VarType.Int64, VarType.Int64) { }
            public override object Calc(object a, object b) => ((long)a) % ((long)b);
        }
        public class UInt8Function : FunctionWithTwoArgs
        {
            public UInt8Function() : base(CoreFunNames.Add, VarType.UInt8, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object a, object b) => (byte)(((byte)a) % ((byte)b));
        }
        public class UInt16Function : FunctionWithTwoArgs
        {
            public UInt16Function() : base(CoreFunNames.Add, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object a, object b) => (ushort)(((ushort)a) % ((ushort)b));
        }
        public class UInt32Function : FunctionWithTwoArgs
        {
            public UInt32Function() : base(CoreFunNames.Add, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object a, object b) => (uint)(((uint)a) % ((uint)b));
        }
        public class UInt64Function : FunctionWithTwoArgs
        {
            public UInt64Function() : base(CoreFunNames.Add, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object a, object b) => (ulong)(((ulong)a) % (ulong)b);
        }
    }
    public class AbsFunction : GenericFunctionBase
    {
        public AbsFunction() : base(
            id,
            GenericConstrains.SignedNumber,
            VarType.Generic(0), VarType.Generic(0))
        { }
        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
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
        public class RealFunction : FunctionWithSingleArg
        {
            public RealFunction() : base(id, VarType.Real, VarType.Real) { }

            public override object Calc(object a) => Math.Abs(((double)a));
        }
        public class Int16Function : FunctionWithSingleArg
        {
            public Int16Function() : base(id, VarType.Int16, VarType.Int16) { }
            public override object Calc(object a) => (short)Math.Abs(((short)a));
        }
        public class Int32Function : FunctionWithSingleArg
        {
            public Int32Function() : base(id, VarType.Int32, VarType.Int32) { }
            public override object Calc(object a) => Math.Abs(((int)a));
        }
        public class Int64Function : FunctionWithSingleArg
        {
            public Int64Function() : base(id, VarType.Int64, VarType.Int64) { }
            public override object Calc(object a) => Math.Abs(((long)a));
        }

    }
    public class InvertFunction : GenericFunctionBase
    {
        public InvertFunction() : base(
            CoreFunNames.Negate,
            GenericConstrains.SignedNumber,
            VarType.Generic(0), VarType.Generic(0))
        { }
        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
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

        private class RealFunction : FunctionWithSingleArg
        {
            public RealFunction() : base(CoreFunNames.Negate, VarType.Real, VarType.Real) { }

            public object Calc(object a, object b) => -((double)a);
            public override object Calc(object a) => -(double) a;
        }
        public class Int16Function : FunctionWithTwoArgs
        {
            public Int16Function() : base(CoreFunNames.Negate, VarType.Int16, VarType.Int16) { }
            public override object Calc(object a, object b) => (short)-((short)b);
        }
        public class Int32Function : FunctionWithTwoArgs
        {
            public Int32Function() : base(CoreFunNames.Negate, VarType.Int32, VarType.Int32) { }
            public override object Calc(object a, object b) => -((int)b);
        }
        public class Int64Function : FunctionWithTwoArgs
        {
            public Int64Function() : base(CoreFunNames.Negate, VarType.Int64, VarType.Int64) { }
            public override object Calc(object a, object b) => -((long)b);
        }
       
    }
    public class AddFunction : GenericFunctionBase
    {
        public AddFunction(string name) : base(
            name, 
            GenericConstrains.Arithmetical, 
            VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }

        public AddFunction() : this(CoreFunNames.Add) { }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
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
        }
        
        private class RealFunction : FunctionWithTwoArgs
        {
            public RealFunction() : base(CoreFunNames.Add, VarType.Real, VarType.Real, VarType.Real) { }
            public override object Calc(object a, object b)  => ((double)a) + ((double)b);
        }
        public class Int16Function : FunctionWithTwoArgs {
            public Int16Function() : base(CoreFunNames.Add, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object a, object b)  => ((Int16)a) + ((Int16)b);
        }
        public class Int32Function : FunctionWithTwoArgs {
            public Int32Function() : base(CoreFunNames.Add, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object a, object b)  => ((int)a) + ((int)b);
        }
        public class Int64Function : FunctionWithTwoArgs {
            public Int64Function() : base(CoreFunNames.Add, VarType.Int64, VarType.Int64, VarType.Int64) { }
            public override object Calc(object a, object b)  => ((long)a) + ((long)b);

        }
        public class UInt16Function : FunctionWithTwoArgs {
            public UInt16Function() : base(CoreFunNames.Add, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object a, object b)  => ((UInt16)a) + ((UInt16)b);
        }
        public class UInt32Function : FunctionWithTwoArgs {
            public  UInt32Function() : base(CoreFunNames.Add, VarType.UInt32, VarType.UInt32, VarType.UInt32){}
            public override object Calc(object a, object b)  => ((uint)a) + ((uint)b);

        }
        public class UInt64Function : FunctionWithTwoArgs {
            public  UInt64Function() : base(CoreFunNames.Add, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object a, object b)  => ((ulong)a) + ((ulong)b);
        }
    }

    public class SubstractFunction : GenericFunctionBase
    {
        public SubstractFunction(string name) : base(name, GenericConstrains.Arithmetical, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }
        public SubstractFunction() : this(CoreFunNames.Substract) { }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
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
        
        private class RealFunction : FunctionWithTwoArgs
        {
            public RealFunction() : base(CoreFunNames.Substract, VarType.Real, VarType.Real, VarType.Real) { }

            public override object Calc(object a, object b) => ((double) a) - ((double) b);
        }
        public class Int16Function : FunctionWithTwoArgs
        {
            public Int16Function() : base(CoreFunNames.Substract, VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object a, object b) => (short)((short)a) - ((short)b);
        }
        public class Int32Function : FunctionWithTwoArgs
        {
            public Int32Function() : base(CoreFunNames.Substract, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object a, object b) => ((int) a) - ((int) b);
        }
        public class Int64Function : FunctionWithTwoArgs
        {
            public Int64Function() : base(CoreFunNames.Substract, VarType.Int64, VarType.Int64, VarType.Int64) { }

            public override object Calc(object a, object b) => ((long)a) - ((long)b);
        }
        public class UInt8Function : FunctionWithTwoArgs
        {
            public UInt8Function() : base(CoreFunNames.Substract, VarType.UInt16, VarType.UInt8, VarType.UInt8) { }

            public override object Calc(object a, object b) => (byte)(((byte)a) - ((byte)b));
        }
        public class UInt16Function : FunctionWithTwoArgs
        {
            public UInt16Function() : base(CoreFunNames.Substract, VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object a, object b) => (ushort)(((ushort)a) - ((ushort)b));
        }
        public class UInt32Function : FunctionWithTwoArgs
        {
            public UInt32Function() : base(CoreFunNames.Substract, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object a, object b) => (uint)(((uint)a) - ((uint)b));
        }
        public class UInt64Function : FunctionWithTwoArgs
        {
            public UInt64Function() : base(CoreFunNames.Substract, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }

            public override object Calc(object a, object b) => (ulong)(((ulong)a) - (ulong)b);
        }
    }

    public class MultiplyFunction : GenericFunctionBase
    {
        public MultiplyFunction(string name) : base(name, GenericConstrains.Arithmetical, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }
        public MultiplyFunction() : this(CoreFunNames.Multiply) { }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
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
        
        private class RealFunction : FunctionWithTwoArgs
        {
            public RealFunction() : base(CoreFunNames.Substract, VarType.Real, VarType.Real, VarType.Real) { }

            public override object Calc(object a, object b) => ((double) a) * ((double) b);
        }
     
        public class Int32Function : FunctionWithTwoArgs
        {
            public Int32Function() : base(CoreFunNames.Substract, VarType.Int32, VarType.Int32, VarType.Int32) { }
            public override object Calc(object a, object b) => (int) a * (int) b;
        }
        public class Int64Function : FunctionWithTwoArgs
        {
            public Int64Function() : base(CoreFunNames.Substract, VarType.Int64, VarType.Int64, VarType.Int64) { }

            public override object Calc(object a, object b) => ((long)a) * ((long)b);
        }
      
        public class UInt32Function : FunctionWithTwoArgs
        {
            public UInt32Function() : base(CoreFunNames.Substract, VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object a, object b) => (uint)(((uint)a) * ((uint)b));
        }
        public class UInt64Function : FunctionWithTwoArgs
        {
            public UInt64Function() : base(CoreFunNames.Substract, VarType.UInt64, VarType.UInt64, VarType.UInt64) { }

            public override object Calc(object a, object b) => (ulong)(((ulong)a) * (ulong)b);
        }
    }

}
