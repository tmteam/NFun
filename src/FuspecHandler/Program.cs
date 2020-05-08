using System;
using System.IO;

namespace FuspecHandler
{
    class Program
    {
        static string GetFuspecRootOrNull()
        {
            var currentDir = Directory.GetCurrentDirectory();
            while (true)
            {
                var combined = Path.Combine(currentDir, "fuspecs");
                if (Directory.Exists(combined))
                    return combined;
                var info = Directory.GetParent(currentDir);
                if (info.Root.FullName == info.FullName)
                    return null;
                currentDir = info.FullName;
            }
        }

        static void Main(string[] args)
        {
            var path = GetFuspecRootOrNull(); //  Path.Combine(Directory.GetCurrentDirectory(), "fuspecs");
            if (path == null)
            {
                Console.WriteLine("Fuspecs are not found");
                return;
            }

            Console.WriteLine($"Executing tests from: path");

         //   while (true)
            {

                Console.WriteLine($"Fuspec runner. Path: {path}");
                var testHandler = new TestHandler();
                var stats = testHandler.RunTests(path);

                stats.PrintStatistic();

                Console.WriteLine();
                Console.WriteLine("######################################");
                Console.Write("[D] - detail error. [E] - exit. [R] - repeat?   ");
                Console.WriteLine();
                var answer = Console.ReadLine();
                if (answer.ToLower() == "d")
                    stats.PrintErrorDetails();
                if (answer.ToLower() == "e")
                    return;
            }
        }
    }
}