using System.Collections.Generic;
using System.Linq;
using Funny.Runtime;

namespace Funny.Interpritation
{
    public class FunExpressionNode : IExpressionNode
    {
        private readonly FunctionBase _fun;
        private readonly IExpressionNode[] _args;

        public FunExpressionNode(FunctionBase fun, IExpressionNode[] args)
        {
            _fun = fun;
            _args = args;
            Children = _args;
        }
        public IEnumerable<IExpressionNode> Children { get; }
        public object Calc() 
            => _fun.Calc(_args.Select(a => a.Calc()).Cast<double>().ToArray());
        
        public VarType Type => VarType.NumberType;

    }
}