using System.Collections.Generic;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

internal class SafeFieldAccessExpressionNode : IExpressionNode {
    private readonly string _fieldName;
    private readonly IExpressionNode _source;

    public SafeFieldAccessExpressionNode(string fieldName, IExpressionNode source, Interval interval) {
        _fieldName = fieldName;
        _source = source;
        if (source.Type.BaseType == BaseFunnyType.None) {
            // none?.field = none — safe access on None always returns None
            Type = FunnyType.None;
        } else {
            // Determine the inner struct type — unwrap Optional if present
            var innerStructType = source.Type.BaseType == BaseFunnyType.Optional
                ? source.Type.OptionalTypeSpecification.ElementType
                : source.Type; // struct source (inline constructor with Optional declared field)
            FunnyType fieldType;
            if (innerStructType.BaseType == BaseFunnyType.Struct
                && innerStructType.StructTypeSpecification.TryGetValue(fieldName, out var ft))
                fieldType = ft;
            else
                fieldType = FunnyType.Any; // recursive type cycle — field type unknown at compile time
            // Don't double-wrap: if field is already optional, result is opt(T) not opt(opt(T))
            Type = fieldType.BaseType == BaseFunnyType.Optional ? fieldType : FunnyType.OptionalOf(fieldType);
        }
        Interval = interval;
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => new[] { _source };

    public object Calc() {
        var source = _source.Calc();
        if (source is FunnyNone)
            return FunnyNone.Instance;
        return ((FunnyStruct)source).GetValue(_fieldName);
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new SafeFieldAccessExpressionNode(_fieldName, _source.Clone(context), Interval);
}
