using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Funny.Interpritation;
using Funny.Parsing;
using Funny.Runtime;
using Funny.Tokenization;

namespace Funny
{
    class Program
    {
        static void Main(string[] args)
        {
            var y = 4 / 2 * 2;
            var ex = "y1 = (1+3)*7+2*(x+1)";
            PrintParsing(ex);

            var runtime = Interpreter.BuildOrThrow(ex);
            
            var tokens = Tokenizer.ToTokens(ex);
            var flow = new TokenFlow(tokens);
            var eq =  Parser.Parse(flow).Equatations;
            var res = runtime.Calculate(Var.New("x", 3));
            
            Console.WriteLine("Res: "+ res);
            Console.ReadLine();
        }

        private static void TestParse()
        {
            var examples = new[]
            {
                "y1=2",
                "y1 = 2*x",
                "y1 = 2*x+1",
                "y1 = 1+2*x",
                "y1 = 2*3*4*5*6",
                "y1 = (1)",
                "y1 = (1+2)",
                "y1 = (1+2)*3",
                "y1 = 1+2*(x+1)",
                "y1 = (1+3)+2*(x+1)",
                "y1 = (1+3)*7+2*(x+1)",
                "y1 = 12*x-1\ny2 = x1 * (x2+ 4) - 13"
            };

            foreach (var example in examples)
            {
                try
                {
                    Console.WriteLine();
                    PrintParsing(example);
                }
                catch (ParseException e)
                {
                    Console.WriteLine("parse err: " + e.Message);
                }
            }
        }

        private static void PrintParsing(string example1)
        {
            
            Console.WriteLine("EXAMPLE:" +example1);
            var tokens = Tokenizer.ToTokens(example1);
            foreach (var token in tokens)
            {
                Console.Write(token + " ");
                if (token.Type == TokType.NewLine)
                    Console.Write("\n");
            }
            Console.WriteLine();

            var flow = new TokenFlow(tokens);
            var eq = Parser.Parse(flow);
            foreach (var equatation in eq.Equatations)
            {
                Console.WriteLine($"{equatation.Id}={equatation.Expression}");
                PrintNode(equatation.Expression,1);
            }
            
            Console.WriteLine("Eq countL " + eq.Equatations.Length);
        }

        private static void PrintNode(LexNode n, int offset)
        {
            PrintLnWithOffset(n.ToString(), offset);
            foreach (var node in n.Children)
            {
                PrintNode(node,offset+1);
            }

        }
        private static void PrintLnWithOffset(string msg, int offset)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(new string('.', offset*2));
            sb.Append(msg+Environment.NewLine);
            Console.Write(sb.ToString());
        }
    }


}