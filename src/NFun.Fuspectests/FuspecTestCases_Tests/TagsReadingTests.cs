using NFun.Fuspec.Parser.Model;
using NUnit.Framework;
using ParcerV1;
using System.Linq;

namespace NFun.Fuspectests.FuspecTestCasesTests
{
    class TagsReadingTests
    {
        private FuspecTestCases _fuspecTestCases;

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CorrectTestCaseWithEmptyTag_ReturnNoTags()
        {
           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
                 @"|************************
| TEST Complex example 
| TAGS
|************************
  x = round(a + b + c)
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Wrong number of Tests. Expected 1 TestCase!");
                Assert.AreEqual("Complex example", _fuspecTestCases.TestCases.FirstOrDefault().Name, "Set wrong Name");
                Assert.AreEqual(0, _fuspecTestCases.TestCases.FirstOrDefault().Tags.Length,
                    "Set wrong Tags. Nonexistent tag was added");
            });
        }

        [Test]
        public void CorrectTestCaseWithOneTag_ReturnOneTag()
        {
           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS Tag1
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Wrong number of Tests. Expected 1 TestCase!");
                Assert.AreEqual(1, _fuspecTestCases.TestCases.FirstOrDefault().Tags.Length,
                    "Wrong number of tags. Expected 1");
                Assert.AreEqual("Tag1", _fuspecTestCases.TestCases.FirstOrDefault().Tags[0],
                    "Set wrong Tag. Expected Tag1");
            });
        }

        [Test]
        public void CorrectTestCaseWithManyTags_ReturnListOfTags()
        {
           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS Tag1, Tag2
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Wrong number of Tests. Expected 1 TestCase!");
                Assert.AreEqual(2, _fuspecTestCases.TestCases.FirstOrDefault().Tags.Length,
                    "Wrong number of tags. Expected 2");
                Assert.AreEqual("Tag1", _fuspecTestCases.TestCases[0].Tags[0], "Set wrong Tag. Expected Tag1");
                Assert.AreEqual("Tag2", _fuspecTestCases.TestCases[0].Tags[1], "Set wrong Tag. Expected Tag2");
            });
        }

        [Test]
        public void CorrectTestCaseWithSpaceBarTag_ReturnTagWithoutSpaces()
        {
           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS   Tag1  
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Wrong number of Tests. Expected 1 TestCase!");
                Assert.AreEqual(1, _fuspecTestCases.TestCases.FirstOrDefault().Tags.Length,
                    "Wrong number of tags. Expected 1");
                Assert.AreEqual("Tag1", _fuspecTestCases.TestCases.FirstOrDefault().Tags[0],
                    "Set wrong Tag. Expected Tag1");
            });
        }

        [Test]
        public void CorrectTestCaseWithComma_ReturnNoTags()
        {
           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
| TAGS ,  ,  
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                TestHelper.AssertForCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(1, _fuspecTestCases.TestCases.Length, "Wrong number of Tests. Expected 1 TestCase!");
                Assert.AreEqual(0, _fuspecTestCases.TestCases.FirstOrDefault().Tags.Length,
                    "Wrong number of tags. Expected 0");
            });
        }

        [Test]
        public void WrongTestCaseWithoutKeWordTAGS_ReturnError_EndingHeadMissed()
        {
           _fuspecTestCases= TestHelper.GenerateFuspecTestCases(
                @"|************************
| TEST Complex example 
|  Tag1, Tag2
|************************
  x = round(a + b + c)");

            Assert.Multiple(() =>
            {
                TestHelper.StandardAssertForNotCorrectTestCase(_fuspecTestCases);
                Assert.AreEqual(FuspecErrorType.EndingHeadMissed,
                    _fuspecTestCases.Errors[0].ErrorType, "Fuspec didn't write EndingHeadMissed Error");
            });

        }
    }
}
