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
using Funny.Types;

namespace Funny
{
    class Program
    {
        /*
        @"fibrec(n, iter, p1,p2) =
                          if n <iter then fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if n<3 then 1 else fibrec(n,2,1,1)
                   y = fib(x)";*/
        
        static int fibrec(int n, int iter, int p1, int p2)
        {
            if (n > iter) return fibrec(n, iter + 1, p1 + p2, p1);
            else return p1 + p2;
        }

        static int fib(int n)
        {
            if (n < 3) return 1;
            else return fibrec(n-1, 2, 1, 1);
        }
        
        static void Main(string[] args)
        {
            fib(6);
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine($"{i}: {fib(i)}");
            }
            Console.ReadLine();

            
            
            var y = 4 / 2 * 2;
            var ex = "y1 = (1+3)*7+2*(x+1)";
            PrintParsing(ex);

            var runtime = Interpreter.BuildOrThrow(ex);
            
            var tokens = Tokenizer.ToTokens(ex);
            var flow = new TokenFlow(tokens);
            var eq =  Parser.Parse(flow).Equations;
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
                catch (FunParseException e)
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
            foreach (var equation in eq.Equations)
            {
                Console.WriteLine($"{equation.Id}={equation.Expression}");
                PrintNode(equation.Expression,1);
            }
            
            Console.WriteLine("Eq countL " + eq.Equations.Length);
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
            var sb = new StringBuilder();
            sb.Append(new string('.', offset*2));
            sb.Append(msg+Environment.NewLine);
            Console.Write(sb.ToString());
        }
    }


}