using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class FunVariableExpressionNode : IExpressionNode
    {
        public FunVariableExpressionNode(FunctionBase fun)
        {
            _value = fun;
            Type = VarType.Fun(_value.OutputType, _value.ArgTypes);
        }
        public VarType Type { get; }
        private readonly FunctionBase _value;
        public object Calc() => _value;
    }
}