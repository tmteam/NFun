using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class IfThanElseExpressionNode: IExpressionNode
    {
        private readonly IfCaseExpressionNode[] _ifCaseNodes;
        private readonly IExpressionNode[] _ifCaseConvertedNodes;
        private readonly IExpressionNode _elseNode;
        public IfThanElseExpressionNode(IfCaseExpressionNode[] ifCaseNodes, IExpressionNode elseNode)
        {
            _ifCaseNodes = ifCaseNodes;
            _ifCaseConvertedNodes = new IExpressionNode[_ifCaseNodes.Length];
            Type = GetMostCommonType(ifCaseNodes.Select(c => c.Type).Append(elseNode.Type));
            if(Type.BaseType== BaseVarType.Empty)
                throw new OutpuCastFunParseException("There are no common convertion for if  cases");
            
            for (var index = 0; index < ifCaseNodes.Length; index++)
            {
                var ifCase = ifCaseNodes[index];
                if (ifCase.Type != Type)
                {
                    _ifCaseConvertedNodes[index] = new CastExpressionNode(ifCase, Type,
                        CastExpressionNode.GetConverterOrThrow(ifCase.Type, Type));
                }
                else
                    _ifCaseConvertedNodes[index] = ifCase;
            }

            if (elseNode.Type != Type)
            {
                _elseNode = new CastExpressionNode(elseNode, Type, 
                    CastExpressionNode.GetConverterOrThrow(elseNode.Type, Type));
            }
            else
            {
                _elseNode = elseNode;
            }
        }

        public object Calc()
        {
            for (var index = 0; index < _ifCaseNodes.Length; index++)
            {
                if (_ifCaseNodes[index].IsSatisfied())
                    return _ifCaseConvertedNodes[index].Calc(); //ifCase.Calc();
            }

            return _elseNode.Calc(); //caster(_elseNode.Calc());
        }
        public VarType Type { get; }


        VarType GetMostCommonType(IEnumerable<VarType> types)
        {
            var mostCommon = types.First();
            
            foreach (var varType in types.Skip(1))
            {
                if (varType.CanBeConvertedTo(mostCommon))
                  continue;
                if (mostCommon.CanBeConvertedTo(varType))
                    mostCommon = varType;
                else
                    return  VarType.Empty;
            }
            return mostCommon;
           
        }
    }
}