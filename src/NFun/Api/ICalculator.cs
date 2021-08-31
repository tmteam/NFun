using System;
using System.Reflection;
using NFun.Interpretation;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun
{
    public interface ICalculator<in TInput>
    {
        object Calc(string expression, TInput input);
        Func<TInput, object> ToLambda(string expression);
    }

    public interface ICalculator<in TInput, out TOutput>
    {
        TOutput Calc(string expression, TInput inputModel);
        Func<TInput, TOutput> ToLambda(string expression);
    }

    public interface IConstantCalculator<out TOutput>
    {
        TOutput Calc(string expression);
    }

    class Calculator<TInput> : ICalculator<TInput>
    {
        private readonly FunnyCalculatorBuilder _builder;
        private readonly AprioriTypesMap _apriories;
        private readonly Memory<(string, IInputFunnyConverter, PropertyInfo)> _inputsMap;

        public Calculator(FunnyCalculatorBuilder builder)
        {
            _builder = builder;

            _apriories = new AprioriTypesMap();
            _inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(_apriories);
        }

        public object Calc(string expression, TInput input)
            => ToLambda(expression)(input);

        public Func<TInput, object> ToLambda(string expression)
        {
            var runtime = _builder.CreateRuntime(expression, _apriories);
            FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);
            FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);

            return input =>
            {
                FluentApiTools.SetInputValues(runtime, _inputsMap, input);
                runtime.Run();
                return FluentApiTools.GetClrOut(runtime);
            };
        }
    }

    class CalculatorMany<TInput, TOutput> : ICalculator<TInput, TOutput> where TOutput : new()
    {
        private readonly FunnyCalculatorBuilder _builder;
        private readonly AprioriTypesMap _apriories;
        private readonly Memory<(string, IInputFunnyConverter, PropertyInfo)> _inputsMap;
        private readonly Memory<(string, IOutputFunnyConverter, PropertyInfo)> _outputsMap;

        public CalculatorMany(FunnyCalculatorBuilder builder)
        {
            _builder = builder;
            _apriories = new AprioriTypesMap();
            _inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(_apriories);
            _outputsMap = FluentApiTools.SetupManyAprioriOutputs<TOutput>(_apriories);
        }

        public TOutput Calc(string expression, TInput input) => ToLambda(expression)(input);

        public Func<TInput, TOutput> ToLambda(string expression)
        {
            var runtime = _builder.CreateRuntime(expression, _apriories);
            FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);
            return input =>
            {
                FluentApiTools.SetInputValues(runtime, _inputsMap, input);
                runtime.Run();
                return FluentApiTools.CreateOutputValueFromResults<TOutput>(runtime, _outputsMap);
            };
        }
    }

    class CalculatorSingle<TInput, TOutput> : ICalculator<TInput, TOutput>
    {
        private readonly FunnyCalculatorBuilder _builder;
        private readonly AprioriTypesMap _apriories;
        private readonly Memory<(string, IInputFunnyConverter, PropertyInfo)> _inputsMap;
        private readonly IOutputFunnyConverter _outputConverter;

        public CalculatorSingle(FunnyCalculatorBuilder builder)
        {
            _builder = builder;
            _apriories = new AprioriTypesMap();
            _inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(_apriories);

            _outputConverter = FunnyTypeConverters.GetOutputConverter(typeof(TOutput));
            _apriories.Add(Parser.AnonymousEquationId, _outputConverter.FunnyType);
        }

        public TOutput Calc(string expression, TInput input) => ToLambda(expression)(input);

        public Func<TInput, TOutput> ToLambda(string expression)
        {
            var runtime = _builder.CreateRuntime(expression, _apriories);

            FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);
            FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

            var outVariable = runtime[Parser.AnonymousEquationId];

            return input =>
            {
                FluentApiTools.SetInputValues(runtime, _inputsMap, input);
                runtime.Run();
                return (TOutput)_outputConverter.ToClrObject(outVariable.FunnyValue);
            };
        }
    }

    class ConstantCalculatorSingle<TOutput> : IConstantCalculator<TOutput>
    {
        private readonly FunnyCalculatorBuilder _builder;
        private readonly AprioriTypesMap _apriories;
        private readonly IOutputFunnyConverter _outputConverter;

        public ConstantCalculatorSingle(FunnyCalculatorBuilder builder)
        {
            _outputConverter = FunnyTypeConverters.GetOutputConverter(typeof(TOutput));
            _apriories = new AprioriTypesMap { { Parser.AnonymousEquationId, _outputConverter.FunnyType } };
            _builder = builder;
        }

        public TOutput Calc(string expression)
        {
            var runtime = _builder.CreateRuntime(expression, _apriories);
            FluentApiTools.ThrowIfHasInputs(runtime);
            FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

            runtime.Run();

            return (TOutput)_outputConverter.ToClrObject(FluentApiTools.GetOut(runtime).FunnyValue);
        }
    }

    class ConstantCalculatorMany<TOutput> : IConstantCalculator<TOutput> where TOutput : new()
    {
        private readonly FunnyCalculatorBuilder _builder;
        private readonly AprioriTypesMap _apriories;
        private readonly Memory<(string, IOutputFunnyConverter, PropertyInfo)> _outputsMap;

        public ConstantCalculatorMany(FunnyCalculatorBuilder builder)
        {
            _apriories = new AprioriTypesMap();
            _outputsMap = FluentApiTools.SetupManyAprioriOutputs<TOutput>(_apriories);
            _builder = builder;
        }

        public TOutput Calc(string expression)
        {
            var runtime = _builder.CreateRuntime(expression, _apriories);
            FluentApiTools.ThrowIfHasInputs(runtime);
            runtime.Run();
            return FluentApiTools.CreateOutputValueFromResults<TOutput>(runtime, _outputsMap);
        }
    }

    class ConstantCalculatorSingle : IConstantCalculator<object>
    {
        private readonly FunnyCalculatorBuilder _builder;
        private static readonly AprioriTypesMap Apriories = new();

        public ConstantCalculatorSingle(FunnyCalculatorBuilder builder)
        {
            _builder = builder;
        }

        public object Calc(string expression)
        {
            var runtime = _builder.CreateRuntime(expression, Apriories);
            FluentApiTools.ThrowIfHasInputs(runtime);
            FluentApiTools.ThrowIfHasNoDefaultOutput(runtime);

            runtime.Run();

            return FluentApiTools.GetOut(runtime).Value;
        }
    }
}