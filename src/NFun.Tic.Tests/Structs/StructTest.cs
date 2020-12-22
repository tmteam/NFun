using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Structs
{
    public class StructTest
    {
        [Test]
        public void SingleStrictStructMember()
        {
            //        0 2  1
            //y:int = a . name
            var graph = new GraphBuilder();
            graph.SetVar("a",    0);
            graph.SetFieldAccess(0, 2, "name");
            graph.SetVarType("y", StatePrimitive.I32);
            graph.SetDef("y", 2);
            
            var result = graph.Solve();
            
            result.AssertNoGenerics();
            result.AssertNamed(StateStruct.WithField("name", StatePrimitive.I32),"a");
            result.AssertNamed(StatePrimitive.I32, "y");
        }
        [Test]
        public void SeveralStrictStructMembers()
        {
            //        0 2  1
            //y:int = a . name

            //         3 5  4
            //z:real = a . age

            var graph = new GraphBuilder();
            graph.SetVar("a",    0);
            graph.SetFieldAccess(0, 2, "name");
            graph.SetVarType("y", StatePrimitive.I32);
            graph.SetDef("y", 2);

            graph.SetVar("a",    3);
            graph.SetFieldAccess(3, 5, "age");
            graph.SetVarType("z", StatePrimitive.Real);
            graph.SetDef("z", 5);

            var result = graph.Solve();
            
            result.AssertNoGenerics();
            result.AssertNamed(StateStruct.WithField("name", StatePrimitive.I32),"a");
            result.AssertNamed(StatePrimitive.I32, "y");
        }
    }
}