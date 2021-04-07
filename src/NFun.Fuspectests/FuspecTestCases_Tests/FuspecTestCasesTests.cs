using System.Collections.Generic;
using System.IO;
using System.Linq;
using NFun.Fuspec.Parser;
using NFun.Fuspec.Parser.Model;
using NUnit.Framework;
using ParcerV1;

namespace NFun.Fuspectests.FuspecTestCasesTests
{
    public class FuspecTestCasesTests
    {
        private FuspecTestCases _fuspecTestCases;

        [SetUp]
        public void Setup()
        {
        }

      
        [Test]
        public void TestSuccessfulCase_CorrectTestCaseWithoutTags_NotReturnNull_NoErrors()
        {
            GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS 
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(_fuspecTestCases, "FuspecTestCases = null");
                Assert.IsNotNull(_fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null");
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Parser wrote wrong number of test. Excpected 1 TestCase");
                Assert.AreEqual(0, _fuspecTestCases.Errors.Length, "Parser wrote nonexistent error");
                Assert.IsNotNull(_fuspecTestCases.Errors, "FuspecTestCases.Errors = null");
            });
        }

        [Test]
        public void TestSuccessfulCase_CorrectTestCaseWithTags_NotReturnNull_NoErrors()
        {
            GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS tag1
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(_fuspecTestCases, "FuspecTestCases = null");
                Assert.IsNotNull(_fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null");
                Assert.AreEqual(0, _fuspecTestCases.Errors.Length, "Parser wrote nonexistent error");
                Assert.IsNotNull(_fuspecTestCases.Errors, "FuspecTestCases.Errors = null");
            });
        }

        [Test]
        public void NameReading_CorrectTestCaseWithName_ReturnName()
        {
            {
                GenerateFuspecTestCases(
                    @"|************************
| TEST Complex example 
| TAGS tag1
|************************
  x = round(a + b + c)");

                Assert.Multiple(() =>
                {
                    StandardAssertForCorrectTestCase();
                    Assert.AreEqual("Complex example", _fuspecTestCases.TestCases.FirstOrDefault().Name);
                });
            }

        }

        [Test]
        public void NameReading_CorrectTestCaseWithNameWithSpaces_ReturnNameWithoutSpaces()
        {
            {
                GenerateFuspecTestCases(
                    @"|************************
| TEST   Complex example   
| TAGS tag1
|************************
  x = round(a + b + c)");

                Assert.Multiple(() =>
                {
                    StandardAssertForCorrectTestCase();
                    Assert.AreEqual("Complex example", _fuspecTestCases.TestCases.FirstOrDefault().Name);
                });
            }
        }

        [Test]
        public void NameReading_CorrectTestCaseWithoutName_ReturnError_NamedMissed()
        {
            {
                GenerateFuspecTestCases(
                    @"|************************
| TEST
| TAGS tag1
|************************
  x = round(a + b + c)");

                Assert.Multiple(() =>
                {
                    StandardAssertForNotCorrectTestCase();
                    Assert.AreEqual(FuspecErrorType.NamedMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
                });
            }
        }

        [Test]
        public void NameReading_CorrectTestCaseWithSpaceInsteadName_ReturnError_NameMissed()
        {
            {
                GenerateFuspecTestCases(
                    @"|************************
| TEST    
| TAGS tag1
|************************
  x = round(a + b + c)");

                Assert.Multiple(() =>
                {
                    StandardAssertForNotCorrectTestCase();
                    Assert.AreEqual(FuspecErrorType.NamedMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
                });
            }
        }

        [Test]
        public void OpeningStringReading_TestCaseWithoutOpening_returnErrorMissedOpeningString()
        {
            {
                GenerateFuspecTestCases(
                    @"
| TEST
| TAGS tag1
|************************
  x = round(a + b + c)");

                Assert.Multiple(() =>
                {
                    StandardAssertForNotCorrectTestCase();
                    Assert.AreEqual(FuspecErrorType.OpeningStringMissed,
                        _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
                });
            }
        }

        [Test]
        public void OpeningStringReading_TestCaseWithPsevdoOpening_returnErrorMissedOpeningString()
        {
            {
                GenerateFuspecTestCases(
                    @"|*
| TEST
| TAGS tag1
|************************
  x = round(a + b + c)");

                Assert.Multiple(() =>
                {
                    StandardAssertForNotCorrectTestCase();
                    Assert.AreEqual(FuspecErrorType.OpeningStringMissed,
                        _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
                });
            }
        }

        [Test]
        public void OpeningStringReading_TestCaseWithErrorInOpening_returnErrorMissedOpeningString()
        {
            {
                GenerateFuspecTestCases(
                    @"|****************tssg****
| TEST
| TAGS tag1
|************************
  x = round(a + b + c)");

                Assert.Multiple(() =>
                {
                    StandardAssertForNotCorrectTestCase();
                    Assert.AreEqual(FuspecErrorType.OpeningStringMissed,
                        _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
                });
            }
        }
        
        [Test]
        public void OpeningStringReading_TestCaseWithErrorInOpeningInSecondCase_returnErrorMissedOpeningString()
        {
            {
                GenerateFuspecTestCases(
                    @"|*******************
| TEST test1
| TAGS tag1
|************************
  x = round(a + b + c)

|***************9**9");

                Assert.Multiple(() =>
                {
                    StandardAssertForNotCorrectTestCase();
                    Assert.AreEqual(FuspecErrorType.OpeningStringMissed,
                        _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
                });
            }

        }
        [Test]
        public void HeadEndReading_TestCaseWithoutHeadEnd_returnErrorEndingHeadMissed()
        {
            GenerateFuspecTestCases(
                @"|*********
| TEST Name
| TAGS tag1
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.EndingHeadMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void HeadEndReading_TestCaseWithErrorInHeadEnd_returnErrorEndingHeadMissed()
        {
            GenerateFuspecTestCases(
                @"|*********
| TEST Name
| TAGS tag1
hjfghfghs
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.EndingHeadMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]
        public void HeadEndReading_TestCaseWithPsevdoHeadEnd_returnError_EndingHeadMissed()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|*******
  y=y+i
|************");
            
            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.EndingHeadMissed,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
       
        
        [Test]
        public void NoEndingOfCase_TestCaseWithoutEndingHead_returnError_NoEndingTestCase()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1


");
            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.NoEndingTestCase,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]
        public void NoEndingOfCase_TestCaseWithoutScript_returnError_NoEndingTestCase()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|********************
");
            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.NoEndingTestCase,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
       
        [Test]
        public void BodyReading_TestCaseWithoutBody_returnError_NoEndingTestCase()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.NoEndingTestCase,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
    
        
        [Test]
        public void BodyReading_TestCaseWhithOneExpression_returnScript()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("  x = round(a + b + c)", _fuspecTestCases.TestCases.FirstOrDefault().Script);
            });
        }

        [Test]
        public void BodyReading_ManyExpressionsInBody_returnScript()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
  x = round(a + b + c)
  x = round(a - b - c)");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("  x = round(a + b + c)\r\n  x = round(a - b - c)", _fuspecTestCases.TestCases[0].Script);
            });
        }
        
        [Test]
        public void BodyReading_PsevdoSeparatingStringAfterBody_returnScript()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
  x = round(a + b + c)
x = round(a - b - c)
|**");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("  x = round(a + b + c)\r\nx = round(a - b - c)\r\n|**", _fuspecTestCases.TestCases[0].Script);
            });
        }
      
        [Test]
        public void BodyReading_SpaceBarInsteadOneOfTwoExpression_returnOneExpression_()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
      
  x = round(a - b - c)");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("      \r\n  x = round(a - b - c)", _fuspecTestCases.TestCases.FirstOrDefault().Script);
            });
        }

        

        [Test]
        public void BodyReading_SpaceBarIntoOneExpression_returnOneExpression_()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
     x = round(a - b - c)  ");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("     x = round(a - b - c)  ", _fuspecTestCases.TestCases.FirstOrDefault().Script);
            });
        }

        [Test]
        public void BodyReading_UnknownStringInBody_returnBody()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
     x = round(a - b - c)  
пвпавпвапва");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("     x = round(a - b - c)  \r\nпвпавпвапва", _fuspecTestCases.TestCases.FirstOrDefault().Script);

            });
        }

        [Test]
        public void BodyReading_AfterBodyIsNewCase_returnTwoCasesWithScript()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
  x = round(a - b - c)
