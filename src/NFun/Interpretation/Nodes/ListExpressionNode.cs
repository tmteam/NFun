using System.Collections.Generic;
using NFun.Runtime.Lists;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

/// <summary>
/// Lang-mode runtime node for the <c>[1,2,3]</c> literal — sibling of
/// <see cref="ArrayExpressionNode"/>. Materialises a <see cref="MutableFunnyList"/>
/// with the inferred element type.
/// </summary>
internal class ListExpressionNode : IExpressionNode {
    public ListExpressionNode(IExpressionNode[] elements, Interval interval, FunnyType type) {
        Type = type;
        _elements = elements;
        Interval = interval;
    }

    private readonly IExpressionNode[] _elements;

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => _elements;

    public object Calc() {
        var items = new object[_elements.Length];
        for (int i = 0; i < _elements.Length; i++)
            items[i] = _elements[i].Calc();
        return new MutableFunnyList(Type.ListTypeSpecification.FunnyType, items);
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new ListExpressionNode(_elements.SelectToArray(s => s.Clone(context)), Interval, Type);
}
