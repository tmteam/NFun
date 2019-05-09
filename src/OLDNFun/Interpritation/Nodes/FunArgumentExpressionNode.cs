using System;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class FunArgumentExpressionNode : IExpressionNode
    {
        public string Name { get; }
        public Interval Interval { get; }
        public FunArgumentExpressionNode(string name, VarType type, Interval interval)
        {
            Type = type;
            Interval = interval;
            Name = name;
        }
        
        public VarType Type { get; } 
        
        public object Calc() => throw new InvalidOperationException();
        
        public override string ToString() => $"{Name}: {Type}";
    }
}