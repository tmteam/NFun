using System.Collections.Generic;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

public interface IExpressionNode {
    Interval Interval { get; }
    FunnyType Type { get; }
    IEnumerable<IExpressionNode> Children { get; }

    object Calc();
    /// <summary>
    /// Creates deep copy of expression that can be used in paralell
    /// </summary>
    IExpressionNode Clone(ICloneContext context);
}