using System;
using System.Linq;
using System.Reflection;
using NFun.HindleyMilner.Tyso;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.HindleyMilner
{
    public class RealTypeConverter: ISolvedTypeConverter{

        private VarType MakeGeneric(GenericType type)
        {
            
            return VarType.Anything;
            
            //return VarType.Generic(((GenericType) type).GenericId);
        }
        public VarType ToSimpleType(FType type)
        {
            if (type.IsPrimitiveGeneric)
            {
                if(!(type is GenericType))
                    throw new InvalidOperationException($"type {type} is not instance of GenericType");
                return MakeGeneric((GenericType) type);
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
    }
    public interface ISolvedTypeConverter
    {
        VarType ToSimpleType(FType type);
    }
    public class ApplyHmResultVisitor: EnterVisitorBase
    {
        private readonly FunTypeSolving _solving;
        private readonly ISolvedTypeConverter _solvedTypeConverter;

        public ApplyHmResultVisitor(FunTypeSolving solving, ISolvedTypeConverter solvedTypeConverter)
        {
            _solving = solving;
            _solvedTypeConverter = solvedTypeConverter;
        }

        protected override VisitorResult DefaultVisit(ISyntaxNode node)
        {
            var type = _solving.GetNodeTypeOrEmpty(node.NodeNumber, _solvedTypeConverter);
            
            node.OutputType = type;
            
            return VisitorResult.Continue;
        }


        public override VisitorResult Visit(UserFunctionDefenitionSyntaxNode node)
        {
            return VisitorResult.Continue;
        }

    }
}