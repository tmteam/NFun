using System.Linq;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace Nfun.ModuleTests
{
    public class SolvingFunctionsTest
    {
        [TestCase(0,1,2)]
        [TestCase(0,2,1)]
        [TestCase(1,2,0)]
        [TestCase(1,0,2)]
        [TestCase(2,1,0)]
        [TestCase(2,0,1)]
        public void MergeGroupWithCycle_ReturnsSingle(params int[] order)
        {
            //a[i32,r]
            //b[i24,r]
            //r ==> a
            var a = CreateNode("a", new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real));
            var b = CreateNode("b", new ConstrainsState(StatePrimitive.I24, StatePrimitive.Real));
            var r = CreateNode("r", new StateRefTo(a));
            var group = new TicNode[3];
            group[order[0]] = a; group[order[1]] = b; group[order[2]] = r;
            
            var merged = SolvingFunctions.MergeGroup(group);
            Assert.AreNotEqual(r, merged);
            Assert.AreEqual(new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real), merged.State);
        }
        
        [TestCase(0,1,2)]
        [TestCase(0,2,1)]
        [TestCase(1,2,0)]
        [TestCase(1,0,2)]
        [TestCase(2,1,0)]
        [TestCase(2,0,1)]
        public void MergeGroupWithCycle_AncestorsAreCorrect(params int[] order)
        {
            //Arrange:
            
            //a[i32,r]
            //b[i24,r]
            //r ==> a
            var a = CreateNode("a", new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real));
            var b = CreateNode("b", new ConstrainsState(StatePrimitive.I24, StatePrimitive.Real));
            var r = CreateNode("r", new StateRefTo(a));

            var anc1 = CreateNode("anc1");
            var anc2 = CreateNode("anc2");
            var anc3 = CreateNode("anc3");
            a.AddAncestor(anc1);
            b.AddAncestor(anc2);
            r.AddAncestor(anc3);
            // Shuffle group order
            var group = new TicNode[3];
            group[order[0]] = a; group[order[1]] = b; group[order[2]] = r;

            //Act:
            var merged = SolvingFunctions.MergeGroup(group);

            //Assert:
            //All non main node have to loose all ancestors
            foreach (var nonRef in new []{a,b,r}.Where(i=>i!=merged)) 
                Assert.IsEmpty(nonRef.Ancestors);
            //All ancestors move to main node
            Assert.AreEqual(3, merged.Ancestors.Count);
            Assert.Contains(anc1, merged.Ancestors.ToArray());
            Assert.Contains(anc2, merged.Ancestors.ToArray());
            Assert.Contains(anc3, merged.Ancestors.ToArray());
        }
        [TestCase(true)]
        [TestCase(false)]
        public void MergeGroupWithSmallCycle_ReturnsSingle(bool reversedOrder)
        {
            //a[i32,r]
            //r ==> a
            var a = CreateNode("a", new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real));
            var r = CreateNode("r", new StateRefTo(a));
            var merged = SolvingFunctions.MergeGroup(reversedOrder?new[] {r,a}: new[] {a,r});
            Assert.AreEqual(a, merged);
            Assert.AreEqual(r.State, new StateRefTo(merged));
            Assert.AreEqual(new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real), merged.State);
        }
        private static TicNode CreateNode(string name, ITicNodeState state= null) 
            => TicNode.CreateNamedNode(name, state??new ConstrainsState());
    }
}