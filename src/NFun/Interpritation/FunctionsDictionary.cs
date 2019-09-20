using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.Interpritation
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
                return false;
            else
            {
                _functions[function.Name].Add(function);
                return true;
            }
        }

        
        public GenericFunctionBase GetGenericOrNull(string name, int argCount)
        {
            if (!_genericFunctions.TryGetValue(name, out var res))
                return null;
            return res.FirstOrDefault(r => r.ArgTypes.Length == argCount);
        }
        public IList<FunctionBase> GetNonGeneric(string name)
        {
            if (_functions.TryGetValue(name, out var res))
                return res;
            return new FunctionBase[0];
        }
        public FunctionBase GetOrNullWithOverloadSearch(string name,VarType returnType, params VarType[] args)
        {
            //If there is no function with specified name
            if (!_functions.TryGetValue(name, out var funs)) 
                return GetOrNullGenerics(name,returnType, args);

            //Filter functions with specified arguments count
            var filtered = funs.Where(f => f.ArgTypes.Length == args.Length).ToArray();
            if (!filtered.Any())
                return GetOrNullGenerics(name,returnType,args);
            
            
            //Search full match:
            var fullMatch = filtered.Where(f => f.ArgTypes.SequenceEqual(args)).ToArray();
            if (fullMatch.Length == 1)
                return fullMatch.First();
            //Then search generics
            var genericFun = GetOrNullGenerics(name,returnType, args);
            if (genericFun != null)
                return genericFun;
            //If concrete candidate is only one - try to cast it
            if (filtered.Length == 1)
            {
                var candidate = filtered.First();
                return CanBeConverted(args, candidate.ArgTypes) ? candidate : null;
            }
            
            //Try to find most close of castable functions
            int maxCloseness = 0;
            bool severalFunctionsWithSameCloseness = false;
            FunctionBase winner = null;
            foreach (var function in filtered)
            {
                var closeness = GetCloseness(args, function.ArgTypes);
                if(closeness==0)
                    continue;
                else if (closeness > maxCloseness)
                {
                    winner = function;
                    maxCloseness = closeness;
                    severalFunctionsWithSameCloseness = false;
                }
                else if (closeness == maxCloseness)
                    severalFunctionsWithSameCloseness = true;
            }

            if (severalFunctionsWithSameCloseness)
                return null;
            return winner;
        }
        
        public FunctionBase GetOrNullConcrete(string name,VarType returnType, params VarType[] args)
        {
            //If there is no function with specified name
            if (!_functions.TryGetValue(name, out var funs)) 
                return GetOrNullGenerics(name,returnType, args);

            //Filter functions with specified arguments count
            var filtered = funs.Where(f => f.ArgTypes.Length == args.Length).ToArray();
            if (!filtered.Any())
                return GetOrNullGenerics(name,returnType,args);
            
            
            //Search full match:
            var fullMatch = filtered.Where(f => f.ArgTypes.SequenceEqual(args)).ToArray();
            if (fullMatch.Length == 1)
                return fullMatch.First();
            //Then search generics
            var genericFun = GetOrNullGenerics(name,returnType, args);
            if (genericFun != null)
                return genericFun;
            //If concrete candidate is only one - try to cast it
            if (filtered.Length == 1)
            {
                var candidate = filtered.First();
                return CanBeConverted(args, candidate.ArgTypes) ? candidate : null;
            }

            return null;
        }
        
        private FunctionBase GetOrNullGenerics(string name, VarType returnType, params VarType[] args)
        {
            //If there is no function with specified name
            if (!_genericFunctions.TryGetValue(name, out var funs)) 
                return null;

            //Filter functions with specified arguments count
            var filtered = funs.Where(f => f.ArgTypes.Length == args.Length).ToArray();
            if (!filtered.Any())
                return null;
            
            var res =  filtered
                  .Select(f => f.CreateConcreteOrNull(returnType, args))
                  .Where(f => f != null)
                  .ToList();
            if (res.Count() != 1)
                return null;
            return res.First();
        }
        
        
        private int GetCloseness(VarType[] from, VarType[] to)
        {
            if (from.Length != to.Length)
                return 0;
            int closeness = 0;
            for (int i = 0; i < from.Length; i++)
            {
                if (from[i] == to[i])
                {
                    closeness++;
                }
                if (!from[i].CanBeConvertedTo(to[i]))
                    return 0;
            }

            return closeness;
        }
        
        private bool CanBeConverted(VarType[] from, VarType[] to)
        {
            if (from.Length != to.Length)
                return false;
            
            for (int i = 0; i < from.Length; i++)
            {
                if(from[i]== to[i])
                    continue;
                if (!from[i].CanBeConvertedTo(to[i]))
                    return false;
            }

            return true;
        }
        
    }
}