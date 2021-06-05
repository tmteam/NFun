using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public interface IGenericFunction : IFunctionSignature
    {
        IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap);

        /// <summary>
        /// calculates generic call arguments  based on a concrete call signature
        /// </summary> 
        FunnyType[] CalcGenericArgTypeList(FunTypeSpecification funTypeSpecification);
    }
}