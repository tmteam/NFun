using System.Collections.Generic;
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
            result.AssertNamed(new StateStruct(new Dictionary<string, TicNode>
            {
                {"name", TicNode.CreateTypeVariableNode(StatePrimitive.I32)},
                {"age", TicNode.CreateTypeVariableNode(StatePrimitive.Real)}}), 
                "a");
            result.AssertNamed(StatePrimitive.I32, "y");
            result.AssertNamed(StatePrimitive.Real, "z");
        }
        
        [Test]
        public void StructConstructor()
        {
            //    2       0       1
            //y = @{ a = 12i, b = 1.0} 
            var graph = new GraphBuilder();
            graph.SetConst(0, StatePrimitive.I32);
            graph.SetConst(1, StatePrimitive.Real);
            graph.SetStructInit(new[]{"a", "b"},new[]{0,1}, 2);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(new StateStruct(new Dictionary<string, TicNode>()
            {
                {"a", TicNode.CreateNamedNode("a",StatePrimitive.I32)},
                {"b", TicNode.CreateNamedNode("b",StatePrimitive.Real)}
            }),"y");
        }
        
        [Test]
        public void NestedStructConstructor()
        {
            //    4       0        3     1       2
            //y = @{ a = 12i, b = @{c = true,d = 1.0 } 
            var graph = new GraphBuilder();
            graph.SetConst(0, StatePrimitive.I32);
            graph.SetConst(1, StatePrimitive.Bool); 
            graph.SetConst(2, StatePrimitive.Real);
            graph.SetStructInit(new[]{"c", "d"},new[]{1,2}, 3);
            graph.SetStructInit(new[]{"a", "b"},new[]{0,3}, 4);
            graph.SetDef("y", 4);
            var result = graph.Solve();
            result.AssertNoGenerics();
            
            var yStruct =result.GetVariableNode("y").State as StateStruct;
            var aField = yStruct.GetFieldOrNull("a").State as StatePrimitive;
            Assert.AreEqual(StatePrimitive.I32.Name,  aField.Name);
            var bField = yStruct.GetFieldOrNull("b").State as StateStruct;
            var cField = bField.GetFieldOrNull("c").State as StatePrimitive;
            Assert.AreEqual(StatePrimitive.Bool.Name,  cField.Name);
            var dField = bField.GetFieldOrNull("d").State as StatePrimitive;
            Assert.AreEqual(StatePrimitive.Real.Name,  dField.Name);
        }
    }
}