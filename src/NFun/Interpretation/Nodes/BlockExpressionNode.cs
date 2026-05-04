using System.Collections.Generic;
using System.Linq;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class BlockExpressionNode : IExpressionNode {
    private readonly IExpressionNode[] _statements;
    private readonly VariableAssignment[] _assignments;

    public BlockExpressionNode(
        IExpressionNode[] statements,
        VariableAssignment[] assignments,
        FunnyType type,
        Interval interval) {
        _statements = statements;
        _assignments = assignments;
        Type = type;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => _statements;

    public object Calc() {
        object result = null;
        for (int i = 0; i < _statements.Length; i++) {
            result = _statements[i].Calc();
            // Sentinel check: one pointer comparison per statement (~0.5ns).
            // Branch predictor will almost always predict false (return is rare).
            if (result is ReturnSignal or BreakSignal or ContinueSignal)
                return result; // propagate sentinel up
            if (i < _assignments.Length && _assignments[i] != null)
                _assignments[i].Source.SetFunnyValueUnsafe(result);
        }
        return result;
    }

    public IExpressionNode Clone(ICloneContext context) {
        var stmtsCopy = _statements.SelectToArray(s => s.Clone(context));
        var assignCopy = new VariableAssignment[_assignments.Length];
        for (int i = 0; i < _assignments.Length; i++) {
            if (_assignments[i] != null)
                assignCopy[i] = new VariableAssignment(context.GetVariableSourceClone(_assignments[i].Source));
        }
        return new BlockExpressionNode(stmtsCopy, assignCopy, Type, Interval);
    }
}

internal class VariableAssignment {
    public readonly Runtime.VariableSource Source;
    public VariableAssignment(Runtime.VariableSource source) => Source = source;
}
