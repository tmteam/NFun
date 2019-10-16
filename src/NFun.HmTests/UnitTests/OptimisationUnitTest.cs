using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests.UnitTests
{
    [TestFixture]
    public class OptimisationUnitTest
    {
        
        [Test]
        public void OptimizeLcaWith2Generics_AllNodesEqualToSingleGeneric()
        {
            var genericA = new SolvingNode();
            var genericB = new SolvingNode();
            var lca = SolvingNode.CreateLca(genericA,genericB);

            Assert.IsTrue(TiSolver.Optimize(new[] {genericA, genericB, lca}));
            
            var actualGenericA = genericA.GetActualNode().Behavior;
            var actualGenericB = genericB.GetActualNode().Behavior;
            var actualLca      = lca.GetActualNode().Behavior;

            Assert.IsInstanceOf<GenericTypeBehaviour>(actualGenericA);
            Assert.AreEqual(actualGenericA,actualGenericB);
            Assert.AreEqual(actualGenericA,actualLca);

        }
        [Test]
        public void OptimizeLcaWith2ComplexGenerics_AllNodesEqualToSingleComplexGeneric()
        {
            var Ta = new SolvingNode();
            var Tb = new SolvingNode();
            var arrayOfTa = SolvingNode.CreateStrict(TiType.ArrayOf(Ta));
            var arrayOfTb = SolvingNode.CreateStrict(TiType.ArrayOf(Tb));
            
            var lca = SolvingNode.CreateLca(arrayOfTa,arrayOfTb);

            Assert.IsTrue(TiSolver.Optimize(new[] {arrayOfTa, arrayOfTb, lca}));
            /*
            var actualGenericA = Ta.GetActualNode().Behavior as StrictNodeBehaviour;
            var actualGenericB = Tb.GetActualNode().Behavior as StrictNodeBehaviour;
            var actualLca      = lca.GetActualNode().Behavior;
            */
            var actualGenericA = Ta.GetActualNode().Behavior;
            var actualGenericB = Tb.GetActualNode().Behavior;

            Assert.IsInstanceOf<GenericTypeBehaviour>(actualGenericA);
            Assert.AreEqual(actualGenericA,actualGenericB);
            //Assert.AreEqual(actualGenericA,actualLca);
            /* 
            Assert.IsNotNull(actualGenericA);
            Assert.IsNotNull(actualGenericB);
            var arrA =actualGenericA.MakeType();
            var arrB =actualGenericB.MakeType();
            var actualA =arrA.Arguments[0].GetActualNode().Behavior;
            var actualB =arrB.Arguments[0].GetActualNode().Behavior;
            
            Assert.AreEqual(actualA,actualB);*/
        }
        [Test]
        public void OptimizeLcaWith2GenericsRefs_AllNodesEqualToSingleGeneric()
        {
            var genericA = new SolvingNode();
            var genericB = new SolvingNode();
            var refToA = SolvingNode.CreateRefTo(genericA);
            var refToB = SolvingNode.CreateRefTo(genericB);

            var lca = SolvingNode.CreateLca(refToA,refToB);

            Assert.IsTrue(TiSolver.Optimize(new[] {refToA, refToB, lca}));
            
            var actualGenericA = refToA.GetActualNode().Behavior;
            var actualGenericB = refToB.GetActualNode().Behavior;
            var actualLca      = lca.GetActualNode().Behavior;

            Assert.IsInstanceOf<GenericTypeBehaviour>(actualGenericA);
            Assert.AreEqual(actualGenericA,actualGenericB);
            Assert.AreEqual(actualGenericA,actualLca);

        }
        [Test]
        public void OptimizeRefToLcaWith2GenericsRefs_AllNodesEqualToSingleGeneric()
        {
            var genericA = new SolvingNode();
            var genericB = new SolvingNode();
            var refToA = SolvingNode.CreateRefTo(genericA);
            var refToB = SolvingNode.CreateRefTo(genericB);

            var lca = SolvingNode.CreateLca(refToA,refToB);
            var refTolca = SolvingNode.CreateRefTo(lca);
            
            Assert.IsTrue(TiSolver.Optimize(new[] {refToA, refToB, refTolca}));
            
            var actualGenericA = refToA.GetActualNode().Behavior;
            var actualGenericB = refToB.GetActualNode().Behavior;
            var actualLca      = refTolca.GetActualNode().Behavior;

            Assert.IsInstanceOf<GenericTypeBehaviour>(actualGenericA);
            Assert.AreEqual(actualGenericA,actualGenericB);
            Assert.AreEqual(actualGenericA,actualLca);

        }   
    }
}