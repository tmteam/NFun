using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 

public interface IExpressionNode {
    Interval Interval { get; }
    FunnyType Type { get; }
    object Calc();
}