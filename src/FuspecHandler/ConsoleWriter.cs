using System;
using NFun.Fuspec.Parser.FuspecParserErrors;
using NFun.BuiltInFunctions;
using NFun.Exceptions;
using NFun.ParseErrors;

namespace FuspecHandler
{
    public class ConsoleWriter
    {
        public void PrintStatisticHeader()
        {
            Console.WriteLine();
            Console.WriteLine("##########################");
            Console.WriteLine("Statistic:");
            Console.WriteLine("##########################");
        }

        public void PrintTestingFileName(string name)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("###########################################################");
            Console.WriteLine("Now testing {0} file... ", name);
            Console.WriteLine("###########################################################");
            Console.ResetColor();
        }

        public void PrintFuspecName(string fusName) =>
            Console.Write("{0,-50}   ", fusName);

        public void PrintOkTest()
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.White;
            PrintLineAndResetColor("  Ok   ");

        }

        public void PrintTODOTest()
        {
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.ForegroundColor = ConsoleColor.White;
            PrintLineAndResetColor(" TODO! ");

        }

        public void PrintNoTests()
        {
            Console.Write("\t\t\t\t\t\t   ");
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.ForegroundColor = ConsoleColor.White;
            PrintLineAndResetColor(" No tests! ");

        }

        public void PrintError(Exception e)
        {
            switch (e)
            {
                case FuspecParserException fusParsExc:
                    Console.Write("\t\t\t\t\t\t  ");
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    Console.ForegroundColor = ConsoleColor.White;
                    PrintLineAndResetColor(" Can't parse! ");
                    break;
                case FunParseException funParsException:
                    PrintError(ConsoleColor.Yellow, ConsoleColor.Black);
                    break;
                case FunRuntimeException funParsException:
                    PrintError(ConsoleColor.Yellow, ConsoleColor.Black);
                    break;
                case TypeAndValuesException outputInputException:
                    PrintError(ConsoleColor.DarkCyan, ConsoleColor.White);
                    break;
                default:
                    PrintError(ConsoleColor.Red, ConsoleColor.White);
                    break;
            }
        }

     

        private void PrintError(ConsoleColor backgroundColor, ConsoleColor foregroundColor)
        {
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = foregroundColor;
            PrintLineAndResetColor(" Error ");
        }

        public void PrintFuspecParserException(string fileName, FuspecParserError e)
        {
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\tName of broken file: ");
            Console.ResetColor();
            Console.WriteLine("{0} \tLINE: {1}  \tMESSAGE: {2}", fileName, e.LineNumber, e.ErrorType);
        }

        public void PrintFuspecRunTimeException(FunRuntimeException e, string file, string test)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("ERROR :");
            Console.ResetColor();

            Console.WriteLine("in file:  {0}\n      in test:  {1}.", file, test);
            Console.WriteLine("\t\tError in file: {0}.", file);

            Console.WriteLine("Expression cannot be calculated: " + e.Message);
            Console.WriteLine();
        }

        public void PrintFunParseException(FunParseException e, string file, string script, string test, int startLine)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("ERROR [FU" + e.Code + "] ");
            PrintLineAndResetColor($" {e.Message} ");
            Console.WriteLine("FILE:       {0}\nTEST:       {1}\nSTARTLINE:  {2}", file, test, startLine);
            Console.WriteLine("\r\n");
            if (e.End != -1)
            {
                if (e.Start > 0)
                    Console.Write(script.Substring(0, e.Start));

                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;

                Console.Write(e.Interval.SubString(script));
                Console.ResetColor();
                if (script.Length >= e.End)
                    Console.Write(script.Substring(e.End));
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        public void PrintUnknownException(string fileName, string testName, Exception e)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Unknown Exception {e.GetType().Name}");

            Console.Write("File: ");
            Console.ResetColor();
            Console.Write("{0}\t\t\r\n", fileName);
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Name: ");
            Console.ResetColor();
            Console.WriteLine(testName);

            Console.WriteLine(e.Message);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(e.StackTrace);
            Console.WriteLine();
        }

        public void PrintNumberOfFiles(int numberOfFiles)
            => Console.WriteLine("Number of files: {0}", numberOfFiles);

        public void PrintNumberOfSuccessfulParsedFiles(int numberOfSuccessfulParsedFiles)
            => Console.WriteLine("Number of Successful parsed Files: {0}", numberOfSuccessfulParsedFiles);

        public void PrintOutpitInputException(string fileName, string testName, string[] messages)
        {
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.Black;
            PrintLineAndResetColor("ERROR! Types check failed! :");

            Console.Write("File: ");
            Console.WriteLine("{0}\t\t ", fileName);
            Console.Write("Name: ");
            Console.WriteLine(testName);

            foreach (var message in messages)
            {
                Console.WriteLine("\t\t " + message);
            }

        }

        private void PrintLineAndResetColor(string val)
        {
            Console.Write(val);
            Console.ResetColor();
            Console.Write("\r\n");
        }
    }
}