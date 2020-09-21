using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using NFun.Tic.SolvingStates;

namespace NFun.TypeInferenceCalculator
{
    public static class TraceLog
    {
        public static bool IsEnabled { get; set; } = false;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(string message)
        {
#if DEBUG
            if(IsEnabled)
                Console.Write(message);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLine(string message)
        {
#if DEBUG
            if (IsEnabled)
                Console.WriteLine(message);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(string locator, ConsoleColor green)
        {
#if DEBUG
            if (IsEnabled)
            {
                Console.ForegroundColor = green;
                Console.Write(locator);
                Console.ResetColor();
            }
#endif
        }
    }
}
