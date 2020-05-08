using Nfun.Fuspec.Parser.Model;
using NFun.Fuspectests;
using NFun.Types;
using NUnit.Framework;
using ParcerV1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFun.Fuspectests.FuspecTestCasesTests
{
    class SetValuesReadingTests
    {
        private FuspecTestCases _fuspecTestCases;

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void SetValuesReading_ReadJustSetValue()
        {

           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(2, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);
                Assert.IsFalse(_fuspecTestCases.TestCases[0].SetChecks[0].Check.Any());

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set[1].Type);
            });

        }

        [Test]
        public void SetValuesReading_ReadSetCheckValue()
        {

           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);


                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[0].Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[0].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[1].Type);
            });

        }

        [Test]
        public void SetValuesReading_ReadSetSetCheckValue()
        {

           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(2, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);
                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set[1].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set[2].Type);

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[0].Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[0].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[1].Type);
            });

        }

        [Test]
        public void SetValuesReading_ReadCheckValue()
        {

           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(2, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[1].Type);
                Assert.IsFalse(_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.Any());

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[0].Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[0].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check[1].Type);
                Assert.IsFalse(_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.Any());
            });

        }

        [Test]
        public void SetValuesReading_ReadSetCheckSetValue()
        {

           _fuspecTestCases =TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(2, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);



                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[0].Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[0].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Check[1].Type);

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set.FirstOrDefault().Type);
                Assert.AreEqual(VarType.Int32, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set[1].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Set[2].Type);
                Assert.IsFalse(_fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1].Check.Any());
            });

        }

        [Test]
        public void SetValuesReading_ReadArrayValue()
        {

           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.ArrayOf(VarType.Int32), _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[0].Type);
                Assert.AreEqual(VarType.ArrayOf(VarType.Anything), _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);
            });

        }

       

        [Test]
        public void SetValuesReading_ReadBoolValue()
        {

           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
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
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Bool, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[0].Type);
                Assert.AreEqual(VarType.ArrayOf(VarType.Anything), _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);
            });

        }

        [Test]
        public void SetValuesReading_ReadTextValue()
        {

           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
                @"|********************
| TEST Name
| TAGS tag1
|************************
| in a:real b:int
| out x:real
|------------------------
  x = a-b 
|---------------------
| set a:'rtr', b:[""r"",""ttt""], c:4.4

");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                Assert.AreEqual("a", _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set.FirstOrDefault().Name);
                Assert.AreEqual(VarType.Text, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[0].Type);
                Assert.AreEqual(VarType.ArrayOf(VarType.Text), _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[1].Type);
                Assert.AreEqual(VarType.Real, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0].Set[2].Type);
            });

        }

        [Test]
        public void SetValuesReading_ReadTwoFuspecCaseWithSetCheckKits_ReturnTwoFuspecCases()
        {
            _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
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
                Assert.AreEqual(2, _fuspecTestCases.TestCases.Length);

            });
        }

        [Test]
        public void SetValuesReading_ReadWrongSetKit_ReturnError()
        {
           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.WrongSetCheckKit, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void SetValuesReading_ReadWrongCheckKit_ReturnError()
        {
           _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.WrongSetCheckKit, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
        [Test]
        public void SetValuesReading_ReadWrongStringAfterSetCheckKit_ReturnError()
        {
           _fuspecTestCases = TestHelper.GenerateFuspecTestCases(
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
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.ExpectedOpeningLine, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }
    }
}
