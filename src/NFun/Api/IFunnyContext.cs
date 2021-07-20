using System;
using System.Reflection;
using NFun.Interpretation;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun
{
    class FunnyContext<TInput> : IFunnyContext<TInput>
    {
        private readonly FunnyContextBuilder _builder;
        private readonly AprioriTypesMap _apriories;
        private readonly Memory<(string, IinputFunnyConverter, PropertyInfo)> _inputsMap;

        public FunnyContext(FunnyContextBuilder builder)
        {
            _builder = builder;
            
            _apriories = AprioriTypesMap.Empty;
            _inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(_apriories);
        }

        public object Calc(string expression, TInput input)
            => Build(expression)(input);

        public Func<TInput, object> Build(string expression)
        {
            var runtime = _builder.CreateRuntime(expression, _apriories);
            if (!runtime.HasDefaultOutput)
                throw ErrorFactory.OutputIsUnset();
            FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);

            return input =>
            {
                FluentApiTools.SetInputValues(runtime, _inputsMap, input);
                runtime.Run();
                return FluentApiTools.GetClrOut(runtime);
            };
        }
    }

    class FunnyContextMany<TInput, TOutput> : IFunnyContext<TInput, TOutput> where TOutput : new()
    {
        private readonly FunnyContextBuilder _builder;
        private readonly AprioriTypesMap _apriories;
        private readonly Memory<(string, IinputFunnyConverter, PropertyInfo)> _inputsMap;
        private readonly Memory<(string, IOutputFunnyConverter, PropertyInfo)> _outputsMap;

        public FunnyContextMany(FunnyContextBuilder builder)
        {
            _builder = builder;
            _apriories = AprioriTypesMap.Empty;
            _inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(_apriories);
            _outputsMap = FluentApiTools.SetupManyAprioriOutputs<TOutput>(_apriories);
        }

        public TOutput Calc(string expression, TInput input) => Build(expression)(input);

        public Func<TInput, TOutput> Build(string expression)
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

    class FunnyContextSingle<TInput, TOutput> : IFunnyContext<TInput, TOutput>
    {
        private readonly FunnyContextBuilder _builder;
        private readonly AprioriTypesMap _apriories;
        private readonly Memory<(string, IinputFunnyConverter, PropertyInfo)> _inputsMap;
        private readonly IOutputFunnyConverter _outputConverter;

        public FunnyContextSingle(FunnyContextBuilder builder)
        {
            _builder = builder;
            _apriories = AprioriTypesMap.Empty;
            _inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(_apriories);

            _outputConverter = FunnyTypeConverters.GetOutputConverter(typeof(TOutput));
            _apriories.Add(Parser.AnonymousEquationId, _outputConverter.FunnyType);
        }

        public TOutput Calc(string expression, TInput input) => Build(expression)(input);

        public Func<TInput, TOutput> Build(string expression)
        {
            var runtime = _builder.CreateRuntime(expression, _apriories);

            FluentApiTools.ThrowIfHasUnknownInputs(runtime, _inputsMap);

            if (!runtime.HasDefaultOutput)
                throw ErrorFactory.OutputIsUnset(_outputConverter.FunnyType);
            var outVariable = runtime[Parser.AnonymousEquationId];


            return input =>
            {
                FluentApiTools.SetInputValues(runtime, _inputsMap, input);
                runtime.Run();
                return (TOutput)_outputConverter.ToClrObject(outVariable.FunnyValue);
            };
        }
    }

    public interface IFunnyContext<TInput>
    {
        object Calc(string expression, TInput input);
        Func<TInput, object> Build(string expression);
    }

    public interface IFunnyContext<TInput, TOutput>
    {
        TOutput Calc(string expression, TInput inputModel);
        Func<TInput, TOutput> Build(string expression);
    }
}