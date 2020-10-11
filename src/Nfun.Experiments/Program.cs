using System;
using NFun;

namespace Nfun.Experiments
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var ex1 = "10*x*x + 12*x + 1";

            while (true)
            {
                FunBuilder.Build(ex1);
            }
        }
    }
}