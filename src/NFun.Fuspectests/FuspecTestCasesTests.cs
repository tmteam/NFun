using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;
using NUnit.Framework;
using ParcerV1;

namespace FuspecTests
{
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
        public void TagsReading_CorrectTestCaseWithSpaceBarTag_ReturnTagWithoutSpaces()
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
        public void TagsReading_WrongTestCaseWithoutKeWordTAGS_ReturnError_EndingHeadMissed()
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
        public void NoEndingOfCase_TestCaseWithEndingOnName_returnError_NoEndingTestCase()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************

|****************
| TEST test1
");
            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.NoEndingTestCase,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);
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
        public void BodyReading_TestCaseWithSpaseInsteadBody_returnBody()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
  
");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("  ",_fuspecTestCases.TestCases.FirstOrDefault().Script,"Wrote not right script");
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
                Assert.AreEqual("  x = round(a + b + c)\r  x = round(a - b - c)", _fuspecTestCases.TestCases[0].Script);
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
                Assert.AreEqual("  x = round(a + b + c)\rx = round(a - b - c)\r|**", _fuspecTestCases.TestCases[0].Script);
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
                Assert.AreEqual("      \r  x = round(a - b - c)", _fuspecTestCases.TestCases.FirstOrDefault().Script);
            });
        }

        [Test]
        public void BodyReading_SpaceBarInsteadOneExpression_returnScript_()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
  
");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("  ",_fuspecTestCases.TestCases.FirstOrDefault().Script,"Didn't write script");
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
                Assert.AreEqual("     x = round(a - b - c)  \rпвпавпвапва", _fuspecTestCases.TestCases.FirstOrDefault().Script);

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
                Assert.AreEqual("  x = round(a - b - c)\r", _fuspecTestCases.TestCases.FirstOrDefault().Script);
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
     x = round(a - b - c)");

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(_fuspecTestCases, "FuspecTestCases = null");
                Assert.IsNotNull(_fuspecTestCases.TestCases, "FuspecTestCases.TestCases = null");
                Assert.IsNotNull(_fuspecTestCases.Errors, "FuspecTestCases.Errors = null");
                Assert.AreEqual(0, _fuspecTestCases.Errors.Length, "Parser wrote nonexistent error ");
                Assert.AreEqual(2, _fuspecTestCases.TestCases.Length, "Parser didn't write testcase");
                Assert.AreEqual("  x = round(a - b - c)\r    ", _fuspecTestCases.TestCases.FirstOrDefault().Script);
                Assert.AreEqual("     x = round(a - b - c)",_fuspecTestCases.TestCases[1].Script);

            });
        }
        
        [Test]
        public void BodyReading_AfterBodyIsNewCase_FirstBodyHasEnterInsteadScript_returnTwoCasesWithScript()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************

|***********
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
                Assert.AreEqual("", _fuspecTestCases.TestCases.FirstOrDefault().Script);
                Assert.AreEqual("     x = round(a - b - c)",_fuspecTestCases.TestCases[1].Script);
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
                Assert.AreEqual(0, _fuspecTestCases.TestCases.Length, "Parser wrote nonexistent testcase");
            });
        }
        
        [Test ]
        public void ParamsReading_ReadSimpleParams_returnParams()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in a:real
| out y:real
|----------------------
  x = round(a - b - c)  
");
            Param param = new Param("y","Real");
            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("a",_fuspecTestCases.TestCases.FirstOrDefault().ParamsIn[0].Value);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().ParamsIn[0].VarType);
                Assert.AreEqual(param.Value,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].Value);
                Assert.AreEqual(param.VarType,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].VarType);

            });
        }

        [Test ]
        public void ParamsReading_ReadSimpleParamsWithEnterAfterParams_returnParams()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in a:real
| out y:real

|------------------------

  x = round(a - b - c)  
");
            Param param = new Param("y", "Real");
            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("\r  x = round(a - b - c)  ",_fuspecTestCases.TestCases.FirstOrDefault().Script);
                Assert.AreEqual("a",_fuspecTestCases.TestCases.FirstOrDefault().ParamsIn[0].Value);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().ParamsIn[0].VarType);
                Assert.AreEqual(param.Value,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].Value);
                Assert.AreEqual(param.VarType,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].VarType);
            });
        }

        [Test ]
        public void ParamsReading_ReadJustParamsIn_returnParamsIn()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in a:real
