using NFun.Interpritation.Nodes;

namespace NFun.Interpritation
{
    public class Equation
    {
        public bool ReusingWithOtherEquations = false;
        public string Id;
        public IExpressionNode Expression;
        public override string ToString()
        {
            if (ReusingWithOtherEquations)
                return $"\"{Id}\" equation (reusing)";
            else
                return $"\"{Id}\" equation";
        }
    }
}