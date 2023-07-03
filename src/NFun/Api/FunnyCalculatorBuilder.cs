using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun;

public class FunnyCalculatorBuilder {
    internal static FunnyCalculatorBuilder Default => new();
    internal DialectSettings Dialect => _dialect;
    private DialectSettings _dialect = Dialects.Origin;
    private readonly List<(string, object)> _constantList = new();
    private readonly List<Func<DialectSettings,IConcreteFunction>> _customFunctionFactories = new();

    private FunnyCalculatorBuilder WithDialect(DialectSettings dialect) {
        _dialect = dialect;
        return this;
    }

    /// <summary>
    /// Allows to setup syntax and semantics
    /// </summary>
    /// <param name="ifExpressionSyntax">If-expression syntax settings</param>
    /// <param name="integerPreferredType">Which funny type is prefered for integer constant</param>
    /// <param name="realClrType">Which clr type is used for funny type real</param>
    /// <param name="integerOverflow">Checked or Unchecked arithmetic operations</param>
    /// <param name="allowUserFunctions">Allow or deny regular or recursive user functions</param>
    public FunnyCalculatorBuilder WithDialect(IfExpressionSetup ifExpressionSyntax = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble,
        IntegerOverflow integerOverflow = IntegerOverflow.Checked,
        AllowUserFunctions allowUserFunctions = AllowUserFunctions.AllowAll)
        => WithDialect(Dialects.ModifyOrigin(ifExpressionSyntax, integerPreferredType, realClrType, integerOverflow, allowUserFunctions));

    public FunnyCalculatorBuilder WithConstant(string id, object value) {
        _constantList.Add((id, value));
        return this;
    }

    public FunnyCalculatorBuilder WithFunction<Tin, TOut>(string name, Func<Tin, TOut> function) {
        _customFunctionFactories.Add(d=> LambdaWrapperFactory.Create(name, function, d.Converter));
        return this;
    }

    public FunnyCalculatorBuilder WithFunction<Tin1, Tin2, TOut>(string name, Func<Tin1, Tin2, TOut> function) {
        _customFunctionFactories.Add(d=>LambdaWrapperFactory.Create(name, function, d.Converter));
        return this;
    }

    public FunnyCalculatorBuilder WithFunction<Tin1, Tin2, Tin3, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, TOut> function) {
        _customFunctionFactories.Add(d=>LambdaWrapperFactory.Create(name, function, d.Converter));
        return this;
    }

    public FunnyCalculatorBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, TOut> function) {
        _customFunctionFactories.Add(d=>LambdaWrapperFactory.Create(name, function, d.Converter));
        return this;
    }

    public FunnyCalculatorBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, TOut> function) {
        _customFunctionFactories.Add(d=>LambdaWrapperFactory.Create(name, function, d.Converter));
        return this;
    }

    public FunnyCalculatorBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut> function) {
        _customFunctionFactories.Add(d=>LambdaWrapperFactory.Create(name, function, d.Converter));
        return this;
    }

    public FunnyCalculatorBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut> function) {
        _customFunctionFactories.Add(d=>LambdaWrapperFactory.Create(name, function, d.Converter));
        return this;
    }

    public ICalculator<TInput> BuildForCalc<TInput>()
        => new Calculator<TInput>(this);

    public ICalculator<object, TOutput> BuildForDynamicTypeCalc<TOutput>(Type inputType)
        => new CalculatorSingleDynamic<TOutput>(this, inputType);

    public ICalculator<TInput, TOutput> BuildForCalc<TInput, TOutput>()
        => new CalculatorSingle<TInput, TOutput>(this);

    [Obsolete("This method is no longer supported and will be removed in v1.0. Use CalcContext instead.")]
    public ICalculator<TInput, TOutput> BuildForCalcMany<TInput, TOutput>() where TOutput : new()
        => new CalculatorMany<TInput, TOutput>(this);

    public IConstantCalculator<object> BuildForCalcConstant()
        => new ConstantCalculatorSingle(this);

    public IConstantCalculator<TOutput> BuildForCalcConstant<TOutput>()
        => new ConstantCalculatorSingle<TOutput>(this);

    public IConstantCalculator<TOutput> BuildForCalcManyConstants<TOutput>() where TOutput : new()
        => new ConstantCalculatorMany<TOutput>(this);

    public IContextCalculator<TContext> BuildForCalcContext<TContext>()
        => new ContextCalculator<TContext>(this);

    public object Calc(string expression) => BuildForCalcConstant().Calc(expression);

    public TOutput CalcDynamic<TOutput>(string expression, object input) => BuildForDynamicTypeCalc<TOutput>(input.GetType()).Calc(expression, input);

    public TOutput Calc<TOutput>(string expression)
        => BuildForCalcConstant<TOutput>().Calc(expression);

    public object Calc<TInput>(string expression, TInput input) => BuildForCalc<TInput>().Calc(expression, input);

    public TOutput Calc<TInput, TOutput>(string expression, TInput input) =>
        BuildForCalc<TInput, TOutput>().Calc(expression, input);

    public TOutput CalcMany<TOutput>(string expression) where TOutput : new() =>
        BuildForCalcManyConstants<TOutput>().Calc(expression);

    [Obsolete("This method is no longer supported and will be removed in v1.0. Use CalcContext instead.")]
    public TOutput CalcMany<TInput, TOutput>(string expression, TInput input) where TOutput : new()
        => BuildForCalcMany<TInput, TOutput>().Calc(expression, input);

    public void CalcContext<TContext>(string expression, TContext context)
        => BuildForCalcContext<TContext>().Calc(expression, context);

    internal FunnyRuntime CreateRuntime(string expression, IAprioriTypesMap aprioriTypes) {
        IConstantList constants = null;
        if (_constantList.Any())
        {
            var cl = new ConstantList(_dialect.Converter);
            foreach (var constant in _constantList)
            {
                cl.AddConstant(constant.Item1, constant.Item2);
            }

            constants = cl;
        }

        ImmutableFunctionDictionary dic = BaseFunctions.GetFunctions(_dialect.Converter.TypeBehaviour);

        if (_customFunctionFactories.Any())
            dic = dic.CloneWith(_customFunctionFactories.Select(f=>f(_dialect)).ToArray());

        return RuntimeBuilder.Build(
            script: expression,
            constants: constants ?? EmptyConstantList.Instance,
            functionDictionary: dic,
            aprioriTypesMap: aprioriTypes,
            dialect: _dialect);
    }
}
