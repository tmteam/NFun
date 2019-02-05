namespace Funny.Interpritation
{
    public abstract class FunctionBase
    {
        public string Name { get; }
        public int ArgsCount { get; }

        protected FunctionBase(string name, int argsCount)
        {
            Name = name;
            ArgsCount = argsCount;
        }

        public abstract double Calc(double[] args);
    }
}