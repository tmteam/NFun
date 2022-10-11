using System;
using NFun.Types;

namespace NFun.Interpretation.Functions; 

internal static class LambdaWrapperFactory {
    public static IConcreteFunction Create<Tin, Tout>(string name, Func<Tin, Tout> function, FunnyConverter converter)
        => new ConcreteLambdaWrapperFunction<Tin, Tout>(name, function, converter);

    public static IConcreteFunction Create<Tin1, Tin2, Tout>(string name, Func<Tin1, Tin2, Tout> function, FunnyConverter converter)
        => new ConcreteLambdaWrapperFunction<Tin1, Tin2, Tout>(name, function, converter);

    public static IConcreteFunction Create<Tin1, Tin2, Tin3, Tout>(
        string name,
        Func<Tin1, Tin2, Tin3, Tout> function, FunnyConverter converter)
        => new ConcreteLambdaWrapperFunction<Tin1, Tin2, Tin3, Tout>(name, function, converter);

    public static IConcreteFunction Create<Tin1, Tin2, Tin3, Tin4, Tout>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tout> function, FunnyConverter converter)
        => new ConcreteLambdaWrapperFunction<Tin1, Tin2, Tin3, Tin4, Tout>(name, function, converter);

    public static IConcreteFunction Create<Tin1, Tin2, Tin3, Tin4, Tin5, Tout>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tout> function, FunnyConverter converter)
        => new ConcreteLambdaWrapperFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tout>(name, function, converter);

    public static IConcreteFunction Create<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tout>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tout> function, FunnyConverter converter)
        => new ConcreteLambdaWrapperFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tout>(name, function, converter);

    public static IConcreteFunction Create<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, Tout>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, Tout> function, FunnyConverter converter)
        => new ConcreteLambdaWrapperFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, Tout>(name, function, converter);
}

class ConcreteLambdaWrapperFunction<Tin, Tout> : FunctionWithSingleArg {
    private readonly Func<Tin, Tout> _function;
    private readonly IOutputFunnyConverter _argConverter;
    private readonly IInputFunnyConverter _resultConverter;

    public ConcreteLambdaWrapperFunction(string id, Func<Tin, Tout> function, FunnyConverter converter) {
        Name = id;
        _function = function;
        _argConverter = converter.GetOutputConverterFor(typeof(Tin));
        _resultConverter = converter.GetInputConverterFor(typeof(Tout));
        ArgTypes = new[] { _argConverter.FunnyType };
        ReturnType = _resultConverter.FunnyType;
    }

    public override object Calc(object a) =>
        _resultConverter.ToFunObject(_function((Tin)_argConverter.ToClrObject(a)));
}

class ConcreteLambdaWrapperFunction<Tin1, Tin2, Tout> : FunctionWithManyArguments {
    private readonly Func<Tin1, Tin2, Tout> _function;
    private readonly IOutputFunnyConverter _arg1;
    private readonly IOutputFunnyConverter _arg2;

    private readonly IInputFunnyConverter _resultConverter;

    public ConcreteLambdaWrapperFunction(string id, Func<Tin1, Tin2, Tout> function, FunnyConverter converter) : base(id) {
        _function = function;
        _arg1 = converter.GetOutputConverterFor(typeof(Tin1));
        _arg2 = converter.GetOutputConverterFor(typeof(Tin2));

        _resultConverter = converter.GetInputConverterFor(typeof(Tout));
        ArgTypes = new[] {
            _arg1.FunnyType,
            _arg2.FunnyType
        };
        ReturnType = _resultConverter.FunnyType;
    }

    public override object Calc(object[] args) =>
        _resultConverter.ToFunObject(
            _function(
                (Tin1)_arg1.ToClrObject(args[0]),
                (Tin2)_arg2.ToClrObject(args[1])
            ));
}

class ConcreteLambdaWrapperFunction<Tin1, Tin2, Tin3, Tout> : FunctionWithManyArguments {
    private readonly Func<Tin1, Tin2, Tin3, Tout> _function;
    private readonly IOutputFunnyConverter _arg1;
    private readonly IOutputFunnyConverter _arg2;
    private readonly IOutputFunnyConverter _arg3;

    private readonly IInputFunnyConverter _resultConverter;

