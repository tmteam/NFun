using NFun.Interpretation.Functions;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes
{
    public class FunOfSingleArgExpressionNode : IExpressionNode
    {
        private readonly FunctionWithSingleArg _fun;
        private readonly IExpressionNode _arg1;

        public FunOfSingleArgExpressionNode(FunctionWithSingleArg fun, IExpressionNode argsNode, Interval interval)
        {
            _fun = fun;
            _arg1 = argsNode;
            Interval = interval;
        }
        public Interval Interval { get; }
        public FunnyType Type => _fun.ReturnType;
        public object Calc() => _fun.Calc(_arg1.Calc());
    }
}