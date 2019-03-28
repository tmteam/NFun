using System;
using System.Linq;
using Funny.Runtime;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    [TestFixture]
    public class FunArrayTest
    {
        [TestCase(new[]{1,2,3}, null, null, null, new[]{1,2,3})]
        [TestCase(new[]{1,2,3}, 1, null, null, new[]{2,3})]
        [TestCase(new[]{1,2,3}, 1, 1, null, new[]{2})]
        [TestCase(new[]{1,2,3}, 1, 1, 1, new[]{2})]
        [TestCase(new[]{1,2,3}, 1, 1, 2, new[]{2})]
        [TestCase(new[]{1,2,3}, 1, 1, 100, new[]{2})]
        [TestCase(new[]{1,2,3}, null, 1, null, new[]{1,2})]
        [TestCase(new[]{1,2,3}, null, 1, 1, new[]{1,2})]
        [TestCase(new[]{1,2,3}, null, 1, 2, new[]{1})]
        [TestCase(new[]{1,2,3}, null, 1, 100, new[]{1})]
        [TestCase(new[]{1,2,3}, null, 1, 100, new[]{1})]
        [TestCase(new[]{1,2,3}, 100, null, null, new int[0])]
        [TestCase(new[]{1,2,3}, 100, 200, null, new int[0])]
        [TestCase(new[]{1,2,3}, 100, 200, 2, new int[0])]
        [TestCase(new[]{1,2,3}, 0, 3, null, new[]{1,2,3})]
        [TestCase(new[]{1,2,3}, 0, 200, null, new[]{1,2,3})]
        [TestCase(new[]{1,2,3}, 0, 200, 1, new[]{1,2,3})]
        [TestCase(new[]{1,2,3}, null, 0, null, new []{1})]
        [TestCase(new[]{1,2,3}, null, 0, 100, new []{1})]
        [TestCase(new[]{1,2,3}, 0, 0, 100, new []{1})]
        [TestCase(new[]{1,2,3}, null, null, 2, new []{1,3})]
        [TestCase(new[]{1,2,3,4}, null, 3, 2, new []{1,3})]
        [TestCase(new[]{1,2,3,4}, 0, 3, 2, new []{1,3})]
        [TestCase(new int[0], null, 0, 100, new int[0])]
        [TestCase(new int[0], 0, 0, 100, new int[0])]
        [TestCase(new int[0], 1, 3, null, new int[0])]
        [TestCase(new int[0], null, null, null, new int[0])]
        public void IntArr_Slice_ReturnsExpected(int[] origin, int? start, int? end, int? step, int[] expected)
        {
            var funOrigin = new FunArray(origin.Cast<Object>().ToArray(), VarType.Int);
            var funExpected = new FunArray(expected.Cast<Object>().ToArray(), VarType.Int);
            CollectionAssert.AreEquivalent(funExpected,funOrigin.Slice(start,end,step));
        }
        [Test]
        public void IntArr_Equialent_IsEquivalentReturnsTrue()
        {
            var a1 = new FunArray(new object[] {1, 2, 3}, VarType.Int);
            var a2 = new FunArray(new object[] {1, 2, 3}, VarType.Int);
            Assert.IsTrue(a1.IsEquivalent(a2));
        }
        [Test]
        public void IntArr_NotEquialent_IsEquivalentReturnsFalse()
        {
            var a1 = new FunArray(new object[] {1, 2, 3}, VarType.Int);
            var a2 = new FunArray(new object[] {1, 2}, VarType.Int);
            Assert.IsFalse(a1.IsEquivalent(a2));
        }
        [Test]
        public void IntArr_BothEmpty_IsEquivalentReturnsTrue()
        {
            var a1 = new FunArray(new object[0], VarType.Int);
            var a2 = new FunArray(new object[0], VarType.Int);
            Assert.IsTrue(a1.IsEquivalent(a2));
        }
        
        [Test]
        public void IntArrOfArr_BothEmpty_IsEquivalentReturnsTrue()
        {
            var a1 = new FunArray(new object[0], VarType.ArrayOf(VarType.Int));
            var a2 = new FunArray(new object[0], VarType.ArrayOf(VarType.Int));
            Assert.IsTrue(a1.IsEquivalent(a2));
        }
        
        [Test]
        public void IntArrOfArr_Equivalent_IsEquivalentReturnsTrue()
        {

            var a1 = new FunArray(VarType.ArrayOf(VarType.Int),
                new FunArray(new object[0], VarType.Int),
                new FunArray(new object[] {1, 2}, VarType.Int),
                new FunArray(new object[] {1, 2,3}, VarType.Int)
                );
            var a2 = new FunArray(VarType.ArrayOf(VarType.Int),
                new FunArray(new object[0], VarType.Int),
                new FunArray(new object[] {1, 2}, VarType.Int),
                new FunArray(new object[] {1, 2,3}, VarType.Int)
            );

            Assert.IsTrue(a1.IsEquivalent(a2));
        }
        [Test]
        public void IntArrOfArr_NotEquivalent_IsEquivalentReturnsFalse()
        {

            var a1 = new FunArray(VarType.ArrayOf(VarType.Int),
                new FunArray(new object[0], VarType.Int),
                new FunArray(new object[] {1, 2}, VarType.Int),
                new FunArray(new object[] {1, 2,3}, VarType.Int)
            );
            var a2 = new FunArray(VarType.ArrayOf(VarType.Int),
                new FunArray(new object[0], VarType.Int),
                new FunArray(new object[] {1, 2}, VarType.Int),
                new FunArray(new object[] {1, 2}, VarType.Int)
            );

            Assert.IsFalse(a1.IsEquivalent(a2));
        }
        [Test]
        public void TextArrOfArr_Equivalent_IsEquivalentReturnsTrue()
        {

            var a1 = new FunArray(VarType.ArrayOf(VarType.Text),
                new FunArray(new object[0], VarType.Text),
                new FunArray(new object[] {"1", "2"}, VarType.Text),
                new FunArray(new object[] {"1", "2","3"}, VarType.Text)
            );
            var a2 = new FunArray(VarType.ArrayOf(VarType.Text),
                new FunArray(new object[0], VarType.Text),
                new FunArray(new object[] {"1", "2"}, VarType.Text),
                new FunArray(new object[] {"1", "2","3"}, VarType.Text)
            );

            Assert.IsTrue(a1.IsEquivalent(a2));
        }
        [Test]
        public void TextArrOfArr_NotEquivalent_IsEquivalentReturnsFalse()
        {

            var a1 = new FunArray(VarType.ArrayOf(VarType.Text),
                new FunArray(new object[0], VarType.Text),
                new FunArray(new object[] {"1", "2"}, VarType.Text),
                new FunArray(new object[] {"1", "2","3"}, VarType.Text)
            );
            var a2 = new FunArray(VarType.ArrayOf(VarType.Text),
                new FunArray(new object[]{"lalala"}, VarType.Text),
                new FunArray(new object[] {"1", "2"}, VarType.Text),
                new FunArray(new object[] {"1", "2","3"}, VarType.Text)
            );

            Assert.IsFalse(a1.IsEquivalent(a2));
        }
    }
}