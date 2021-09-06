using System.Collections.Generic;
using NFun.Interpretation.Nodes;
using NFun.Tokenization;

namespace NFun.Interpretation.Functions {

public interface IConcreteFunction : IFunctionSignature {
    object Calc(object[] parameters);
    IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, Interval interval);
}

}