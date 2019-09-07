using System;
using System.Collections.Generic;
using System.Linq;
using Nfun.Fuspec.Parser.Model;

namespace ConsoleUseParser
{
    public static class ConsoleRender
    {
        public static void Print(this FuspecTestCase fuspecTestCase)
        {
            Console.WriteLine("----------------------");
            
            Console.WriteLine("| Name of test: {0}", fuspecTestCase.Name);
            
            Console.Write("| Tags: ");
            if (fuspecTestCase.Tags.Length == 0)
                Console.WriteLine("No tags");
            else
                foreach (var tag in fuspecTestCase.Tags)
                    Console.WriteLine(tag+"   ");
            
            if (fuspecTestCase.ParamsIn.Length!=0)
                PrintElement("| Type for input parameters:  ",fuspecTestCase.ParamsIn);
            
            if (fuspecTestCase.ParamsOut.Length!=0)
                PrintElement("| Type for output parameters:  ",fuspecTestCase.ParamsOut);
            
            Console.WriteLine("| Body script:");
            Console.WriteLine(fuspecTestCase.Script);

            if (fuspecTestCase.SetCheckKits.Length != 0)
            {
                Console.WriteLine("| Sets of values ");
                foreach (var setCheckKit in fuspecTestCase.SetCheckKits)
                {
                    Console.WriteLine("###");
              //      if (setCheckKit.SetKit.Any())
                //        PrintElement("  Set",setCheckKit.SetKit);
                       
                  //  if (setCheckKit.CheckKit.Any())
                    //    PrintElement("  Check:",setCheckKit.CheckKit);
                }
            }
        }

        private static void PrintElement(string message, IEnumerable<string> element)
        {
            Console.Write(message);
            foreach (var str in element)
                Console.Write(" "+ str);
            Console.WriteLine();
        }
        
        private static void PrintElement(string message, IEnumerable<Param> paramsInOut)
        {
            Console.Write(message);
            foreach (var param in paramsInOut)
                Console.Write(param.Value+" : "+param.VarType+"   ");
            Console.WriteLine();
        }
    }
}