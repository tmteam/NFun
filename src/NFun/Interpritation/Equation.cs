using System;
using NFun.Interpritation.Nodes;

namespace NFun.Interpritation
{
    public sealed class Equation
    {
        public readonly string Id;
        public readonly IExpressionNode Expression;

        public Equation(string id, IExpressionNode expression)
        {
            Id = id;
            Expression = expression;
        }

        public override string ToString() => $"\"{Id}\" equation";
    }
}