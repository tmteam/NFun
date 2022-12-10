using System.Collections.Generic;
using NFun.Interpretation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Functions; 

public abstract class FunctionWithManyArguments : IConcreteFunction {
    public string Name { get; }
    public FunnyType[] ArgTypes { get; protected set; }

    protected FunctionWithManyArguments(string name) => Name = name;

    protected FunctionWithManyArguments(string name, FunnyType returnType, params FunnyType[] argTypes) {
        Name = name;
        ArgTypes = argTypes;
        ReturnType = returnType;
    }

    public FunnyType ReturnType { get; protected set; }
    public abstract object Calc(object[] args);
    public virtual IConcreteFunction Clone(ICloneContext context) => this;
    

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

        return new FunOfManyArgsExpressionNode(this, castedChildren, interval);
    }

    public override string ToString() => $"FUN-many {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";
}