|*********************
| TEST Name
| TAGS tag1
|************************
     x = round(a - b - c)");

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(_fuspecTestCases, "FuspecTestCases = null");
                Assert.IsNotNull(_fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null");
                Assert.IsNotNull(_fuspecTestCases.Errors, "FuspecTestCases.Errors = null");
                Assert.AreEqual(0, _fuspecTestCases.Errors.Length, "Parser wrote nonexistent error ");
                Assert.AreEqual(2, _fuspecTestCases.TestCases.Length, "Parser didn't write testcase");
                Assert.AreEqual("  x = round(a - b - c)", _fuspecTestCases.TestCases.FirstOrDefault().Script);
                Assert.AreEqual("     x = round(a - b - c)",_fuspecTestCases.TestCases[1].Script);
            });
        }
        [Test]
        public void BodyReading_AfterBodyIsNewCase_CasesIsSeparateWhitEnter_returnTwoCasesWithScript()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
  x = round(a - b - c)

|*****************
| TEST Name
| TAGS tag1
|************************
     x = round(a - b - c)");

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(_fuspecTestCases, "FuspecTestCases = null");
                Assert.IsNotNull(_fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null");
                Assert.IsNotNull(_fuspecTestCases.Errors, "FuspecTestCases.Errors = null");
                Assert.AreEqual(0, _fuspecTestCases.Errors.Length, "Parser wrote nonexistent error ");
                Assert.AreEqual(2, _fuspecTestCases.TestCases.Length, "Parser didn't write testcase");
                Assert.AreEqual("  x = round(a - b - c)\r\n", _fuspecTestCases.TestCases.FirstOrDefault().Script);
                Assert.AreEqual("     x = round(a - b - c)",_fuspecTestCases.TestCases[1].Script);

            });
        }
        [Test]
        public void BodyReading_AfterBodyIsNewCase_CasesIsSeparatedWithEnterWithSpaces_returnTwoCasesWithScript()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
  x = round(a - b - c)
    
