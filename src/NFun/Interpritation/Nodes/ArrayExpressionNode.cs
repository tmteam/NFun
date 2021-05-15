using NFun.Runtime.Arrays;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class ArrayExpressionNode : IExpressionNode
    {
        private readonly IExpressionNode[] _elements;
        
        public ArrayExpressionNode(IExpressionNode[] elements, Interval interval, VarType type)
        {
            Type = type;
            _elements = elements;
            Interval = interval;
        }
        public Interval Interval { get; }
        public VarType Type { get; }

        public object Calc()
        {
            var arr = new object[_elements.Length];
            for (int i = 0; i < _elements.Length; i++)
            {
                arr[i] = _elements[i].Calc();
            }
            return new ImmutableFunArray(arr, Type.ArrayTypeSpecification.VarType);
        }

        public IExpressionNode Fork(ForkScope scope) => new ArrayExpressionNode(_elements.Fork(scope), Interval, Type);
    }
}