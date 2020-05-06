using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests
{
    class FunTest
    {
        [Test]
        public void ConcreteTypes_SameTypes_EqualsReturnsTrue()
        {
            var funA = Fun.Of(Primitive.Any, Primitive.I32);
            var funB = Fun.Of(Primitive.Any, Primitive.I32);
            Assert.IsTrue(funA.Equals(funB));
        }

        [Test]
        public void ConcreteTypes_DifferentArgs_EqualsReturnsFalse()
        {
            var funA = Fun.Of(Primitive.Any, Primitive.I32);
            var funB = Fun.Of(Primitive.Any, Primitive.Real);
            Assert.IsFalse(funA.Equals(funB));
        }

        [Test]
        public void ConcreteTypes_DifferentReturns_EqualsReturnsFalse()
        {
            var funA = Fun.Of(Primitive.Any, Primitive.I32);
            var funB = Fun.Of(Primitive.Real, Primitive.I32);
            Assert.IsFalse(funA.Equals(funB));
        }
        [Test]
        public void NonConcreteTypes_SameNodes_EqualsReturnsTrue()
        {
            var retNode = CreateConstrainsNode();
            var argNode = CreateConstrainsNode();

            var funA = Fun.Of(retNode, argNode);
            var funB = Fun.Of(retNode, argNode);
            Assert.IsTrue(funA.Equals(funB));
        }
        [Test]
        public void NonConcreteTypes_DifferentNodes_EqualsReturnsTrue()
        {
            var retNodeA = CreateConstrainsNode();
            var retNodeB = CreateConstrainsNode();

            var argNode  = CreateConstrainsNode();

            var funA = Fun.Of(retNodeA, argNode);
            var funB = Fun.Of(retNodeB, argNode);
            Assert.IsFalse(funA.Equals(funB));
        }
        [Test]
        public void ConcreteTypes_IsSolvedReturnsTrue()
        {
            var fun = Fun.Of(Primitive.Any, Primitive.I32);
            Assert.IsTrue(fun.IsSolved);
        }

        [Test]
        public void GenericTypes_IsSolvedReturnsFalse()
        {
            var fun = Fun.Of(CreateConstrainsNode(), CreateConstrainsNode());
            Assert.IsFalse(fun.IsSolved);
        }

        [Test]
        public void GetLastCommonAncestorOrNull_SameConcreteTypes_ReturnsEqualType()
        {
            var funA = Fun.Of(Primitive.Any, Primitive.I32);
            var funB = Fun.Of(Primitive.Any, Primitive.I32);
            var ancestor = funA.GetLastCommonAncestorOrNull(funB);
            Assert.AreEqual(funA, ancestor);
            var ancestor2 = funB.GetLastCommonAncestorOrNull(funA);
            Assert.AreEqual(ancestor2, ancestor);
        }

        [Test]
        public void GetLastCommonAncestorOrNull_ConcreteType_ReturnsAncestor()
        {
            var funA = Fun.Of(Primitive.I32, Primitive.I64);
            var funB = Fun.Of(Primitive.U16, Primitive.U64);
            var expected = Fun.Of(Primitive.U16, Primitive.I96);

            Assert.AreEqual(expected, funA.GetLastCommonAncestorOrNull(funB));
            Assert.AreEqual(expected, funB.GetLastCommonAncestorOrNull(funA));
        }

        [Test]
        public void GetLastCommonAncestorOrNull_NotConcreteTypes_ReturnsNull()
        {
            var funA = Fun.Of(CreateConstrainsNode(), SolvingNode.CreateTypeNode(Primitive.I32));
            var funB = Fun.Of(CreateConstrainsNode(), SolvingNode.CreateTypeNode(Primitive.I32));

            Assert.IsNull(funA.GetLastCommonAncestorOrNull(funB));
            Assert.IsNull(funB.GetLastCommonAncestorOrNull(funA));
        }

        [Test]
        public void GetLastCommonAncestorOrNull_ConcreteAndNotConcreteType_ReturnsNull()
        {
            var funA     = Fun.Of(CreateConstrainsNode(), SolvingNode.CreateTypeNode(Primitive.I32));
            var funB     = Fun.Of(Primitive.U16, Primitive.U64);

            Assert.IsNull(funA.GetLastCommonAncestorOrNull(funB));
            Assert.IsNull(funB.GetLastCommonAncestorOrNull(funA));
        }

        private SolvingNode CreateConstrainsNode()
            => new SolvingNode("", new Constrains(), SolvingNodeType.TypeVariable);
    }
}
