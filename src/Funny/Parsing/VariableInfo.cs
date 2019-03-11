using Funny.Runtime;

namespace Funny.Parsing
{
    public class VariableInfo
    {
        public readonly string Id;
        public readonly VarType Type;

        public VariableInfo(string id, VarType type)
        {
            Id = id;
            Type = type;
        }
    }
}