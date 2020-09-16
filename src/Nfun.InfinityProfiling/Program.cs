using System;

namespace Nfun.InfinityProfiling
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Build in loop");
            Console.WriteLine("Press esc to exit");
            int choose = 0;

            var choise = EnterUserChoise();
            if(choise== ProfileMode.DoNotProfile)
                return;
            Console.WriteLine("Profiling...");
            switch (choise)
            {
                case ProfileMode.BuildAndCalcAll:
                    ProfileModes.RunAll(false); break;
                case ProfileMode.ParseAll:
                    ProfileModes.RunParse(false);  break;
                case ProfileMode.BuildAll:
                    ProfileModes.RunBuild(false);  break;
                case ProfileMode.CalcAll:
                    ProfileModes.RunCalc(false); break;
                case ProfileMode.BuildAndCalcBasics:
                    ProfileModes.RunAll(true); break;
                case ProfileMode.CalcBasicis:
                    ProfileModes.RunCalc(false); break;
                case ProfileMode.ParseBasicis:
                    ProfileModes.RunParse(true);  break;
                case ProfileMode.BuildBasicis:
                    ProfileModes.RunBuild(true); break;
                case ProfileMode.DoNotProfile:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.WriteLine("Build stopped. Bye bye");
        }
        private enum ProfileMode
        {
            DoNotProfile,

            ParseAll,
            BuildAll,
            BuildAndCalcAll,
            CalcAll,

            BuildAndCalcBasics,
            CalcBasicis,
            ParseBasicis,
            BuildBasicis
        }
        
        private static ProfileMode EnterUserChoise()
        {
            while (true)
            {
                Console.WriteLine("Choose profiling mode:");
                Console.WriteLine("[ENTER] All. Parse+Build+Calculation");
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
                        return ProfileMode.ParseBasicis;
                    case ConsoleKey.D7:
                    case ConsoleKey.NumPad7:
                        return ProfileMode.BuildBasicis;
                    case ConsoleKey.D8:
                    case ConsoleKey.NumPad8:
                        return ProfileMode.CalcBasicis;
                    case ConsoleKey.Escape:
                        return ProfileMode.DoNotProfile;
                }
            }
        }
    }
}