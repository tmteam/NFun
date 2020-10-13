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
            var ex2 = "if(a>0) 10*x*x + 12*x + 1 else 0";
            while (true)
            {
                FunBuilder.Build(ex2);
            }
        }
    }
}