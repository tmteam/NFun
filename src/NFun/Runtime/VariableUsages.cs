using System.Collections.Generic;
using NFun.Interpritation.Nodes;
using NFun.Types;

namespace NFun.Runtime
{
    public class VariableUsages
    {
        public readonly VariableSource Source;
        public readonly LinkedList<VariableExpressionNode> Usages = new();

        internal VariableUsages(VariableSource source)
        {
            Source = source;
        }

        private IFunnyVariable _variable;

        public IFunnyVariable GetVariable()
        {
            if (_variable == null)
            {
                if (Source.IsOutput)
                    _variable = new FunnyOutput(Source, FunnyTypeConverters.GetOutputConverter(Source.Type));
                else
                    _variable = new FunnyInput(Source, FunnyTypeConverters.GetInputConverter(Source.Type));
            }

            return _variable;
        }
    }
}