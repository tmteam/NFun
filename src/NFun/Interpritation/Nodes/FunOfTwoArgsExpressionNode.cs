using NFun.Interpritation.Functions;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class FunOfTwoArgsExpressionNode : IExpressionNode
    {
        private readonly FunctionWithTwoArgs _fun;
        private readonly IExpressionNode arg1;
        private readonly IExpressionNode arg2;

        public FunOfTwoArgsExpressionNode(FunctionWithTwoArgs fun, IExpressionNode argNode1, IExpressionNode argNode2, Interval interval)
        {
            _fun = fun;
            arg1 = argNode1;
            arg2 = argNode2;
            Interval = interval;
        }
        public Interval Interval { get; }
        public VarType Type => _fun.ReturnType;
        public object Calc() => _fun.Calc(arg1.Calc(), arg2.Calc());
    }
}