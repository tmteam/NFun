using Funny.Runtime;

namespace Funny.Interpritation
{
    public abstract class FunctionBase
    {
        public string Name { get; }
        public int ArgsCount { get; }

        protected FunctionBase(string name, int argsCount, VarType type)
        {
            Name = name;
            ArgsCount = argsCount;
            Type = type;
        }
        public VarType Type { get; }
        public abstract object Calc(double[] args);
    }
}