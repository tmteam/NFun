using Funny.Interpritation;

namespace Funny.BuiltInFunctions
{
    public class AddFunction: FunctionBase
    {
        public AddFunction() : base("add", 2)
        {
        }

        public override double Calc(double[] args)
        {
            return args[0] + args[1];
        }
    }

    public class AbsFunction : FunctionBase
    {
        public AbsFunction() : base("abs", 1)
        {
        }

        public override double Calc(double[] args) => args[0] > 0 ? args[0] : -args[0];
    }
}