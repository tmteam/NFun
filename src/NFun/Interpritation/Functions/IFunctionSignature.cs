using System.Collections.Generic;
using NFun.Interpritation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public interface IConcreteFunction : IFunctionSignature
    {
        IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, Interval interval);
    }
    
    public interface IFunctionSignature
    {
        string Name { get; }
        VarType[] ArgTypes { get; }
        VarType ReturnType { get; }
    }
}