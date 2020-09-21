using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Arrays
{
    public class ArrayConcatCallTest
    {
        [Test(Description = "y = concat(a,b)")]
        public void Concat_Generic()
        {
            //     2     0 1
            //y = concat(a,b) 
            var graph = new GraphBuilder();
            graph.SetVar("a", 0);
            graph.SetVar("b", 1);
            graph.SetConcatCall(0, 1, 2);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null);
            result.AssertNamedEqualToArrayOf(generic, "a","b","y");
        }


        [Test(Description = "a:int[]; y = concat(a,b)")]
        public void Concat_LeftConcreteArg()
        {
            //              2     0 1
            //a:int[]; y = concat(a,b) 
            var graph = new GraphBuilder();
            graph.SetVarType("a", StateArray.Of(StatePrimitive.I32));
            graph.SetVar("a", 0);
            graph.SetVar("b", 1);
            graph.SetConcatCall(0, 1, 2);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.I32,"y","a","b");
        }
       
        [Test(Description = "b:int[]; y = concat(a,b)")]
        public void Concat_RightConcreteArg()
        {
            //              2     0 1
            //b:int[]; y = concat(a,b) 
            var graph = new GraphBuilder();
            graph.SetVarType("b", StateArray.Of(StatePrimitive.I32));
            graph.SetVar("a", 0);
            graph.SetVar("b", 1);
            graph.SetConcatCall(0, 1, 2);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.I32, "y", "a", "b");
        }

        [Test(Description = "a:int[]; b:int[] y = concat(a,b)")]
        public void Concat_BothSameConcreteArgs()
        {
            //              2     0 1
            //a:int[]; b:int[]; y = concat(a,b) 
            var graph = new GraphBuilder();
            graph.SetVarType("a", StateArray.Of(StatePrimitive.I32));
            graph.SetVarType("b", StateArray.Of(StatePrimitive.I32));

            graph.SetVar("a", 0);
            graph.SetVar("b", 1);
            graph.SetConcatCall(0, 1, 2);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.I32, "y", "a", "b");
        }
        [Test(Description = "a:int[]; b:char[] y = concat(a,b)")]
        public void Concat_BothDifferentConcreteArgs()
        {
            //              2     0 1
            //a:int[]; b:char[]; y = concat(a,b) 
            var graph = new GraphBuilder();
            graph.SetVarType("a", StateArray.Of(StatePrimitive.I32));
            graph.SetVarType("b", StateArray.Of(StatePrimitive.Char));

            graph.SetVar("a", 0);
            graph.SetVar("b", 1);
            graph.SetConcatCall(0, 1, 2);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.Any, "y");
            result.AssertNamedEqualToArrayOf(StatePrimitive.Char, "b");
            result.AssertNamedEqualToArrayOf(StatePrimitive.I32, "a");
        }

        [Test(Description = "y:real[] = concat(a,b)")]
        public void Concat_ConcreteDef()
        {
            //              2     0 1
            //y:real[] = concat(a,b) 
            var graph = new GraphBuilder();
            graph.SetVar("a", 0);
            graph.SetVar("b", 1);
            graph.SetConcatCall(0, 1, 2);
            graph.SetVarType("y", StateArray.Of(StatePrimitive.Real));
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.Real, "y","a","b");
        }

        [Test(Description = "y:real[] = concat(a,b)")]
        public void Concat_ConcreteDefAndSameLeftArg()
        {
            //              2     0 1
            //a:real[]; y:real[] = concat(a,b)

            var graph = new GraphBuilder();
            graph.SetVarType("a", StateArray.Of(StatePrimitive.Real));

            graph.SetVar("a", 0);
            graph.SetVar("b", 1);
            graph.SetConcatCall(0, 1, 2);
            graph.SetVarType("y", StateArray.Of(StatePrimitive.Real));
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.Real, "y", "a", "b");
        }

        [Test(Description = "y:real[] = concat(a,b)")]
        public void Concat_ConcreteDefAndDescRightArg()
        {
            //              2     0 1
            //b:int[]; y:real[] = concat(a,b)

            var graph = new GraphBuilder();
            graph.SetVarType("b", StateArray.Of(StatePrimitive.I32));

            graph.SetVar("a", 0);
            graph.SetVar("b", 1);
            graph.SetConcatCall(0, 1, 2);
            graph.SetVarType("y", StateArray.Of(StatePrimitive.Real));
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.Real, "y", "a");
            result.AssertNamedEqualToArrayOf(StatePrimitive.I32, "b");

        }

        [Test(Description = "y = concat(a,concat(b,c))")]
        public void TwinConcat_Generic()
        {
            //     4     3    2   0 1
            //y = concat(a,concat(b,c)) 
            var graph = new GraphBuilder();
            graph.SetVar("b", 0);
            graph.SetVar("c", 1);

            graph.SetConcatCall(0, 1, 2);
            graph.SetVar("a", 3);
            graph.SetConcatCall(3, 2, 4);
            graph.SetDef("y", 4);
            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null);
            result.AssertNamed(new StateArray(generic), "b", "c", "a", "y");
        }
    }
}
