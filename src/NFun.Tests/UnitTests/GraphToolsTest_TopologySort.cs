using NFun.SyntaxParsing;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    [TestFixture]
    public class GraphToolsTest_TopologySort
    {
        [Test]
        public void OneNodeCycle()
        {
            var graph = new[]
            {
                From(0),
            };
            var res = GraphTools.SortTopology(graph);
            AssertHasCycle(new []{0}, res);
        }
        
        [Test]
        public void TwoNodesCycle()
        {
            var graph = new[]
            {
                From(1),
                From(0),
            };
            var res = GraphTools.SortTopology(graph);
            AssertHasCycle(new []{0,1}, res);
        }
        [Test]
        public void ThreeNodesCycle()
        {
            var graph = new[]
            {
                From(3),
                From(0),
                From(1),
                From(2),
            };
            var res = GraphTools.SortTopology(graph);
            AssertHasCycle(new []{0,1,2,3}, res);
        }
        
        [Test]
        public void ComplexNodesCycle()
        {
            //         |<------|
            //0->1->2->|3->4->5|->6
            var graph = new[]
            {
                NoParents,
                From(0),
                From(1),
                From(2,5), //cycle here
                From(3),
                From(4),
                From(5),
                From(6),
            };
            var res = GraphTools.SortTopology(graph);
            AssertHasCycle(new []{3,4,5}, res);
        }
        [Test]
        public void TwoNodesGraphSorting()
        {
            //1->0
            var graph = new[]
            {
                From(1),
                NoParents
            };
            var res = GraphTools.SortTopology(graph);
            AssertHasRoute(new[]{1,0}, res);
        }
        [Test]
        public void ThreeNodesInLineSorting()
        {
            //2->1->0
            var graph = new[]
            {
                From(1),
                From(2),
                NoParents
            };
            var res = GraphTools.SortTopology(graph);
            AssertHasRoute(new[]{2,1,0}, res);
        }
        [Test]
        public void ThreeNodesInLineRevertSorting()
        {
            //0->1->2
            var graph = new[]
            {
                NoParents,
                From(0),
                From(1)
            };
            var res = GraphTools.SortTopology(graph);
            AssertHasRoute(new[]{0,1,2}, res);
        }
        [Test]
        public void ComplexGraphSorting()
        {
            //{5,3}->|6->|
            //   {1,4} ->|0->2
            var graph = new[]
            {
                From(1,4,6),
                NoParents,
                From(0),
                NoParents,
                NoParents,
                NoParents,
                From(5,3),
            };
            var res = GraphTools.SortTopology(graph);
            AssertHasRoute(new[]{1,4,5,3,6,0,2}, res);
        }
        [Test]
        public void SeveralComplexGraphSorting()
        {
            var graph = new int[17][];
            
            //{5,3}->|6->|
            //   {1,4} ->|0->2
            graph[0] = From(1, 4, 6);
            graph[1] = NoParents;
            graph[2] = From(0);
            graph[3]=  NoParents;
            graph[4] = NoParents;
            graph[5] = NoParents;
            graph[6] = From(5, 3);
            //{12,8}->|10->|
            //    {9,11} ->|13->7
            graph[7] = From(13);
            graph[8] = NoParents;
            graph[9] = NoParents;
            graph[10] = From(12,8);
            graph[11] = NoParents;
            graph[12] = NoParents;
            graph[13] = From(9,11,10);
            //14
            graph[14] = NoParents;
            //15->16
            graph[15] = NoParents;
            graph[16] = From(15);
            
            var res = GraphTools.SortTopology(graph);
            AssertHasRoute(new[]
            {
                1,4,5,3,6,0,2,  
                9,11,12,8,10,13,7, 
                14, 
                15,16
            }, res);
        }
        
        [Test]
        public void ThreeSeparatedNodesSorting()
        {
            //2,1,0
            var graph = new[]
            {
                new int[0],
                new int[0],
                new int[0]
            };
            var res = GraphTools.SortTopology(graph);
            AssertHasRoute(new[]{0,1,2}, res);
        }
        private int[] NoParents => new int[0];
        private int[] From(params int[] routes) => routes;
        private string ArrayToString(int[] arr)
        {
            return $"[{string.Join(",", arr)}]";
        }

        private void AssertHasCycle(int[] cycle, TopologySortResults actual)
        {
            Assert.IsTrue(actual.HasCycle , "Cycle not found");
            CollectionAssert.AreEqual(cycle ,actual.NodeNames, 
                $"expected: {ArrayToString(cycle)} but was: {ArrayToString(actual.NodeNames)}");
    
        }
        
        private void AssertHasRoute(int[] expected, TopologySortResults actual)
        {
            Assert.IsFalse(actual.HasCycle, "Order not found");
            CollectionAssert.AreEqual(expected,actual.NodeNames, 
                $"expected: {ArrayToString(expected)} but was: {ArrayToString(actual.NodeNames)}");

        }
    }
}