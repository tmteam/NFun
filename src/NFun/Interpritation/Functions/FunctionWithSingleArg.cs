using System.Collections.Generic;
using NFun.Interpritation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class FunctionWithSingleArg : IConcreteFunction
    {
        protected FunctionWithSingleArg(string name, VarType returnType, VarType argType)
        {
            Name = name;
            ArgTypes = new[] {argType};
            ReturnType = returnType;
        }

        public string Name { get; }
        public VarType[] ArgTypes { get; }
        public VarType ReturnType { get; }
        public abstract object Calc(object a);

        public object Calc(object[] parameters)
            => Calc(parameters[0]);


        public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, Interval interval)
        {
            var i = 0;
            var argNode = children[0];
            var toType = ArgTypes[i];
            var fromType = argNode.Type;
            var castedNode = argNode;

            if (fromType != toType)
            {
                var converter = VarTypeConverter.GetConverterOrThrow(fromType, toType, argNode.Interval);
                castedNode = new CastExpressionNode(argNode, toType, converter, argNode.Interval);
            }

            i++;

            return new FunOfSingleArgExpressionNode(this, castedNode, interval);
        }
    }
}