using System;
using System.Collections.Generic;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 

internal class VariableExpressionNode : IExpressionNode {
    internal VariableExpressionNode(VariableSource source, Interval interval) {
        Source = source;
        Interval = interval;
    }
    internal VariableSource Source { get; }
    public Interval Interval { get; }
    public FunnyType Type => Source.Type;
    public IEnumerable<IExpressionNode> Children => Array.Empty<IExpressionNode>();

    public object Calc() => 
        Source.FunnyValue;

    public IExpressionNode Clone(ICloneContext context) {
        var sourceCopy = context.GetVariableSourceClone(Source);
        return new VariableExpressionNode(sourceCopy, Interval);
    }
    
    public override string ToString() => 
        $"{Source.Name}: {Source.FunnyValue}";
    
}