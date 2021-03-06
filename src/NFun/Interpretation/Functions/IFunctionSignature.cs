using System.Collections.Generic;
using NFun.Interpretation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Functions
{
    public interface IConcreteFunction : IFunctionSignature
    {
        object Calc(object[] parameters);
        IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, Interval interval);
    }

    public interface IFunctionSignature
    {
        string Name { get; }
        FunnyType[] ArgTypes { get; }
        FunnyType ReturnType { get; }
    }
}