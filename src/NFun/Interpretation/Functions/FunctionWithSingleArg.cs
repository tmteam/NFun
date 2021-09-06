using System.Collections.Generic;
using NFun.Interpretation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Functions {

public abstract class FunctionWithSingleArg : IConcreteFunction {
    protected FunctionWithSingleArg(string name, FunnyType returnType, FunnyType argType) {
        Name = name;
        ArgTypes = new[] { argType };
        ReturnType = returnType;
    }

    protected FunctionWithSingleArg() { }

    public string Name { get; internal set; }
    public FunnyType[] ArgTypes { get; internal set; }
    public FunnyType ReturnType { get; internal set; }
    public abstract object Calc(object a);

    public object Calc(object[] parameters)
        => Calc(parameters[0]);


    public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, Interval interval) {
        var argNode = children[0];
        var toType = ArgTypes[0];
        var fromType = argNode.Type;
        var castedNode = argNode;

        if (fromType != toType)
        {
            var converter = VarTypeConverter.GetConverterOrThrow(fromType, toType, argNode.Interval);
            castedNode = new CastExpressionNode(argNode, toType, converter, argNode.Interval);
        }

        return new FunOfSingleArgExpressionNode(this, castedNode, interval);
    }
}

}