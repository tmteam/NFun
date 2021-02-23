using System.Linq;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace Nfun.ModuleTests
{
    /* deprecated
     
    public class ToposortTest
    {
        [Test]
        public void TwoNodesReferencesEachOther_OneNodeInResult()
        {
            var a1 = CreateNode("a1");
            var a2 = CreateNode("a2");
            a1.State = new StateRefTo(a2);
            a2.State = new StateRefTo(a1);
            var algorithm = new NodeToposort(3);
            algorithm.AddToTopology(a1,a2);
            algorithm.OptimizeTopology();
            Assert.AreEqual(1,algorithm.NonReferenceOrdered.Length);
        }

        [Test]
        public void NodeReferencesOtherNode_OneOfTheNodesHasAncestorsOfBoth()
        {
            var a1 = CreateNode("a1");
            var a2 = CreateNode("a2");
            var c1 = CreateNode("c1");
            var c2 = CreateNode("c2");
            c1.AddAncestor(a1);
            c2.AddAncestor(a2);
            c1.State = new StateRefTo(c2);
            
            var algorithm = new NodeToposort(3);
            algorithm.AddToTopology(a1,a2,c1,c2);
            algorithm.OptimizeTopology();

            Assert.AreEqual(3, algorithm.NonReferenceOrdered.Length);
            TicNode central = null;
            if (algorithm.NonReferenceOrdered.Contains(c1)) 
                central = c1;
            if (algorithm.NonReferenceOrdered.Contains(c2))
            {
                if(central!=null)
                    Assert.Fail("Both referenced still remains in algorithm");
                central = c2;
            }
            Assert.IsNotNull(central);
            Assert.AreEqual(2, central.Ancestors.Count);
            Assert.True(central.Ancestors.Contains(a1));
            Assert.True(central.Ancestors.Contains(a2));
        }
        
        [Test]
        public void ThreeNodesAncestorLine()
        {
            var n1 = CreateNode("1");
            var n2 = CreateNode("2");
            var n3 = CreateNode("3");
            
            n1.AddAncestor(n3);
            n2.AddAncestor(n1);

            var algorithm = new NodeToposort(3);
            algorithm.AddToTopology(n1);
            algorithm.AddToTopology(n2);
            algorithm.AddToTopology(n3);
            algorithm.OptimizeTopology();
            CollectionAssert.AreEqual(new []{n2,n1,n3}, algorithm.NonReferenceOrdered);
        }
        
        [Test]
        public void ThreeRefCycle()
        {
            var n1 = CreateNode("1");
            var n2 = CreateNode("2");
            var n3 = CreateNode("3");

            
            n1.State = new StateRefTo(n2);
            n2.State = new StateRefTo(n3);
            n3.State = new StateRefTo(n1);
            var algorithm = new NodeToposort(3);
            
            algorithm.AddToTopology(n2);
            algorithm.AddToTopology(n1);
            algorithm.AddToTopology(n3);
            
            algorithm.OptimizeTopology();
            
            Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
            Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
            Assert.IsEmpty( n1.Ancestors);
            Assert.IsEmpty(n2.Ancestors);
            Assert.IsEmpty( n3.Ancestors);
        }
        [Test]
        public void NotObviousThreeRefCycle()
        {
            var n1 = CreateNode("1");
            var n2 = CreateNode("2");
            var n3 = CreateNode("3");

            n1.State = new StateRefTo(n2);
            n3.State = new StateRefTo(n2);
            n3.State = new StateRefTo(n1);
            var algorithm = new NodeToposort(3);
            
            algorithm.AddToTopology(n2);
            algorithm.AddToTopology(n1);
            algorithm.AddToTopology(n3);
            
            algorithm.OptimizeTopology();
            
            Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
            Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
            Assert.IsEmpty( n1.Ancestors);
            Assert.IsEmpty(n2.Ancestors);
            Assert.IsEmpty( n3.Ancestors);
        }
        
        [Test]
        public void AllRefsToSingle_SingleItemAppears()
        {
            var n1 = CreateNode("1");
            var n2 = CreateNode("2");
            var n3 = CreateNode("3");
            var central = CreateNode("C");

            n1.State = new StateRefTo(central);
            n3.State = new StateRefTo(central);
            n3.State = new StateRefTo(central);
            var algorithm = new NodeToposort(3);
            
            algorithm.AddToTopology(n2);
            algorithm.AddToTopology(n1);
            algorithm.AddToTopology(central);
            algorithm.AddToTopology(n3);
            
            algorithm.OptimizeTopology();
            
            Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
            Assert.AreEqual(central, algorithm.NonReferenceOrdered[0]);
            Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
            Assert.IsEmpty(n1.Ancestors);
            Assert.IsEmpty(n2.Ancestors);
            Assert.IsEmpty(n3.Ancestors);
        }
        
        [Test]
        public void NotObviousRefAndAncestorCycle()
        {
            var n1 = CreateNode("1");
            var n2 = CreateNode("2");
            var n3 = CreateNode("3");

            n1.State = new StateRefTo(n2);
            n1.AddAncestor(n3);
            n3.AddAncestor(n2);

            var algorithm = new NodeToposort(3);
            
            algorithm.AddToTopology(n2);
            algorithm.AddToTopology(n1);
            algorithm.AddToTopology(n3);
            
            algorithm.OptimizeTopology();
            
            Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
            Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
            Assert.IsEmpty(n1.Ancestors);
            Assert.IsEmpty(n2.Ancestors);
            Assert.IsEmpty(n3.Ancestors);
        }
        
        [Test]
        public void ThreeAncestorsCycle_HasSingleNodeWithNoAncestors()
        {
            var n1 = CreateNode("1");
            var n2 = CreateNode("2");
            var n3 = CreateNode("3");
            
            n1.AddAncestor(n3);
            n2.AddAncestor(n1);
            n3.AddAncestor(n2);
            
            var algorithm = new NodeToposort(3);
            algorithm.AddToTopology(n1);
            algorithm.AddToTopology(n2);
            algorithm.AddToTopology(n3);
            algorithm.OptimizeTopology();
            
            Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
            Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
            Assert.IsEmpty( n1.Ancestors);
            Assert.IsEmpty(n2.Ancestors);
            Assert.IsEmpty( n3.Ancestors);
        }

        private static TicNode CreateNode(string name) 
            => TicNode.CreateNamedNode(name, new ConstrainsState());
    }
    */
}