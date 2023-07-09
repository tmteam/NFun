using System;
using System.Runtime.CompilerServices;
using System.Threading;
using NFun.Exceptions;
using NFun.Interpretation;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun;

public interface ICalculator<in TInput> {
    /// <summary>
    /// Calculates given expression. The operation is thread-safe.
    /// </summary>
    object Calc(string expression, TInput input);

    /// <summary>
    /// Creates Func that can be called later. The Func is thread-safe. The operation is thread-safe.
    /// </summary>
    Func<TInput, object> ToLambda(string expression);
}

public interface ICalculator<in TInput, out TOutput> {
    /// <summary>
    /// Calculates given expression. The operation is thread-safe.
    /// </summary>
    TOutput Calc(string expression, TInput inputModel);

    /// <summary>
    /// Creates Func that can be called later. The Func is thread-safe. The operation is thread-safe.
    /// </summary>
    Func<TInput, TOutput> ToLambda(string expression);
}

public interface IContextCalculator<TContext> {
    /// <summary>
    /// Calculates given expression. The operation is thread-safe.
    /// </summary>
    void Calc(string expression, TContext context);

    /// <summary>
    /// Creates Action that can be called later. The Action is thread-safe. The operation is thread-safe.
    /// </summary>
    Action<TContext> ToLambda(string expression);
}

public interface IConstantCalculator<out TOutput> {
    /// <summary>
    /// Calculates given expression. The operation is thread-safe.
    /// </summary>
    TOutput Calc(string expression);
}

internal class Calculator<TInput> : ICalculator<TInput> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<InputProperty> _inputsMap;

    public Calculator(FunnyCalculatorBuilder builder) {
        _builder = builder;
        _mutableApriori = new MutableAprioriTypesMap();
        _inputsMap = _mutableApriori.AddAprioriInputs<TInput>(Dialects.Origin.Converter);
    }

    public object Calc(string expression, TInput input)
        => ToLambda(expression)(input);

    public Func<TInput, object> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);
        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);

        int isRunning = 0;
        return input => {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
            {
                // if runtime is already running - create runtime copy, and run it
                return Run(runtime.Clone(), input);
            }

            try
            {
                return Run(runtime, input);
            }
            finally
            {
                isRunning = 0;
            }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object Run(FunnyRuntime runtime, TInput input) {
        FluentApiTools.SetInputValues(runtime, _inputsMap, input);
        runtime.Run();
        return FluentApiTools.GetFunnyOut(runtime).Value;
    }
}

[Obsolete("This type is no longer supported and will be removed in v1.0. Use CalcContext instead.")]
internal class CalculatorMany<TInput, TOutput> : ICalculator<TInput, TOutput> where TOutput : new() {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<InputProperty> _inputsMap;
    private readonly Memory<OutputProperty> _outputsMap;

    public CalculatorMany(FunnyCalculatorBuilder builder) {
        _builder = builder;
        _mutableApriori = new MutableAprioriTypesMap();
        _inputsMap = _mutableApriori.AddAprioriInputs<TInput>(_builder.Dialect.Converter);
        _outputsMap = _mutableApriori.AddManyAprioriOutputs<TOutput>(_builder.Dialect);
    }

    [Obsolete("This method is no longer supported and will be removed in v1.0. Use CalcContext instead.")]
    public TOutput Calc(string expression, TInput input) => ToLambda(expression)(input);

    [Obsolete("This method is no longer supported and will be removed in v1.0. Use CalcContext instead.")]
    public Func<TInput, TOutput> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);
        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);

        int isRunning = 0;
        return input => {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
            {
                // if runtime already run - create runtime copy, and run it
                return Run(runtime.Clone(), input);
            }

            try
            {
                return Run(runtime, input);
            }
            finally
            {
                isRunning = 0;
            }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TOutput Run(FunnyRuntime runtime, TInput input) {
        FluentApiTools.SetInputValues(runtime, _inputsMap, input);
        runtime.Run();
        return FluentApiTools.CreateOutputModelFromResults<TOutput>(runtime, _outputsMap);
    }
}

