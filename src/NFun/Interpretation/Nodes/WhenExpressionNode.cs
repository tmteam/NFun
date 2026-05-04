using System;
using System.Collections.Generic;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

/// <summary>When (pattern matching) execution node.</summary>
internal sealed class WhenExpressionNode : IExpressionNode {
    private readonly IExpressionNode _subject;  // null for condition-based
    private readonly (IExpressionNode condition, IExpressionNode body)[] _arms;
    private readonly IExpressionNode _elseBody;  // null if no else

    public WhenExpressionNode(
        IExpressionNode subject,
        (IExpressionNode condition, IExpressionNode body)[] arms,
        IExpressionNode elseBody,
        FunnyType type, Interval interval) {
        _subject = subject;
        _arms = arms;
        _elseBody = elseBody;
        Type = type;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children {
        get {
            if (_subject != null) yield return _subject;
            foreach (var (condition, body) in _arms) {
                yield return condition;
                yield return body;
            }
            if (_elseBody != null) yield return _elseBody;
        }
    }

    public object Calc() {
        if (_subject != null) {
            // Value-based matching
            var subjectVal = _subject.Calc();
            foreach (var (condition, body) in _arms) {
                var armVal = condition.Calc();
                if (TypeHelper.AreEqual(subjectVal, armVal))
                    return body.Calc();
            }
        } else {
            // Condition-based matching (like chained if-else)
            foreach (var (condition, body) in _arms) {
                if ((bool)condition.Calc())
                    return body.Calc();
            }
        }
        return _elseBody?.Calc() ?? FunnyNone.Instance;
    }

    public IExpressionNode Clone(ICloneContext context) {
        var armsCopy = new (IExpressionNode, IExpressionNode)[_arms.Length];
        for (int i = 0; i < _arms.Length; i++)
            armsCopy[i] = (_arms[i].condition.Clone(context), _arms[i].body.Clone(context));
        return new WhenExpressionNode(
            _subject?.Clone(context), armsCopy,
            _elseBody?.Clone(context), Type, Interval);
    }
}
