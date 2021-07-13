using System;
using System.Linq;
using NFun.Interpretation;
using NFun.ParseErrors;
using NFun.Types;

namespace NFun
{
    public static class Funny
    {
        public static HardcoreBuilder Hardcore { get; } = new();
        #region calcs

        public static object Calc(string expression)
        {
            var runtime = RuntimeBuilder.Build(expression, BaseFunctions.DefaultFunctions);
            if (runtime.Variables.Any(v=>!v.IsOutput))
                throw ErrorFactory.UnknownInputs(runtime.GetInputVariableUsages());
            runtime.Run();
            
            return FluentApiTools.GetClrOut(runtime);
        }
        public static TOutput Calc<TOutput>(string expression) 
            => FluentApiTools.CalcSingleOutput<TOutput>(expression);

        public static object Calc<TInput>(string expression, TInput input) 
            => ForCalc<TInput>().Calc(expression, input);

        public static TOutput Calc<TInput, TOutput>(string expression, TInput input) 
            => ForCalc<TInput, TOutput>().Calc(expression, input);

        public static TOutput CalcMany<TOutput>(string expression) where TOutput: new()
        {
            var apriories = AprioriTypesMap.Empty; 
            var outputs   = FluentApiTools.SetupManyAprioriOutputs<TOutput>(apriories);

            var runtime = RuntimeBuilder.Build(expression, BaseFunctions.DefaultFunctions, aprioriTypesMap:apriories);
            if (runtime.Variables.Any(v=>!v.IsOutput))
                throw ErrorFactory.UnknownInputs(runtime.GetInputVariableUsages());
            
            runtime.Run();
            return FluentApiTools.CreateOutputValueFromResults<TOutput>(runtime, outputs);
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
         public static FunnyContextBuilder WithFunction<Tin1,Tin2,TOut>(string id, Func<Tin1,Tin2,TOut> function)
             => new FunnyContextBuilder().WithFunction(id,function);
         public static FunnyContextBuilder WithFunction<Tin1,Tin2,Tin3,TOut>(string id, Func<Tin1,Tin2,Tin3,TOut> function)
             => new FunnyContextBuilder().WithFunction(id,function);
         public static FunnyContextBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,TOut>(string id, Func<Tin1,Tin2,Tin3,Tin4,TOut> function)
             => new FunnyContextBuilder().WithFunction(id,function);
         public static FunnyContextBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,TOut>(string id, Func<Tin1,Tin2,Tin3,Tin4,Tin5,TOut> function)
             => new FunnyContextBuilder().WithFunction(id,function);
         public static FunnyContextBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,TOut>(string id, Func<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,TOut> function)
             => new FunnyContextBuilder().WithFunction(id,function);
         public static FunnyContextBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,Tin7,TOut>(string id, Func<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,Tin7,TOut> function)
             => new FunnyContextBuilder().WithFunction(id,function);
         #endregion
    }
}