|*****************
| TEST Name
| TAGS tag1
|************************
     x = round(a - b + c)");

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(_fuspecTestCases, "FuspecTestCases = null");
                Assert.IsNotNull(_fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null");
                Assert.IsNotNull(_fuspecTestCases.Errors, "FuspecTestCases.Errors = null");
                Assert.AreEqual(0, _fuspecTestCases.Errors.Length, "Parser wrote nonexistent error ");
                Assert.AreEqual(2, _fuspecTestCases.TestCases.Length, "Parser didn't write testcase");
                Assert.AreEqual("  x = round(a - b - c)\r\n    ", _fuspecTestCases.TestCases.FirstOrDefault().Script);
                Assert.AreEqual("     x = round(a - b + c)",_fuspecTestCases.TestCases[1].Script);

            });
        }
        
        
        
        [Test]
        public void EmptyFile_returnEmptyTestcase()
        {
            GenerateFuspecTestCases(
                @"");

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(_fuspecTestCases, "FuspecTestCases = null");
                Assert.IsNotNull(_fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null");
                Assert.IsNotNull(_fuspecTestCases.Errors, "FuspecTestCases.Errors = null");
                Assert.AreEqual(0, _fuspecTestCases.Errors.Length, "Parser wrote nonexistent error ");
                Assert.AreEqual(0, _fuspecTestCases.TestCases.Length, "Parser didn't write testcase");
            });
        }

        [Test]
        public void EmptyFileWhithSpaces_returnEmptyTestcases()
        {
            GenerateFuspecTestCases(
                @"         

    
");

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(_fuspecTestCases, "FuspecTestCases = null");
                Assert.IsNotNull(_fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null");
                Assert.IsNotNull(_fuspecTestCases.Errors, "FuspecTestCases.Errors = null");
                Assert.AreEqual(0, _fuspecTestCases.Errors.Length, "Parser wrote nonexistent error ");
                Assert.AreEqual(0, _fuspecTestCases.TestCases.Length, "Parser wrote nonexistent testcase");
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
            Assert.AreEqual(0, _fuspecTestCases.TestCases.Length, "Parser wrote not correct testcase");
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
            using (TextReader tr = new StringReader(str))
            {
                string line;
                while ((line = tr.ReadLine()) != null)
                {
                    listOfString.Add(line);
                }
            }
            var inputText = InputText.Read(new StreamReader(GenerateStreamFromString(str)));
            _fuspecTestCases = new TestCasesReader().Read(inputText);
        }
    }
}