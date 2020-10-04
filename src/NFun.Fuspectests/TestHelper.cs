using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NFun.Fuspectests
{
    public static class TestHelper
    {

        internal static FuspecTestCases GenerateFuspecTestCases(string str)
        {
            List<string> listOfString = new List<string>();
            using (TextReader tr = new StringReader(str))
            {
                string line;
                while ((line = tr.ReadLine()) != null)
                {
                    listOfString.Add(line);
                }
            }
            var inputText = InputText.Read(new StreamReader(GenerateStreamFromString(str)));
            return new TestCasesReader().Read(inputText);
        }

        internal static void AssertForCorrectTestCase(FuspecTestCases fuspecTestCases)
        {
            Assert.IsNotNull(fuspecTestCases, "FuspecTestCases = null. It shouldn't be null!");
            Assert.IsNotNull(fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null. It shouldn't be null!");
            Assert.IsNotNull(fuspecTestCases.Errors, "FuspecTestCases.Errors = null. It shouldn't be null!");
            Assert.AreEqual(0, fuspecTestCases.Errors.Length, "Parser wrote nonexistent error. Expected 0 errors!");
            Assert.AreEqual(true, fuspecTestCases.TestCases.Any(), "Parser didn't write testcase. Expected some sucsessful tests!");
        }

        internal static void StandardAssertForNotCorrectTestCase(FuspecTestCases fuspecTestCases)
        {
            Assert.IsNotNull(fuspecTestCases, "FuspecTestCases = null. It shouldn't be null!");
            Assert.IsNotNull(fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null. It shouldn't be null!");
            Assert.IsNotNull(fuspecTestCases.Errors, "FuspecTestCases.Errors = null. It shouldn't be null!");
            Assert.IsTrue(fuspecTestCases.Errors.Any(), "Parser didn't write error. Expected some errors!");
            Assert.AreEqual(0, fuspecTestCases.TestCases.Length, "Wrong number of Tests.Expected 0 TestCase!");
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
    }
}
