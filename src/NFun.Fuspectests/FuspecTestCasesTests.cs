using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;
using NFun.Types;
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
  x = round(a + b + c)
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
            
            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("a",_fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].VarType.ToString());
                Assert.AreEqual("y",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].VarType.ToString());

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
            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("\r\n  x = round(a - b - c)  ",_fuspecTestCases.TestCases.FirstOrDefault().Script);
                Assert.AreEqual("a",_fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].VarType.ToString());
                Assert.AreEqual("y",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].VarType.ToString());
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
          

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("a",_fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].VarType.ToString());
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

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("y",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Id);
                Assert.AreEqual("Real[]",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].VarType.ToString());
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
                Assert.AreEqual(FuspecErrorType.ParamOutMissed,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);
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
    

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual("a",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].VarType.ToString());
                Assert.AreEqual("b",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[1].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[1].VarType.ToString());
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
            
// не организована проверка!
            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
                //    Assert.AreEqual("a",_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].Value);
                //    Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[0].VarType.ToString());
                //    Assert.AreEqual("b",_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[1].Value);
                //    Assert.AreEqual("Int32",_fuspecTestCases.TestCases.FirstOrDefault().ParamsOut[1].VarType.ToString());
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
                Assert.AreEqual("a",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].VarType.ToString());
                
                Assert.AreEqual("b",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[1].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[1].VarType.ToString());
                
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
| in a:real,  b:real
|---------------------------
  x = round(a - b - c)  
");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].VarType.ToString());
                Assert.AreEqual( "b",_fuspecTestCases.TestCases.FirstOrDefault().InputVarList[1].Id);
                Assert.AreEqual("Real",_fuspecTestCases.TestCases.FirstOrDefault().InputVarList[1].VarType.ToString());
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
                Assert.AreEqual(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
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
                Assert.AreEqual(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
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
        public void ParamsReading_ParamsWithError_returnErrorInParsing()
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
                Assert.AreEqual(FuspecErrorType.NFunMessage_ICantParseParamTypeString,_fuspecTestCases.Errors.FirstOrDefault().ErrorType);

            });
        }

        [Test]
        public void SetValuesReading_ReadJustSetValue()
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
| set a:1, b:2, c:4.4
| set a:4, b:44.4
");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual(2,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);
                Assert.IsFalse(_fuspecTestCases.TestCases[0].SetChecks[0].Check.Any());
                
                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set[1].Type);
            });

        }

        [Test]
        public void SetValuesReading_ReadSetCheckValue()
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
| set a:1, b:2, c:4.4
| check a:4, b:44.4
");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual(1,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);
                
                
                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[0].Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[0].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[1].Type);
            });

        }
        
        [Test]
        public void SetValuesReading_ReadSetSetCheckValue()
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
| set a:1, b:2, c:4.4
| set a:1, b:7, c:4.4
| check a:4, b:44.4
");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual(2,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);
                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);
                
                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set[1].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set[2].Type);
                
                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[0].Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[0].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[1].Type);
            });

        }
        
        [Test]
        public void SetValuesReading_ReadCheckValue()
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
| check a:4, b:44.4
| check a:4, b:44.4
");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual(2,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[1].Type);
                Assert.IsFalse(_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.Any());
              
                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[0].Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[0].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[1].Type);
                Assert.IsFalse(_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.Any());
            });

        }
        
        [Test]
        public void SetValuesReading_ReadSetCheckSetValue()
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
| set a:1, b:2, c:4.4
| check a:4, b:44.4
| set a:1, b:7, c:4.4
");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual(2,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);
                
             
                
                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[0].Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[0].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[1].Type);
                
                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set[1].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set[2].Type);
                Assert.IsFalse(_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check.Any());
            });

        }
        
         [Test]
        public void SetValuesReading_ReadArrayValue()
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
| set a:[1,2], b:[8.8,6], c:4.4

");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual(1,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.ArrayOf(VarType.Int32),_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[0].Type);
                Assert.AreEqual(VarType.ArrayOf(VarType.Anything),_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);
            });

        }
        
        [Test]
        public void SetValuesReading_ReadBoolValue()
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
| set a:true, b:[8.8,6], c:4.4

");

            Assert.Multiple(() =>
            {
                StandardAssertForCorrectTestCase();
                Assert.AreEqual(1,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual( "a",_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Bool,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[0].Type);
                Assert.AreEqual(VarType.ArrayOf(VarType.Anything),_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real,_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);
            });

        }

        [Test]
        public void SetValuesReading_ReadTwoFuspecCaseWithSetCheckKits_ReturnTwoFuspecCases()
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
| set a:5 ,b:4
| check h:5, y:4
| set a:5, b:4
| check h:5, y:4

 
|***************
| TEST Name
| TAGS tag1
|************************
| in a:real b:int
| out x:real
|------------------------
  x = a-b  
|---------------------
| set a:5, b:4
| check h:5, y:4
");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, _fuspecTestCases.Errors.Length, "Parser wrote nonexistent error ");
                Assert.AreEqual(2,_fuspecTestCases.TestCases.Length);

            });
        }
        
        [Test]
        public void SetValuesReading_ReadWrongSetKit_ReturnError()
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
        public void SetValuesReading_ReadWrongCheckKit_ReturnError()
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
| set a:5,b:4
| checkff
");

            Assert.Multiple(() =>
            {
                StandardAssertForNotCorrectTestCase();
                Assert.AreEqual(FuspecErrorType.WrongSetCheckKit, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        [Test]
        public void SetValuesReading_ReadWrongStringAfterSetCheckKit_ReturnError()
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