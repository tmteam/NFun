using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation.Functions;
using Funny.Types;

namespace Funny.Interpritation
{
    public class FunctionsDictionary
    {
        private readonly Dictionary<string, List<FunctionBase>> _functions 
            = new Dictionary<string, List<FunctionBase>>();
        
        public bool Add(FunctionBase function)
        {
            if (!_functions.ContainsKey(function.Name))
            {
                _functions.Add(function.Name, new List<FunctionBase>(){ function});
                return true;
            }

            if (_functions[function.Name].Any(a => a.ArgTypes.SequenceEqual(function.ArgTypes)))
            {
                return false;
            }
            else
            {
                _functions[function.Name].Add(function);
                return true;
            }
        }
        
        public FunctionBase GetOrNull(string name, params VarType[] args)
        {
            //If there is no function with specified name
            if (!_functions.TryGetValue(name, out var funs)) 
                return null;

            //Filter functions with specified arguments count
            var filtered = funs.Where(f => f.ArgTypes.Length == args.Length).ToArray();
            if (!filtered.Any())
                return null;
            if (filtered.Length == 1)
            {
                var candidate = filtered.First();
                return CanBeConverted(args, candidate.ArgTypes) ? candidate : null;
            }
            //Search full match:
            var fullMatch = filtered.Where(f => f.ArgTypes.SequenceEqual(args)).ToArray();
            if (fullMatch.Length == 1)
                return fullMatch.First();
            
            //Try to find convertion
            var canBeConverted = filtered.Where(f => CanBeConverted(args, f.ArgTypes)).ToArray();
            if (canBeConverted.Length != 1)
                return null;
            return canBeConverted[0];
        }
        
        private bool CanBeConverted(VarType[] from, VarType[] to)
        {
            if (from.Length != to.Length)
                return false;
            
            for (int i = 0; i < from.Length; i++)
            {
                if(from[i]== to[i])
                    continue;
                if (!from[i].CanBeConverted(to[i]))
                    return false;
            }

            return true;
        }
        
    }
}