|--------------------
  x = round(a - b - c)  
");
            Param param = new Param("a", "Real");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                
                Assert.AreEqual(param.Value,_fuspecTestCases.TestCases.FirstOrDefault().ParamsIn[0].Value);
                Assert.AreEqual(param.VarType,_fuspecTestCases.TestCases.FirstOrDefault().ParamsIn[0].VarType);
            });
        }
        
        [Test ]
        public void ParamsReading_ReadJustParamsOut_returnParamsIn()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| out y:real[]
|----------------
  x = round(a - b - c)  
");

            Param param = new Param("y","Real[]");
            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual(param.Value,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].Value);
                Assert.AreEqual(param.VarType,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].VarType);
            });
        }
        [Test]
        public void ParamsReading_AfterKeyWordAreEmptyString_returnErrorParamOutMissed()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| out
|------------------
  x = round(a - b - c) 
 
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.ParamOutMissed,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]
        public void ParamsReading_AfterKeyWordOutAreSpaces_returnErrorParamOutMissed()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| out   
|--------------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.WrongParamType,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]
        public void ParamsReading_AfterKeyWordInAreEmptyString_returnErrorParamInMissed()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in
|------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.ParamInMissed,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]
        public void ParamsReading_AfterKeyWordInAreSpaces_returnErrorParamInMissed()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in   
|-----------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.ParamInMissed,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]
        public void ParamsReading_TwoOutParams_returnTwoOutParams()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| out a:real, b:real
|-----------------------------
  x = round(a - b - c)  
");
            Param param = new Param("a","Real");
            Param param2 = new Param("b","Real");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual(param.Value,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].Value);
                Assert.AreEqual(param.VarType,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].VarType);
                Assert.AreEqual(param2.Value,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[1].Value);
                Assert.AreEqual(param2.VarType,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[1].VarType);
            });
        }
        
        [Test]//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1
        public void ParamsReading_TwoOutParamsWithSimilarValues_returnError()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| out a:real, a:int
|-----------------------------
  x = round(a - b - c)  
");
            Param param = new Param("a","Real");
            Param param2 = new Param("a","Int32");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
          //      Assert.AreEqual(param.Value,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].Value);
           //     Assert.AreEqual(param.VarType,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].VarType);
            //    Assert.AreEqual(param2.Value,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[1].Value);
             //   Assert.AreEqual(param2.VarType,_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[1].VarType);
            });
        }
        
        [Test]
        public void ParamsReading_TwoOutParamsWithSpaces_returnTwoOutParams()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| out a:real,  b:real
|-----------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("a",_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].Value);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].VarType);
                
                Assert.AreEqual("b",_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[1].Value);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[1].VarType);
                
            });
        }
        
        [Test]
        public void ParamsReading_TwoParamsIn_returnTwoInParams()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in a:real,b:real
|---------------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().ParamsIn[0].Value);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().ParamsIn[0].VarType);
                Assert.AreEqual( "b",_fuspecTestCases.TestCases.FirstOrDefault().ParamsIn[1].Value);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().ParamsIn[1].VarType);
            });
        }
        
        [Test]
        public void ParamsReading_OneCommaInsteadParams_returnError()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| out ,
|-----------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.WrongParamType, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        [Test]
        public void ParamsReading_TwoCommasInsteadParams_returnError()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| out ,,
|-----------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.WrongParamType, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]
        public void ParamsReading_NoSpaceAfterKeyWord_returnErrorParamMissed()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| outa:real
|--------------------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.ParamOutMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        
        [Test]//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public void ParamsReading_ParamsAndTwoCommas_returnErrorParamMissed()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| out a:real,,
|-----------------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
            });
        }
        
        
        [Test ]
        public void ParamsReading_ParamsWhithError_returnErrorInParsing()
        {
            GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in a:real
| out f:realsfdf
|------------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.WrongParamType,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);

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