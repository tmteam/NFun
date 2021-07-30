using System;

namespace NFun
{
    public static class Funny
    {
        public static HardcoreBuilder Hardcore { get; } = new();

        #region calcs

        public static object Calc(string expression)
            => FunnyCalculatorBuilder.Default.Calc(expression);

        public static TOutput Calc<TOutput>(string expression) 
            => FunnyCalculatorBuilder.Default.Calc<TOutput>(expression);

        public static object Calc<TInput>(string expression, TInput input)
            => FunnyCalculatorBuilder.Default.Calc<TInput>(expression, input);

        public static TOutput Calc<TInput, TOutput>(string expression, TInput input)
            => FunnyCalculatorBuilder.Default.Calc<TInput, TOutput>(expression, input);

        public static TOutput CalcMany<TOutput>(string expression) where TOutput : new()
            => FunnyCalculatorBuilder.Default.CalcMany<TOutput>(expression);

        public static TOutput CalcMany<TInput, TOutput>(string expression, TInput input) where TOutput : new()
            => FunnyCalculatorBuilder.Default.CalcMany<TInput, TOutput>(expression, input);

        #endregion

        #region Calculator factories
        
        public static ICalculator<TInput> ForCalc<TInput>()
            => FunnyCalculatorBuilder.Default.BuildForCalc<TInput>();

        public static ICalculator<TInput, TOutput> ForCalc<TInput, TOutput>()
            => FunnyCalculatorBuilder.Default.BuildForCalc<TInput, TOutput>();

        public static ICalculator<TInput, TOutput> ForCalcMany<TInput, TOutput>() where TOutput : new()
            => FunnyCalculatorBuilder.Default.BuildForCalcMany<TInput, TOutput>();
        
        public static IConstantCalculator<object> ForCalcConstant()
            => FunnyCalculatorBuilder.Default.BuildForCalcConstant();
        
        public static IConstantCalculator<TOutput> ForCalcConstant<TOutput>()
            => FunnyCalculatorBuilder.Default.BuildForCalcConstant<TOutput>();
        
        public static IConstantCalculator<TOutput> ForCalcManyConstants<TOutput>() where TOutput : new() 
            => FunnyCalculatorBuilder.Default.BuildForCalcManyConstants<TOutput>();
        
        #endregion

        #region builder

        public static FunnyCalculatorBuilder WithConstant(string id, object value)
            => new FunnyCalculatorBuilder().WithConstant(id, value);

        public static FunnyCalculatorBuilder WithFunction<Tin, TOut>(string id, Func<Tin, TOut> function)
            => new FunnyCalculatorBuilder().WithFunction(id, function);

        public static FunnyCalculatorBuilder WithFunction<Tin1, Tin2, TOut>(string id, Func<Tin1, Tin2, TOut> function)
            => new FunnyCalculatorBuilder().WithFunction(id, function);

        public static FunnyCalculatorBuilder WithFunction<Tin1, Tin2, Tin3, TOut>(string id,
            Func<Tin1, Tin2, Tin3, TOut> function)
            => new FunnyCalculatorBuilder().WithFunction(id, function);

        public static FunnyCalculatorBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, TOut>(string id,
            Func<Tin1, Tin2, Tin3, Tin4, TOut> function)
            => new FunnyCalculatorBuilder().WithFunction(id, function);

        public static FunnyCalculatorBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, TOut>(string id,
            Func<Tin1, Tin2, Tin3, Tin4, Tin5, TOut> function)
            => new FunnyCalculatorBuilder().WithFunction(id, function);

        public static FunnyCalculatorBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut>(string id,
            Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut> function)
            => new FunnyCalculatorBuilder().WithFunction(id, function);

        public static FunnyCalculatorBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut>(string id,
            Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut> function)
            => new FunnyCalculatorBuilder().WithFunction(id, function);

        public static FunnyCalculatorBuilder WithDialect(DialectSettings dialect)
            => new FunnyCalculatorBuilder().WithDialect(dialect);

        #endregion
    }
}