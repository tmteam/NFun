using System.Collections.Generic;
using NFun.Interpritation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class FunctionWithTwoArgs : IConcreteFunction
    {
        protected FunctionWithTwoArgs(string name,  VarType returnType, params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            ReturnType = returnType;
        }

        internal FunctionWithTwoArgs()
        {
            
        }

        internal void Setup(string name, VarType type)
        {
            Name = Name;
            ArgTypes = new[] {type, type};
            ReturnType = type;
        }
        public string Name { get; internal set; }
        public VarType[] ArgTypes { get;internal set; }
        public VarType ReturnType { get; internal set;}
        public abstract object Calc(object a, object b);

        public object Calc(object[] parameters) => Calc(parameters[0], parameters[1]);

        public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children,  Interval interval)
        {
            var castedChildren = new IExpressionNode[children.Count];

            var i = 0;
            foreach (var argNode in children)
            {
                var toType = ArgTypes[i];
                var fromType = argNode.Type;
                var castedNode = argNode;
                
                if (fromType != toType)
                {
                    var converter = VarTypeConverter.GetConverterOrThrow(fromType, toType, argNode.Interval);
                    castedNode = new CastExpressionNode(argNode, toType, converter,argNode.Interval);
                }

                castedChildren[i] = castedNode;
                i++;
            }

            return new FunOfTwoArgsExpressionNode(this, castedChildren[0], castedChildren[1], interval);
        }
    }
}