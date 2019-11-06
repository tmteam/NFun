using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class FunVariableExpressionNode : IExpressionNode
    {
        public  FunctionBase FunctionDeclaration { get; }

        public FunVariableExpressionNode(FunctionBase fun, Interval interval)
        {
            FunctionDeclaration = fun;
            Interval = interval;
            Type = VarType.Fun(FunctionDeclaration.ReturnType, FunctionDeclaration.ArgTypes);
        }

        public Interval Interval { get; }
        public VarType Type { get; }
        public object Calc() => FunctionDeclaration;

        public void Apply(IExpressionNodeVisitor visitor)
        {
            visitor.Visit(this, FunctionDeclaration.Name);
            if(FunctionDeclaration.Name== Constants.AnonymousFunctionId)
                ((UserFunction)FunctionDeclaration).Apply(visitor);
        }
    }
}