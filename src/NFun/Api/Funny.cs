using System;

namespace NFun
{
    public static class Funny
    {
        public static HardcoreBuilder Hardcore { get; } = new();

        #region calcs

        public static object Calc(string expression)
            => FunnyContextBuilder.Empty.Calc(expression);

        public static TOutput Calc<TOutput>(string expression)
            => FunnyContextBuilder.Empty.Calc<TOutput>(expression);

        public static object Calc<TInput>(string expression, TInput input)
            => FunnyContextBuilder.Empty.Calc<TInput>(expression, input);

        public static TOutput Calc<TInput, TOutput>(string expression, TInput input)
            => FunnyContextBuilder.Empty.Calc<TInput, TOutput>(expression, input);

        public static TOutput CalcMany<TOutput>(string expression) where TOutput : new()
            => FunnyContextBuilder.Empty.CalcMany<TOutput>(expression);

        public static TOutput CalcMany<TInput, TOutput>(string expression, TInput input) where TOutput : new()
            => FunnyContextBuilder.Empty.CalcMany<TInput, TOutput>(expression, input);

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
            => new FunnyContextBuilder().WithFunction(id, function);

        public static FunnyContextBuilder WithFunction<Tin1, Tin2, TOut>(string id, Func<Tin1, Tin2, TOut> function)
            => new FunnyContextBuilder().WithFunction(id, function);

        public static FunnyContextBuilder WithFunction<Tin1, Tin2, Tin3, TOut>(string id,
            Func<Tin1, Tin2, Tin3, TOut> function)
            => new FunnyContextBuilder().WithFunction(id, function);

        public static FunnyContextBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, TOut>(string id,
            Func<Tin1, Tin2, Tin3, Tin4, TOut> function)
            => new FunnyContextBuilder().WithFunction(id, function);

        public static FunnyContextBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, TOut>(string id,
            Func<Tin1, Tin2, Tin3, Tin4, Tin5, TOut> function)
            => new FunnyContextBuilder().WithFunction(id, function);

        public static FunnyContextBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut>(string id,
            Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut> function)
            => new FunnyContextBuilder().WithFunction(id, function);

        public static FunnyContextBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut>(string id,
            Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut> function)
            => new FunnyContextBuilder().WithFunction(id, function);

        public static FunnyContextBuilder WithDialect(ClassicDialectSettings dialect)
            => new FunnyContextBuilder().WithDialect(dialect);

        #endregion
    }
}