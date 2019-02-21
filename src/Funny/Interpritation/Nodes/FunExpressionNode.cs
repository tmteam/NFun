using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation.Functions;
using Funny.Runtime;

namespace Funny.Interpritation.Nodes
{
    public class FunExpressionNode : IExpressionNode
    {
        private readonly FunctionBase _fun;
        private readonly IExpressionNode[] _args;

        public FunExpressionNode(FunctionBase fun, IExpressionNode[] args)
        {
            _fun = fun;
            _args = args;
            foreach (var node in args)
            {
                if (node.Type != VarType.IntType && node.Type != VarType.RealType)
                    throw new OutpuCastParseException("Input variables have to be number or int types");
            }

            Children = _args;
        }

        public IEnumerable<IExpressionNode> Children { get; }

        public object Calc()
        {
            var doubleArgs = _args
                .Select(a => Convert.ToDouble(a.Calc()))
                .ToArray();
            return _fun.Calc(doubleArgs);
        }

        public VarType Type => _fun.Type;
    }
}