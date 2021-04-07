using System.Collections.Generic;
using System.IO;
using System.Linq;
using NFun.Fuspec.Parser;
using NFun.Fuspec.Parser.Model;
using NUnit.Framework;
using ParcerV1;

namespace NFun.Fuspectests
{
    public class ErrorTests
    {
        
        private FuspecTestCases _fuspecTestCases;
        
        [Test]
        public void OpeningLineMissed()
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
        
        [Test]
        public void NamedMissedJustSpaces()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST 
| TAGS
|************************
  x = round(a + b + c)");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("NamedMissed",error.ErrorType.ToString());
                Assert.AreEqual(1,error.LineNumber);
            });
        }
        [Test]
        public void NamedMissedNoSymbols()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST
| TAGS
|************************
  x = round(a + b + c)");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("NamedMissed",error.ErrorType.ToString());
                Assert.AreEqual(1,error.LineNumber);
            });
        }
        
        [Test]
        public void EndingHeadMissed()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
  x = round(a + b + c)");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("EndingHeadMissed",error.ErrorType.ToString());
                Assert.AreEqual(3,error.LineNumber);
            });
        }
        
        [Test]
        public void  SeparatedStringMissedAfterInOutPart()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************
| in a:int

  x = round(a + b + c)
");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("SeparatedStringMissed",error.ErrorType.ToString());
                Assert.AreEqual(6,error.LineNumber);
            });
        }
       
       
        [Test]
        public void  SeparatedStringMissedAfterScript()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************
| in a:int
|---------------------------
  x = round(a + b + c)
| set a:5
");
// Восприниммает как скрипт! нет понимания, что должны быть разделительная строка!!!
            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("SeparatedStringMissed",error.ErrorType.ToString());
                Assert.AreEqual(7,error.LineNumber);
            });
        }

        [Test]
        public void  ScriptMissedBetweenSeparatedLines()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************
| in a:int
|---------------------------
|--------------------------
| set a:5
");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("ScriptMissed",error.ErrorType.ToString());
                Assert.AreEqual(6,error.LineNumber);
            });
        }
        
    
        
        [Test]
        public void   ScriptMissedWithOneSeparatedLinesAndSetLine ()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************
| in a:int
|-----------------
| set a:5
");
// воспринимант как скрипт!!!! | set a:5
            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("ScriptMissed",error.ErrorType.ToString());
                Assert.AreEqual(6,error.LineNumber);
            });
        }
        
        [Test]
        public void  NoEndingTestCaseInScriptPart()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************
| in a:int
|-----------------

");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("NoEndingTestCase",error.ErrorType.ToString());
                Assert.AreEqual(7,error.LineNumber);
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

        [Test]
        public void  ScriptMissed()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************


|****************
| TEST NAME
| TAGS
|*******************
 a=a+1



");

            var error = _fuspecTestCases.Errors.FirstOrDefault();
            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("ScriptMissed",error.ErrorType.ToString());
                Assert.AreEqual(6,error.LineNumber);
            });
        }        
        
        [Test]
        public void  NoEndingTestCaseAfterEndingHead()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************
 a=a+1

|****************
| TEST NAME
| TAGS
|*******************


");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("NoEndingTestCase",error.ErrorType.ToString());
                Assert.AreEqual(12,error.LineNumber);
            });
        }        
        
        [Test]
        public void  NoEndingTestCaseInHeadPart()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("NoEndingTestCase",error.ErrorType.ToString());
                Assert.AreEqual(2,error.LineNumber);
            });
        }
        
        [Test]
        public void  ParamInMissed()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************
| in
|---------

");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("ParamInMissed",error.ErrorType.ToString());
                Assert.AreEqual(4,error.LineNumber);
            });
        }        

        [Test]
        public void  ParamInMissed_SpaceInsteadParam()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************
| in 
|---------

");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("ParamInMissed",error.ErrorType.ToString());
                Assert.AreEqual(4,error.LineNumber);
            });
        }     
        
        [Test]
        public void  ParamOutMissed()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************
| out
|---------

");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("ParamOutMissed",error.ErrorType.ToString());
                Assert.AreEqual(4,error.LineNumber);
            });
        }     
        
        public void  ParamOutMissedSpaceInsteadOut()
        {
            GenerateFuspecTestCases(
                @"|****************
| TEST NAME
| TAGS
|*******************
| out  
|---------

");

            var error = _fuspecTestCases.Errors.FirstOrDefault();

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.IsTrue(_fuspecTestCases.HasErrors);
                Assert.AreEqual("ParamOutMissed",error.ErrorType.ToString());
                Assert.AreEqual(4,error.LineNumber);
            });
        }    
        [Test]
        public void SetKitMissed()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in a:real b:int
| out x:real
|------------------------
  x = a-b  
|---------------------
| set
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.SetOrCheckKitMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]
        public void CheckKitMissed()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in a:real b:int
| out x:real
|------------------------
  x = a-b  
|---------------------
| check
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.SetOrCheckKitMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]
        public void WrongSetCheckKit()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in a:real b:int
| out x:real
|------------------------
  x = a-b  
|---------------------
| setsdf a:5,b:4  
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.WrongSetCheckKit, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]
        public void ExpectedOpeningLine()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in a:real b:int
| out x:real
|------------------------
  x = a-b  
|---------------------
| set a:5, b:4
| check a:5

sdfsdfdf
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.ExpectedOpeningLine, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
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
            List<string> listOfString = new List<string>();
            using (TextReader tr = new StringReader(str)){
                string line;
                while ((line= tr.ReadLine()) != null)
                {
                    listOfString.Add(line);
                }
            }
            
            
            var inputText = InputText.Read(new StreamReader(GenerateStreamFromString(str)));
            _fuspecTestCases = new TestCasesReader().Read(inputText);
        }
    }
}