using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace Nfun.ModuleTests
{
    public class ToposortTest
    {
        TODO Закрыть тестами на RefTo и сложные примеры 
            
        [Test]
        public void ThreeNodesReferencesEachOther()
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
        {
            return TicNode.CreateNamedNode(name, new ConstrainsState());
        }
    }
}