using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class VariableExpressionNode: IExpressionNode
    {
        internal VariableExpressionNode(VariableSource source, Interval interval)
        {
            Source = source;
            Interval = interval;
        }

        internal VariableSource Source { get; }
        public Interval Interval { get; }
        public FunnyType Type => Source.Type;
        public object Calc() => Source.InternalFunnyValue;
        public override string ToString() => $"{Source.Name}: {Source.InternalFunnyValue}";

    }
}