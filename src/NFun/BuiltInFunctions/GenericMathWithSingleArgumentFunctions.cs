using System;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class InvertFunction : PureGenericFunctionBase
    {
        public InvertFunction() : base(CoreFunNames.Negate, GenericConstrains.SignedNumber, 1) { }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            FunctionWithSingleArg result = concreteTypes[0].BaseType switch
            {
                BaseVarType.Int16 => new Int16Function(),
                BaseVarType.Int32 => new Int32Function(),
                BaseVarType.Int64 => new Int64Function(),
                BaseVarType.Real => new RealFunction(),
                _ => throw new ArgumentOutOfRangeException()
            };
            result.Name = CoreFunNames.Negate;
            result.ArgTypes = concreteTypes;
            result.ReturnType = concreteTypes[0];
            return result;
        }

        private class RealFunction : FunctionWithSingleArg
        {
            public override object Calc(object a) => -(double) a;
        }

        private class Int16Function : FunctionWithSingleArg
        {
            public override object Calc(object a) => (short) -((short) a);
        }

        private class Int32Function : FunctionWithSingleArg
        {
            public override object Calc(object a) => -((int) a);
        }

        private class Int64Function : FunctionWithSingleArg
        {
            public override object Calc(object a) => -((long) a);
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

        private class Int16Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((Int16)a) + ((Int16)b);
        }

        private class Int32Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((int)a) + ((int)b);
        }

        private class Int64Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((long)a) + ((long)b);

        }

        private class UInt16Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((UInt16)a) + ((UInt16)b);
        }

        private class UInt32Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((uint)a) + ((uint)b);

        }

        private class UInt64Function : FunctionWithTwoArgs {
            public override object Calc(object a, object b)  => ((ulong)a) + ((ulong)b);
        }
    }

    public class AbsFunction : PureGenericFunctionBase
    {
        public AbsFunction() : base(id, GenericConstrains.SignedNumber,1) { }
        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            FunctionWithSingleArg res =  concreteTypes[0].BaseType switch
            {
                BaseVarType.Int16 => new Int16Function(),
                BaseVarType.Int32 => new Int32Function(),
                BaseVarType.Int64 => new Int64Function(),
                BaseVarType.Real => new RealFunction(),
                _ => throw new ArgumentOutOfRangeException()
            };
            res.Name = Name;
            res.ArgTypes = concreteTypes;
            res.ReturnType = concreteTypes[0];
            return res;
        }
        private const string id = "abs";
        private class RealFunction : FunctionWithSingleArg
        {
            public override object Calc(object a) => Math.Abs(((double)a));
        }
        private  class Int16Function : FunctionWithSingleArg
        {
            public override object Calc(object a) => (short)Math.Abs(((short)a));
        }
        private class Int32Function : FunctionWithSingleArg
        {
            public override object Calc(object a) => Math.Abs(((int)a));
        }
        private class Int64Function : FunctionWithSingleArg
        {
            public override object Calc(object a) => Math.Abs(((long)a));
        }
    }
}