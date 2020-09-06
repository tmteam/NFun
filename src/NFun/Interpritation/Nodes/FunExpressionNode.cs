using System;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
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