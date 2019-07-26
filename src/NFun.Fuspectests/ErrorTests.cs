using System.IO;
using System.Linq;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.Model;
using NFun.Types;
using NUnit.Framework;

namespace FuspecTests
{
    public class ErrorTests
    {
        
        private FuspecTestCases _fuspecTestCases;
        
        [Test]
        public void OpeningLineIsMissed()
        {
            GenerateFuspecTestCases(
                @"| TEST Complex example 
| TAGS
|************************
  x = round(a + b + c)");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("OpeningStringMissed",error.ErrorType.ToString());
                Assert.AreEqual(0,error.LineNumber);
            });
        }
        
        private void StandardAssertForCorrectTestCase()
        {
            Assert.IsNotNull(_fuspecTestCases, "FuspecTestCases = null");
            Assert.IsNotNull(_fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null");
            Assert.IsNotNull(_fuspecTestCases.Errors, "FuspecTestCases.Errors = null");
            Assert.AreEqual(0, _fuspecTestCases.Errors.Length, "Parser wrote nonexistent error ");
            Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Parser didn't write testcase");
        }

        private void StandardAssertForNotCorrectTestCase()
        {
            Assert.IsNotNull(_fuspecTestCases, "FuspecTestCases = null");
            Assert.IsNotNull(_fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null");
            Assert.IsNotNull(_fuspecTestCases.Errors, "FuspecTestCases.Errors = null");
            Assert.IsTrue(_fuspecTestCases.Errors.Any(), "Parser didn't write error");
            Assert.AreEqual(0, _fuspecTestCases.TestCases.Length, "Parser wrote notcorrect testcase");
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

        private void GenerateFuspecTestCases(string str)
        {
            GenerateStreamFromString(str);
            var specs = new TestCasesReader(new StreamReader(GenerateStreamFromString(str)));
            _fuspecTestCases = new TestCasesReader(new StreamReader(GenerateStreamFromString(str))).Read();
        }
    }
}