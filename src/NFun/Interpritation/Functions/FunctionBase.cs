using System.Collections.Generic;
using NFun.Interpritation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class FunctionBase
    {
        public string Name { get; }
        public VarType[] ArgTypes { get; }
        protected FunctionBase(string name,  VarType outputType, params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            OutputType = outputType;
        }
        public VarType OutputType { get; }
        public abstract object Calc(object[] args);

        public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, Interval interval)
        {
            var castedChildren = new List<IExpressionNode>();

            var i = 0;
            foreach (var argNode in children)
            {
                var toType = ArgTypes[i];
                var fromType = argNode.Type;
                var castedNode = argNode;
                if (fromType != toType)
                {
                    var converter = CastExpressionNode.GetConverterOrThrow(fromType, toType, argNode.Interval);
                    castedNode = new CastExpressionNode(argNode, toType, converter,argNode.Interval);
                }

                castedChildren.Add(castedNode);
                i++;
            }

            return new FunExpressionNode(this, castedChildren.ToArray(),interval);
        }
    }
        
   
}