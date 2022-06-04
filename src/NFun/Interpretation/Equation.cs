using NFun.Interpretation.Nodes;
using NFun.Runtime;

namespace NFun.Interpretation; 

internal sealed class Equation {
    private readonly VariableSource _outputVariableSource;
    public readonly string Id;
    public readonly IExpressionNode Expression;

    internal Equation(string id, IExpressionNode expression, VariableSource outputVariableSource) {
        _outputVariableSource = outputVariableSource;
        Id = id;
        Expression = expression;
    }

    internal void Run()
        => _outputVariableSource.SetFunnyValueUnsafe(Expression.Calc());

    public override string ToString() => $"\"{Id}\" equation";
}