internal class NonGenericCalculator<TOutput> : ICalculator<object, TOutput> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<InputProperty> _inputsMap;
    private readonly IOutputFunnyConverter _outputConverter;
    public NonGenericCalculator (FunnyCalculatorBuilder builder, Type inputType) {
        FluentApiTools.ThrowIfInvalidDecimalDialectSettings<TOutput>(builder);

        _builder = builder;
        _mutableApriori = new MutableAprioriTypesMap();
        _inputsMap = _mutableApriori.AddAprioriTypeInputs(inputType, Dialects.Origin.Converter);

        _outputConverter = _builder.Dialect.Converter.GetOutputConverterFor(typeof(TOutput));
        if(_outputConverter.FunnyType!=FunnyType.Any)
            _mutableApriori.Add(Parser.AnonymousEquationId, _outputConverter.FunnyType);
    }

    public TOutput Calc(string expression, object inputModel) => ToLambda(expression)(inputModel);

    public Func<object, TOutput> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);

        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

        var outVariable = runtime[Parser.AnonymousEquationId];

        int isRunning = 0;
        return input => {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
            {
                // if runtime already run - create runtime copy, and run it
                var clone = runtime.Clone();
                return Run(clone, input, clone[Parser.AnonymousEquationId]);
            }

            try
            {
                return Run(runtime, input, outVariable);
            }
            finally
            {
                isRunning = 0;
            }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TOutput Run(FunnyRuntime runtime, object input, IFunnyVar outVariable) {
        FluentApiTools.SetInputValues(runtime, _inputsMap, input);
        runtime.Run();
        return (TOutput)_outputConverter.ToClrObject(outVariable.FunnyValue);
    }
}

internal class NonGenericCalculator : ICalculator<object, object> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<InputProperty> _inputsMap;
    private readonly IOutputFunnyConverter _outputConverter;
    public NonGenericCalculator (FunnyCalculatorBuilder builder, Type inputType) {
        _builder = builder;
        _mutableApriori = new MutableAprioriTypesMap();
        _inputsMap = _mutableApriori.AddAprioriTypeInputs(inputType, Dialects.Origin.Converter);
        _outputConverter = DynamicTypeOutputFunnyConverter.AnyConverter;
    }


    public object Calc(string expression, object inputModel) => ToLambda(expression)(inputModel);

    public Func<object, object> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);

        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

        var outVariable = runtime[Parser.AnonymousEquationId];

        int isRunning = 0;
        return input => {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
            {
                // if runtime already run - create runtime copy, and run it
                var clone = runtime.Clone();
                return Run(clone, input, clone[Parser.AnonymousEquationId]);
            }

            try
            {
                return Run(runtime, input, outVariable);
            }
            finally
            {
                isRunning = 0;
            }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object Run(FunnyRuntime runtime, object input, IFunnyVar outVariable) {
        FluentApiTools.SetInputValues(runtime, _inputsMap, input);
        runtime.Run();
        return _outputConverter.ToClrObject(outVariable.FunnyValue);
    }
}

internal class Calculator<TInput, TOutput> : ICalculator<TInput, TOutput> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<InputProperty> _inputsMap;
    private readonly IOutputFunnyConverter _outputConverter;

    public Calculator(FunnyCalculatorBuilder builder) {
        FluentApiTools.ThrowIfInvalidDecimalDialectSettings<TOutput>(builder);

        _builder = builder;
        _mutableApriori = new MutableAprioriTypesMap();
        _inputsMap = _mutableApriori.AddAprioriInputs<TInput>(Dialects.Origin.Converter);

        _outputConverter = _builder.Dialect.Converter.GetOutputConverterFor(typeof(TOutput));
        if (_outputConverter.FunnyType != FunnyType.Any)
            _mutableApriori.Add(Parser.AnonymousEquationId, _outputConverter.FunnyType);
    }

    public TOutput Calc(string expression, TInput input) => ToLambda(expression)(input);


    public Func<TInput, TOutput> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);

        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

        var outVariable = runtime[Parser.AnonymousEquationId];

        int isRunning = 0;
        return input => {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
            {
                // if runtime already run - create runtime copy, and run it
                var clone = runtime.Clone();
                return Run(clone, input, clone[Parser.AnonymousEquationId]);
            }

            try
            {
                return Run(runtime, input, outVariable);
            }
            finally
            {
                isRunning = 0;
            }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TOutput Run(FunnyRuntime runtime, TInput input, IFunnyVar outVariable) {
        FluentApiTools.SetInputValues(runtime, _inputsMap, input);
        runtime.Run();
        return (TOutput)_outputConverter.ToClrObject(outVariable.FunnyValue);
    }
}

