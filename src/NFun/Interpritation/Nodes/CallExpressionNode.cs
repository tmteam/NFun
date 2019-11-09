using System.Linq;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class CallExpressionNode : IExpressionNode
    {
        public FunctionBase FunctionDefenition { get; }

        private readonly IExpressionNode[] _argsNodes;

        public CallExpressionNode(FunctionBase fun, IExpressionNode[] argsNodes, Interval interval)
        {
            FunctionDefenition = fun;
            _argsNodes = argsNodes;
            Interval = interval;
        }

        

        public Interval Interval { get; }
        public VarType Type => FunctionDefenition.ReturnType;
        public object Calc()
        {
            var argValues = _argsNodes
                .Select(a => a.Calc())
                .ToArray();
            return FunctionDefenition.Calc(argValues);
        }

        public void Apply(IExpressionNodeVisitor visitor)
        {
            visitor.Visit(this , FunctionDefenition.Name, _argsNodes.Select(a=>a.Type).ToArray());
            foreach (var expressionNode in _argsNodes)
            {
                expressionNode.Apply(visitor);
            }
        }
    }

  
}