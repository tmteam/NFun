using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation.Nodes;

namespace Funny.Interpritation
{
    public class ExpressionHelper
    {
        public static void CheckForUnknownVariables(string[] originVariables, 
            Dictionary<string, VariableExpressionNode> resultVariables)
        {
            var unknownVariables = resultVariables.Values.Select(v => v.Name).Except(originVariables);
            if (unknownVariables.Any())
            {
                if (unknownVariables.Count() == 1)
                    throw new ParseException($"Unknown variable \"{unknownVariables.First()}\"");
                else
                    throw new ParseException($"Unknown variables \"{string.Join(", ", unknownVariables)}\"");
            }
        }
    }
}