using System.Reflection;
using NFun.Types;

namespace NFun; 

internal readonly struct InputProperty {
    public readonly string PropertyName;
    public readonly IInputFunnyConverter Converter;
    private readonly PropertyInfo _propertyInfo;
    public InputProperty(string propertyName, IInputFunnyConverter converter, PropertyInfo propertyInfo) {
        PropertyName = propertyName;
        Converter = converter;
        _propertyInfo = propertyInfo;
    }
    public object GetFunValue(object clrSource) => Converter.ToFunObject(_propertyInfo.GetValue(clrSource));
}