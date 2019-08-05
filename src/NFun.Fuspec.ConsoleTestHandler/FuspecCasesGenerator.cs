using System.IO;
using System.Text;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.Model;

namespace NFun.Fuspec.ConsoleTestHandler
{
    public static class FuspecCasesGenerator
    {
        private static Stream GenerateStreamFromString(string inputString) 
            => new MemoryStream(Encoding.UTF8.GetBytes(inputString));

        public static FuspecTestCase[] Generate(string inputString) 
            => new TestCasesReader(new StreamReader(GenerateStreamFromString(inputString))).Read().TestCases;
    }
}