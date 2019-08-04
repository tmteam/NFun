using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests.UnitTests
{
    public class LcaNodeBehaviourTest
    {
        [TestCase]
        public void SingleType_MakeTypeReturnsTheType()
        {
            var beh = new LcaNodeBehaviour(new[]{SolvingNode.CreateStrict(TiType.Int64)});
            Assert.AreEqual(TiType.Int64, beh.MakeType(100));
        }
        
        [TestCase]
        public void TwoSameTypes_MakeTypeResturnsTheType()
        {
            var beh = new LcaNodeBehaviour(new[]
            {
                SolvingNode.CreateStrict(TiType.Int64),
                SolvingNode.CreateStrict(TiType.Int64)
            });
            Assert.AreEqual(TiType.Int64, beh.MakeType(100));
        }
        
        [TestCase]
        public void ParentAndChild_MakeTypeReturnsParent()
        {
            var beh = new LcaNodeBehaviour(new[]
            {
                SolvingNode.CreateStrict(TiType.Real),
                SolvingNode.CreateStrict(TiType.UInt8)
            });
            Assert.AreEqual(TiType.Real, beh.MakeType(100));
        }
        
        [TestCase]
        public void ParentAndChildren_MakeTypeReturnsParent()
        {
            var beh = new LcaNodeBehaviour(new[]
            {
                SolvingNode.CreateStrict(TiType.UInt32),
                SolvingNode.CreateStrict(TiType.Real),
                SolvingNode.CreateStrict(TiType.UInt8),
                SolvingNode.CreateStrict(TiType.UInt8)
            });
            Assert.AreEqual(TiType.Real, beh.MakeType(100));
        }

        
            [TestCase]
        public void ManyNumberTypes_MakeTypeReturnsLastCommonAncestor()
        {
            var beh = new LcaNodeBehaviour(new[]
            {
                SolvingNode.CreateStrict(TiType.Int64),
                SolvingNode.CreateStrict(TiType.UInt64),
                SolvingNode.CreateStrict(TiType.UInt8)
            });
            Assert.AreEqual(new TiType(TiTypeName.SomeInteger), beh.MakeType(100));
        }
        
        [TestCase]
        public void TwoSiblings_MakeTypeReturnsParent()
        {
            var beh = new LcaNodeBehaviour(new[]
            {
                SolvingNode.CreateStrict(TiType.Int16),
                SolvingNode.CreateStrict(TiType.UInt16),
            });
            Assert.AreEqual(TiType.Int32, beh.MakeType(100));
        }
        
        [Test]
        public void OptimizeTextAndNumber_ReturnsFalse(){
            var lca = SolvingNode.CreateLca(SolvingNode.CreateStrict(TiType.Text), SolvingNode.CreateStrict(TiType.Real));
            Assert.IsFalse(lca.Optimize(out _));
        }
        
        [Test]
        public void OptimizeRealAndInt_ReturnsTrue(){
            var lca = SolvingNode.CreateLca(
                SolvingNode.CreateStrict(TiType.Int32), 
                SolvingNode.CreateStrict(TiType.Real));
            
            Assert.IsTrue(lca.Optimize(out _));
        }
    }
}