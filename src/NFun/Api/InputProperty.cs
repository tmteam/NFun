using System.Reflection;
using NFun.Types;

namespace NFun {

internal readonly struct InputProperty {
    public readonly string PropertyName;
    public readonly IInputFunnyConverter Converter;
    public readonly PropertyInfo PropertyInfo;
    public InputProperty(string propertyName, IInputFunnyConverter converter, PropertyInfo propertyInfo) {
        PropertyName = propertyName;
        Converter = converter;
        PropertyInfo = propertyInfo;
    }
    public object GetFunValue(object clrSource) => Converter.ToFunObject(PropertyInfo.GetValue(clrSource));
}

}