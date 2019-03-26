using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Runtime;
using Funny.Types;

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
            switch (Type.BaseType)
            {
                case BaseVarType.Bool:
                    caster = o => o;
                    break;
                case BaseVarType.Int:
                    caster = o => o;
                    break;
                case BaseVarType.Real:
                    caster = o => Convert.ToDouble(o);
                    break;
                case BaseVarType.ArrayOf:
                    caster = o => o;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

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
            bool hasArray = false;
            foreach (var varType in types)
            {
                switch (varType.BaseType)
                {
                    case BaseVarType.Bool:
                        hasBool = true;
                        break;
                    case BaseVarType.Int:
                        hasInt = true;
                        break;
                    case BaseVarType.Real:
                        hasReal = true;
                        break;
                    case BaseVarType.ArrayOf:
                        hasArray = true;
                        break;
                        ;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (hasArray)
            {
                if(hasInt|| hasBool || hasReal)
                    throw new OutpuCastFunParseException("Cannot convert array type to primitive");
                return VarType.ArrayOf(VarType.Real);
            }
            if (hasBool)
            {
                if(hasInt||hasReal)
                    throw new OutpuCastFunParseException("Cannot convert bool type to number type");
                return VarType.Bool;
            }

            if (hasReal)
                return VarType.Real;
            if (hasInt)
                return VarType.Int;
            
            throw new NotSupportedException("IfThenElse upcast for unknown types");
        }
    }
}