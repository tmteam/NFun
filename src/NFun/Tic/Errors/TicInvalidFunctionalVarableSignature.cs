using NFun.Tic.SolvingStates;

namespace NFun.Tic.Errors {

public class TicInvalidFunctionalVarableSignature : TicException {
    public TicNode FuncNode { get; }
    public StateFun StateFun { get; }
    public TicInvalidFunctionalVarableSignature(TicNode funcNode, StateFun stateFun) : base("InvalidFunctionalVarableSignature") {
        FuncNode = funcNode;
        StateFun = stateFun;
    }
}

}