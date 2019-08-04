using System;
using System.Linq;

namespace NFun.Types
{
    public struct VarType
    {
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) BaseType * 397) ^
                       (ArrayTypeSpecification != null ? ArrayTypeSpecification.GetHashCode() : 0);
            }
        }

        public static VarType Empty => new VarType();
        public static VarType PrimitiveOf(BaseVarType baseType) => new VarType(baseType);
        public static VarType Anything => new VarType(BaseVarType.Any);
        public static VarType Bool => new VarType(BaseVarType.Bool);
        public static VarType Char => new VarType(BaseVarType.Char);

        public static VarType UInt8 => new VarType(BaseVarType.UInt8);
        public static VarType UInt16 => new VarType(BaseVarType.UInt16);
        public static VarType UInt32 => new VarType(BaseVarType.UInt32);
        public static VarType UInt64 => new VarType(BaseVarType.UInt64);

        public static VarType Int16 => new VarType(BaseVarType.Int16);
        public static VarType Int32 => new VarType(BaseVarType.Int32);
        public static VarType Int64 => new VarType(BaseVarType.Int64);
        public static VarType Real => new VarType(BaseVarType.Real);
        public static VarType  Text =>  ArrayOf(Char);
        
        public static VarType ArrayOf(VarType type) => new VarType(type);

        public static VarType Fun(VarType outputType, params VarType[] inputTypes)
            => new VarType(output: outputType, inputs: inputTypes);

        public static VarType Generic(int genericId) => new VarType(genericId);

        private VarType(VarType output, VarType[] inputs)
        {
            FunTypeSpecification = new FunTypeSpecification(output, inputs);
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

        public bool IsText => BaseType == BaseVarType.ArrayOf &&
                              ArrayTypeSpecification.VarType.BaseType == BaseVarType.Char;
        public readonly BaseVarType BaseType;
        public readonly AdditionalTypeSpecification ArrayTypeSpecification;
        public readonly FunTypeSpecification FunTypeSpecification;
        public readonly int? GenericId;

        public static bool operator ==(VarType obj1, VarType obj2)
            => obj1.Equals(obj2);

        // this is second one '!='
        public static bool operator !=(VarType obj1, VarType obj2)
            => !obj1.Equals(obj2);

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

                case BaseVarType.Int16:
                case BaseVarType.Int32:
                case BaseVarType.Int64:
                case BaseVarType.UInt8:
                case BaseVarType.UInt16:
                case BaseVarType.UInt32:
                case BaseVarType.UInt64:

                case BaseVarType.Real:
                case BaseVarType.Char:
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
                    return GenericId == obj.GenericId;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Substitude concrete types to generic type defenition (if it is)
        ///
        /// Example:
        /// generic:   Fun(T1, int)-> T0[];   solved: {int, text}
        /// returns:   Fun(text,int)-> int[];
        /// </summary>
        public static VarType SubstituteConcreteTypes(VarType genericOrNot, VarType[] solvedTypes)
        {
            switch (genericOrNot.BaseType)
            {
                case BaseVarType.Empty:
                case BaseVarType.Bool:
                case BaseVarType.Int16:
                case BaseVarType.Int32:
                case BaseVarType.Int64:
                case BaseVarType.UInt8:
                case BaseVarType.UInt16:
                case BaseVarType.UInt32:
                case BaseVarType.UInt64:
                case BaseVarType.Real:
                case BaseVarType.Char:
                case BaseVarType.Any:
                    return genericOrNot;
                case BaseVarType.ArrayOf:
                    return ArrayOf(SubstituteConcreteTypes(genericOrNot.ArrayTypeSpecification.VarType, solvedTypes));
                case BaseVarType.Fun:
                    var outputTypes = new VarType[genericOrNot.FunTypeSpecification.Inputs.Length];
                    for (int i = 0; i < genericOrNot.FunTypeSpecification.Inputs.Length; i++)
                        outputTypes[i] =
                            SubstituteConcreteTypes(genericOrNot.FunTypeSpecification.Inputs[i], solvedTypes);
                    return Fun(SubstituteConcreteTypes(genericOrNot.FunTypeSpecification.Output, solvedTypes),
                        outputTypes);
                case BaseVarType.Generic:
                    return solvedTypes[genericOrNot.GenericId.Value];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool TrySolveGenericTypes(VarType[] genericArguments, VarType genericType, VarType concreteType,
            bool strict = false)
        {
            switch (genericType.BaseType)
            {
                case BaseVarType.Generic:
                {
                    var id = genericType.GenericId.Value;
                    if (genericArguments[id].BaseType == BaseVarType.Empty)
                    {
                        genericArguments[id] = concreteType;
                    }
                    else if (genericArguments[id] != concreteType)
                    {
                        if (genericArguments[id].CanBeConvertedTo(concreteType))
                        {
                            genericArguments[id] = concreteType;
                            return true;
                        }

                        if (strict)
                            return false;

                        if (!concreteType.CanBeConvertedTo(genericArguments[id]))
                            return false;
                    }

                    return true;
                }
                case BaseVarType.ArrayOf when concreteType.BaseType != BaseVarType.ArrayOf:
                    return false;
                case BaseVarType.ArrayOf:
                    return TrySolveGenericTypes(genericArguments, genericType.ArrayTypeSpecification.VarType,
                        concreteType.ArrayTypeSpecification.VarType);
                case BaseVarType.Fun when concreteType.BaseType != BaseVarType.Fun:
                    return false;
                case BaseVarType.Fun:
                {
                    var genericFun = genericType.FunTypeSpecification;
                    var concreteFun = concreteType.FunTypeSpecification;

                    if (!TrySolveGenericTypes(genericArguments, genericFun.Output, concreteFun.Output))
                        return false;
                    if (concreteFun.Inputs.Length != genericFun.Inputs.Length)
                        return false;
                    for (int i = 0; i < concreteFun.Inputs.Length; i++)
                    {
                        if (!TrySolveGenericTypes(genericArguments, genericFun.Inputs[i], concreteFun.Inputs[i]))
                            return false;
                    }

                    return true;
                }
                default:
                    return concreteType.CanBeConvertedTo(genericType);
            }
        }

        public int? SearchMaxGenericTypeId()
        {
            switch (BaseType)
            {
                case BaseVarType.Bool:
                case BaseVarType.Int16:
                case BaseVarType.Int32:
                case BaseVarType.Int64:
                case BaseVarType.UInt8:
                case BaseVarType.UInt16:
                case BaseVarType.UInt32:
                case BaseVarType.UInt64:
                case BaseVarType.Real:
                case BaseVarType.Char:
                case BaseVarType.Any:
                    return null;
                case BaseVarType.ArrayOf:
                    return ArrayTypeSpecification.VarType.SearchMaxGenericTypeId();
                case BaseVarType.Fun:
                    var iId = FunTypeSpecification.Inputs.Select(i => i.SearchMaxGenericTypeId()).Max();
                    var oId = FunTypeSpecification.Output.SearchMaxGenericTypeId();
                    if (!iId.HasValue)
                        return oId;
                    if (!oId.HasValue)
                        return iId;
                    return Math.Max(iId.Value, oId.Value);
                case BaseVarType.Generic:
                    return GenericId;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            switch (BaseType)
            {
                case BaseVarType.ArrayOf:
                    return ArrayTypeSpecification.VarType + "[]";
                case BaseVarType.Fun:
                    return $"({string.Join(",", FunTypeSpecification.Inputs)})->{FunTypeSpecification.Output}";
                case BaseVarType.Generic:
                    return "T_" + GenericId;
                default:
                    return BaseType.ToString();
            }
        }
        
        public bool CanBeConvertedTo(VarType to)
            => VarTypeConverter.CanBeConverted(this, to);
    }
}