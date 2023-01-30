using NFun.Interpretation.Nodes;
using NFun.Runtime;

namespace NFun.Interpretation;

internal sealed class Equation {
    internal readonly VariableSource OutputVariableSource;
    public readonly string Id;
    public readonly IExpressionNode Expression;

    internal Equation(string id, IExpressionNode rootExpression, VariableSource outputVariableSource) {
        OutputVariableSource = outputVariableSource;
        Id = id;
        Expression = rootExpression;
    }

    internal void Run()
        => OutputVariableSource.SetFunnyValueUnsafe(Expression.Calc());

    public override string ToString() => $"\"{Id}\" equation";

    /// <summary>
    /// Creates deep copy of equation, that can be used in parallel
    /// </summary>
    internal Equation Clone(CloneContext context)
        => new(Id, Expression.Clone(context), context.GetVariableSourceClone(OutputVariableSource));
}
