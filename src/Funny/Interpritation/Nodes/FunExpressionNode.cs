using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation.Functions;
using Funny.Runtime;
using Funny.Types;

namespace Funny.Interpritation.Nodes
{
    public class FunExpressionNode : IExpressionNode
    {
        private readonly FunctionBase _fun;
        private readonly IExpressionNode[] _argsNodes;

        public FunExpressionNode(FunctionBase fun, IExpressionNode[] argsNodes)
        {
            _fun = fun;
            _argsNodes = argsNodes;
            Children = _argsNodes;
        }

        public IEnumerable<IExpressionNode> Children { get; }

        public object Calc()
        {
            var argValues = _argsNodes
                .Select(a => a.Calc())
                .ToArray();
            return _fun.Calc(argValues);
        }

        public VarType Type => _fun.OutputType;
    }
}