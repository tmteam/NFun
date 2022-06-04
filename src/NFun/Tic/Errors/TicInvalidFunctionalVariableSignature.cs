using NFun.Tic.SolvingStates;

namespace NFun.Tic.Errors; 

internal class TicInvalidFunctionalVariableSignature : TicException {
    public TicNode FuncNode { get; }
    public StateFun StateFun { get; }
    public TicInvalidFunctionalVariableSignature(TicNode funcNode, StateFun stateFun) : base("InvalidFunctionalVarableSignature") {
        FuncNode = funcNode;
        StateFun = stateFun;
    }
}