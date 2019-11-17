using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Nfun.Fuspec.Parser.Model;
using NFun.Types;

namespace ConsoleUseParser
{
    public static class ConsoleRender
    {
        public static void Print(this FuspecTestCase fuspecTestCase)
        {
            Console.WriteLine("----------------------");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("| Name of test: ");
            Console.ResetColor();
            Console.WriteLine("{0}", fuspecTestCase.Name);
            
            Console.Write("| Tags: ");
            if (fuspecTestCase.Tags.Length == 0)
                Console.WriteLine("No tags");
            else
                foreach (var tag in fuspecTestCase.Tags)
                    Console.WriteLine(tag+"   ");
            
            if (fuspecTestCase.InputVarList.Length!=0)
                PrintElement("| Type for input parameters:  ",fuspecTestCase.InputVarList);
            
            if (fuspecTestCase.OutputVarList.Length!=0)
                PrintElement("| Type for output parameters:  ",fuspecTestCase.OutputVarList);
            
            Console.WriteLine("| Body script:");
            Console.WriteLine(fuspecTestCase.Script);

            if (fuspecTestCase.SetChecks.Length != 0)
            {
                Console.WriteLine("| Sets of values ");
               
                int i = 0;
                foreach (var setCheckKit in fuspecTestCase.SetChecks)
                {
                    i++;
                    Console.Write("\t");
                    Console.Write(i + " set: ");
                    if (setCheckKit.Set.Any())
                        foreach (var set in setCheckKit.Set)
                        {
                            Console.Write(set.Name + ":" + set.Value + "(" + set.Type + "), ");
                        }
                    else Console.Write("None");

                    Console.WriteLine();
                    Console.Write("\t");
                    Console.Write(i + " check: ");
                    if (setCheckKit.Check.Any())
                        foreach (var check in setCheckKit.Check)
                        {
                            Console.Write(check.Name + ":" + check.Value + "(" + check.Type + "), ");
                        }
                    else Console.Write("None");

                    Console.WriteLine();
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
        
        private static void PrintElement(string message, IEnumerable<VarInfo> paramsInOut)
        {
            Console.Write(message);
            foreach (var param in paramsInOut)
                Console.Write(param.Name+" : "+param.Type+"   ");
            Console.WriteLine();
        }
    }
}