using System;
using System.Linq;
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
    public class FunOf2ArgsExpressionNode : IExpressionNode
    {
        private readonly FunctionWithTwoArgs _fun;
        private readonly IExpressionNode arg1;
        private readonly IExpressionNode arg2;

        public FunOf2ArgsExpressionNode(FunctionWithTwoArgs fun, IExpressionNode argNode1, IExpressionNode argNode2, Interval interval)
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