using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.Interpritation
{
    public sealed class FunctionsDictionary
    {
        private readonly Dictionary<string, List<FunctionBase>> _userFunctions 
            = new Dictionary<string, List<FunctionBase>>();

        private readonly Dictionary<string, List<GenericFunctionBase>> _genericUserFunctions 
            = new Dictionary<string, List<GenericFunctionBase>>();

        private readonly BuiltInFunctions _builtInFunctions;

        public FunctionsDictionary(BuiltInFunctions builtInFunctions)
        {
            _builtInFunctions = builtInFunctions;
        }

        public bool Add(GenericFunctionBase generic)
        {
            if (!_genericUserFunctions.ContainsKey(generic.Name))
            {
                _genericUserFunctions.Add(generic.Name, new List<GenericFunctionBase>(){ generic});
                return true;
            }

            if (_genericUserFunctions[generic.Name].Any(a => a.ArgTypes.SequenceEqual(generic.ArgTypes)))
            {
                return false;
            }
            else
            {
                _genericUserFunctions[generic.Name].Add(generic);
                return true;
            }
        }
        public bool Add(FunctionBase function)
        {
            if (!_userFunctions.ContainsKey(function.Name))
            {
                _userFunctions.Add(function.Name, new List<FunctionBase>(){ function});
                return true;
            }

            if (_userFunctions[function.Name].Any(a => a.ArgTypes.SequenceEqual(function.ArgTypes)))
            {
                return false;
            }
            else
            {
                _userFunctions[function.Name].Add(function);
                return true;
            }
        }
        
        public GenericFunctionBase GetGenericOrNull(string name, int argCount) 
            => GetAllGenerics(name, argCount).FirstOrDefault();

        public IEnumerable<ConcreteUserFunctionPrototype> GetConcreteUserFunctions(string name)
        {
            if (_userFunctions.TryGetValue(name, out var res))
                return res.OfType<ConcreteUserFunctionPrototype>();
            return new ConcreteUserFunctionPrototype[0];
        }
        public IList<FunctionBase> GetConcretes(string name) 
            => GetAllConcretes(name).ToList();

        public IList<FunctionBase> GetConcretes(string name, int argsCount) 
            => GetAllConcretes(name).Where(r => r.ArgTypes.Length == argsCount).ToList();

        public FunctionBase GetOrNullWithOverloadSearch(string name,VarType returnType, params VarType[] args)
        {
            var filtered = GetConcretes(name, args.Length);
            //If there is no function with specified name and count
            if (filtered.Count==0)
                return GetOrNullGenerics(name, returnType, args);
            
            //Search full match:
            var fullMatch = filtered.Where(f => f.ArgTypes.SequenceEqual(args)).ToArray();
            if (fullMatch.Length == 1)
                return fullMatch.First();

            //Then search generics
            var genericFun = GetOrNullGenerics(name,returnType, args);
            if (genericFun != null)
                return genericFun;
            //If concrete candidate is only one - try to cast it
            if (filtered.Count == 1)
            {
                var candidate = filtered.First();
                return VarTypeConverter.CanBeConverted(args, candidate.ArgTypes) ? candidate : null;
            }
            
            //Try to find most close of castable functions
            int maxCloseness = 0;
            bool severalFunctionsWithSameCloseness = false;
            FunctionBase winner = null;
            foreach (var function in filtered)
            {
                var closeness = VarTypeConverter.GetCloseness(args, function.ArgTypes);
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
            var filtered = GetConcretes(name, args.Length);
            //If there is no function with specified name and count
            if (filtered.Count == 0)
                return GetOrNullGenerics(name, returnType, args);
            
            //Search full match:
            var fullMatch = filtered.Where(f => f.ArgTypes.SequenceEqual(args)).ToArray();
            if (fullMatch.Length == 1)
                return fullMatch.First();

            //Then search generics
            var genericFun = GetOrNullGenerics(name,returnType, args);
            if (genericFun != null)
                return genericFun;
            //If concrete candidate is only one - try to cast it
            if (filtered.Count == 1)
            {
                var candidate = filtered.First();
                return VarTypeConverter.CanBeConverted(args, candidate.ArgTypes) ? candidate : null;
            }

            return null;
        }
        
        private FunctionBase GetOrNullGenerics(string name, VarType returnType, params VarType[] args)
        {
            //Filter functions with specified arguments count
            var filtered = GetAllGenerics(name, args.Length);
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


        private IEnumerable<FunctionBase> GetAllConcretes(string name)
        {
            var builtIn = _builtInFunctions.GetConcretes(name);
            if (_userFunctions.TryGetValue(name, out var res))
            {
                return builtIn.Concat(res);
            }

            return builtIn;
        }


        private IEnumerable<GenericFunctionBase> GetAllGenerics(string name, int count)
        {
            var builtIn = _builtInFunctions.GetGenerics(name, count);
            if (_genericUserFunctions.TryGetValue(name, out var res))
            {
                return builtIn.Concat(res.Where(r=>r.ArgTypes.Length== count));
            }

            return builtIn;
        }

    }
}