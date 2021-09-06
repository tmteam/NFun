using System;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Runtime {

public interface IFunnyVar {
    /// <summary>
    /// Variable name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Variable attributes
    /// </summary>
    FunnyAttribute[] Attributes { get; }

    /// <summary>
    /// Nfun type of variable
    /// </summary>
    FunnyType Type { get; }

    /// <summary>
    /// internal representation of value
    /// </summary>
    object FunnyValue { get; }

    /// <summary>
    /// The variable is calculated in the script and can be used as one of the results of the script
    /// </summary>
    bool IsOutput { get; }

    /// <summary>
    /// Represents current CLR value of the funny variable.
    /// In case of multiple use, use GetTypedGetter and GetTypedSetter, since the value get and value set methods can be quite slow
    /// </summary>
    object Value { get; set; }

    /// <summary>
    /// Returns getter function with a converter to the specified clr type
    /// </summary>
    Func<T> GetTypedGetter<T>();

    /// <summary>
    /// Returns setter function with a converter to the specified clr type
    /// </summary>
    Action<T> GetTypedSetter<T>();
}


internal class VariableSource : IFunnyVar {
    private object _funnyValue;

    private readonly FunnyVarAccess _access;

    internal static VariableSource CreateWithStrictTypeLabel(
        string name,
        FunnyType type,
        Interval typeSpecificationIntervalOrNull,
        FunnyVarAccess access,
        FunnyAttribute[] attributes = null)
        => new(name, type, typeSpecificationIntervalOrNull, access, attributes);

    internal static VariableSource CreateWithoutStrictTypeLabel(
        string name, FunnyType type, FunnyVarAccess access, FunnyAttribute[] attributes = null)
        => new(name, type, access, attributes);

    private VariableSource(
        string name,
        FunnyType type,
        Interval typeSpecificationIntervalOrNull,
        FunnyVarAccess access,
        FunnyAttribute[] attributes = null) {
        _access = access;
        _funnyValue = type.GetDefaultValueOrNull();
        TypeSpecificationIntervalOrNull = typeSpecificationIntervalOrNull;
        Attributes = attributes ?? Array.Empty<FunnyAttribute>();
        Name = name;
        Type = type;
    }

    public void SetFunnyValueUnsafe(object funnyValue) => _funnyValue = funnyValue;

    public bool IsOutput => _access.HasFlag(FunnyVarAccess.Output);


    private VariableSource(string name, FunnyType type, FunnyVarAccess access, FunnyAttribute[] attributes = null) {
        _access = access;
        _funnyValue = type.GetDefaultValueOrNull();
        Attributes = attributes ?? Array.Empty<FunnyAttribute>();
        Name = name;
        Type = type;
    }

    public FunnyAttribute[] Attributes { get; }
    public string Name { get; }
    internal Interval? TypeSpecificationIntervalOrNull { get; }
    public FunnyType Type { get; }
    public object FunnyValue => _funnyValue;

    private bool _outputConverterLoaded;
    private IOutputFunnyConverter _outputConverter;

    public object Value
    {
        get
        {
            if (!_outputConverterLoaded)
            {
                _outputConverterLoaded = true;
                _outputConverter = FunnyTypeConverters.GetOutputConverter(Type);
            }

            return _outputConverter.ToClrObject(_funnyValue);
        }
        set => _funnyValue = FunnyTypeConverters.ConvertInputOrThrow(value, Type);
    }

    public Func<T> GetTypedGetter<T>() {
        if (!IsOutput)
            throw new NotSupportedException("Cannot create value getter for non output variable");
        var outputConverter = FunnyTypeConverters.GetOutputConverter(typeof(T));
        if (outputConverter == null || (Type.IsPrimitive && outputConverter.FunnyType != Type))
            throw new InvalidOperationException(
                $"Funny type {Type} cannot be converted to clr type {typeof(T).Name}");
        return () => (T)outputConverter.ToClrObject(FunnyValue);
    }

    public Action<T> GetTypedSetter<T>() {
        if (IsOutput)
            throw new NotSupportedException("Cannot create value getter for output variable");

        //var inputConverter = FunnyTypeConverters.GetInputConverter(typeof(T));
        var inputConverter = FunnyTypeConverters.GetInputConverter(Type, typeof(T));

        if (inputConverter == null || inputConverter.FunnyType != Type)
        {
            throw new InvalidOperationException(
                $"Funny type {Type} cannot be converted to clr type {typeof(T).Name}");
        }

        return input => _funnyValue = inputConverter.ToFunObject(input);
    }
}

internal enum FunnyVarAccess {
    NoInfo = 0,

    /// <summary>
    /// Funny variable is input, so can be modified from the outside before calculation
    /// </summary>
    Input = 1 << 0,

    /// <summary>
    /// Funny variable is output so it can be considered as the result of the calculation
    /// </summary>
    Output = 1 << 1,
}

}