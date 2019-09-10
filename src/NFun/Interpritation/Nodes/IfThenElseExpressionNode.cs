using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class IfThenElseExpressionNode: IExpressionNode
    {
        private readonly IfCaseExpressionNode[] _ifCaseNodes;
        private readonly IExpressionNode[] _ifCaseConvertedNodes;
        private readonly IExpressionNode _elseNode;
        public IfThenElseExpressionNode(IfCaseExpressionNode[] ifCaseNodes, IExpressionNode elseNode, Interval interval, VarType type)
        {
            _ifCaseNodes = ifCaseNodes;
            Interval = interval;
            _ifCaseConvertedNodes = new IExpressionNode[_ifCaseNodes.Length];
            
            //Type = GetMostCommonType(ifCaseNodes.Select(c => c.Type).Append(elseNode.Type));
          
            Type = type;
            
            //if (Type.BaseType == BaseVarType.Empty)
            //    throw ErrorFactory.NoCommonCast(ifCaseNodes.Append(elseNode));
            
                
            for (var index = 0; index < ifCaseNodes.Length; index++)
            {
                var ifCase = ifCaseNodes[index];
                var bodyType = ifCase.Body.Type;
                if (bodyType != Type)
                {
                    _ifCaseConvertedNodes[index] = new CastExpressionNode(ifCase.Body, Type,
                        VarTypeConverter.GetConverterOrThrow(bodyType, Type, ifCase.Interval)
                        ,ifCase.Interval);
                }
                else
                    _ifCaseConvertedNodes[index] = ifCase;
            }

            if (elseNode.Type != Type)
            {
                _elseNode = new CastExpressionNode(elseNode, Type, 
                    VarTypeConverter.GetConverterOrThrow(elseNode.Type, Type, elseNode.Interval)
                    , elseNode.Interval);
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
        public Interval Interval { get; }

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