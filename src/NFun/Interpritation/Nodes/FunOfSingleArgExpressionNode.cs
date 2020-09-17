using NFun.Interpritation.Functions;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class FunOfSingleArgExpressionNode : IExpressionNode
    {
        private readonly FunctionWithSingleArg _fun;
        private readonly IExpressionNode arg1;

        public FunOfSingleArgExpressionNode(FunctionWithSingleArg fun, IExpressionNode argsNode, Interval interval)
        {
            _fun = fun;
            arg1 = argsNode;
            Interval = interval;
        }
        public Interval Interval { get; }
        public VarType Type => _fun.ReturnType;
        public object Calc() => _fun.Calc(arg1.Calc());
    }
}