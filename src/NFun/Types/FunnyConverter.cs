using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NFun.Runtime.Arrays;

namespace NFun.Types
{
    //todo to separate input and output converters?
    public abstract class FunnyConverter
    {
        public static bool TryGetConverter(Type clrType, out FunnyConverter converter)
        {
            converter = default;
            if (clrType == typeof(string))
            {
                converter = new StringTypesFunnyConverter();
                return true;
            }
            if (clrType.IsArray)
            {
                
                var elementType = clrType.GetElementType();
                if (!TryGetConverter(elementType, out var elementConverter)) 
                    return false;
                converter = new ClrArrayTypeFunnyConverter(elementConverter);
                return true;
            }
            if (typeof(IEnumerable).IsAssignableFrom(clrType))
            {
                var elementType = clrType.GetGenericArguments();
                if (!TryGetConverter(elementType[0], out var elementConverter)) 
                    return false;
                converter = new ClrEnumerableTypeFunnyConverter(elementConverter);
                return true;
            }
            converter = new PrimitiveTypeFunnyConverter(VarVal.ToPrimitiveFunType(clrType));
            return true;
        } 
        public VarType FunnyType { get; protected set; }
        public abstract object ToFunObject(object clrObject);
        public abstract object FromFunObject(object clrObject);

    }
    
    public class StructTypeFunnyConverter : FunnyConverter
    {

        public StructTypeFunnyConverter(Type clrType)
        {
            var properties = clrType.GetProperties(BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField);
            foreach (var property in properties)
            {
                TryGetConverter(property.PropertyType, out var propertyConverter);
                throw new NotImplementedException();
            }
        }

        public override object FromFunObject(object funObject)
        {
            var funnyArray = (IFunArray) funObject;
            return funnyArray.ClrArray;
        }

        public override object ToFunObject(object clrObject)
        {
            var array = clrObject as Array;
            return new ImmutableFunArray(array, this.FunnyType.ArrayTypeSpecification.VarType);
        }
    }
    public class ClrArrayTypeFunnyConverter : FunnyConverter
    {
        private readonly FunnyConverter _elementConverter;

        public ClrArrayTypeFunnyConverter(FunnyConverter elementConverter)
            : base()
        {
            this.FunnyType = VarType.ArrayOf(elementConverter.FunnyType);
            _elementConverter = elementConverter;
        }

        public override object FromFunObject(object funObject)
        {
            var funnyArray = (IFunArray) funObject;
            return funnyArray.ClrArray;
        }

        public override object ToFunObject(object clrObject)
        {
            var array = clrObject as Array;
            return new ImmutableFunArray(array, this.FunnyType.ArrayTypeSpecification.VarType);
        }
    }
    public class ClrEnumerableTypeFunnyConverter : FunnyConverter
    {
        private readonly FunnyConverter _elementConverter;

        public ClrEnumerableTypeFunnyConverter(FunnyConverter elementConverter)
        {
            this.FunnyType = VarType.ArrayOf(elementConverter.FunnyType);
            _elementConverter = elementConverter;
        }

        public override object FromFunObject(object funObject)
        {
            var funnyArray = (IFunArray) funObject;
            return funnyArray.Select(f=>_elementConverter.FromFunObject(f));
        }

        public override object ToFunObject(object clrObject)
        {
            var array = clrObject as IEnumerable;
            return new EnumerableFunArray(array.Cast<object>(), FunnyType.ArrayTypeSpecification.VarType);
        }
    }
    
    public class PrimitiveTypeFunnyConverter : FunnyConverter
    {
        public PrimitiveTypeFunnyConverter(VarType funnyType)
        {
            this.FunnyType = funnyType;
        }

        public override object FromFunObject(object funObject) => funObject;

        public override object ToFunObject(object clrObject) => clrObject;
    }
    
    public class StringTypesFunnyConverter: FunnyConverter
    {
        public StringTypesFunnyConverter()
        {
            this.FunnyType = VarType.Text;
        }
        public override object ToFunObject(object clrObject) => new TextFunArray(clrObject.ToString());
        public override object FromFunObject(object clrObject) => (clrObject as IFunArray).ToText();
    }
}