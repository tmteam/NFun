using NFun.Interpritation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpritation
{
    public sealed class Equation
    {
        public VariableSource OutputVariableSource { get; }
        public readonly string Id;
        public readonly IExpressionNode Expression;

        public Equation(string id, IExpressionNode expression, VariableSource outputVariableSource)
        {
            OutputVariableSource = outputVariableSource;
            Id = id;
            Expression = expression;
        }

        public VarVal CalcExpression()
        {
            var val  = new VarVal(Id, Expression.Calc(), Expression.Type);
            OutputVariableSource.InternalFunnyValue = val.Value;
            return val;
        }

        internal void UpdateExpression() 
            => OutputVariableSource.InternalFunnyValue = Expression.Calc();
        public override string ToString() => $"\"{Id}\" equation";
    }
}