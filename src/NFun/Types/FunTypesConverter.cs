using System;
using NFun.Runtime.Arrays;

namespace NFun.Types
{
    public abstract class FunTypesConverter
    {
        public static bool TryGetSpecificConverter(Type clrType, out FunTypesConverter converter)
        {
            converter = default;
            if (clrType == typeof(string))
            {
                converter = new StringFunTypesConverter();
                return true;
            }

            if (clrType.IsArray)
            {
                var elementType = clrType.GetElementType();
                if (elementType == typeof(object)) {
                    //if type is object than we can convert objects only at runtime
                    //
                    converter = new ArrayOfAnythingFunTypesConverter();
                }
                else if(elementType == typeof(char))
                    converter =  new CharArrayFunTypesConverter();
                else if(TryGetSpecificConverter(elementType, out var elementConverter))
                    converter = new ArrayOfCompositesFunTypesConverter(elementConverter);
                else
                    converter = new ArrayOfPrimitivesFunTypesConverter(VarVal.ToPrimitiveFunType(elementType));
                return true;
            }
            return false;
        }
        protected FunTypesConverter(VarType funType)
        {
            FunType = funType;
        }

        public VarType FunType { get; }
        public abstract object ToFunObject(object clrObject);
    }
    public class ArrayOfCompositesFunTypesConverter : FunTypesConverter
    {
        private readonly FunTypesConverter _elementConverter;

        public ArrayOfCompositesFunTypesConverter(FunTypesConverter elementConverter) 
            : base(VarType.ArrayOf(elementConverter.FunType))
        {
            _elementConverter = elementConverter;
        }

        public override object ToFunObject(object clrObject)
        {
            var array = clrObject as Array;
            object[] converted = new object[array.Length]; 
            for (int i = 0; i < array.Length; i++)
            {
                converted[i] = _elementConverter.ToFunObject(array.GetValue(i));
            }
            return new ImmutableFunArray(converted,_elementConverter.FunType);
        }
    }
    
    public class ArrayOfAnythingFunTypesConverter : FunTypesConverter
    {

        public ArrayOfAnythingFunTypesConverter() 
            : base(VarType.ArrayOf(VarType.Anything)) { }

        public override object ToFunObject(object clrObject)
        {
            var array = clrObject as Array;
            object[] converted = new object[array.Length]; 
            for (int i = 0; i < array.Length; i++)
            {
                var element = array.GetValue(i);
                if (TryGetSpecificConverter(element.GetType(), out var specific))
                    converted[i] = specific.ToFunObject(element);
                else
                    converted[i] = element;
            }
            return new ImmutableFunArray(converted, VarType.Anything);
        }
    }
    
    public class ArrayOfPrimitivesFunTypesConverter : FunTypesConverter
    {
        private readonly VarType _elementType;
        public ArrayOfPrimitivesFunTypesConverter(VarType elementType) : base(VarType.ArrayOf(elementType))
        {
            _elementType = elementType;
        }

        public override object ToFunObject(object clrObject) 
            => new ImmutableFunArray(((Array) clrObject),_elementType);
    }
    public class CharArrayFunTypesConverter: FunTypesConverter
    {
        public CharArrayFunTypesConverter() : base(VarType.Text)
        {
        }
        public override object ToFunObject(object clrObject) 
            => new TextFunArray(new string((char[]) clrObject));
    }
    public class StringFunTypesConverter: FunTypesConverter
    {
        public StringFunTypesConverter() : base(VarType.Text) { }
        public override object ToFunObject(object clrObject) => new TextFunArray(clrObject.ToString());
    }
}