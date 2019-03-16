using Funny.Runtime;

namespace Funny
{
    public class AdditionalTypeSpecification
    {
        public readonly VarType VarType;

        public AdditionalTypeSpecification(VarType varType)
        {
            VarType = varType;
        }
    }
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
        public static VarType  BoolType => new VarType(){BaseType = PrimitiveVarType.BoolType};
        public static VarType  IntType => new VarType(){BaseType = PrimitiveVarType.IntType};
        public static VarType  RealType => new VarType(){BaseType = PrimitiveVarType.RealType};
        public static VarType  TextType => new VarType(){BaseType = PrimitiveVarType.TextType};
        public static VarType  ArrayOf(VarType type) => new VarType()
        {
            BaseType = PrimitiveVarType.Array,
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
            if (BaseType == PrimitiveVarType.Array)
                return ArrayTypeSpecification.VarType.Equals(obj.ArrayTypeSpecification.VarType);
            return true;
        }
    }
    public enum PrimitiveVarType
    {
        BoolType = 1,
        IntType = 2,
        RealType = 3,
        TextType = 4,
        Array = 5,
    }
    
    /*
     * |Primitive
     *     - bool
     *     - int
     *     - real
     *     - text
     * | array
     *     - Type
     * | Any
     */
}