namespace NFun.Types
{
    public class FunTypeSpecification
    {
        public readonly VarType Output;
        public readonly VarType[] Inputs;
        public FunTypeSpecification(VarType output, VarType[] inputs)
        {
            Output = output;
            Inputs = inputs;
        }
    }
}