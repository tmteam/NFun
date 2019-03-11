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
        private readonly IExpressionNode[] _argsNodes;

        public FunExpressionNode(FunctionBase fun, IExpressionNode[] argsNodes)
        {
            _fun = fun;
            _argsNodes = argsNodes;
            /*foreach (var node in argsNodes)
            {
                if (node.Type != VarType.IntType && node.Type != VarType.RealType)
                    throw new OutpuCastParseException("Input variables have to be number or int types");
            }*/

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