using System;
using System.Threading.Tasks;

namespace NFun.Fuspec.ConsoleTestHandler
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                RunFuspecTests.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
        }
    }
}
