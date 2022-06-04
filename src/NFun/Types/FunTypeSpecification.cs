namespace NFun.Types; 

public class FunTypeSpecification {
    public readonly FunnyType Output;
    public readonly FunnyType[] Inputs;

    public FunTypeSpecification(FunnyType output, FunnyType[] inputs) {
        Output = output;
        Inputs = inputs;
    }
}