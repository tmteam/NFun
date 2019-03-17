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

        public static VarType PrimitiveOf(PrimitiveVarType primitiveType)
        {
            return new VarType() {BaseType = primitiveType};
        }
        public static VarType  BoolType => new VarType(){BaseType = PrimitiveVarType.Bool};
        public static VarType  IntType => new VarType(){BaseType = PrimitiveVarType.Int};
        public static VarType  RealType => new VarType(){BaseType = PrimitiveVarType.Real};
        public static VarType  TextType => new VarType(){BaseType = PrimitiveVarType.Text};
        public static VarType  ArrayOf(VarType type) => new VarType()
        {
            BaseType = PrimitiveVarType.ArrayOf,
            ArrayTypeSpecification = new AdditionalTypeSpecification(type)
        };
        
        public PrimitiveVarType BaseType;
        public AdditionalTypeSpecification ArrayTypeSpecification;
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
            if (BaseType == PrimitiveVarType.ArrayOf)
                return ArrayTypeSpecification.VarType.Equals(obj.ArrayTypeSpecification.VarType);
            return true;
        }

        public override string ToString()
        {
            if (BaseType == PrimitiveVarType.ArrayOf)
                return ArrayTypeSpecification.VarType.ToString() + "[]";
            else
                return BaseType.ToString();
        }
    }
}