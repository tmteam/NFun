using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;

namespace NFun.Interpritation
{
    public class BuiltInFunctions
    {
        private readonly Dictionary<string, List<FunctionBase>> _functions
            = new Dictionary<string, List<FunctionBase>>();

        private readonly Dictionary<string, List<GenericFunctionBase>> _genericFunctions
            = new Dictionary<string, List<GenericFunctionBase>>();

        public BuiltInFunctions(IEnumerable<FunctionBase> concreteFunctions, IEnumerable<GenericFunctionBase> genericFunctions)
        {
            foreach (var concreteFunction in concreteFunctions)
            {
                if(!_functions.ContainsKey(concreteFunction.Name))
                    _functions.Add(concreteFunction.Name, new List<FunctionBase>());
                _functions[concreteFunction.Name].Add(concreteFunction);
            }
            foreach (var genericFunction in genericFunctions)
            {
                if (!_genericFunctions.ContainsKey(genericFunction.Name))
                    _genericFunctions.Add(genericFunction.Name, new List<GenericFunctionBase>());
                _genericFunctions[genericFunction.Name].Add(genericFunction);
            }
        }

        public IEnumerable<FunctionBase> GetConcretes(string name)
        {
            if (_functions.TryGetValue(name, out var res))
                return res;
            return new FunctionBase[0];
        }

        public IEnumerable<GenericFunctionBase> GetGenerics(string name, int count)
        {
            if (_genericFunctions.TryGetValue(name, out var res))
                return res.Where(r=>r.ArgTypes.Length== count);
            return new GenericFunctionBase[0];
        }
    }
}