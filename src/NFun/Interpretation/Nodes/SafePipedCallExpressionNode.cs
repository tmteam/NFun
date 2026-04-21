using System.Collections.Generic;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

/// <summary>
/// Runtime node for ?.method() — safe piped call on Optional.
/// If source is None → returns None. Else → calls inner function, returns result.
/// TIC already types the result as Optional(R).
/// </summary>
internal class SafePipedCallExpressionNode : IExpressionNode {
    private readonly IExpressionNode _source;
    private readonly IExpressionNode _innerCall;
    private readonly CachedSourceNode _cachedSource;

    public SafePipedCallExpressionNode(
        IExpressionNode source, CachedSourceNode cachedSource, IExpressionNode innerCall,
        FunnyType outputType, Interval interval) {
        _source = source;
        _cachedSource = cachedSource;
        _innerCall = innerCall;
        Type = outputType;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => new IRuntimeNode[] { _source, _innerCall };

    public object Calc() {
        var val = _source.Calc();
        if (val is FunnyNone)
            return FunnyNone.Instance;
        _cachedSource.Value = val;
        return _innerCall.Calc();
    }

    public IExpressionNode Clone(ICloneContext context) {
        var clonedCache = new CachedSourceNode(_cachedSource.Type, _cachedSource.Interval);
        // Clone inner call — the cloned CachedSourceNode replaces the original in children[0]
        // This is handled by the clone context since CachedSourceNode implements Clone
        return new SafePipedCallExpressionNode(
            _source.Clone(context), clonedCache, _innerCall.Clone(context), Type, Interval);
    }
}

/// <summary>
/// Value cache for SafePipedCallExpressionNode — avoids double-evaluation of source in chains.
/// SafePipedCallExpressionNode writes to Value after None check; inner call reads via Calc().
/// </summary>
internal class CachedSourceNode : IExpressionNode {
    internal object Value;

    public CachedSourceNode(FunnyType type, Interval interval) {
        Type = type;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => System.Array.Empty<IExpressionNode>();
    public object Calc() => Value;

    public IExpressionNode Clone(ICloneContext context) =>
        new CachedSourceNode(Type, Interval);
}

/// <summary>
/// Runtime node for ?? (null coalesce): if left is None → right, else left (unwrapped).
/// TIC resolves result type as LCA(unwrap(left), right) via SetCoalesce.
/// </summary>
internal class CoalesceExpressionNode : IExpressionNode {
    private readonly IExpressionNode _left;
    private readonly IExpressionNode _right;
    private readonly System.Func<object, object> _leftConverter;

    /// <param name="leftConverter">Converts unwrapped left value to result type (e.g., int→real). Null = no conversion.</param>
    public CoalesceExpressionNode(IExpressionNode left, IExpressionNode right,
        System.Func<object, object> leftConverter, FunnyType resultType, Interval interval) {
        _left = left;
        _right = right;
        _leftConverter = leftConverter;
        Type = resultType;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => new IRuntimeNode[] { _left, _right };

    public object Calc() {
        var val = _left.Calc();
        if (val is FunnyNone)
            return _right.Calc();
        return _leftConverter != null ? _leftConverter(val) : val;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new CoalesceExpressionNode(
            _left.Clone(context), _right.Clone(context), _leftConverter, Type, Interval);
}

/// <summary>
/// Overrides the declared Type of an expression node without changing runtime behavior.
/// Used for proven-safe unwraps: type narrowing (if x!=none) and safe access (?.).
/// At runtime Calc() delegates to inner — NFun Optional values are already the inner value or FunnyNone.
/// </summary>
internal class TypeOverrideNode : IExpressionNode {
    private readonly IExpressionNode _inner;

    public TypeOverrideNode(IExpressionNode inner, FunnyType overriddenType) {
        _inner = inner;
        Type = overriddenType;
    }

    public Interval Interval => _inner.Interval;
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => _inner.Children;
    public object Calc() => _inner.Calc();

    public IExpressionNode Clone(ICloneContext context) =>
        new TypeOverrideNode(_inner.Clone(context), Type);
}
