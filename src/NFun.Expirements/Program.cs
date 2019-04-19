using System;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.Expirements
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public class NowFunction : FunctionBase
    {
        public NowFunction() : base("now", VarType.Int64, new VarType[0])
        {
        }

        public override object Calc(object[] args) 
            => DateTime.Now.Ticks;
    }
}