using System;
using Nfun.Fuspec.Parser.Model;

namespace ConsoleUseParser
{
    public static class ConsoleRender
    {
        public static void Print(this FuspecTestCase fuspecTestCase)
        {
            Console.WriteLine("----------------------");
            Console.WriteLine("Name of test: {0}", fuspecTestCase.Name);
            Console.Write("Tags: ");
            if (fuspecTestCase.Tags.Length == 0) // если таг "", не добавлять его!!! поправить в FindTas
                Console.WriteLine("No tags");
            else
                foreach (var tag in fuspecTestCase.Tags)
                    Console.WriteLine("    {0}", tag);
            Console.WriteLine("Body script:");
            Console.WriteLine(fuspecTestCase.Script);
        }
    }
}