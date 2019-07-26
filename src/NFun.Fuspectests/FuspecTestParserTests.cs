using System.Collections.Generic;
using System.IO;
using FuspecTests;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.Model;
using NFun.Fuspec.TestHandler;
using NFun.Fuspec.TestHandler.Models;
using NUnit.Framework;

namespace NFun.Fuspectests
{
    public class FuspecTestParserTests
    {
        [Test]
        public void FuspecParser_ParseAndRunFuspec_ReturnParsedTests()
        {
            Assert.DoesNotThrow(() =>
            {
                //todo пример использования fuspecParser
                FuspecTestCase[] testCases = GenerateFuspecTestCases(FuspecTestCasesCollection.TestCollection);
                IEnumerable<BaseFuspecTestCaseResult> testsResult = FuspecTestBase.RunTests(testCases);
            });
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