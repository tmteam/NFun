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

        private readonly Dictionary<string, List<GenericFunctionBase>> _genericFunctions 
            = new Dictionary<string, List<GenericFunctionBase>>();

        public bool Add(GenericFunctionBase generic)
        {
            if (!_genericFunctions.ContainsKey(generic.Name))
            {
                _genericFunctions.Add(generic.Name, new List<GenericFunctionBase>(){ generic});
                return true;
            }

            if (_genericFunctions[generic.Name].Any(a => a.ArgTypes.SequenceEqual(generic.ArgTypes)))
            {
                return false;
            }
            else
            {
                _genericFunctions[generic.Name].Add(generic);
                return true;
            }
        }
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

        public IList<FunctionBase> Get(string name)
        {
            if (_functions.TryGetValue(name, out var res))
                return res;
            return new FunctionBase[0];
        }
        public FunctionBase GetOrNull(string name, params VarType[] args)
        {
            //If there is no function with specified name
            if (!_functions.TryGetValue(name, out var funs)) 
                return GetOrNullGenerics(name,args);

            //Filter functions with specified arguments count
            var filtered = funs.Where(f => f.ArgTypes.Length == args.Length).ToArray();
            if (!filtered.Any())
                return GetOrNullGenerics(name,args);
            
            
            //Search full match:
            var fullMatch = filtered.Where(f => f.ArgTypes.SequenceEqual(args)).ToArray();
            if (fullMatch.Length == 1)
                return fullMatch.First();
            //Then search generics
            var genericFun = GetOrNullGenerics(name, args);
            if (genericFun != null)
                return genericFun;
            //If concrete candidate is only one - try to cast it
            if (filtered.Length == 1)
            {
                var candidate = filtered.First();
                return CanBeConverted(args, candidate.ArgTypes) ? candidate : null;
            }
            
            //Try to find castable function
            var canBeConverted = filtered.Where(f => CanBeConverted(args, f.ArgTypes)).ToArray();
            if (canBeConverted.Length != 1)
                return null;
            return canBeConverted[0];
        }
        
        
        private FunctionBase GetOrNullGenerics(string name, params VarType[] args)
        {
            //If there is no function with specified name
            if (!_genericFunctions.TryGetValue(name, out var funs)) 
                return null;

            //Filter functions with specified arguments count
            var filtered = funs.Where(f => f.ArgTypes.Length == args.Length).ToArray();
            if (!filtered.Any())
                return null;
            
            if (filtered.Length == 1)
            {
                var candidate = filtered.First();
                return candidate.CreateConcreteOrNull(args);
            }
            else
            {
                return null;
            }
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