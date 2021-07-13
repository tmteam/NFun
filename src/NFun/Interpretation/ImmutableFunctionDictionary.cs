using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation.Functions;

namespace NFun.Interpretation
{
    internal sealed class ImmutableFunctionDictionary : IFunctionDictionary
    {
        private readonly Dictionary<string, IFunctionSignature> _functions;

        //TODO - OVERLOADS?!?! O_o_O
        private readonly Dictionary<string, List<IFunctionSignature>> _overloads;

        public ImmutableFunctionDictionary(IConcreteFunction[] concretes, GenericFunctionBase[] generics)
        {
            _functions = new Dictionary<string, IFunctionSignature>(concretes.Length + generics.Length);
            _overloads = new Dictionary<string, List<IFunctionSignature>>();
            foreach (var concrete in concretes) TryAdd(concrete);
            foreach (var generic in generics) TryAdd(generic);
        }

        private ImmutableFunctionDictionary(Dictionary<string, IFunctionSignature> functions,
            Dictionary<string, List<IFunctionSignature>> overloads)
        {
            _functions = functions;
            _overloads = overloads;
        }

        public IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount)
        {
            var lowerName = GetOverloadName(name.ToLower(), argCount);
            var results = new List<IFunctionSignature>();
            foreach (var function in _functions)
            {
                if (function.Key.ToLower() == lowerName)
                {
                    results.Add(function.Value);
                }
            }

            return results;
        }

        public IList<IFunctionSignature> GetOverloads(string name)
        {
            if (!_overloads.TryGetValue(name, out var signatures))
                return Array.Empty<IFunctionSignature>();
            return signatures;
        }

        public IFunctionSignature GetOrNull(string name, int argCount)
        {
            var overloadName = GetOverloadName(name, argCount);
            _functions.TryGetValue(overloadName, out var signature);
            return signature;
        }

        public ImmutableFunctionDictionary CloneWith(params IFunctionSignature[] functions)
        {
            var newFunctions = new Dictionary<string, IFunctionSignature>(_functions);
            var newOverloads = _overloads.ToDictionary(
                o => o.Key,
                o => o.Value.ToList());
            var dic = new ImmutableFunctionDictionary(newFunctions, newOverloads);
            foreach (var function in functions)
            {
                if (!dic.TryAdd(function))
                    throw new InvalidOperationException(
                        $"function with signature {GetOverloadName(function.Name, function.ArgTypes.Length)} already exists");
            }

            return dic;
        }

        private bool TryAdd(IFunctionSignature function)
        {
            var name = GetOverloadName(function.Name, function.ArgTypes.Length);
            if (_functions.ContainsKey(name))
                return false;
            _functions.Add(name, function);
            if (!_overloads.ContainsKey(function.Name))
            {
                _overloads.Add(function.Name, new List<IFunctionSignature>());
            }

            _overloads[function.Name].Add(function);
            return true;
        }

        private static string GetOverloadName(string name, int argCount)
            => name + " " + argCount;
    }
}