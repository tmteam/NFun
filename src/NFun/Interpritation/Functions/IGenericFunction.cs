using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public interface IGenericFunction : IFunctionSignature
    {
        IConcreteFunction CreateConcrete(VarType[] concreteTypesMap);

        /// <summary>
        /// calculates generic call arguments  based on a concrete call signature
        /// </summary> 
        VarType[] CalcGenericArgTypeList(FunTypeSpecification funTypeSpecification);
    }
}