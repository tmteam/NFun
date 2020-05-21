using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;

namespace NFun.Interpritation
{
    public sealed class ScopeFunctionDictionary : IFunctionDictionary
    {
        private readonly IFunctionDictionary _originalDictionary;
        private readonly Dictionary<string, IFunctionSignature> _functions
            = new Dictionary<string, IFunctionSignature>();
        private readonly Dictionary<string, List<IFunctionSignature>> _overloads
            = new Dictionary<string, List<IFunctionSignature>>();
        public ScopeFunctionDictionary(IFunctionDictionary originalDictionary)
        {
            _originalDictionary = originalDictionary;
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
            if(results.Any())
                return results;
            return _originalDictionary.SearchAllFunctionsIgnoreCase(name, argCount);
        }

        public IList<IFunctionSignature> GetOverloads(string name)
        {
            var origins = _originalDictionary.GetOverloads(name);
            if (!_overloads.TryGetValue(name, out var signatures))
                return origins;
            return signatures.Union(origins).ToList();
        }
        public IFunctionSignature GetOrNull(string name, int argCount)
        {
            var overloadName = GetOverloadName(name, argCount);
            _functions.TryGetValue(overloadName, out var signature);
            if (signature == null)
                return _originalDictionary.GetOrNull(name, argCount);
            return signature;
        }
        public bool Add(IFunctionSignature function)
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
    public interface IFunctionDictionary
    {
        IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount);

        IList<IFunctionSignature> GetOverloads(string name);
        IFunctionSignature GetOrNull(string name, int argCount);
    }

    public sealed class FunctionDictionary: IFunctionDictionary
    {
        private readonly Dictionary<string, IFunctionSignature> _functions 
            = new Dictionary<string, IFunctionSignature>();

        private readonly Dictionary<string, List<IFunctionSignature>> _overloads
            = new Dictionary<string, List<IFunctionSignature>>();

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