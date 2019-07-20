using System;
using NFun.Interpritation.Nodes;

namespace NFun.Interpritation
{
    public class Equation
    {
        //public bool ReusingWithOtherEquations = false;
        public bool IsTyped => throw new NotImplementedException();
        
        public readonly string Id;
        public readonly IExpressionNode Expression;

        public Equation(string id, IExpressionNode expression)
        {
            Id = id;
            Expression = expression;
        }

        public override string ToString()
        {
          //  if (ReusingWithOtherEquations)
          //      return $"\"{Id}\" equation (reusing)";
          //  else
                return $"\"{Id}\" equation";
        }
    }
}