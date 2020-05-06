using System;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic.Tests.UnitTests
{
    class SolvingFunctionsTest
    {
        [Test]
        public void GetMergedState_TwoSamePrimitives()
        {
            var res = SolvingFunctions.GetMergedState(Primitive.I32, Primitive.I32);
            Assert.AreEqual(res, Primitive.I32);
        }

        [Test]
        public void GetMergedState_PrimitiveAndEmptyConstrains()
        {
            var res = SolvingFunctions.GetMergedState(Primitive.I32, new Constrains());
            Assert.AreEqual(res, Primitive.I32);
        }

        [Test]
        public void GetMergedState_EmptyConstrainsAndPrimitive()
        {
            var res = SolvingFunctions.GetMergedState(new Constrains(), Primitive.I32);
            Assert.AreEqual(res, Primitive.I32);
        }
        [Test]
        public void GetMergedState_PrimitiveAndConstrainsThatFit()
        {
            var res = SolvingFunctions.GetMergedState(Primitive.I32, new Constrains(Primitive.U24, Primitive.I48));
            Assert.AreEqual(res, Primitive.I32);
        }
        [Test]
        public void GetMergedState_ConstrainsThatFitAndPrimitive()
        {
            var res = SolvingFunctions.GetMergedState(new Constrains(Primitive.U24, Primitive.I48), Primitive.I32);
            Assert.AreEqual(res, Primitive.I32);
        }
        [Test]
        public void GetMergedState_TwoSameConcreteArrays()
        {
            var res = SolvingFunctions.GetMergedState(Array.Of(Primitive.I32), Array.Of(Primitive.I32));
            Assert.AreEqual(res, Array.Of(Primitive.I32));
        }

        #region obviousFailed

        [Test]
        public void GetMergedState_PrimitiveAndConstrainsThatNotFit() 
            => AssertGetMergedStateThrows(Primitive.I32, new Constrains(Primitive.U24, Primitive.U48));

        [Test]
        public void GetMergedState_TwoDifferentPrimitivesThrows() 
            => AssertGetMergedStateThrows(Primitive.I32, Primitive.Real);

        [Test]
        public void GetMergedState_TwoDifferentConcreteArraysThrows()
            => AssertGetMergedStateThrows(
                    stateA: Array.Of(Primitive.I32), 
                    stateB: Array.Of(Primitive.Real));
        #endregion

        void AssertGetMergedStateThrows(IState stateA, IState stateB)
        {
            Assert.Throws<InvalidOperationException>(
                () => SolvingFunctions.GetMergedState(stateA, stateB));
            Assert.Throws<InvalidOperationException>(
                () => SolvingFunctions.GetMergedState(stateB, stateA));
        }
    }
}
