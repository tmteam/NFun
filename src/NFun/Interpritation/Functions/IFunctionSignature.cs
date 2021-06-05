using System.Collections.Generic;
using NFun.Interpritation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
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