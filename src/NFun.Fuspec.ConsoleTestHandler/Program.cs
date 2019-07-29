using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.Model;
using NFun.Fuspec.TestHandler;
using NFun.Fuspec.TestHandler.Models;

namespace NFun.Fuspec.ConsoleTestHandler
{
    class Program
    {
        static void Main(string[] args)
        {
            var ff = new TryFindFuspecTests();
            ff.FuspecTests();
            Console.ReadLine();
            Console.WriteLine("Hello World!");
        }
    }

    public class TryFindFuspecTests
    {
        private const string FuspecTestExtension = "*.fuspec";

        public void FuspecTests()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var fuspec = Directory.EnumerateFiles(currentDirectory, FuspecTestExtension, SearchOption.AllDirectories);

            var fuspecFromFiles = fuspec.Select(File.ReadAllText).ToList();
            var str = string.Join(String.Empty, fuspecFromFiles);
            FuspecTestCase[] testCases = GenerateFuspecTestCases(str);
            IEnumerable<BaseFuspecTestCaseResult> testsResult = FuspecTestBase.RunTests(testCases);



        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private FuspecTestCase[] GenerateFuspecTestCases(string str)
        {
            GenerateStreamFromString(str);
            //var specs = new TestCasesReader(new StreamReader(GenerateStreamFromString(str)));
            return new TestCasesReader(new StreamReader(GenerateStreamFromString(str))).Read().TestCases;
        }

    }
}
