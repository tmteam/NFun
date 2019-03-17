using System;
using System.Linq;
using Funny.Types;

namespace Funny.Interpritation.Nodes
{
    public class ArrayExpressionNode : IExpressionNode
    {
        private IExpressionNode[] _elements;

        public ArrayExpressionNode(IExpressionNode[] elements)
        {
            _elements = elements;
            if (!elements.Any())
                Type = VarType.ArrayOf(VarType.RealType);
            else
            {
                var elementType = elements[0].Type;

                for (int i = 1; i < elements.Length; i++)
                {
                    var iType = elements[i].Type;
                    if (iType != elementType)
                        throw new NotImplementedException("Array contains different types");
                }
                Type = VarType.ArrayOf(elementType);
            }

            switch (Type.ArrayTypeSpecification.VarType.BaseType)
            {
                case PrimitiveVarType.Bool:
                    _caster = nodes => nodes.Select(c => (bool) c.Calc()).ToArray();
                    break;
                case PrimitiveVarType.Int:
                    _caster = nodes => nodes.Select(c => (int) c.Calc()).ToArray();
                    break;
                case PrimitiveVarType.Real:
                    _caster = nodes => nodes.Select(c => (double) c.Calc()).ToArray();
                    break;
                case PrimitiveVarType.Text:
                    _caster = nodes => nodes.Select(c => c?.Calc().ToString()).ToArray();
                    break;
                case PrimitiveVarType.ArrayOf:
                    throw new ArgumentOutOfRangeException();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Func<IExpressionNode[], object> _caster = null;
        public VarType Type { get; }
        public object Calc()
            => _elements.Select(e => e.Calc());
    }
}