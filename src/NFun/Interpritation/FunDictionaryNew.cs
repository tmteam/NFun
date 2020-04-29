using System.Collections.Generic;
using NFun.Interpritation.Functions;

namespace NFun.Interpritation
{
    public sealed class FunDictionaryNew
    {
        private readonly Dictionary<string, IFunctionSignature> _functions 
            = new Dictionary<string, IFunctionSignature>();

        private readonly Dictionary<string, List<IFunctionSignature>> _overloads
            = new Dictionary<string, List<IFunctionSignature>>();

        public IList<IFunctionSignature> GetOverloads(string name)
        {
            if (!_overloads.TryGetValue(name.ToLower(), out var signatures))
                return new IFunctionSignature[0];
            return signatures;
        }
        public IFunctionSignature GetOrNull(string name, int argCount)
        {
            var overloadName = GetOverloadName(name, argCount);
            _functions.TryGetValue(overloadName, out var signature);
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
                _overloads.Add(function.Name,new List<IFunctionSignature>());
            }
            _overloads[function.Name].Add(function);
            return true;
        }

        private static string GetOverloadName(string name, int argCount) 
            => name.ToLower() + " " + argCount;
    }
}