using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;

namespace NFun.Interpritation
{
    public sealed class FlatMutableFunctionDictionary: IFunctionDictionary
    {
        private readonly Dictionary<string, IFunctionSignature> _functions;
        //TODO - OVERLOADS?!?!
        private readonly Dictionary<string, List<IFunctionSignature>> _overloads;

        public FlatMutableFunctionDictionary()
        {
            _functions = new();
            _overloads = new();
        }

        private FlatMutableFunctionDictionary(Dictionary<string, IFunctionSignature> functions,Dictionary<string, List<IFunctionSignature>> overloads )
        {
            _functions = functions;
            _overloads = overloads;
        }
        public IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount)
        {
            var lowerName = GetOverloadName(name.ToLower(),argCount);
            var results = new List<IFunctionSignature>();
            foreach (var function in _functions)
            {
                if (function.Key.ToLower() == lowerName) {
                    results.Add(function.Value);
                }
            }

            return results;
        }

        public IList<IFunctionSignature> GetOverloads(string name)
        {
            if (!_overloads.TryGetValue(name, out var signatures))
                return new IFunctionSignature[0];
            return signatures;
        }
        public IFunctionSignature GetOrNull(string name, int argCount)
        {
            var overloadName = GetOverloadName(name, argCount);
            _functions.TryGetValue(overloadName, out var signature);
            return signature;
        }

        public FlatMutableFunctionDictionary CloneWith(IFunctionSignature[] functions)
        {
            var newFunctions =new Dictionary<string, IFunctionSignature>(_functions);
            var newOverloads = _overloads.ToDictionary(
                o => o.Key,
                o => o.Value.ToList());
            var dic = new FlatMutableFunctionDictionary(newFunctions, newOverloads);
            foreach (var function in functions) 
                dic.AddOrThrow(function);
            return dic;
        }

        public void AddOrThrow(IFunctionSignature function)
        {
            if(!TryAdd(function))
                throw new InvalidOperationException($"function with signature {GetOverloadName(function.Name, function.ArgTypes.Length)} already exists");
        }
        public bool TryAdd(IFunctionSignature function)
        {
            var name = GetOverloadName(function.Name, function.ArgTypes.Length);
            if (_functions.ContainsKey(name))
                return false;
            _functions.Add(name, function);
            if (!_overloads.ContainsKey(function.Name))
            {
                _overloads.Add(function.Name,new List<IFunctionSignature>());
            }
            _overloads[function.Name].Add(function);
            return true;
        }

        private static string GetOverloadName(string name, int argCount) 
            => name + " " + argCount;
    }
}