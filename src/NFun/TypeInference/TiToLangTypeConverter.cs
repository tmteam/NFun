using System;
using System.Linq;
using NFun.TypeInference.Solving;
using NFun.Types;

namespace NFun.TypeInference
{
    public abstract class TiToLangTypeConverter 
    {
        public static TiToLangTypeConverter SaveGenerics 
            => new SaveGenericsTiToLangTypeConverter();
        
        public static TiToLangTypeConverter SetGenericsToAny 
            => new SetGenericsToAnyTiToLangTypeConverter();

        protected abstract VarType ConvertGeneric(GenericType type);
        public VarType ToSimpleType(TiType type)
        {
            if (type.IsPrimitiveGeneric)
            {
                if(!(type is GenericType))
                    throw new InvalidOperationException($"type {type} is not instance of GenericType");
                return ConvertGeneric((GenericType) type);
            }

            switch (type.Name.Id)
            {
                case TiTypeName.AnyId: return VarType.Anything;
                case TiTypeName.RealId: return VarType.Real;
                case TiTypeName.TextId: return VarType.Text;
                case TiTypeName.BoolId: return VarType.Bool;
                case TiTypeName.Int16Id: return VarType.Int16;
                case TiTypeName.Int64Id: return  VarType.Int64;
                case TiTypeName.Int32Id: return VarType.Int32;
                
                case TiTypeName.UInt8Id:  return VarType.UInt8;
                case TiTypeName.UInt16Id: return VarType.UInt16;
                case TiTypeName.UInt64Id: return VarType.UInt64;
                case TiTypeName.UInt32Id: return VarType.UInt32;
                
                case TiTypeName.SomeIntegerId: return VarType.Int32;
                case TiTypeName.ArrayId: return VarType.ArrayOf(ConvertToSimpleType(type.Arguments[0]));
                case TiTypeName.CharId: return VarType.Char;
                case TiTypeName.FunId :
                    return VarType.Fun(ConvertToSimpleType(type.Arguments[0]), 
                        type.Arguments.Skip(1).Select(ConvertToSimpleType).ToArray()
                    );
            }
            throw new InvalidOperationException("Not supported type "+ type.ToSmartString(SolvingNode.MaxTypeDepth));
            
        }

        private  VarType ConvertToSimpleType(SolvingNode node) 
            => ToSimpleType(node.MakeType());
        
        /// <summary>
        /// Generic types from TI stays nfun generics
        /// </summary>
        class SaveGenericsTiToLangTypeConverter : TiToLangTypeConverter
        {
            protected override VarType ConvertGeneric(GenericType type)
            {
                return VarType.Generic(type.GenericId);

            }
        }
        
        /// <summary>
        /// Generic types from TI become any type
        /// </summary>
        class SetGenericsToAnyTiToLangTypeConverter: TiToLangTypeConverter
        {
            protected override VarType ConvertGeneric(GenericType type)
            {
                return VarType.Anything;
            }
        }
    }
    
}