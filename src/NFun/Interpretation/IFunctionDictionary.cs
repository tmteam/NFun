using System.Collections.Generic;
using NFun.Interpretation.Functions;

namespace NFun.Interpretation
{
    public interface IFunctionDictionary
    {
        IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount);
        IList<IFunctionSignature> GetOverloads(string name);
        IFunctionSignature GetOrNull(string name, int argCount);
    }
}