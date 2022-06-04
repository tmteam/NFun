using System.Reflection;
using NFun.Types;

namespace NFun; 

internal readonly struct OutputProperty {
    public readonly string PropertyName;
    public readonly IOutputFunnyConverter Converter;
    public readonly PropertyInfo PropertyInfo;
    public OutputProperty(string propertyName, IOutputFunnyConverter converter, PropertyInfo propertyInfo) {
        PropertyName = propertyName;
        Converter = converter;
        PropertyInfo = propertyInfo;
    }
    public void SetValueToTargetProperty(object funnyValue, object clrTarget) => PropertyInfo.SetValue(clrTarget, Converter.ToClrObject(funnyValue));
}