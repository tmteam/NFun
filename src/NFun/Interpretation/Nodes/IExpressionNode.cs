namespace NFun.Interpretation.Nodes;

public interface IExpressionNode: IRuntimeNode {
    FunnyType Type { get; }
    object Calc();
    /// <summary>
    /// Creates deep copy of expression that can be used in paralell
    /// </summary>
    IExpressionNode Clone(ICloneContext context);
}
