using System;
using System.Globalization;
using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;
using NFun.BuiltInFunctions;
using NFun.ParseErrors;

namespace FuspecHandler
{
    public class ConsoleWriter
    {

        public void PrintLnText(string Text)
        {
            Console.WriteLine(Text);
        }

        public void PrintStatisticHeader()
        {
            Console.WriteLine();
            Console.WriteLine("##########################");
            Console.WriteLine("Statistic:");
            Console.WriteLine("##########################");
        }

        public void PrintTestName(string name)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("################################################");
            Console.WriteLine("Now testing {0} file... ", name);
            Console.WriteLine("################################################");
            Console.ResetColor();
        }

        public void PrintFuspecName(string fusName) =>
            Console.Write("{0,-50}   ", fusName);


        public void PrintOkTest()
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  Ok!  ");
            Console.ResetColor();
        }

        public void PrintTODOTest()
        {
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" TODO! ");
            Console.ResetColor();
        }

        public void PrintError(Exception e)
        {
            switch (e)
            {
                case FuspecParserException fusParsExc:
                    PrintError(fusParsExc.Errors.FirstOrDefault().LineNumber, ConsoleColor.DarkGray,
                        ConsoleColor.Black);
                    break;
                case FunParseException funParsException:
                    PrintError(-1, ConsoleColor.Red, ConsoleColor.White);
                    break;
                case FunRuntimeException funParsException:
                    PrintError(-1, ConsoleColor.DarkRed, ConsoleColor.White);
                    break;
                default:
                    PrintError(-1, ConsoleColor.Yellow, ConsoleColor.Black);
                    break;
            }
        }

        private void PrintError(int numberOfLine, ConsoleColor backgroundColor, ConsoleColor foregroundColor)
        {
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = foregroundColor;
            if (numberOfLine >= 0)
                Console.WriteLine(" Can't parse the file!!!\n\r Error in line {0}!", numberOfLine);
            else
                Console.WriteLine(" Error ");
            Console.ResetColor();
        }

        public void PrintFuspecParserException(string fileName, FuspecParserError e )
        {
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\tName of broken file: ");
            Console.ResetColor();
            Console.WriteLine("{0} \tLINE: {1}  \tMESSAGE: {2}", fileName, e.LineNumber, e.ErrorType);
        }

        public void PrintFuspecRunTimeException(FunRuntimeException e, string file, string test)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("ERROR :");
            Console.ResetColor();

            Console.WriteLine("in file:  {0}\n      in test:  {1}.", file, test);
            Console.WriteLine("\t\tError in file: {0}.", file);

            Console.WriteLine("Expression cannot be calculated: " + e.Message);
            Console.WriteLine();
        }

        public void PrintFunParseException(FunParseException e, string file, string script, string test)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("ERROR [FU" + e.Code + "] ");
            Console.WriteLine($" {e.Message} ");
            Console.ResetColor();
            Console.WriteLine("FILE:  {0}\nTEST:  {1}.", file, test);
            Console.WriteLine("\r\n");
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

            Console.WriteLine();
        }

        public void PrintUnknownException(string fileName, string testName, Exception e)
        {
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("Unknown Exception!!!!!");

            Console.Write("File: ");
            Console.ResetColor();
            Console.Write("{0}\t\t ",fileName);
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("Name: ");
            Console.ResetColor();
            Console.WriteLine(testName);
     
            Console.WriteLine(e.Message);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(e.StackTrace);
            Console.WriteLine();
        }

        public void PrintNumberOfFiles(int numberOfFiles)
        {
            Console.WriteLine("Number of files: {0}", numberOfFiles);
        }

        public void PrintNumberOfSuccessfulParsedFiles(int numberOfSuccessfulParsedFiles)
        {
            Console.WriteLine("Number of Successful parsed Files: {0}", numberOfSuccessfulParsedFiles);
        }

        public void PrintBrokenFile()
        {
            Console.Write("\t\t\t\t\t\t");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("   Can't parse! ");
            
            Console.ResetColor();
           
        }

        public void PrintNoTests()
        {
            Console.Write("\t\t\t\t\t\t   ");
                        Console.BackgroundColor = ConsoleColor.DarkMagenta;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(" No tests! ");
                        
                        Console.ResetColor();
        }
    }
}