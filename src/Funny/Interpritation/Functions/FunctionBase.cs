using System.Collections.Generic;
using System.IO;
using System.Linq;
using Funny.Interpritation.Nodes;
using Funny.Runtime;
using Funny.Types;

namespace Funny.Interpritation.Functions
{
    public abstract class FunctionBase
    {
        
        public string Name { get; }
        public int ArgsCount { get; }
        public VarType[] ArgTypes { get; }
        protected FunctionBase(string name,  VarType outputType, params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            ArgsCount = ArgTypes.Length;
            OutputType = outputType;
        }
        public VarType OutputType { get; }
        public abstract object Calc(object[] args);

        public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children)
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
                    var converter = CastExpressionNode.GetConverterOrThrow(fromType, toType);
                    castedNode = new CastExpressionNode(argNode, toType, converter);
                }

                castedChildren.Add(castedNode);
                i++;
            }

            return new FunExpressionNode(this, castedChildren.ToArray());
        }
    }
        
   
}