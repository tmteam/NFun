using System.Linq;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class ArrayExpressionNode : IExpressionNode
    {
        private IExpressionNode[] _elements;
        
        public ArrayExpressionNode(IExpressionNode[] elements, Interval interval, VarType type)
        {
            Type = type;
            _elements = elements;
            Interval = interval;
            /*
            if (!elements.Any())
                Type = VarType.ArrayOf(VarType.Anything);
            else
            {
                var elementType = elements[0].Type;

                for (int i = 1; i < elements.Length; i++)
                {
                    var iType = elements[i].Type;
                    if (iType != elementType)
                        throw ErrorFactory.VariousArrayElementTypes(elements, i);
                }
                Type = VarType.ArrayOf(elementType);
            }*/
            if (elements.Any())
            {
                var elementType = elements[0].Type;

                for (int i = 1; i < elements.Length; i++)
                {
                    var iType = elements[i].Type;
                    if (iType != elementType)
                        throw ErrorFactory.VariousArrayElementTypes(elements, i);
                }
            }
        }
        public Interval Interval { get; }
        public VarType Type { get; }
        public object Calc()
            => ImmutableFunArray.By(_elements.Select(e => e.Calc()));
    }
}