using System;
using System.Collections.Generic;
using NFun.Types;

namespace NFun.Interpretation;

internal interface IConstantList {
    bool TryGetConstant(string id, out ConstantValueAndType constant);
}

internal class EmptyConstantList : IConstantList {
    public static readonly EmptyConstantList Instance = new();
    private EmptyConstantList() { }

    public bool TryGetConstant(string id, out ConstantValueAndType constant) {
        constant = default;
        return false;
    }
}

/// <summary>Singleton built-in constants (π, ∞). One per TypeBehaviour.</summary>
internal class BuiltInConstantList : IConstantList {
    internal static readonly BuiltInConstantList Double = Create(new RealIsDoubleTypeBehaviour());
    internal static readonly BuiltInConstantList Decimal = Create(new RealIsDecimalTypeBehaviour());

    private readonly Dictionary<string, ConstantValueAndType> _constants;

    private static BuiltInConstantList Create(TypeBehaviour typeBehaviour) {
        var converter = FunnyConverter.RealIsDouble; // default converter for built-in
        if (typeBehaviour is RealIsDecimalTypeBehaviour)
            converter = FunnyConverter.RealIsDecimal;
        var piConverter = converter.GetInputConverterFor(typeof(double));
        var dict = new Dictionary<string, ConstantValueAndType>(2) {
            ["π"] = new(piConverter.ToFunObject(Math.PI), piConverter.FunnyType),
            ["∞"] = new(double.PositiveInfinity, FunnyType.Real)
        };
        return new BuiltInConstantList(dict);
    }

    private BuiltInConstantList(Dictionary<string, ConstantValueAndType> constants) =>
        _constants = constants;

    public bool TryGetConstant(string id, out ConstantValueAndType constant) =>
        _constants.TryGetValue(id, out constant);
}

internal class ConstantList : IConstantList {
    public ConstantList(FunnyConverter typeBehaviour) {
        _typeBehaviour = typeBehaviour;
        _dictionary = new Dictionary<string, ConstantValueAndType>();
    }

    internal ConstantList(FunnyConverter typeBehaviour, (string id, object value)[] items) {
        _typeBehaviour = typeBehaviour;
        _dictionary = new Dictionary<string, ConstantValueAndType>(items.Length);
        foreach (var (id, value) in items)
        {
            var converter = _typeBehaviour.GetInputConverterFor(value.GetType());
            _dictionary.Add(id, new ConstantValueAndType(converter.ToFunObject(value), converter.FunnyType));
        }
    }

    private readonly FunnyConverter _typeBehaviour;
    private readonly Dictionary<string, ConstantValueAndType> _dictionary;

    public void AddConstant(string id, object val) {
        //constants are readonly so we need to use input converter
        var converter = _typeBehaviour.GetInputConverterFor(val.GetType());
        _dictionary.Add(id, new ConstantValueAndType(converter.ToFunObject(val), converter.FunnyType));
    }

    /// <summary>
    /// Adds built-in math constants (π, ∞). Uses TryAdd so user constants take precedence.
    /// π goes through converter (works with both double and decimal).
    /// ∞ is always double (decimal doesn't support infinity).
    /// </summary>
    internal void AddBuiltIns() {
        var piConverter = _typeBehaviour.GetInputConverterFor(typeof(double));
        _dictionary.TryAdd("π", new ConstantValueAndType(piConverter.ToFunObject(Math.PI), piConverter.FunnyType));
        _dictionary.TryAdd("∞", new ConstantValueAndType(double.PositiveInfinity, FunnyType.Real));
    }

    public bool TryGetConstant(string id, out ConstantValueAndType constant) =>
        _dictionary.TryGetValue(id, out constant);

}
