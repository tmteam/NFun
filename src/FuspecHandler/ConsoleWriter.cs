using System;
using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using NFun.ParseErrors;

namespace FuspecHandler
{
    public static class ConsoleWriter
    {
        public static void PrintTestName(string name)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("################################################");
            Console.WriteLine("Now testing {0} file... ", name);
            Console.WriteLine("################################################");

            Console.WriteLine();
            Console.ResetColor();

        }

        public static void PrintFuspecName(string fusName, string file)
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Name of Test: {0}. File: {1}", fusName, file);
            Console.ResetColor();
        }

        public static void PrintFuspecParserException(FuspecParserException e, string file)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.Black;
            var error = e.Errors.FirstOrDefault();
            Console.WriteLine("Error in file: {0} in line: {1}", file, error.LineNumber);
            Console.WriteLine(error.ErrorType.ToString());
            Console.ResetColor();
        }

        public static void PrintUnknownException(Exception e, string file)
        {
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("Unknown Exeption!!!!!");
            Console.WriteLine(e.Message);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(e.StackTrace);
            Console.WriteLine();
            Console.ResetColor();
        }

        public static void PrintOkTest()
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("It's ok!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
        }

        public static void PrintFunParseException(FunParseException e, string script)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" ERROR [FU" + e.Code + "] ");
            Console.Write($" {e.Message} ");

            Console.ResetColor();

            if (e.End != -1)
            {
                if (e.Start > 0)
                    Console.Write(script.Substring(0, e.Start));

                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;

                Console.Write(e.Interval.SubString(script));
                Console.ResetColor();
                if (script.Length >= e.End)
                    Console.Write(script.Substring(e.End));
                Console.WriteLine();
            }
        }

    }
}