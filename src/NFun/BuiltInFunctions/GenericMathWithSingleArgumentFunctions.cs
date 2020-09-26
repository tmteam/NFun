using System;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
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
        
        public class AddFunction : ArithmeticalGenericFunctionOfTwoArgsBase
        {
            public AddFunction() : base(
                CoreFunNames.Add,
                GenericConstrains.Arithmetical)
            {
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
            public override object Calc(object a, object b)  => ((double)a) + ((double)b);
        }
        public class Int16Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((Int16)a) + ((Int16)b);
        }
        public class Int32Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((int)a) + ((int)b);
        }
        public class Int64Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((long)a) + ((long)b);

        }
        public class UInt16Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((UInt16)a) + ((UInt16)b);
        }
        public class UInt32Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((uint)a) + ((uint)b);

        }
        public class UInt64Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((ulong)a) + ((ulong)b);
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
}