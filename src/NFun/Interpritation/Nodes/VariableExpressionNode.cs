using NFun.BuiltInFunctions;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class VariableExpressionNode: IExpressionNode
    {
        private static int _count = 0;
        private readonly int _uid = _count++;

        public VariableExpressionNode(VariableSource source, Interval interval)
        {
            Source = source;
            Interval = interval;
        }

        public VariableSource Source { get; }
        public Interval Interval { get; }
        public VarType Type => Source.Type;
        
        public object Calc()
        {
            if(Source.Value==null)
                throw new FunRuntimeException($"Variable '{Source.Name}' is not set");
            return Source.Value;
        }
        public override string ToString() => $"{Source.Name}: {Source.Value} uid: {_uid}";

    }
}