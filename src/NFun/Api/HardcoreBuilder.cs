using System;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun;

public class HardcoreBuilder {
    private readonly (string, object)[] _constants;
    private readonly MutableAprioriTypesMap _mutableApriori;
    private readonly DialectSettings _dialect;
    private readonly IFunctionSignature[] _customFunctions;
    private readonly ICustomTypeRegistry _customTypes;

    internal HardcoreBuilder() {
        _customFunctions = Array.Empty<IFunctionSignature>();
        _mutableApriori = new MutableAprioriTypesMap();
        _dialect = Dialects.Origin;
        _constants = Array.Empty<(string, object)>();
        _customTypes = EmptyCustomTypeRegistry.Instance;
    }

    private HardcoreBuilder(
        (string, object)[] constants,
        MutableAprioriTypesMap mutableApriori,
        DialectSettings dialect,
        IFunctionSignature[] customFunctions,
        ICustomTypeRegistry customTypes = null) {
        _dialect = dialect;
        _customFunctions = customFunctions;
        _mutableApriori = mutableApriori;
        _constants = constants;
        _customTypes = customTypes ?? EmptyCustomTypeRegistry.Instance;
    }

    /// <summary>
    /// Allows to setup syntax and semantics
    /// </summary>
    /// <param name="ifExpressionSyntax">If-expression syntax settings</param>
    /// <param name="integerPreferredType">Which funny type is prefered for integer constant</param>
    /// <param name="realClrType">Which clr type is used for funny type real</param>
    /// <param name="integerOverflow">overflow behaviour for integer arithmetics</param>
    /// <param name="allowUserFunctions">User functions restrictions</param>
    /// <param name="optionalTypesSupport">Optional types (T?, none, ??, ?., !) support</param>
    /// <param name="allowNewlineInStrings">Allow or deny raw line breaks inside string literals</param>
    /// <param name="namedTypesSupport">Named struct types (`type t = {...}`, recursive types) support</param>
    /// <param name="tryCatchSupport">try/catch/anyway expressions support</param>
    /// <param name="extensionFunctionsSeparation">Extension function namespace separation: piped definitions (x.f() = expr) get a separate namespace from regular ones</param>
    /// <param name="floatFamilySupport">IEEE 754 float family: with Float32AndFloat64, the `float32` type and the `float64` alias are available, and real literals / math built-ins resolve to float32 in float32 context; with AccordingToRealBehaviour floating point follows the `real` type. Float32AndFloat64 is incompatible with RealClrType.IsDecimal</param>
    public HardcoreBuilder WithDialect(
        IfExpressionSetup ifExpressionSyntax = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble,
        IntegerOverflow integerOverflow = IntegerOverflow.Checked,
        AllowUserFunctions allowUserFunctions = AllowUserFunctions.AllowAll,
        OptionalTypesSupport optionalTypesSupport = OptionalTypesSupport.Disabled,
        AllowNewlineInStrings allowNewlineInStrings = AllowNewlineInStrings.Allow,
        NamedTypesSupport namedTypesSupport = NamedTypesSupport.Disabled,
        TryCatchSupport tryCatchSupport = TryCatchSupport.Enabled,
        ExtensionFunctionsSeparation extensionFunctionsSeparation = ExtensionFunctionsSeparation.Disabled,
        FloatFamilySupport floatFamilySupport = FloatFamilySupport.AccordingToRealBehaviour)
        => WithDialect(Dialects.ModifyOrigin(ifExpressionSyntax, integerPreferredType, realClrType, integerOverflow, allowUserFunctions, optionalTypesSupport, allowNewlineInStrings, namedTypesSupport, tryCatchSupport, extensionFunctionsSeparation, floatFamilySupport));

    private HardcoreBuilder WithDialect(DialectSettings dialect) =>
        new(_constants, _mutableApriori, dialect, _customFunctions, _customTypes);

    public HardcoreBuilder WithConstant<T>(string id, T clrValue) =>
        new(_constants.AppendTail((id, clrValue)), _mutableApriori, _dialect, _customFunctions, _customTypes);

