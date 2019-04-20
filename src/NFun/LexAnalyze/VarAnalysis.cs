using System;
using System.Collections.Generic;
using NFun.Parsing;

namespace NFun.LexAnalyze
{
    public class VarAnalysis
    {
        public readonly HashSet<int> UsedInOutputs = new HashSet<int>();
        public readonly string Id;
        public readonly VarAttribute[] Attributes;
        public bool IsOutput;
        public VarAnalysis(string id, VarAttribute[] attributes, bool isOutput = false)
        {
            Attributes = attributes;
            IsOutput = isOutput;
            Id = id;
            Attributes = attributes;
        }
    }
}