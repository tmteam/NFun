using System;
using NFun.Interpritation;
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
            var apriories = AprioriTypesMap.Empty;
            var inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(apriories);
            
            var runtime =  _builder.CreateRuntime(expression,apriories);
            if(!runtime.HasDefaultOutput)
                throw ErrorFactory.OutputIsUnset();
            FluentApiTools.ThrowIfHasUnknownInputs(runtime,inputsMap);
            
            return input =>
            {
                var inputVals =FluentApiTools.GetInputValues(inputsMap, input);
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
            var apriories = AprioriTypesMap.Empty;
            var inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(apriories);
            var outputs = FluentApiTools.SetupManyAprioriOutputs<TOutput>(apriories);
            var runtime =  _builder.CreateRuntime(expression, apriories);
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
            var apriories = AprioriTypesMap.Empty;
            var inputsMap = FluentApiTools.SetupAprioriInputs<TInput>(apriories);

            var outputConverter = FunnyTypeConverters.GetOutputConverter(typeof(TOutput));
            apriories.Add(Parser.AnonymousEquationId, outputConverter.FunnyType);
            var runtime = _builder.CreateRuntime(expression,  apriories);

            FluentApiTools.ThrowIfHasUnknownInputs(runtime,inputsMap);
            
            if(!runtime.HasDefaultOutput)
                throw ErrorFactory.OutputIsUnset(outputConverter.FunnyType);
            var outVariable = runtime.GetVariable(Parser.AnonymousEquationId);
            
            
            return input => 
            {
                var inputValues = FluentApiTools.GetInputValues(inputsMap, input);
                runtime.CalculateSafe(inputValues);
                return (TOutput) outputConverter.ToClrObject(outVariable.FunnyValue);
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