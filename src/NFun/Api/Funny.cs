using System;
using System.Linq;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.Types;

namespace NFun
{
    public static class Funny
    {
        public static HardcoreBuilder Hardcore { get; } = new HardcoreBuilder();
        #region calcs

        public static object Calc(string expression)
        {
            var runtime = FunBuilder
                .With(expression)
                .Build();
            if (runtime.Inputs.Any())
                throw ErrorFactory.UnknownInputs(
                    runtime.GetInputVariableUsages(),
                    new VarInfo[0]);
            
            var result = runtime.CalculateSafe();
            return FluentApiTools.GetClrOut(result);
        }
        public static TOutput Calc<TOutput>(string expression)
        {
            var builder = FunBuilder.With(expression);
            return FluentApiTools.CalcSingleOutput<TOutput>(builder);
        }
        
        public static object Calc<TInput>(string expression, TInput input) 
            => ForCalc<TInput>().Calc(expression, input);

        public static TOutput Calc<TInput, TOutput>(string expression, TInput input) 
            => ForCalc<TInput, TOutput>().Calc(expression, input);

        public static TOutput CalcMany<TOutput>(string expression) where TOutput: new()
        {
            var builder = FunBuilder.With(expression);
            
            var outputs = FluentApiTools.SetupManyAprioriOutputs<TOutput>(builder);

            var runtime = builder.Build();
            if (runtime.Inputs.Any())
                throw ErrorFactory.UnknownInputs(runtime.GetInputVariableUsages(), new VarInfo[0]);
            
            var calcResults = runtime.CalculateSafe();
            return FluentApiTools.CreateOutputValueFromResults<TOutput>(outputs, calcResults);
        }
        
        public static TOutput CalcMany<TInput, TOutput>(string expression, TInput input) where TOutput: new() 
            => ForCalcMany<TInput, TOutput>().Calc(expression, input);
        #endregion

        #region forCalc
        public static IFunnyContext<TInput, TOutput> ForCalc<TInput, TOutput>() 
            => new FunnyContextSingle<TInput, TOutput>(FunnyContextBuilder.Empty);
        public static IFunnyContext<TInput, TOutput> ForCalcMany<TInput, TOutput>() where TOutput : new()
            => new FunnyContextMany<TInput, TOutput>(FunnyContextBuilder.Empty);
        public static IFunnyContext<TInput> ForCalc<TInput>()
            => new FunnyContext<TInput>(FunnyContextBuilder.Empty);
        #endregion
        
        #region builder
         public static FunnyContextBuilder WithConstant(string id, object value) 
             => new FunnyContextBuilder().WithConstant(id, value);
         public static FunnyContextBuilder WithFunction<Tin, TOut>(string id, Func<Tin, TOut> function)
             => new FunnyContextBuilder().WithFunction(id,function);
         #endregion
    }
}