using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Runtime;

namespace Funny.Interpritation.Nodes
{
    public class IfThanElseExpressionNode: IExpressionNode
    {
        private readonly IfCaseExpressionNode[] _ifCaseNodes;
        private readonly IExpressionNode _elseNode;
        private Func<object, object> caster = null;
        public IfThanElseExpressionNode(IfCaseExpressionNode[] ifCaseNodes, IExpressionNode elseNode)
        {
            _ifCaseNodes = ifCaseNodes;
            _elseNode = elseNode;
            Type = GetUpTypeConverion(ifCaseNodes.Select(c => c.Type).Append(elseNode.Type));
            switch (Type)
            {
                case VarType.BoolType:
                    caster = o => o;
                    break;
                case VarType.IntType:
                    caster = o => o;
                    break;
                case VarType.NumberType:
                    caster = o => Convert.ToDouble(o);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IEnumerable<IExpressionNode> Children 
            => _ifCaseNodes.Append(_elseNode);
        public object Calc()
        {
            
            foreach (var ifCase in _ifCaseNodes)
            {
                if (ifCase.IsSatisfied())
                    return caster(ifCase.Calc());
            }
            
            return caster(_elseNode.Calc());
        }
        public VarType Type { get; }


        VarType GetUpTypeConverion(IEnumerable<VarType> types)
        {
            bool hasNumeric = false;
            bool hasInt = false;
            bool hasReal = false;
            bool hasBool = false;

            foreach (var varType in types)
            {
                switch (varType)
                {
                    case VarType.BoolType:
                        hasBool = true;
                        break;
                    case VarType.IntType:
                        hasInt = true;
                        break;
                    case VarType.NumberType:
                        hasReal = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (hasBool)
            {
                if(hasInt||hasReal)
                    throw new OutpuCastParseException("Cannot convert bool type to number type");
                return VarType.BoolType;
            }

            if (hasReal)
                return VarType.NumberType;
            if (hasInt)
                return VarType.IntType;
            
            throw new NotSupportedException("IfThenElse upcast for unknown types");
        }
    }
}