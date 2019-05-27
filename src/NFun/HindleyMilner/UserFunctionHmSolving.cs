using NFun.HindleyMilner.Tyso;

namespace NFun.HindleyMilner
{
    public class UserFunctionHmSolving
    {
        public UserFunctionHmSolving(string name, int argsCount, HmHumanizerSolver solver)
        {
            Name = name;
            ArgsCount = argsCount;
            Solver = solver;
        }

        public string Name { get; }
        public int ArgsCount { get; }
        public HmHumanizerSolver Solver { get; }
    }
}