using System;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun
{
    class FunnyContext<TInput> : IFunnyContext<TInput>
    {
        private readonly FunnyContextBuilder _builder;
        public FunnyContext(FunnyContextBuilder builder)
        {
            _builder = builder;
        }

        public object Calc(string expression, TInput input) 
            => Build(expression)(input);
        public Func<TInput, object> Build(string expression)
        {
            var builder = _builder.CreateRuntimeBuilder(expression);
            var inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(builder);
            var runtime = builder.Build();
            if(!runtime.HasDefaultOutput)
                throw ErrorFactory.OutputIsUnset();
            FluentApiTools.ThrowIfHasUnknownInputs(runtime,inputsMap);
            
            return input =>
            {
                var inputVals =FluentApiTools.GetInputValues(inputsMap, input);
                //var varsAsArray = inputVals.ToArray();
                // if (runtime.Inputs.Any(i => varsAsArray.All(v => !v.Name.Equals(i.Name.ToLower()))))
                //     throw new FunInvalidUsageException();
                var result = runtime.CalculateSafe(inputVals);
                return FluentApiTools.GetClrOut(result);
            };
        }
    }
    
    class FunnyContextMany<TInput, TOutput>:IFunnyContext<TInput, TOutput> where TOutput: new()
    {
        private readonly FunnyContextBuilder _builder;
        public FunnyContextMany(FunnyContextBuilder builder)
        {
            _builder = builder;
        }
        
        public TOutput Calc(string expression, TInput input) => Build(expression)(input);
        public Func<TInput, TOutput> Build(string expression)
        {
            var builder = _builder.CreateRuntimeBuilder(expression);
            var inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(builder);
            var outputs = FluentApiTools.SetupManyAprioriOutputs<TOutput>(builder);
            var runtime = builder.Build();
            FluentApiTools.ThrowIfHasUnknownInputs(runtime,inputsMap);

            return input =>
            {
                
                var inputValues = FluentApiTools.GetInputValues(inputsMap, input);
                var calcResults = runtime.CalculateSafe(inputValues);
                return FluentApiTools.CreateOutputValueFromResults<TOutput>(outputs, calcResults);
            };
        }
    }
    
    class FunnyContextSingle<TInput, TOutput>:IFunnyContext<TInput, TOutput>
    {
        private readonly FunnyContextBuilder _builder;
        public FunnyContextSingle(FunnyContextBuilder builder)
        {
            _builder = builder;
        }

        public TOutput Calc(string expression, TInput input) => Build(expression)(input);
        public Func<TInput, TOutput> Build(string expression)
        {
            var builder = _builder.CreateRuntimeBuilder(expression);
            var inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(builder);
            
            var outputConverter = FunnyTypeConverters.GetOutputConverter(typeof(TOutput));
            builder.WithApriori(Parser.AnonymousEquationId, outputConverter.FunnyType);
            var runtime = builder.Build();

            FluentApiTools.ThrowIfHasUnknownInputs(runtime,inputsMap);
            
            if(!runtime.HasDefaultOutput)
                throw ErrorFactory.OutputIsUnset(outputConverter.FunnyType);
                
            return input => 
            {
                var inputValues = FluentApiTools.GetInputValues(inputsMap, input);
                var result = runtime
                    .CalculateSafe(inputValues)
                    .Get(Parser.AnonymousEquationId)
                    .Value;
                return (TOutput) outputConverter.ToClrObject(result);
            };
        }
    }

    public interface IFunnyContext<TInput> {
        object Calc(string expression, TInput input);
        Func<TInput, object> Build(string expression);
    }
    
    public interface IFunnyContext<TInput, TOutput> {
        TOutput Calc(string expression, TInput inputModel);
        Func<TInput, TOutput> Build(string expression);
    }
}