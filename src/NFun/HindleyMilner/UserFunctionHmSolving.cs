using NFun.HindleyMilner.Tyso;

namespace NFun.HindleyMilner
{
    public class UserFunctionHmSolving
    {
        public UserFunctionHmSolving(string name, int argsCount, NsHumanizerSolver solver)
        {
            Name = name;
            ArgsCount = argsCount;
            Solver = solver;
        }

        public string Name { get; }
        public int ArgsCount { get; }
        public NsHumanizerSolver Solver { get; }
    }
}