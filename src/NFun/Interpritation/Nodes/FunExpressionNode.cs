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
        }
        
        public Interval Interval { get; }
        public VarType Type => _fun.ReturnType;
        public object Calc()
        {
            var args = new object[_argsNodes.Length];
            for (int i = 0; i < args.Length; i++) 
                args[i] = _argsNodes[i].Calc();
            return _fun.Calc(args);
        }
    }
}