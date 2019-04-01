using System.Collections.Generic;

namespace NFun.LexAnalyze
{
    public class VarAnalysis
    {
        public readonly HashSet<int> UsedInOutputs = new HashSet<int>();
        public readonly string Id;
        public bool IsOutput;
        public VarAnalysis(string id, bool isOutput = false)
        {
            IsOutput = isOutput;
            Id = id;
        }
    }
}