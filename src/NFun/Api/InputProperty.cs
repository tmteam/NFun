using System.Reflection;
using NFun.Types;

namespace NFun;

using Exceptions;

internal readonly struct InputProperty {
    public readonly string PropertyName;
    public readonly IInputFunnyConverter Converter;
    private readonly PropertyInfo _propertyInfo;

    public InputProperty(string propertyName, IInputFunnyConverter converter, PropertyInfo propertyInfo) {
        PropertyName = propertyName;
        Converter = converter;
        _propertyInfo = propertyInfo;
    }

    public object GetFunValue(object clrSource) {
        var val = _propertyInfo.GetValue(clrSource);
        return val == null ? Converter.FunnyType.GetDefaultFunnyValue()  : Converter.ToFunObject(val);
    }
}
