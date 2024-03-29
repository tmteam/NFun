using System.Collections.Generic;
using NFun.Interpretation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Functions; 

public interface IConcreteFunction : IFunctionSignature {
    object Calc(object[] parameters);
    IConcreteFunction Clone(ICloneContext context);
    IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, TypeBehaviour typeBehaviour, Interval interval);
}