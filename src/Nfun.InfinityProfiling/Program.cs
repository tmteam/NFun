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

        public static bool RunOnOfModes()
        {
            var choice = EnterUserChoise();
            if (choice == ProfileMode.DoNotProfile)
                return false;

            Console.WriteLine("Profiling...");
            Console.WriteLine("Press [esc] to stop");

            switch (choice)
            {
                case ProfileMode.BuildAndCalcAll:
                    ProfileModes.RunAll(ProfileSet.All);
                    break;
                case ProfileMode.BuildAll:
                    ProfileModes.RunBuild(ProfileSet.All);
                    break;
                case ProfileMode.CalcAll:
                    ProfileModes.RunCalc(ProfileSet.All);
                    break;
                case ProfileMode.BuildAndCalcMiddle:
                    ProfileModes.RunAll(ProfileSet.Middle);
                    break;
                case ProfileMode.CalcMiddle:
                    ProfileModes.RunCalc(ProfileSet.Middle);
                    break;
                case ProfileMode.BuildMidle:
                    ProfileModes.RunBuild(ProfileSet.Middle);
                    break;
                case ProfileMode.BuildAndCalcPrimitive:
                    ProfileModes.RunAll(ProfileSet.Primitives);
                    break;
                case ProfileMode.CalcPrimitive:
                    ProfileModes.RunCalc(ProfileSet.Primitives);
                    break;
                case ProfileMode.BuildPrimitive:
                    ProfileModes.RunBuild(ProfileSet.Primitives);
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

            BuildAll,
            BuildAndCalcAll,
            CalcAll,

            BuildAndCalcMiddle,
            CalcMiddle,
            BuildMidle,
            
            BuildAndCalcPrimitive,
            CalcPrimitive,
            BuildPrimitive
        }
        
        private static ProfileMode EnterUserChoise()
        {
            while (true)
            {
                Console.WriteLine("Choose profiling mode:");
                Console.WriteLine("[ENTER] All. Build+Calc");
                Console.WriteLine("[1] All. Build+Calc");

                Console.WriteLine("[2] All. Build");
                Console.WriteLine("[3] All. Calc");

                Console.WriteLine("[4] Basics. Build+Calc");
                Console.WriteLine("[5] Basics. Build");
                Console.WriteLine("[6] Basics. Calc");

                Console.WriteLine("[7] Primitives. Build+Calc");
                Console.WriteLine("[8] Primitives. Build");
                Console.WriteLine("[9] Primitives. Calc");

                
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
                        return ProfileMode.BuildAll;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        return ProfileMode.CalcAll;
                    
                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        return ProfileMode.BuildAndCalcMiddle;
                    case ConsoleKey.D5:
                    case ConsoleKey.NumPad5: 
                        return ProfileMode.BuildMidle;
                    case ConsoleKey.D6:
                    case ConsoleKey.NumPad6:
                        return ProfileMode.CalcMiddle;
                    
                    
                    case ConsoleKey.D7:
                    case ConsoleKey.NumPad7:
                        return ProfileMode.BuildAndCalcPrimitive;
                    case ConsoleKey.D8:
                    case ConsoleKey.NumPad8: 
                        return ProfileMode.BuildPrimitive;
                    case ConsoleKey.D9:
                    case ConsoleKey.NumPad9:
                        return ProfileMode.CalcPrimitive;

                    
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