using NFun.Interpritation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpritation
{
    public sealed class Equation
    {
        private readonly VariableSource _outputVariableSource;
        public readonly string Id;
        public readonly IExpressionNode Expression;

        internal  Equation(string id, IExpressionNode expression, VariableSource outputVariableSource)
        {
            _outputVariableSource = outputVariableSource;
            Id = id;
            Expression = expression;
        }

        public VarVal CalcExpression()
        {
            var val  = new VarVal(Id, Expression.Calc(), Expression.Type);
            _outputVariableSource.InternalFunnyValue = val.Value;
            return val;
        }

        internal void UpdateExpression() 
            => _outputVariableSource.InternalFunnyValue = Expression.Calc();
        public override string ToString() => $"\"{Id}\" equation";
    }
}