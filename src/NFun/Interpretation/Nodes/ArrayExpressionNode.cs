using System.Collections.Generic;
using NFun.Runtime.Arrays;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

internal class ArrayExpressionNode : IExpressionNode {
    public ArrayExpressionNode(IExpressionNode[] elements, Interval interval, FunnyType type) {
        Type = type;
        _elements = elements;
        Interval = interval;
    }

    private readonly IExpressionNode[] _elements;

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => _elements;

    public object Calc() {
        var arr = new object[_elements.Length];
        for (int i = 0; i < _elements.Length; i++)
            arr[i] = _elements[i].Calc();

        // Stage C — Concretest(FixedArray)=FixedArray means an [...] literal in
        // ee mode can resolve to fixedArray<T>. Wrap as FixedFunnyArray.
        if (Type.BaseType == BaseFunnyType.FixedArray)
            return new NFun.Runtime.Lists.FixedFunnyArray(
                Type.FixedArrayTypeSpecification.FunnyType, arr);
        return FunnyArrayTools.CreateArray(arr, Type.ArrayTypeSpecification.FunnyType);
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new ArrayExpressionNode(_elements.SelectToArray(s => s.Clone(context)), Interval, Type);
}
