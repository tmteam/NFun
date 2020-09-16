using System;
using System.Collections.Generic;
using System.Linq;

namespace Nfun.InfinityProfiling
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("NFUN A-Profiler ");
            while (RunOnOfModes())
            {
            }
        }

        private static bool RunOnOfModes()
        {
            var choice = EnterUserChoise();
            if (choice == ProfileMode.DoNotProfile)
                return false;

            Console.WriteLine("Profiling...");
            Console.WriteLine("Press [esc] to stop");

            switch (choice)
            {
                case ProfileMode.BuildAndCalcAll:
                    ProfileModes.RunAll(false);
                    break;
                case ProfileMode.ParseAll:
                    ProfileModes.RunParse(false);
                    break;
                case ProfileMode.BuildAll:
                    ProfileModes.RunBuild(false);
                    break;
                case ProfileMode.CalcAll:
                    ProfileModes.RunCalc(false);
                    break;
                case ProfileMode.BuildAndCalcBasics:
                    ProfileModes.RunAll(true);
                    break;
                case ProfileMode.CalcBasics:
                    ProfileModes.RunCalc(true);
                    break;
                case ProfileMode.ParseBasics:
                    ProfileModes.RunParse(true);
                    break;
                case ProfileMode.BuildBasics:
                    ProfileModes.RunBuild(true);
                    break;
                case ProfileMode.DoNotProfile:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        private enum ProfileMode
        {
            DoNotProfile,
            ParseAll,
            BuildAll,
            BuildAndCalcAll,
            CalcAll,
            BuildAndCalcBasics,
            CalcBasics,
            ParseBasics,
            BuildBasics
        }
        
        private static ProfileMode EnterUserChoise()
        {
            while (true)
            {
                Console.WriteLine("Choose profiling mode:");
                Console.WriteLine("[ENTER] All. Build+Calc");
                Console.WriteLine("[1] All. Build+Calc");

                Console.WriteLine("[2] All. Parse");
                Console.WriteLine("[3] All. Build");
                Console.WriteLine("[4] All. Calc");

                Console.WriteLine("[5] Basics. Build+Calc");

                Console.WriteLine("[6] Basics. Parse");
                Console.WriteLine("[7] Basics. Build");
                Console.WriteLine("[8] Basics. Calc");

                Console.WriteLine("[ESC] Exit");
                Console.Write("Enter your choice: ");
                var key = Console.ReadKey();
                Console.WriteLine();
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        return ProfileMode.BuildAndCalcAll;
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        return ProfileMode.ParseAll;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        return ProfileMode.BuildAll;
                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        return ProfileMode.CalcAll;
                    case ConsoleKey.D5:
                    case ConsoleKey.NumPad5:
                        return ProfileMode.BuildAndCalcBasics;
                    case ConsoleKey.D6:
                    case ConsoleKey.NumPad6:
                        return ProfileMode.ParseBasics;
                    case ConsoleKey.D7:
                    case ConsoleKey.NumPad7:
                        return ProfileMode.BuildBasics;
                    case ConsoleKey.D8:
                    case ConsoleKey.NumPad8:
                        return ProfileMode.CalcBasics;
                    case ConsoleKey.Escape:
                        return ProfileMode.DoNotProfile;
                }
            }
        }

        private static void PrintResults(string name, TimeSpan ratioTime,  LinkedList<double> history,
            int iterations)
        {
            var max = history.Max();
            var min = history.Min();
            var avg = history.Average();
            var rms = Math.Sqrt(history.Select(h => Math.Pow(avg - h, 2)).Sum());
            var current = history.Last.Value;
            
            Console.WriteLine($"{name} |  " +
                              $"{100*current/ratioTime.TotalMilliseconds:00.00}% | "+
                              $"{1000 * iterations / current:000000.0} |  " +
                              $"{1000 * iterations / avg:000000.0} |  " +
                              $"{1000 * iterations / max:000000.0} |  " +
                              $"{1000 * iterations / min:000000.0} |  " +
                              $"{rms * 1000 / iterations:0000}  |  ");
        }
    }
}