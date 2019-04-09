using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;

namespace NFun.Interpritation
{
    public class ExpressionHelper
    {

        public static void CheckForUnknownVariables(string[] originVariables, VariableDictionary resultVariables)
        {
            var unknownVariables = resultVariables.GetAllUsages()
                .Where(u=> !originVariables.Contains(u.Source.Name)).ToList();
            if (unknownVariables.Any())
            {
                throw ErrorFactory.UnknownVariables(unknownVariables.SelectMany(u => u.Nodes));
            }        
        }
    }
}