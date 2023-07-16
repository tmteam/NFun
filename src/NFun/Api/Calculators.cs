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

internal abstract class CalculatorBase<TInput, TOutput> : ICalculator<TInput, TOutput> {

    protected readonly FunnyCalculatorBuilder Builder;
    protected readonly MutableAprioriTypesMap MutableApriori;
    protected Memory<InputProperty> InputsMap;

    protected CalculatorBase(FunnyCalculatorBuilder builder) {
        Builder = builder;
        MutableApriori = new MutableAprioriTypesMap();
    }

    public TOutput Calc(string expression, TInput input)
        => ToLambda(expression)(input);

    public Func<TInput, TOutput> ToLambda(string expression) {
        var runtime = Builder.CreateRuntime(expression, MutableApriori);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);
        FluentApiTools.ThrowIfHasUnknownInputs(runtime, InputsMap);

        var outVariable = runtime[Parser.AnonymousEquationId];

        int isRunning = 0;
        return input => {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
            {
                // if runtime is already running - create runtime copy, and run it
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

    protected abstract TOutput Run(FunnyRuntime runtime, TInput input, IFunnyVar outVariable);

}


internal class Calculator<TInput> : CalculatorBase<TInput, object> {

    public Calculator(FunnyCalculatorBuilder builder):base(builder) {
        InputsMap = MutableApriori.AddAprioriInputs<TInput>(Dialects.Origin.Converter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override object Run(FunnyRuntime runtime, TInput input, IFunnyVar outVariable) {
        FluentApiTools.SetInputValues(runtime, InputsMap, input);
        runtime.Run();
        return FluentApiTools.GetFunnyOut(runtime).Value;
    }
}

internal class NonGenericCalculator<TOutput> : CalculatorBase<object, TOutput> {
    private readonly IOutputFunnyConverter _outputConverter;
    public NonGenericCalculator (FunnyCalculatorBuilder builder, Type inputType):base(builder) {
        FluentApiTools.ThrowIfInvalidDecimalDialectSettings<TOutput>(builder);

        InputsMap = MutableApriori.AddAprioriTypeInputs(inputType, Dialects.Origin.Converter);
        _outputConverter = Builder.Dialect.Converter.GetOutputConverterFor(typeof(TOutput));
        if(_outputConverter.FunnyType!=FunnyType.Any)
            MutableApriori.Add(Parser.AnonymousEquationId, _outputConverter.FunnyType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override TOutput Run(FunnyRuntime runtime, object input, IFunnyVar outVariable) {

        FluentApiTools.SetInputValues(runtime, InputsMap, input);
        runtime.Run();
        return (TOutput)_outputConverter.ToClrObject(outVariable.FunnyValue);
    }
}

internal class NonGenericCalculator : CalculatorBase<object, object> {
    public NonGenericCalculator (FunnyCalculatorBuilder builder, Type inputType):base(builder) {
        InputsMap = MutableApriori.AddAprioriTypeInputs(inputType, Dialects.Origin.Converter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override object Run(FunnyRuntime runtime, object input, IFunnyVar outVariable) {
        FluentApiTools.SetInputValues(runtime, InputsMap, input);
        runtime.Run();
        return DynamicTypeOutputFunnyConverter.AnyConverter.ToClrObject(outVariable.FunnyValue);
    }
}

internal class Calculator<TInput, TOutput> : CalculatorBase<TInput, TOutput> {
    private readonly IOutputFunnyConverter _outputConverter;

    public Calculator(FunnyCalculatorBuilder builder):base(builder) {
        FluentApiTools.ThrowIfInvalidDecimalDialectSettings<TOutput>(builder);

        InputsMap = MutableApriori.AddAprioriInputs<TInput>(Dialects.Origin.Converter);
        _outputConverter = Builder.Dialect.Converter.GetOutputConverterFor(typeof(TOutput));
        if (_outputConverter.FunnyType != FunnyType.Any)
            MutableApriori.Add(Parser.AnonymousEquationId, _outputConverter.FunnyType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override TOutput Run(FunnyRuntime runtime, TInput input, IFunnyVar outVariable) {
        FluentApiTools.SetInputValues(runtime, InputsMap, input);
        runtime.Run();
        return (TOutput)_outputConverter.ToClrObject(outVariable.FunnyValue);
    }
}



internal class ContextCalculatorBase<TContext> : IContextCalculator<TContext> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    protected readonly Memory<OutputProperty> _outputsMap;
    protected readonly Memory<InputProperty> _inputsMap;

    public ContextCalculatorBase(FunnyCalculatorBuilder builder) {
        _builder = builder;
        _mutableApriori = new MutableAprioriTypesMap();
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

public static class ContextCalculator {
    public static IContextCalculator<TContext> Create<TContext>(FunnyCalculatorBuilder builder) =>
        new PrivateContextCalculator<TContext>(builder, typeof(TContext));

    public static IContextCalculator<object> Create(FunnyCalculatorBuilder builder, Type type) =>
        new PrivateContextCalculator<object>(builder, type);

    private class PrivateContextCalculator<TContext> : IContextCalculator<TContext> {
        private readonly FunnyCalculatorBuilder _builder;
        private readonly MutableAprioriTypesMap _mutableApriori;
        private readonly Memory<OutputProperty> _outputsMap;
        private readonly Memory<InputProperty> _inputsMap;

        public PrivateContextCalculator(FunnyCalculatorBuilder builder, Type contextType) {
            _builder = builder;
            _mutableApriori = new MutableAprioriTypesMap();

            _outputsMap = _mutableApriori.AddManyAprioriOutputs(contextType, builder.Dialect);
            _inputsMap = _mutableApriori.AddAprioriInputs(contextType, builder.Dialect.Converter, ignoreIfHasSetter: true);
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
