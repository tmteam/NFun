using System;
using System.Reflection;
using NFun.Exceptions;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
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
}


public interface IConstantCalculator<out TOutput> {
    TOutput Calc(string expression);
}

internal class Calculator<TInput> : ICalculator<TInput> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly AprioriTypesMap _apriori;
    private readonly Memory<(string, IInputFunnyConverter, PropertyInfo)> _inputsMap;

    public Calculator(FunnyCalculatorBuilder builder) {
        _builder = builder;

        _apriori = new AprioriTypesMap();
        _inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(_apriori, Dialects.Origin.TypeBehaviour);
    }

    public object Calc(string expression, TInput input)
        => ToLambda(expression)(input);

    public Func<TInput, object> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _apriori);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);
        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);

        return input => {
            FluentApiTools.SetInputValues(runtime, _inputsMap, input);
            runtime.Run();
            return FluentApiTools.GetClrOut(runtime);
        };
    }
}

internal class CalculatorMany<TInput, TOutput> : ICalculator<TInput, TOutput> where TOutput : new() {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly AprioriTypesMap _apriori;
    private readonly Memory<(string, IInputFunnyConverter, PropertyInfo)> _inputsMap;
    private readonly Memory<(string, IOutputFunnyConverter, PropertyInfo)> _outputsMap;

    public CalculatorMany(FunnyCalculatorBuilder builder) {
        _builder = builder;
        _apriori = new AprioriTypesMap();
        _inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(_apriori, _builder.Dialect.TypeBehaviour);
        _outputsMap = FluentApiTools.SetupManyAprioriOutputs<TOutput>(_apriori, _builder.Dialect);
    }

    public TOutput Calc(string expression, TInput input) => ToLambda(expression)(input);

    public Func<TInput, TOutput> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _apriori);
        FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);
        return input => {
            FluentApiTools.SetInputValues(runtime, _inputsMap, input);
            runtime.Run();
            return FluentApiTools.CreateOutputValueFromResults<TOutput>(runtime, _outputsMap);
        };
    }
}

internal class CalculatorSingle<TInput, TOutput> : ICalculator<TInput, TOutput> {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly AprioriTypesMap _apriori;
    private readonly Memory<(string, IInputFunnyConverter, PropertyInfo)> _inputsMap;
    private readonly IOutputFunnyConverter _outputConverter;

    public CalculatorSingle(FunnyCalculatorBuilder builder) {
        if (builder.Dialect.TypeBehaviour.DoubleIsReal && typeof(TOutput) == typeof(decimal))
            throw FunnyInvalidUsageException.DecimalTypeCannotBeUsedAsOutput();
        
        _builder = builder;
        _apriori = new AprioriTypesMap();
        _inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(_apriori, Dialects.Origin.TypeBehaviour);

        _outputConverter = TypeBehaviourExtensions.GetOutputConverterFor(_builder.Dialect.TypeBehaviour, typeof(TOutput));
        _apriori.Add(Parser.AnonymousEquationId, _outputConverter.FunnyType);
    }

    public TOutput Calc(string expression, TInput input) => ToLambda(expression)(input);

    public Func<TInput, TOutput> ToLambda(string expression) {
        var runtime = _builder.CreateRuntime(expression, _apriori);

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
    private readonly AprioriTypesMap _apriori;
    private readonly IOutputFunnyConverter _outputConverter;
   
   
    public ConstantCalculatorSingle(FunnyCalculatorBuilder builder) {
        if (builder.Dialect.TypeBehaviour.DoubleIsReal && typeof(TOutput) == typeof(decimal))
            throw FunnyInvalidUsageException.DecimalTypeCannotBeUsedAsOutput();
        
        _outputConverter = TypeBehaviourExtensions.GetOutputConverterFor(builder.Dialect.TypeBehaviour, typeof(TOutput));
        _apriori = new AprioriTypesMap { { Parser.AnonymousEquationId, _outputConverter.FunnyType } };
        _builder = builder;
    }

    public TOutput Calc(string expression) {
        var runtime = _builder.CreateRuntime(expression, _apriori);
        FluentApiTools.ThrowIfHasInputs(runtime);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

        runtime.Run();

        return (TOutput)_outputConverter.ToClrObject(FluentApiTools.GetOut(runtime).FunnyValue);
    }
}

internal class ConstantCalculatorMany<TOutput> : IConstantCalculator<TOutput> where TOutput : new() {
    private readonly FunnyCalculatorBuilder _builder;
    private readonly AprioriTypesMap _apriori;
    private readonly Memory<(string, IOutputFunnyConverter, PropertyInfo)> _outputsMap;

    public ConstantCalculatorMany(FunnyCalculatorBuilder builder) {
        _apriori = new AprioriTypesMap();
        _outputsMap = FluentApiTools.SetupManyAprioriOutputs<TOutput>(_apriori, builder.Dialect);
        _builder = builder;
    }

    public TOutput Calc(string expression) {
        var runtime = _builder.CreateRuntime(expression, _apriori);
        FluentApiTools.ThrowIfHasInputs(runtime);
        runtime.Run();
        return FluentApiTools.CreateOutputValueFromResults<TOutput>(runtime, _outputsMap);
    }
}

internal class ConstantCalculatorSingle : IConstantCalculator<object> {
    private readonly FunnyCalculatorBuilder _builder;
    private static readonly AprioriTypesMap Apriori = new();

    public ConstantCalculatorSingle(FunnyCalculatorBuilder builder) { _builder = builder; }

    public object Calc(string expression) {
        var runtime = _builder.CreateRuntime(expression, Apriori);
        FluentApiTools.ThrowIfHasInputs(runtime);
        FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

        runtime.Run();

        return FluentApiTools.GetOut(runtime).Value;
    }
}

}