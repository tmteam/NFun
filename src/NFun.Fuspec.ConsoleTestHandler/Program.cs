using System;
using System.Threading.Tasks;

namespace NFun.Fuspec.ConsoleTestHandler
{
    class Program
    {
        static void Main(string[] args)
        {
            var testsFromDirectory = new RunFuspecTestsFromDirectory();
            testsFromDirectory.Run();

            Console.ReadLine();
        }
    }
}
