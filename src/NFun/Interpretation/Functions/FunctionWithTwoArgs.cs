using System.Collections.Generic;
using NFun.Interpretation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Functions {

public abstract class FunctionWithTwoArgs : IConcreteFunction {
    protected FunctionWithTwoArgs(string name, FunnyType returnType, params FunnyType[] argTypes) {
        Name = name;
        ArgTypes = argTypes;
        ReturnType = returnType;
    }

    internal FunctionWithTwoArgs() { }

    internal void Setup(string name, FunnyType type) {
        Name = name;
        ArgTypes = new[] { type, type };
        ReturnType = type;
    }

    public string Name { get; internal set; }
    public FunnyType[] ArgTypes { get; internal set; }
    public FunnyType ReturnType { get; internal set; }
    public abstract object Calc(object a, object b);

    public object Calc(object[] parameters) => Calc(parameters[0], parameters[1]);

    public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, TypeBehaviour typeBehaviour, Interval interval) {
        var castedChildren = new IExpressionNode[children.Count];

        var i = 0;
        foreach (var argNode in children)
        {
            var toType = ArgTypes[i];
            var fromType = argNode.Type;
            var castedNode = argNode;

            if (fromType != toType)
            {
                var converter = VarTypeConverter.GetConverterOrThrow(typeBehaviour, fromType, toType, argNode.Interval);
                castedNode = new CastExpressionNode(argNode, toType, converter, argNode.Interval);
            }

            castedChildren[i] = castedNode;
            i++;
        }

        return new FunOfTwoArgsExpressionNode(this, castedChildren[0], castedChildren[1], interval);
    }
}

}