using System;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.Functions {

public class InvertFunction : PureGenericFunctionBase {
    public InvertFunction() : base(CoreFunNames.Negate, GenericConstrains.SignedNumber, 1) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) {
        FunctionWithSingleArg result = concreteTypes[0].BaseType switch {
                                           BaseFunnyType.Int16 => new Int16Function(),
                                           BaseFunnyType.Int32 => new Int32Function(),
                                           BaseFunnyType.Int64 => new Int64Function(),
                                           BaseFunnyType.Real  => new RealFunction(),
                                           _                   => throw new ArgumentOutOfRangeException()
                                       };
        result.Name = CoreFunNames.Negate;
        result.ArgTypes = concreteTypes;
        result.ReturnType = concreteTypes[0];
        return result;
    }

    private class RealFunction : FunctionWithSingleArg {
        public override object Calc(object a) => -(double)a;
    }

    private class Int16Function : FunctionWithSingleArg {
        public override object Calc(object a) => (short)-((short)a);
    }

    private class Int32Function : FunctionWithSingleArg {
        public override object Calc(object a) => -((int)a);
    }

    private class Int64Function : FunctionWithSingleArg {
        public override object Calc(object a) => -((long)a);
    }
}

public class AbsFunction : PureGenericFunctionBase {
    public AbsFunction() : base(Id, GenericConstrains.SignedNumber, 1) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) {
        FunctionWithSingleArg res = concreteTypes[0].BaseType switch {
                                        BaseFunnyType.Int16 => new Int16Function(),
                                        BaseFunnyType.Int32 => new Int32Function(),
                                        BaseFunnyType.Int64 => new Int64Function(),
                                        BaseFunnyType.Real  => new RealFunction(),
                                        _                   => throw new ArgumentOutOfRangeException()
                                    };
        res.Name = Name;
        res.ArgTypes = concreteTypes;
        res.ReturnType = concreteTypes[0];
        return res;
    }

    private const string Id = "abs";

    private class RealFunction : FunctionWithSingleArg {
        public override object Calc(object a) => Math.Abs(((double)a));
    }

    private class Int16Function : FunctionWithSingleArg {
        public override object Calc(object a) => (short)Math.Abs(((short)a));
    }

    private class Int32Function : FunctionWithSingleArg {
        public override object Calc(object a) => Math.Abs(((int)a));
    }

    private class Int64Function : FunctionWithSingleArg {
        public override object Calc(object a) => Math.Abs(((long)a));
    }
}

}