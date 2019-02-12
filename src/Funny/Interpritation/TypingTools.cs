using Funny.Parsing;
using Funny.Runtime;

namespace Funny.Interpritation
{
    public static class TypeTools
    {
        public static VarType GetUpType(VarType a, VarType b)
        {
            return a > b ? a : b;
        }
    }
}