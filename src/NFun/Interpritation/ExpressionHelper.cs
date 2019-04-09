using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;

namespace NFun.Interpritation
{
    public class ExpressionHelper
    {
        public static void CheckForUnknownVariables(string[] originVariables, 
            Dictionary<string, VariableExpressionNode> resultVariables)
        {
            var unknownVariables = resultVariables.Values.Select(v => v.Name).Except(originVariables);
            if (unknownVariables.Any())
            {
                
                throw ErrorFactory.UnknownVariables(resultVariables.Values.Where(v=>!originVariables.Contains(v.Name))); 
                    
            }
        }
    }
}