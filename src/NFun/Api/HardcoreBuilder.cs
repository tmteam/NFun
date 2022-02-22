using System;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun {

public class HardcoreBuilder {
    private readonly (string, object)[] _constants;
    private readonly AprioriTypesMap _apriori;
    private readonly DialectSettings _dialect;
    private readonly IFunctionSignature[] _customFunctions;
    
    internal HardcoreBuilder() {
        _customFunctions = Array.Empty<IFunctionSignature>();
        _apriori = new AprioriTypesMap();
        _dialect = DialectSettings.Default;
        _constants = Array.Empty<(string, object)>();
    }

    private HardcoreBuilder(
        (string, object)[] constants,
        AprioriTypesMap apriori,
        DialectSettings dialect, 
        IFunctionSignature[] customFunctions) {
        _dialect = dialect;
        _customFunctions = customFunctions;
        _apriori = apriori;
        _constants = constants;
    }

    public HardcoreBuilder WithDialect(DialectSettings dialect) =>
        new(_constants, _apriori, dialect, _customFunctions);

    public HardcoreBuilder WithConstant<T>(string id, T clrValue) =>
        new(_constants.AppendTail((id, clrValue)), _apriori, _dialect, _customFunctions);

    public HardcoreBuilder WithConstants(params (string, object)[] funValues) =>
        new(_constants.AppendTail(funValues), _apriori, _dialect, _customFunctions);

    public HardcoreBuilder WithApriori(string id, FunnyType type) =>
        new(_constants, _apriori.CloneWith(id, type), _dialect, _customFunctions);

    public HardcoreBuilder WithApriori<T>(string id) =>
        //no matter what type beh is used
        WithApriori(id, TypeBehaviourExtensions.GetInputConverterFor(TypeBehaviour.Default, typeof(T)).FunnyType);

    public HardcoreBuilder WithFunction(IFunctionSignature function) =>
        new(_constants, _apriori, _dialect, _customFunctions.AppendTail(function));
    
    public HardcoreBuilder WithFunction<Tin, TOut>(string name, Func<Tin, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, _dialect.TypeBehaviour));

    public HardcoreBuilder WithFunction<Tin1, Tin2, TOut>(string name, Func<Tin1, Tin2, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, _dialect.TypeBehaviour));

    public HardcoreBuilder
        WithFunction<Tin1, Tin2, Tin3, TOut>(string name, Func<Tin1, Tin2, Tin3, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, _dialect.TypeBehaviour));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, _dialect.TypeBehaviour));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, _dialect.TypeBehaviour));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, _dialect.TypeBehaviour));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, _dialect.TypeBehaviour));

    public FunnyRuntime Build(string script) =>
        RuntimeBuilder.Build(
            script, 
            BaseFunctions.GetFunctions(_dialect.TypeBehaviour).CloneWith(_customFunctions), 
            _dialect, new ConstantList(_dialect.TypeBehaviour, _constants), _apriori);

    public StringTemplateCalculator BuildStringTemplate(string script) =>
        StringTemplateRuntimeBuilder.Build(
            script, 
            BaseFunctions.GetFunctions(_dialect.TypeBehaviour).CloneWith(_customFunctions), 
            _dialect, new ConstantList(_dialect.TypeBehaviour, _constants), _apriori);
}

}