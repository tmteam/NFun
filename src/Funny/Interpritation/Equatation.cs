using Funny.Interpritation.Nodes;

namespace Funny.Interpritation
{
    public class Equatation
    {
        public bool ReusingWithOtherEquatations = false;
        public string Id;
        public IExpressionNode Expression;
        public override string ToString()
        {
            if (ReusingWithOtherEquatations)
                return $"\"{Id}\" equatation (reusing)";
            else
                return $"\"{Id}\" equatation";
        }
    }
}