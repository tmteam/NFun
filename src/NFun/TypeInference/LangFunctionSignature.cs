using System.Linq;
using NFun.TypeInference.Solving;
using NFun.Types;

namespace NFun.TypeInference
{
    public class LangFunctionSignature{
               public readonly VarType ReturnType;
               public readonly VarType[] ArgTypes;
               public LangFunctionSignature(VarType output, VarType[] inputs)
               {
                   ReturnType = output;
                   ArgTypes = inputs;
               }
       }
}