    public HardcoreBuilder WithConstants(params (string, object)[] funValues) =>
        new(_constants.AppendTail(funValues), _mutableApriori, _dialect, _customFunctions, _customTypes);

    public HardcoreBuilder WithCustomType(IFunnyCustomTypeDefinition customTypeDefinition) {
        var customType = FunnyType.CustomOf(customTypeDefinition);
        var newRegistry = _customTypes.CloneWith(customTypeDefinition.Name, customType);
        return new HardcoreBuilder(_constants, _mutableApriori, _dialect, _customFunctions, newRegistry);
    }

    public HardcoreBuilder WithApriori(string id, FunnyType type) =>
        new(_constants, _mutableApriori.CloneWith(id, type), _dialect, _customFunctions, _customTypes);

    public HardcoreBuilder WithApriori<T>(string id) =>
        //no matter what type beh is used
        WithApriori(id, Dialects.Origin.Converter.GetInputConverterFor(typeof(T)).FunnyType);

    public HardcoreBuilder WithFunction(IFunctionSignature function) =>
        new(_constants, _mutableApriori, _dialect, _customFunctions.AppendTail(function), _customTypes);

    private FunnyConverter Converter => _dialect.Converter.WithCustomTypes(_customTypes);

    public HardcoreBuilder WithFunction<TIn, TOut>(string name, Func<TIn, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, Converter));

    public HardcoreBuilder WithFunction<TIn1, TIn2, TOut>(string name, Func<TIn1, TIn2, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, Converter));

    public HardcoreBuilder
        WithFunction<Tin1, Tin2, Tin3, TOut>(string name, Func<Tin1, Tin2, Tin3, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, Converter));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, Converter));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, Converter));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, Converter));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function, Converter));

    public FunnyRuntime Build(string script) {
        var converter = Converter;
        return RuntimeBuilder.Build(
            script,
            BaseFunctions.GetFunctions(converter.TypeBehaviour, _dialect.ExtensionFunctionsSeparation).CloneWith(_customFunctions),
            _dialect,
            _constants.Length > 0 ? new ConstantList(converter, _constants) : null,
            _mutableApriori,
            _customTypes);
    }

    public FunnyRuntime BuildLang(string script) {
        var converter = Converter;
        // Lang-mode requires Optional + NamedTypes + MutableStructs (and
        // ExtensionFunctionsSeparation per spec). Hardcore is the entry point,
        // so stamp these features here unconditionally — `RuntimeBuilder.BuildLang`
        // validates that they're set, throwing if a hand-rolled caller passed
        // a dialect with them off.
        var langDialect = new DialectSettings(
            _dialect.IfExpressionSetup,
            _dialect.IntegerPreferredType,
            _dialect.Converter,
            _dialect.AllowIntegerOverflow,
            _dialect.AllowUserFunctions,
            OptionalTypesSupport.Enabled,
            _dialect.AllowNewlineInStrings,
            NamedTypesSupport.Enabled,
            _dialect.TryCatchSupport,
            ExtensionFunctionsSeparation.Enabled,
            useMutableStructs: true);
        return RuntimeBuilder.BuildLang(
            script,
            BaseFunctions.GetFunctionsLang(converter.TypeBehaviour, ExtensionFunctionsSeparation.Enabled).CloneWith(_customFunctions),
            langDialect,
            _constants.Length > 0 ? new ConstantList(converter, _constants) : null,
            _mutableApriori,
            _customTypes);
    }

    public StringTemplateCalculator BuildStringTemplate(string script) =>
        StringTemplateRuntimeBuilder.Build(
            script,
            BaseFunctions.GetFunctions(Converter.TypeBehaviour, _dialect.ExtensionFunctionsSeparation).CloneWith(_customFunctions),
            _dialect,
            _constants.Length > 0 ? new ConstantList(Converter, _constants) : null,
            _mutableApriori,
            _customTypes);
}
