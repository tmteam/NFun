using System;
using System.Linq;
using NFun.HindleyMilner.Tyso;
using NFun.Types;

namespace NFun.HindleyMilner
{
    public abstract class SolvedTypeConverter 
    {
        public static SolvedTypeConverter SaveGenerics => new SaveGenericsSolvedTypeConverter();
        public static SolvedTypeConverter SetGenericsToAny => new SetGenericsToAnySolvedTypeConverter();

        protected abstract VarType ConvertGeneric(GenericType type);
        public VarType ToSimpleType(FType type)
        {
            if (type.IsPrimitiveGeneric)
            {
                if(!(type is GenericType))
                    throw new InvalidOperationException($"type {type} is not instance of GenericType");
                return ConvertGeneric((GenericType) type);
            }

            switch (type.Name.Id)
            {
                case NTypeName.AnyId: return VarType.Anything;
                case NTypeName.RealId: return VarType.Real;
                case NTypeName.TextId: return VarType.Text;
                case NTypeName.BoolId: return VarType.Bool;
                case NTypeName.Int64Id: return  VarType.Int64;
                case NTypeName.Int32Id: return VarType.Int32;
                case NTypeName.SomeIntegerId: return VarType.Int32;
                case NTypeName.ArrayId: return VarType.ArrayOf(ConvertToSimpleType(type.Arguments[0]));
                case NTypeName.FunId :
                    return VarType.Fun(ConvertToSimpleType(type.Arguments[0]), 
                        type.Arguments.Skip(1).Select(ConvertToSimpleType).ToArray()
                    );
            }
            throw new InvalidOperationException("Not supported type "+ type.ToSmartString(SolvingNode.MaxTypeDepth));
            
        }

        private  VarType ConvertToSimpleType(SolvingNode node) 
            => ToSimpleType(node.MakeType(SolvingNode.MaxTypeDepth));
        
        
        class SaveGenericsSolvedTypeConverter : SolvedTypeConverter
        {
            protected override VarType ConvertGeneric(GenericType type)
            {
                return VarType.Generic(((GenericType) type).GenericId);

            }
        }
        class SetGenericsToAnySolvedTypeConverter: SolvedTypeConverter
        {
            protected override VarType ConvertGeneric(GenericType type)
            {
                return VarType.Anything;
            }
        }
    }
    
}