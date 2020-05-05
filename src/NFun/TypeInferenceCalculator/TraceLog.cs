using System;
using System.Collections.Generic;
using System.Text;
using NFun.Tic.SolvingStates;

namespace NFun.TypeInferenceCalculator
{
    public static class TraceLog
    {
        public static bool IsEnabled { get; set; }

        public static void Write(Func<string> locator)
        {
            if(IsEnabled)
                Console.Write(locator());
        }
        
        public static void WriteLine(Func<string> locator)
        {
            if (IsEnabled)
                Console.WriteLine(locator());
        }
        public static void WriteLine()
        {
            if (IsEnabled)
                Console.WriteLine();
        }

        public static void WriteLine(string val, ConsoleColor color)
        {
            if (IsEnabled)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(val);
                Console.ResetColor();
            }
        }
        public static void WriteLine(Func<string> val, ConsoleColor color)
        {
            if (IsEnabled)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(val());
                Console.ResetColor();
            }
        }

        public static void Write(string message)
        {
            if(IsEnabled)
                Console.Write(message);
        }
        public static void WriteLine(string message)
        {
            if (IsEnabled)
                Console.WriteLine(message);
        }

        public static void Write(string locator, ConsoleColor green)
        {
            if (IsEnabled)
            {
                Console.ForegroundColor = green;
                Console.WriteLine(locator);
                Console.ResetColor();
            }
        }
    }
}
