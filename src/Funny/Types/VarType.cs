using System;

namespace Funny.Types
{
    public struct VarType
    {
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) BaseType * 397) ^ (ArrayTypeSpecification != null ? ArrayTypeSpecification.GetHashCode() : 0);
            }
        }
        public static VarType PrimitiveOf(BaseVarType baseType) => new VarType(baseType);
        public static VarType  AnyType => new VarType(BaseVarType.Any);
        public static VarType  BoolType => new VarType(BaseVarType.Bool);
        public static VarType  IntType => new VarType(BaseVarType.Int);
        public static VarType  RealType => new VarType(BaseVarType.Real);
        public static VarType  TextType => new VarType(BaseVarType.Text);
        public static VarType ArrayOf(VarType type) => new VarType(type);
        public VarType(BaseVarType baseType)
        {
            BaseType = baseType;
            ArrayTypeSpecification = null;
        }
        public VarType(VarType arrayElementType)
        {
            BaseType = BaseVarType.ArrayOf;
            ArrayTypeSpecification = new AdditionalTypeSpecification(arrayElementType);
        }
        
        public readonly BaseVarType BaseType;
        public readonly AdditionalTypeSpecification ArrayTypeSpecification;
        public static bool operator== (VarType obj1, VarType obj2)
        {
            return obj1.Equals(obj2);
        }

        // this is second one '!='
        public static bool operator!= (VarType obj1, VarType obj2)
        {
            return !obj1.Equals(obj2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VarType other && Equals(other);
        }
       
        // this is third one 'Equals'
        private bool Equals(VarType obj)
        {
            if (obj.BaseType != BaseType)
                return false;
            if (BaseType == BaseVarType.ArrayOf)
                return ArrayTypeSpecification.VarType.Equals(obj.ArrayTypeSpecification.VarType);
            return true;
        }

        public override string ToString()
        {
            if (BaseType == BaseVarType.ArrayOf)
                return ArrayTypeSpecification.VarType.ToString() + "[]";
            else
                return BaseType.ToString();
        }
        
        public bool CanBeConverted(VarType to)
            =>CanBeConvertedRec(this, to);

        private static bool CanBeConvertedRec(VarType from,VarType to)
        {
            if (to == from)
                return true;
            switch (to.BaseType)
            {
                case BaseVarType.Any:
                    return true;
                case BaseVarType.Text:
                    return true;
                case BaseVarType.Bool:
                    return false;
                case BaseVarType.Int:
                    return false;
                case BaseVarType.Real:
                    return from.BaseType== BaseVarType.Int;
                case BaseVarType.ArrayOf:
                    if (from.BaseType != BaseVarType.ArrayOf)
                        return false;
                    else
                        return CanBeConvertedRec(from.ArrayTypeSpecification.VarType, to.ArrayTypeSpecification.VarType);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}