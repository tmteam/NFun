using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Funs
{
    public class TrickyTest
    {
        [Test]
        public void MapWithGenericArrayOfArray()
        {
            //    4   0  3    2     1     
            //y = map(a, x->reverse(x))
            var graph = new GraphBuilder();
            graph.SetVar("a", 0);
            graph.SetVar("lx", 1);
            var generic = graph.InitializeVarNode();

            graph.SetCall(new []{Array.Of(generic), Array.Of(generic)} , new []{1,2});
            graph.CreateLambda(2,3, "lx");
            graph.SetMap(0, 3,4);
            graph.SetDef("y", 4);

            var result = graph.Solve();

            var t = result.AssertAndGetSingleGeneric(null,null);

            result.AssertNamed(Array.Of(t), "lx");
            result.AssertNamed(Array.Of(Array.Of(t)), "a","y");

            result.AssertNode(Fun.Of(Array.Of(t), Array.Of(t)), 3);
        }
    }
}
