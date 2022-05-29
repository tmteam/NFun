using System;
using System.Reflection;
using NFun.Exceptions;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun {

public interface ICalculator<in TInput> {
    object Calc(string expression, TInput input);
    Func<TInput, object> ToLambda(string expression);
}

public interface ICalculator<in TInput, out TOutput> {
    TOutput Calc(string expression, TInput inputModel);
    Func<TInput, TOutput> ToLambda(string expression);
}

public interface IContextCalculator<TContext> {
    void Calc(string expression, TContext context);
    Action<TContext> ToAction(string expression);
}


public interface IConstantCalculator<out TOutput> {
    TOutput Calc(string expression);
}

internal class Calculator<TInput> : ICalculator<TInput> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<InputProperty> _inputsMap;

    public Calculator(FunnyCalculatorBuilder builder) {
        _builder = builder;

        _mutableApriori = new MutableAprioriTypesMap();
        _inputsMap = FluentApiTools.AddAprioriInputs<TInput>(_mutableApriori, Dialects.Origin.TypeBehaviour);
    }

    public object Calc(string expression, TInput input)
        => ToLambda(expression)(input);

    public Func<TInput, object> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);
        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);

        return input => {
            FluentApiTools.SetInputValues(runtime, _inputsMap, input);
            runtime.Run();
            return FluentApiTools.GetFunnyOut(runtime).Value;
        };
    }
}

internal class CalculatorMany<TInput, TOutput> : ICalculator<TInput, TOutput> where TOutput : new() {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<InputProperty> _inputsMap;
    private readonly Memory<OutputProperty> _outputsMap;

    public CalculatorMany(FunnyCalculatorBuilder builder) {
        _builder = builder;
        _mutableApriori = new MutableAprioriTypesMap();
        _inputsMap = _mutableApriori.AddAprioriInputs<TInput>(_builder.Dialect.TypeBehaviour);
        _outputsMap = _mutableApriori.AddManyAprioriOutputs<TOutput>(_builder.Dialect);
    }

    public TOutput Calc(string expression, TInput input) => ToLambda(expression)(input);

    public Func<TInput, TOutput> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);
        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);
        return input => {
            FluentApiTools.SetInputValues(runtime, _inputsMap, input);
            runtime.Run();
            return FluentApiTools.CreateOutputModelFromResults<TOutput>(runtime, _outputsMap);
        };
    }
}

internal class CalculatorSingle<TInput, TOutput> : ICalculator<TInput, TOutput> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<InputProperty> _inputsMap;
    private readonly IOutputFunnyConverter _outputConverter;

    public CalculatorSingle(FunnyCalculatorBuilder builder) {
        if (builder.Dialect.TypeBehaviour.DoubleIsReal && typeof(TOutput) == typeof(decimal))
            throw FunnyInvalidUsageException.DecimalTypeCannotBeUsedAsOutput();
        
        _builder = builder;
        _mutableApriori = new MutableAprioriTypesMap();
        _inputsMap = FluentApiTools.AddAprioriInputs<TInput>(_mutableApriori, Dialects.Origin.TypeBehaviour);

        _outputConverter = TypeBehaviourExtensions.GetOutputConverterFor(_builder.Dialect.TypeBehaviour, typeof(TOutput));
        _mutableApriori.Add(Parser.AnonymousEquationId, _outputConverter.FunnyType);
    }

    public TOutput Calc(string expression, TInput input) => ToLambda(expression)(input);

    public Func<TInput, TOutput> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);

        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

        var outVariable = runtime[Parser.AnonymousEquationId];

        return input => {
            FluentApiTools.SetInputValues(runtime, _inputsMap, input);
            runtime.Run();
            return (TOutput)_outputConverter.ToClrObject(outVariable.FunnyValue);
        };
    }
}

internal class ConstantCalculatorSingle<TOutput> : IConstantCalculator<TOutput> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly IAprioriTypesMap _mutableApriori;
    private readonly IOutputFunnyConverter _outputConverter;
   
   
    public ConstantCalculatorSingle(FunnyCalculatorBuilder builder) {
        if (builder.Dialect.TypeBehaviour.DoubleIsReal && typeof(TOutput) == typeof(decimal))
            throw FunnyInvalidUsageException.DecimalTypeCannotBeUsedAsOutput();
        
        _outputConverter = builder.Dialect.TypeBehaviour.GetOutputConverterFor(typeof(TOutput));
        _mutableApriori = new SingleAprioriTypesMap( Parser.AnonymousEquationId, _outputConverter.FunnyType);
        _builder = builder;
    }

    public TOutput Calc(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);
        FluentApiTools.ThrowIfHasInputs(runtime);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

        runtime.Run();

        return (TOutput)_outputConverter.ToClrObject(FluentApiTools.GetFunnyOut(runtime).FunnyValue);
    }
}

internal class ConstantCalculatorMany<TOutput> : IConstantCalculator<TOutput> where TOutput : new() {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly Memory<OutputProperty> _outputsMap;

    public ConstantCalculatorMany(FunnyCalculatorBuilder builder) {
        _mutableApriori = new MutableAprioriTypesMap();
        _outputsMap = _mutableApriori.AddManyAprioriOutputs<TOutput>(builder.Dialect);
        _builder = builder;
    }

    public TOutput Calc(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);
        FluentApiTools.ThrowIfHasInputs(runtime);
        runtime.Run();
        return FluentApiTools.CreateOutputModelFromResults<TOutput>(runtime, _outputsMap);
    }
}

internal class ConstantCalculatorSingle : IConstantCalculator<object> {
    private readonly FunnyCalculatorBuilder _builder;

    public ConstantCalculatorSingle(FunnyCalculatorBuilder builder) { _builder = builder; }

    public object Calc(string expression) {
        var runtime = _builder.CreateRuntime(expression, EmptyAprioriTypesMap.Instance);
        FluentApiTools.ThrowIfHasInputs(runtime);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

        runtime.Run();

        return FluentApiTools.GetFunnyOut(runtime).Value;
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
        _inputsMap = _mutableApriori.AddAprioriInputs<TContext>(builder.Dialect.TypeBehaviour, ignoreIfHasSetter: true);
        //todo - method implementations
    }

    public void Calc(string expression, TContext context) => ToAction(expression)(context);

    public Action<TContext> ToAction(string expression) {
        var runtime = _builder.CreateRuntime(expression, _mutableApriori);
        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);
        return context => {
            FluentApiTools.SetInputValues(runtime, _inputsMap, context);
            runtime.Run();
            var settedCount = FluentApiTools.SetResultsToModel(runtime, _outputsMap, context);
            if (settedCount == 0)
                throw Errors.NoOutputVariablesSetted(_outputsMap);
        };
    }
}

}