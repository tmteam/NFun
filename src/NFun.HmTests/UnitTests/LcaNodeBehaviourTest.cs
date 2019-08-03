using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests.UnitTests
{
    public class LcaNodeBehaviourTest
    {
        [TestCase]
        public void SingleType_MakeTypeReturnsTheType()
        {
            var beh = new LcaNodeBehaviour(new[]{SolvingNode.CreateStrict(FType.Int64)});
            Assert.AreEqual(FType.Int64, beh.MakeType(100));
        }
        
        [TestCase]
        public void TwoSameTypes_MakeTypeResturnsTheType()
        {
            var beh = new LcaNodeBehaviour(new[]
            {
                SolvingNode.CreateStrict(FType.Int64),
                SolvingNode.CreateStrict(FType.Int64)
            });
            Assert.AreEqual(FType.Int64, beh.MakeType(100));
        }
        
        [TestCase]
        public void ParentAndChild_MakeTypeReturnsParent()
        {
            var beh = new LcaNodeBehaviour(new[]
            {
                SolvingNode.CreateStrict(FType.Real),
                SolvingNode.CreateStrict(FType.UInt8)
            });
            Assert.AreEqual(FType.Real, beh.MakeType(100));
        }
        
        [TestCase]
        public void ParentAndChildren_MakeTypeReturnsParent()
        {
            var beh = new LcaNodeBehaviour(new[]
            {
                SolvingNode.CreateStrict(FType.UInt32),
                SolvingNode.CreateStrict(FType.Real),
                SolvingNode.CreateStrict(FType.UInt8),
                SolvingNode.CreateStrict(FType.UInt8)
            });
            Assert.AreEqual(FType.Real, beh.MakeType(100));
        }

        
            [TestCase]
        public void ManyNumberTypes_MakeTypeReturnsLastCommonAncestor()
        {
            var beh = new LcaNodeBehaviour(new[]
            {
                SolvingNode.CreateStrict(FType.Int64),
                SolvingNode.CreateStrict(FType.UInt64),
                SolvingNode.CreateStrict(FType.UInt8)
            });
            Assert.AreEqual(new FType(HmTypeName.SomeInteger), beh.MakeType(100));
        }
        
        [TestCase]
        public void TwoSiblings_MakeTypeReturnsParent()
        {
            var beh = new LcaNodeBehaviour(new[]
            {
                SolvingNode.CreateStrict(FType.Int16),
                SolvingNode.CreateStrict(FType.UInt16),
            });
            Assert.AreEqual(FType.Int32, beh.MakeType(100));
        }
        
        [Test]
        public void OptimizeTextAndNumber_ReturnsFalse(){
            var lca = SolvingNode.CreateLca(SolvingNode.CreateStrict(FType.Text), SolvingNode.CreateStrict(FType.Real));
            Assert.IsFalse(lca.Optimize(out _));
        }
        
        [Test]
        public void OptimizeRealAndInt_ReturnsTrue(){
            var lca = SolvingNode.CreateLca(
                SolvingNode.CreateStrict(FType.Int32), 
                SolvingNode.CreateStrict(FType.Real));
            
            Assert.IsTrue(lca.Optimize(out _));
        }
    }
}