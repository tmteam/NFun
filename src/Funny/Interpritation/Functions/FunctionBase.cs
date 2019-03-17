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
    }
}