    public ConcreteLambdaWrapperFunction(string id, Func<Tin1, Tin2, Tin3, Tout> function, FunnyConverter converter) : base(id) {
        _function = function;
        _arg1 = converter.GetOutputConverterFor(typeof(Tin1));
        _arg2 = converter.GetOutputConverterFor(typeof(Tin2));
        _arg3 = converter.GetOutputConverterFor(typeof(Tin3));

        _resultConverter = converter.GetInputConverterFor(typeof(Tout));
        ArgTypes = new[] {
            _arg1.FunnyType,
            _arg2.FunnyType,
            _arg3.FunnyType
        };
        ReturnType = _resultConverter.FunnyType;
    }

    public override object Calc(object[] args) =>
        _resultConverter.ToFunObject(
            _function(
                (Tin1)_arg1.ToClrObject(args[0]),
                (Tin2)_arg2.ToClrObject(args[1]),
                (Tin3)_arg3.ToClrObject(args[2])
            ));
}

class ConcreteLambdaWrapperFunction<Tin1, Tin2, Tin3, Tin4, Tout> : FunctionWithManyArguments {
    private readonly Func<Tin1, Tin2, Tin3, Tin4, Tout> _function;
    private readonly IOutputFunnyConverter _arg1;
    private readonly IOutputFunnyConverter _arg2;
    private readonly IOutputFunnyConverter _arg3;
    private readonly IOutputFunnyConverter _arg4;

    private readonly IInputFunnyConverter _resultConverter;

    public ConcreteLambdaWrapperFunction(string id, Func<Tin1, Tin2, Tin3, Tin4, Tout> function, FunnyConverter converter) : base(id) {
        _function = function;
        _arg1 = converter.GetOutputConverterFor(typeof(Tin1));
        _arg2 = converter.GetOutputConverterFor(typeof(Tin2));
        _arg3 = converter.GetOutputConverterFor(typeof(Tin3));
        _arg4 = converter.GetOutputConverterFor(typeof(Tin4));

        _resultConverter = converter.GetInputConverterFor(typeof(Tout));
        ArgTypes = new[] {
            _arg1.FunnyType,
            _arg2.FunnyType,
            _arg3.FunnyType,
            _arg4.FunnyType
        };
        ReturnType = _resultConverter.FunnyType;
    }

    public override object Calc(object[] args) =>
        _resultConverter.ToFunObject(
            _function(
                (Tin1)_arg1.ToClrObject(args[0]),
                (Tin2)_arg2.ToClrObject(args[1]),
                (Tin3)_arg3.ToClrObject(args[2]),
                (Tin4)_arg4.ToClrObject(args[3])
            ));
}

class ConcreteLambdaWrapperFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tout> : FunctionWithManyArguments {
    private readonly Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tout> _function;
    private readonly IOutputFunnyConverter _arg1;
    private readonly IOutputFunnyConverter _arg2;
    private readonly IOutputFunnyConverter _arg3;
    private readonly IOutputFunnyConverter _arg4;
    private readonly IOutputFunnyConverter _arg5;

    private readonly IInputFunnyConverter _resultConverter;

    public ConcreteLambdaWrapperFunction(string id, Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tout> function, FunnyConverter converter) : base(id) {
        _function = function;
        _arg1 = converter.GetOutputConverterFor(typeof(Tin1));
        _arg2 = converter.GetOutputConverterFor(typeof(Tin2));
        _arg3 = converter.GetOutputConverterFor(typeof(Tin3));
        _arg4 = converter.GetOutputConverterFor(typeof(Tin4));
        _arg5 = converter.GetOutputConverterFor(typeof(Tin5));

        _resultConverter = converter.GetInputConverterFor(typeof(Tout));
        ArgTypes = new[] {
            _arg1.FunnyType,
            _arg2.FunnyType,
            _arg3.FunnyType,
            _arg4.FunnyType,
            _arg5.FunnyType
        };
        ReturnType = _resultConverter.FunnyType;
    }

    public override object Calc(object[] args) =>
        _resultConverter.ToFunObject(
            _function(
                (Tin1)_arg1.ToClrObject(args[0]),
                (Tin2)_arg2.ToClrObject(args[1]),
                (Tin3)_arg3.ToClrObject(args[2]),
                (Tin4)_arg4.ToClrObject(args[3]),
                (Tin5)_arg5.ToClrObject(args[4])
            ));
}

class ConcreteLambdaWrapperFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tout> : FunctionWithManyArguments {
    private readonly Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tout> _function;
    private readonly IOutputFunnyConverter _arg1;
    private readonly IOutputFunnyConverter _arg2;
    private readonly IOutputFunnyConverter _arg3;
    private readonly IOutputFunnyConverter _arg4;
    private readonly IOutputFunnyConverter _arg5;
    private readonly IOutputFunnyConverter _arg6;

    private readonly IInputFunnyConverter _resultConverter;

    public ConcreteLambdaWrapperFunction(string id, Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tout> function, FunnyConverter converter) :
        base(id) {
        _function = function;
        _arg1 = converter.GetOutputConverterFor(typeof(Tin1));
        _arg2 = converter.GetOutputConverterFor(typeof(Tin2));
        _arg3 = converter.GetOutputConverterFor(typeof(Tin3));
        _arg4 = converter.GetOutputConverterFor(typeof(Tin4));
        _arg5 = converter.GetOutputConverterFor(typeof(Tin5));
        _arg6 = converter.GetOutputConverterFor(typeof(Tin6));

        _resultConverter = converter.GetInputConverterFor(typeof(Tout));
        ArgTypes = new[] {
            _arg1.FunnyType,
            _arg2.FunnyType,
            _arg3.FunnyType,
            _arg4.FunnyType,
            _arg5.FunnyType,
            _arg6.FunnyType
        };
        ReturnType = _resultConverter.FunnyType;
    }

    public override object Calc(object[] args) =>
        _resultConverter.ToFunObject(
            _function(
                (Tin1)_arg1.ToClrObject(args[0]),
                (Tin2)_arg2.ToClrObject(args[1]),
                (Tin3)_arg3.ToClrObject(args[2]),
                (Tin4)_arg4.ToClrObject(args[3]),
                (Tin5)_arg5.ToClrObject(args[4]),
                (Tin6)_arg6.ToClrObject(args[5])
            ));
}

class ConcreteLambdaWrapperFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, Tout> : FunctionWithManyArguments {
    private readonly Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, Tout> _function;
    private readonly IOutputFunnyConverter _arg1;
    private readonly IOutputFunnyConverter _arg2;
    private readonly IOutputFunnyConverter _arg3;
    private readonly IOutputFunnyConverter _arg4;
    private readonly IOutputFunnyConverter _arg5;
    private readonly IOutputFunnyConverter _arg6;
    private readonly IOutputFunnyConverter _arg7;

    private readonly IInputFunnyConverter _resultConverter;

    public ConcreteLambdaWrapperFunction(string id, Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, Tout> function, FunnyConverter converter) :
        base(id) {
        _function = function;
        _arg1 = converter.GetOutputConverterFor(typeof(Tin1));
        _arg2 = converter.GetOutputConverterFor(typeof(Tin2));
        _arg3 = converter.GetOutputConverterFor(typeof(Tin3));
        _arg4 = converter.GetOutputConverterFor(typeof(Tin4));
        _arg5 = converter.GetOutputConverterFor(typeof(Tin5));
        _arg6 = converter.GetOutputConverterFor(typeof(Tin6));
        _arg7 = converter.GetOutputConverterFor(typeof(Tin7));

        _resultConverter = converter.GetInputConverterFor(typeof(Tout));
        ArgTypes = new[] {
            _arg1.FunnyType,
            _arg2.FunnyType,
            _arg3.FunnyType,
            _arg4.FunnyType,
            _arg5.FunnyType,
            _arg6.FunnyType,
            _arg7.FunnyType
        };
        ReturnType = _resultConverter.FunnyType;
    }

    public override object Calc(object[] args) =>
        _resultConverter.ToFunObject(
            _function(
                (Tin1)_arg1.ToClrObject(args[0]),
                (Tin2)_arg2.ToClrObject(args[1]),
                (Tin3)_arg3.ToClrObject(args[2]),
                (Tin4)_arg4.ToClrObject(args[3]),
                (Tin5)_arg5.ToClrObject(args[4]),
                (Tin6)_arg6.ToClrObject(args[5]),
                (Tin7)_arg7.ToClrObject(args[6])
            ));
}