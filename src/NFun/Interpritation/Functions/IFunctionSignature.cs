using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public interface IFunctionSignature
    {
        string Name { get; }
        VarType[] ArgTypes { get; }
        VarType ReturnType { get; }
    }
}