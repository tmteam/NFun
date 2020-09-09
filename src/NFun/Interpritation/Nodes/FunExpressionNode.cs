using System;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class FunOf2ArgsExpressionNode : IExpressionNode
    {
        private readonly FunctionWithTwoArgs _fun;
        private readonly IExpressionNode arg1;
        private readonly IExpressionNode arg2;

        public FunOf2ArgsExpressionNode(FunctionWithTwoArgs fun, IExpressionNode[] argsNodes, Interval interval)
        {
            _fun = fun;
            arg1 = argsNodes[0];
            arg2 = argsNodes[1];
            Interval = interval;
        }
        public Interval Interval { get; }
        public VarType Type => _fun.ReturnType;
        public object Calc() => _fun.Calc(arg1.Calc(), arg2.Calc());
    }
    public class FunExpressionNode : IExpressionNode
    {
        private readonly FunctionBase _fun;
        private readonly IExpressionNode[] _argsNodes;

        public FunExpressionNode(FunctionBase fun, IExpressionNode[] argsNodes, Interval interval)
        {
            _fun = fun;
            _argsNodes = argsNodes;
            Interval = interval;
            _argsCount = argsNodes.Length;
        }
        private readonly int _argsCount;
        public Interval Interval { get; }
        public VarType Type => _fun.ReturnType;
        public object Calc()
        {
            var _args = new object[_argsCount];
            for (int i = 0; i < _argsCount; i++) 
                _args[i] = _argsNodes[i].Calc();
            return _fun.Calc(_args);
        }
    }
}