using NFun.Fuspec.Parser.Model;
using NFun.Types;
using NUnit.Framework;
using ParcerV1;
using System.Linq;

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
        public void ReadSetValue()
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
| set a:1, b:2, c:4.4
");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                var setOrChecksKit1 = _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0];

                Assert.IsTrue(setOrChecksKit1 is SetData, "First kit isn't SET type");
                if (setOrChecksKit1 is SetData setKit1)
                {
                    Assert.AreEqual("a", setKit1.ValuesKit[0].Name, "Wrong Name of first data in the first kit");
                    Assert.AreEqual(VarType.Real, setKit1.ValuesKit[0].Type, "Wrong Type of first data in the first kit");
                    Assert.AreEqual(1, setKit1.ValuesKit[0].Value, "Wrong Value of first data in the first kit");
                    Assert.AreEqual("b", setKit1.ValuesKit[1].Name, "Wrong Name of second data in the first kit");
                    Assert.AreEqual(VarType.Real, setKit1.ValuesKit[1].Type, "Wrong Type of second data in the first kit");
                    Assert.AreEqual(2, setKit1.ValuesKit[1].Value, "Wrong Value of second data in the first kit");
                    Assert.AreEqual("c", setKit1.ValuesKit[2].Name, "Wrong Name of third data in the first kit");
                    Assert.AreEqual(VarType.Real, setKit1.ValuesKit[2].Type, "Wrong Type of third data in the first kit");
                    Assert.AreEqual(4.4, setKit1.ValuesKit[2].Value, "Wrong Value of third data in the first kit");
                }
            });
        }


        [Test]
        public void ReadTwoSetKits()
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
| set a:1, b:2, c:4.4
| set a:2, b:3, c:3.0
");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(2, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                var setOrChecksKit1 = _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0];
                var setOrChecksKit2 = _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1];

                Assert.IsTrue(setOrChecksKit1 is SetData, "First kit isn't SET type");
                Assert.IsTrue(setOrChecksKit2 is SetData, "Second kit isn't SET type");
            });
        }

        [Test]
        public void ReadCheckValue()
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
| check a:1, b:2, c:4.4
");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                var setOrChecksKit1 = _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0];

                Assert.IsTrue(setOrChecksKit1 is CheckData, "First kit isn't Check type");
                if (setOrChecksKit1 is CheckData CheckKit1)
                {
                    Assert.AreEqual("a", CheckKit1.ValuesKit[0].Name, "Wrong Name of first data in the first kit");
                    Assert.AreEqual(VarType.Real, CheckKit1.ValuesKit[0].Type, "Wrong Type of first data in the first kit");
                    Assert.AreEqual(1, CheckKit1.ValuesKit[0].Value, "Wrong Value of first data in the first kit");
                    Assert.AreEqual("b", CheckKit1.ValuesKit[1].Name, "Wrong Name of second data in the first kit");
                    Assert.AreEqual(VarType.Real, CheckKit1.ValuesKit[1].Type, "Wrong Type of second data in the first kit");
                    Assert.AreEqual(2, CheckKit1.ValuesKit[1].Value, "Wrong Value of second data in the first kit");
                    Assert.AreEqual("c", CheckKit1.ValuesKit[2].Name, "Wrong Name of third data in the first kit");
                    Assert.AreEqual(VarType.Real, CheckKit1.ValuesKit[2].Type, "Wrong Type of third data in the first kit");
                    Assert.AreEqual(4.4, CheckKit1.ValuesKit[2].Value, "Wrong Value of third data in the first kit");
                }
            });
        }
        [Test]
        public void ReadTwoCheckKits()
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
| check a:1, b:2, c:4.4
| check a:1, b:2, c:4.4
");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(2, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                var setOrChecksKit1 = _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0];
                var setOrChecksKit2 = _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1];

                Assert.IsTrue(setOrChecksKit1 is CheckData, "First kit isn't Check type");
                Assert.IsTrue(setOrChecksKit2 is CheckData, "First kit isn't Check type");

            });
        }

        [Test]
        public void ReadSetCheckKits()
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
| set a:1, b:2, c:4.4
| check a:1, b:2, c:4.4
");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(2, _fuspecTestCases.TestCases.FirstOrDefault().SetChecks.Length);

                var setOrChecksKit1 = _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[0];
                var setOrChecksKit2 = _fuspecTestCases.TestCases.FirstOrDefault().SetChecks[1];

                Assert.IsTrue(setOrChecksKit1 is SetData, "First kit isn't Set type");
                Assert.IsTrue(setOrChecksKit2 is CheckData, "First kit isn't Check type");

            });
        }

        [Test]
        public void ReadTwoFuspecCaseWithSetCheckKits_ReturnTwoFuspecCases()
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
        public void ReadWrongSetKit_ReturnError()
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
| setsdf a:5,b:4  
    ");

            Assert.Multiple(() =>
            {
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.WrongSetCheckKit, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void MissedSpaseAfterSet_ReturnError()
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
| seta:5,b:4  
    ");

            Assert.Multiple(() =>
            {
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.WrongSetCheckKit, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void MissedSpaseAfterCheck_ReturnError()
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
| checka:5,b:4  
    ");

            Assert.Multiple(() =>
            {
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.WrongSetCheckKit, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void ReadWrongCheckKit_ReturnError()
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
        public void ReadWrongStringAfterSetCheckKit_ReturnError()
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


        [Test]
        public void ReadWrongStringBeforeSetCheckKit_ReturnError()
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
| sdfsdfdf
| set a:5, b:4
| check a:5

    ");

            Assert.Multiple(() =>
            {
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.ExpectedOpeningLine, _fuspecTestCases.Errors.FirstOrDefault().ErrorType);
            });
        }

        [Test]
        public void NoSetCheckBlock_ReadOneTest()
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
|---------------------    ");
            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length);

            });
        }
    }
}

