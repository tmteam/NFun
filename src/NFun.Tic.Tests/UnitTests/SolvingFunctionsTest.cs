using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests
{
    class SolvingFunctionsTest
    {
        [Test]
        public void GetMergedState_TwoSamePrimitives()
        {
            var res = SolvingFunctions.GetMergedStateOrNull(StatePrimitive.I32, StatePrimitive.I32);
            Assert.AreEqual(res, StatePrimitive.I32);
        }

        [Test]
        public void GetMergedState_PrimitiveAndEmptyConstrains()
        {
            var res = SolvingFunctions.GetMergedStateOrNull(StatePrimitive.I32, new ConstrainsState());
            Assert.AreEqual(res, StatePrimitive.I32);
        }

        [Test]
        public void GetMergedState_EmptyConstrainsAndPrimitive()
        {
            var res = SolvingFunctions.GetMergedStateOrNull(new ConstrainsState(), StatePrimitive.I32);
            Assert.AreEqual(res, StatePrimitive.I32);
        }
        [Test]
        public void GetMergedState_PrimitiveAndConstrainsThatFit()
        {
            var res = SolvingFunctions.GetMergedStateOrNull(StatePrimitive.I32, new ConstrainsState(StatePrimitive.U24, StatePrimitive.I48));
            Assert.AreEqual(res, StatePrimitive.I32);
        }
        [Test]
        public void GetMergedState_ConstrainsThatFitAndPrimitive()
        {
            var res = SolvingFunctions.GetMergedStateOrNull(new ConstrainsState(StatePrimitive.U24, StatePrimitive.I48), StatePrimitive.I32);
            Assert.AreEqual(res, StatePrimitive.I32);
        }
        [Test]
        public void GetMergedState_TwoSameConcreteArrays()
        {
            var res = SolvingFunctions.GetMergedStateOrNull(StateArray.Of(StatePrimitive.I32), StateArray.Of(StatePrimitive.I32));
            Assert.AreEqual(res, StateArray.Of(StatePrimitive.I32));
        }

        #region obviousFailed

        [Test]
        public void GetMergedState_PrimitiveAndConstrainsThatNotFit() 
            => AssertGetMergedStateThrows(StatePrimitive.I32, new ConstrainsState(StatePrimitive.U24, StatePrimitive.U48));

        [Test]
        public void GetMergedState_TwoDifferentPrimitivesThrows() 
            => AssertGetMergedStateThrows(StatePrimitive.I32, StatePrimitive.Real);

        [Test]
        public void GetMergedState_TwoDifferentConcreteArraysThrows()
            => AssertGetMergedStateThrows(
                    stateA: StateArray.Of(StatePrimitive.I32), 
                    stateB: StateArray.Of(StatePrimitive.Real));
        #endregion

        void AssertGetMergedStateThrows(ITicNodeState stateA, ITicNodeState stateB)
        {
            Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(stateA, stateB));
            Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(stateB, stateA));
        }
    }
}
