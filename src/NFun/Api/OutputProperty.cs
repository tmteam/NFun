using System.Reflection;
using NFun.Types;

namespace NFun; 

internal readonly struct OutputProperty {
    public readonly string PropertyName;
    public readonly IOutputFunnyConverter Converter;
    private readonly PropertyInfo _propertyInfo;
    public OutputProperty(string propertyName, IOutputFunnyConverter converter, PropertyInfo propertyInfo) {
        PropertyName = propertyName;
        Converter = converter;
        _propertyInfo = propertyInfo;
    }
    public void SetValueToTargetProperty(object funnyValue, object clrTarget) => _propertyInfo.SetValue(clrTarget, Converter.ToClrObject(funnyValue));
}