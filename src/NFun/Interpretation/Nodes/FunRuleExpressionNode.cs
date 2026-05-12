using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class FunRuleExpressionNode : IExpressionNode {
    /// <summary>
    /// Captured outer-scope variables referenced by the rule body. When non-empty,
    /// <see cref="Calc"/> snapshots their current values into fresh VariableSources
    /// and returns a Clone of the underlying function bound to that snapshot.
    /// This makes each emission of the rule a closed term (correct closure semantics
    /// per Specs/Rules.md), so subsequent writes to outer-scope variables cannot
    /// mutate already-returned closures.
    /// </summary>
    private readonly VariableSource[] _captures;
    private readonly ConcreteUserFunction _value;

    public FunRuleExpressionNode(ConcreteUserFunction fun, VariableSource[] captures, Interval interval) {
        _value = fun;
        _captures = captures ?? System.Array.Empty<VariableSource>();
        Interval = interval;
        Type = FunnyType.FunOf(_value.ReturnType, _value.ArgTypes);
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => new []{_value.Expression};

    public object Calc() {
        if (_captures.Length == 0)
            return _value;
        // SnapshotCloneContext snapshots every outer-scope VariableSource it sees,
        // so we don't need to pre-populate from `_captures` — they're a *trigger*
        // ("has captures? clone") rather than a complete list.
        return _value.Clone(new SnapshotCloneContext());
    }

    public IExpressionNode Clone(ICloneContext context) {
        var clonedFun = (ConcreteUserFunction)_value.Clone(context);
        VariableSource[] clonedCaptures;
        if (_captures.Length == 0) {
            clonedCaptures = System.Array.Empty<VariableSource>();
        } else {
            clonedCaptures = new VariableSource[_captures.Length];
            for (int i = 0; i < _captures.Length; i++)
                clonedCaptures[i] = context.GetVariableSourceClone(_captures[i]);
        }
        return new FunRuleExpressionNode(clonedFun, clonedCaptures, Interval);
    }

    /// <summary>
    /// ICloneContext that lazily snapshots each outer-scope VariableSource the body
    /// references. Rule-internal scopes (the rule's own args, or any nested rule's
    /// args) are handled by the ScopedContext wrapper added on each
    /// <see cref="GetScopedContext"/> call. Anything reaching this root context is
    /// by definition an outer-scope capture and gets snapshot-cloned on first ask
    /// (subsequent asks return the same snapshot — preserves identity within one
    /// emission).
    /// </summary>
    private sealed class SnapshotCloneContext : ICloneContext {
        private readonly Dictionary<VariableSource, VariableSource> _snapshots = new();
        private readonly Dictionary<IConcreteFunction, IConcreteFunction> _userFunctions = new();

        public VariableSource GetVariableSourceClone(VariableSource origin) {
            if (!_snapshots.TryGetValue(origin, out var snap)) {
                snap = origin.CloneWithValueSnapshot();
                _snapshots[origin] = snap;
            }
            return snap;
        }

        public ICloneContext GetScopedContext(VariableSource[] sources) =>
            new ScopedContext(this, sources);

        public IConcreteFunction GetUserFunctionClone(IConcreteFunction concreteUserFunction) {
            _userFunctions.TryGetValue(concreteUserFunction, out var result);
            return result;
        }

        public void AddUserFunctionClone(IConcreteFunction origin, IConcreteFunction clone) =>
            _userFunctions.Add(origin, clone);

        private sealed class ScopedContext : ICloneContext {
            private readonly ICloneContext _outer;
            private readonly VariableSource[] _scope;

            public ScopedContext(ICloneContext outer, VariableSource[] scope) {
                _outer = outer;
                _scope = scope;
            }

            public VariableSource GetVariableSourceClone(VariableSource origin) {
                for (int i = 0; i < _scope.Length; i++)
                    if (_scope[i].Name == origin.Name) return _scope[i];
                return _outer.GetVariableSourceClone(origin);
            }

            public ICloneContext GetScopedContext(VariableSource[] sources) =>
                new ScopedContext(this, sources);

            public IConcreteFunction GetUserFunctionClone(IConcreteFunction f) =>
                _outer.GetUserFunctionClone(f);

            public void AddUserFunctionClone(IConcreteFunction origin, IConcreteFunction clone) =>
                _outer.AddUserFunctionClone(origin, clone);
        }
    }
}
