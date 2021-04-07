using NFun.Fuspec.Parser.Model;
using NUnit.Framework;
using ParcerV1;
using System.Linq;

namespace NFun.Fuspectests.FuspecTestCasesTests
{
    class ParamReadingTests
    {
        private FuspecTestCases _fuspecTestCases;

        [SetUp]
        public void Setup()
        {
        }
        [Test]
        public void ParamsReading_ReadSimpleParams_returnParams()
        {
           _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Type.ToString());
                Assert.AreEqual("y", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Type.ToString());

            });
        }

        [Test]
        public void ParamsReading_ReadSimpleParamsWithEnterAfterParams_returnParams()
        {
           _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual("\r\n  x = round(a - b - c)  ", _fuspecTestCases.TestCases.FirstOrDefault().Script);
                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Type.ToString());
                Assert.AreEqual("y", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Type.ToString());
            });
        }

        [Test]
        public void ParamsReading_ReadJustParamsIn_returnParamsIn()
        {
           _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Type.ToString());
            });
        }

        [Test]
        public void ParamsReading_ReadJustParamsOut_returnParamsIn()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual("y", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Name);
                Assert.AreEqual("Real[]", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Type.ToString());
            });
        }
        [Test]
        public void ParamsReading_AfterKeyWordAreEmptyString_returnErrorParamOutMissed()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.ParamOutMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void ParamsReading_AfterKeyWordOutAreSpaces_returnErrorParamOutMissed()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.ParamOutMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void ParamsReading_AfterKeyWordInAreEmptyString_returnErrorParamInMissed()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.ParamInMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void ParamsReading_AfterKeyWordInAreSpaces_returnErrorParamInMissed()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.ParamInMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void ParamsReading_TwoOutParams_returnTwoOutParams()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Type.ToString());
                Assert.AreEqual("b", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[1].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[1].Type.ToString());
            });
        }

        [Test]//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1
        public void ParamsReading_TwoOutParamsWithSimilarValues_returnError()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
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
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[0].Type.ToString());

                Assert.AreEqual("b", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[1].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().OutputVarList[1].Type.ToString());

            });
        }

        [Test]
        public void ParamsReading_TwoParamsIn_returnTwoInParams()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().InputVarList[0].Type.ToString());
                Assert.AreEqual("b", _fuspecTestCases.TestCases.FirstOrDefault().InputVarList[1].Name);
                Assert.AreEqual("Real", _fuspecTestCases.TestCases.FirstOrDefault().InputVarList[1].Type.ToString());
            });
        }

        [Test]
        public void ParamsReading_OneCommaInsteadParams_returnError()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        [Test]
        public void ParamsReading_TwoCommasInsteadParams_returnError()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void ParamsReading_NoSpaceAfterKeyWord_returnErrorParamMissed()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.ParamOutMissed, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public void ParamsReading_ParamsAndTwoCommas_returnErrorParamMissed()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
            });
        }


        [Test]
        public void ParamsReading_ParamsWithError_returnErrorInParsing()
        {
            _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);

            });
        }

    }
}
