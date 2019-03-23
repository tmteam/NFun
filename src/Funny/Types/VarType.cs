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
        public static VarType Empty => new VarType();
        public static VarType PrimitiveOf(BaseVarType baseType) => new VarType(baseType);
        public static VarType  Any => new VarType(BaseVarType.Any);
        public static VarType  Bool => new VarType(BaseVarType.Bool);
        public static VarType  Int => new VarType(BaseVarType.Int);
        public static VarType  Real => new VarType(BaseVarType.Real);
        public static VarType  Text => new VarType(BaseVarType.Text);
        public static VarType ArrayOf(VarType type) => new VarType(type);
        
        public static VarType Fun(VarType outputType, params VarType[]inputTypes)
            => new VarType(output: outputType, inputs: inputTypes);
        public static VarType Generic(int genericId) => new VarType(genericId);

        private VarType(VarType output, VarType[] inputs)
        {
            this.FunTypeSpecification = new FunTypeSpecification(output, inputs);
            BaseType = BaseVarType.Fun;
            ArrayTypeSpecification = null;
            GenericId = null;
        }
        private VarType(int genericId)
        {
            BaseType = BaseVarType.Generic;
            FunTypeSpecification = null;
            ArrayTypeSpecification = null;
            GenericId = genericId;
        }
        private VarType(BaseVarType baseType)
        {
            BaseType = baseType;
            FunTypeSpecification = null;
            ArrayTypeSpecification = null;
            GenericId = null;
        }

        private VarType(VarType arrayElementType)
        {
            BaseType = BaseVarType.ArrayOf;
            FunTypeSpecification = null;
            ArrayTypeSpecification = new AdditionalTypeSpecification(arrayElementType);
            GenericId = null;
        }
        
        public readonly BaseVarType BaseType;
        public readonly AdditionalTypeSpecification ArrayTypeSpecification;
        public readonly FunTypeSpecification FunTypeSpecification;
        public readonly int? GenericId;
        
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
            
            switch (BaseType)
            {
                case BaseVarType.Bool:
                case BaseVarType.Int:
                case BaseVarType.Real:
                case BaseVarType.Text:
                case BaseVarType.Any:
                    return true;
                case BaseVarType.ArrayOf:
                    return ArrayTypeSpecification.VarType.Equals(obj.ArrayTypeSpecification.VarType);
                case BaseVarType.Fun:
                {
                    var funA = FunTypeSpecification;
                    var funB = obj.FunTypeSpecification;
                
                    if (!funA.Output.Equals(funB.Output))
                        return false;
                
                    for (int i = 0; i < funA.Inputs.Length; i++)
                    {
                        if (!funA.Inputs[i].Equals(funB.Inputs[i]))
                            return false;
                    }

                    return true;
                }
                
                case BaseVarType.Generic:
                    return GenericId== obj.GenericId;
                default:
                    return true;
            }
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