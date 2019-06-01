using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;
using NUnit.Framework;
using ParcerV1;

//todo cr namespace
namespace FuspecTests
{
    //todo cr naming
    public class FuspecTestCasesTests
    {
        private FuspecTestCases _fuspecTestCases;

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TagsReading_CorrectTestCaseWithEmptyTag_ReturnNoTags()
        {
            GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {

                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Test case is missed after the parsing");
                Assert.AreEqual("Complex example", _fuspecTestCases.TestCases.FirstOrDefault().Name, "Set wrong Name");
                Assert.AreEqual(0, _fuspecTestCases.TestCases.FirstOrDefault().Tags.Length,
                    "Set wrong Tags. Nonexistent tag was added");
            });
        }

        [Test]
        public void TagsReading_CorrectTestCaseWithOneTag_ReturnOneTag()
        {
            GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS Tag1
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Test case is missed after the parsing");
                Assert.AreEqual(1, _fuspecTestCases.TestCases.FirstOrDefault().Tags.Length,
                    "Wrong number of tags. Expected 1");
                Assert.AreEqual("Tag1", _fuspecTestCases.TestCases.FirstOrDefault().Tags[0],
                    "Set wrong Tag. Expected Tag1");
            });
        }

        [Test]
        public void TagsReading_CorrectTestCaseWithManyTags_ReturnListOfTags()
        {
            GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS Tag1, Tag2
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Test case is missed after the parsing");
                Assert.AreEqual(2, _fuspecTestCases.TestCases.FirstOrDefault().Tags.Length,
                    "Wrong number of tags. Expected 2");
                Assert.AreEqual("Tag1", _fuspecTestCases.TestCases[0].Tags[0], "Set wrong Tag. Expected Tag1");
                Assert.AreEqual("Tag2", _fuspecTestCases.TestCases[0].Tags[1], "Set wrong Tag. Expected Tag2");
            });

        }

        [Test]
        public void TagsReading_CorrectTestCaseWithSpaceBarTag_ReturnListOfTags()
        {
            GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS   Tag1  
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Test case is missed after the parsing");
                Assert.AreEqual(1, _fuspecTestCases.TestCases.FirstOrDefault().Tags.Length,
                    "Wrong number of tags. Expected 1");
                Assert.AreEqual("Tag1", _fuspecTestCases.TestCases.FirstOrDefault().Tags[0],
                    "Set wrong Tag. Expected Tag1");
            });
        }

        [Test]
        public void TagsReading_CorrectTestCaseWithComma_ReturnNoTags()
        {
            GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS ,  ,  
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Test case is missed after the parsing");
                Assert.AreEqual(0, _fuspecTestCases.TestCases.FirstOrDefault().Tags.Length,
                    "Wrong number of tags. Expected 0");
            });
        }

        [Test]
        public void TagsReading_WrongTestCaseWithoutKeWordTAGS_ReturnError()
        {
            GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
|  Tag1, Tag2
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, _fuspecTestCases.TestCases.Length, "Wrong test case is added");
                Assert.AreEqual(FuspecErrorType.EndingHeadMissed,
                    _fuspecTestCases.Errors[0].ErrorType, "Wrong Errors!");
            });

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
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Parser wrote wrong number of test");
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
        public void NameReading_CorrectTestCaseWithNameWithSpaces_ReturnError()
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
        public void NameReading_CorrectTestCaseWithoutName_ReturnError()
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
        public void NameReading_CorrectTestCaseWithSpaceInsteadName_ReturnError()
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
        public void BodyReading_TestCaseWithoutBody_returnErrorScriptMissed()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
|*********");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.ScriptMissed,
                    _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void BodyReading_OneExpression_returnScript()
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
                Assert.AreEqual("x = round(a + b + c)", _fuspecTestCases.TestCases.FirstOrDefault().Script);
            });
        }

        [Test]
        public void BodyReading_ManyExpressions_returnScript()
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
                Assert.AreEqual("x = round(a + b + c)\rx = round(a - b - c)", _fuspecTestCases.TestCases[0].Script);
            });
        }

        [Test]
        public void BodyReading_NoKeyWordAtTheFirstExpression_returnError()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
sss x = round(a + b + c)
  x = round(a - b - c)");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.ScriptMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void BodyReading_NoKeyWordAtTheSecondExpression_returnError()
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
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.OpeningStringMissed,
                    _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void BodyReading_SpaseBarInsteadOneOfTwoExpression_returnOneExpression_()
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
                Assert.AreEqual("x = round(a - b - c)", _fuspecTestCases.TestCases.FirstOrDefault().Script);
            });
        }

        [Test]
        public void BodyReading_SpaseBarInsteadOneExpression_returnError_()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
      
|************8
       ");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.ScriptMissed,
                    _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void BodyReading_SpaseBarIntoOneExpression_returnOneExpression_()
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
                Assert.AreEqual("   x = round(a - b - c)  ", _fuspecTestCases.TestCases.FirstOrDefault().Script);
            });
        }

        [Test]
        public void BodyReading_AfterBodyIsUnknownString_returnError_OpeningStringMissed()
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
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.OpeningStringMissed,
                    _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void BodyReading_AfterBodyIsNewCase_returnScript()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
  x = round(a - b - c)

|*******
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
                Assert.AreEqual("x = round(a - b - c)", _fuspecTestCases.TestCases.FirstOrDefault().Script);
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
        public void EmptyFile_returnEmptyTestcaseWhithSpaces()
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
                Assert.AreEqual(0, _fuspecTestCases.TestCases.Length, "Parser didn't write testcase");
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