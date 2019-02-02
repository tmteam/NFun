using Funny.Interpritation;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class GraphToolsTests
    {
        [Test]
        public void TwoNodesGraphSorting()
        {
            //1->0
            var graph = new[]
            {
                new[] {1},
                new int[0]
            };
            var res = GraphTools.SortTopology(graph);
            AssertEquals(new[]{0,1}, res);
        }
        [Test]
        public void ThreeNodesInLineSorting()
        {
            //2->1->0
            var graph = new[]
            {
                new[] {1},
                new[] {2},
                new int[0]
            };
            var res = GraphTools.SortTopology(graph);
            AssertEquals(new[]{0,1,2}, res);
        }
        [Test]
        public void ThreeNodesInLineRevertSorting()
        {
            //0->1->2
            var graph = new[]
            {
                new int[0],
                new[] {2},
                new[] {1}
            };
            var res = GraphTools.SortTopology(graph);
            AssertEquals(new[]{2,1,0}, res);
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
            AssertEquals(new[]{0,1,2}, res);
        }

        private string ArrayToString(int[] arr)
        {
            return $"[{string.Join(',', arr)}]";
        }

        private void AssertEquals(int[] expected, TopologySortResults actual)
        {
            Assert.IsFalse(actual.HasCycle);
            CollectionAssert.AreEqual(expected,actual.Order, 
                $"expected: {ArrayToString(expected)} but was: {ArrayToString(actual.Order)}");

        }
    }
}