using System.Collections.Generic;

namespace Funny.ParserAnylizer
{
    public class LexVarAnalytics
    {
        public readonly HashSet<int> UsedInOutputs = new HashSet<int>();
        public readonly string Id;
        public bool IsOutput;
        public LexVarAnalytics(string id, bool isOutput = false)
        {
            IsOutput = isOutput;
            Id = id;
        }
    }
}