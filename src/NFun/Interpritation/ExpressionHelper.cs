using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.Runtime;

namespace NFun.Interpritation
{
    public static class ExpressionHelper
    {
        public static void ThrowIfSomeVariablesNotExistsInTheList(this VariableDictionary resultVariables, IEnumerable<string> list )
        {
            var unknownVariables = resultVariables.GetAllUsages()
                .Where(u=> !list.Contains(u.Source.Name)).ToList();
            if (unknownVariables.Any())
            {
                throw ErrorFactory.UnknownVariables(unknownVariables.SelectMany(u => u.Usages));
            }        
        }
    }
}