internal class ContextCalculator<TContext> : IContextCalculator<TContext> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<OutputProperty> _outputsMap;
    private readonly Memory<InputProperty> _inputsMap;

    public ContextCalculator(FunnyCalculatorBuilder builder) {
        _builder = builder;
        _mutableApriori = new MutableAprioriTypesMap();

        _outputsMap = _mutableApriori.AddManyAprioriOutputs<TContext>(builder.Dialect);
        _inputsMap = _mutableApriori.AddAprioriInputs<TContext>(builder.Dialect.Converter, ignoreIfHasSetter: true);
    }

    public void Calc(string expression, TContext context) => ToLambda(expression)(context);

    public Action<TContext> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);
        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);
        int isRunning = 0;
        return context => {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
            {
                // if runtime already run - create runtime copy, and run it
                Run(runtime.Clone(), context);
            }
            else
            {
                try
                {
                    Run(runtime, context);
                }
                finally
                {
                    isRunning = 0;
                }
            }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Run(FunnyRuntime runtime, TContext context) {
        FluentApiTools.SetInputValues(runtime, _inputsMap, context);
        runtime.Run();
        var settedCount = FluentApiTools.SetResultsToModel(runtime, _outputsMap, context);
        if (settedCount == 0)
            throw Errors.NoOutputVariablesSetted(_outputsMap);
    }
}

internal class ConstantCalculator<TOutput> : IConstantCalculator<TOutput> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly IAprioriTypesMap _mutableApriori;
    private readonly IOutputFunnyConverter _outputConverter;

    public ConstantCalculator(FunnyCalculatorBuilder builder) {
        if (builder.Dialect.Converter.TypeBehaviour.RealType != typeof(decimal) && typeof(TOutput) == typeof(decimal))
            throw FunnyInvalidUsageException.DecimalTypeCannotBeUsedAsOutput();

        _outputConverter = builder.Dialect.Converter.GetOutputConverterFor(typeof(TOutput));
        _mutableApriori = new SingleAprioriTypesMap(Parser.AnonymousEquationId, _outputConverter.FunnyType);
        _builder = builder;
    }

    public TOutput Calc(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);
        FluentApiTools.ThrowIfHasInputs(runtime);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);
        // If there is no inputs, - it is thread safe. TODO - What about non pure functions?
        runtime.Run();
        return (TOutput)_outputConverter.ToClrObject(FluentApiTools.GetFunnyOut(runtime).FunnyValue);
    }
}

internal class ManyConstantsCalculator<TOutput> : IConstantCalculator<TOutput> where TOutput : new() {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<OutputProperty> _outputsMap;

    public ManyConstantsCalculator(FunnyCalculatorBuilder builder) {
        _mutableApriori = new MutableAprioriTypesMap();
        _outputsMap = _mutableApriori.AddManyAprioriOutputs<TOutput>(builder.Dialect);
        _builder = builder;
    }

    public TOutput Calc(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);
        FluentApiTools.ThrowIfHasInputs(runtime);
        // If there is no inputs, - it is thread safe. TODO - What about non pure functions?
        runtime.Run();
        return FluentApiTools.CreateOutputModelFromResults<TOutput>(runtime, _outputsMap);
    }
}

internal class ConstantCalculator : IConstantCalculator<object> {
    private readonly FunnyCalculatorBuilder _builder;

    public ConstantCalculator(FunnyCalculatorBuilder builder) => _builder = builder;

    public object Calc(string expression) {
        var runtime = _builder.CreateRuntime(expression, EmptyAprioriTypesMap.Instance);
        FluentApiTools.ThrowIfHasInputs(runtime);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);
        // If there is no inputs, - it is thread safe. TODO - What about non pure functions?
        runtime.Run();

        return FluentApiTools.GetFunnyOut(runtime).Value;
    }
}
