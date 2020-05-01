using System.Linq;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests
{
    class TrickyPrimitives
    {
        
        [Test(Description = "y = isNan(1) ")]
        public void SimpleConcreteFunctionWithConstant()
        {
            //node |    1     0
            //expr |y = isNan(1) 
            var graph = new GraphBuilder();
            graph.SetIntConst(0, Primitive.U8);
            graph.SetCall(new []{Primitive.Real, Primitive.Bool}, new []{0,1});
            graph.SetDef("y", 1);
            var result = graph.Solve();

            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Bool, "y");
        }

        [Test(Description = "y = isNan(x) ")]
        //[Ignore("Обобщенный вход без выхода")]
        public void SimpleConcreteFunctionWithVariable()
        {
            //node |    1     0
            //expr |y = isNan(x) 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetCall(new[] { Primitive.Real, Primitive.Bool }, new[] { 0, 1 });
            graph.SetDef("y", 1);
            var result = graph.Solve();

            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Real, "x");
            result.AssertNamed(Primitive.Bool, "y");
        }

        [Test(Description = "x:int; y = isNan(x) ")]
        //[Ignore("Обобщенный вход без выхода")]

        public void SimpleConcreteFunctionWithVariableOfConcreteType()
        {
            //node |           1     0
            //expr |x:int; y = isNan(x) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Primitive.I32);
            graph.SetVar("x", 0);
            graph.SetCall(new[] { Primitive.Real, Primitive.Bool }, new[] { 0, 1 });
            graph.SetDef("y", 1);
            var result = graph.Solve();

            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "x");
            result.AssertNamed(Primitive.Bool, "y");
        }

        [Test(Description = "y = isNan(1i)")]
        public void SimpleConcreteFunctionWithConstLimit()
        {
            //node |    1     0       
            //expr |y = isNan(1i);
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.I32);
            graph.SetCall(new[] { Primitive.Real, Primitive.Bool }, new[] { 0, 1 });
            graph.SetDef("y", 1);

            var result = graph.Solve();
            result.AssertNoGenerics();
        }

        [Test(Description = "y = isNan(x); z = ~x")]
        //[Ignore("Обобщенный вход без выхода")]

        public void SimpleConcreteFunctionWithVariableThatLimitisAfterwards()
        {
            //node |    1     0       3        2
            //expr |y = isNan(x); z = isMaxInt(x) 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetCall(new[] { Primitive.Real, Primitive.Bool }, new[] { 0, 1 });
            graph.SetDef("y", 1);

            graph.SetVar("x",2);
            graph.SetCall(new []{Primitive.I32, Primitive.Bool}, new []{2,3});
            graph.SetDef("z", 3);

            var result = graph.Solve();

            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "x");
            result.AssertNamed(Primitive.Bool, "y","z");
        }

        [Test(Description = "y = x ")]
        public void OutputEqualsInput_simpleGeneric()
        {
            //node |1   0
            //expr |y = x 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetDef("y", 0);
            var result = graph.Solve();

            var generic = result.AssertAndGetSingleGeneric(null, null, false);
            result.AssertAreGenerics(generic, "x", "y");
        }

        [Test(Description = "y = x; | y2 = x2")]
        public void TwoSimpleGenerics()
        {
            //node |     0  |       1
            //expr s|y = x; | y2 = x2
            
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetDef("y", 0);

            graph.SetVar("x2", 1);
            graph.SetDef("y2", 1);

            var result = graph.Solve();

            Assert.AreEqual(2, result.GenericsCount);

            var generics = result.Generics.ToArray();

            generics[0].AssertGenericType(null, null, false);
            generics[1].AssertGenericType(null, null, false);

            var yRes = result.GetVariableNode("y").GetNonReference();
            var y2Res = result.GetVariableNode("y2").GetNonReference();
            CollectionAssert.AreEquivalent(generics, new[]{y2Res, yRes});

            var xRes = result.GetVariableNode("x").GetNonReference();
            var x2Res = result.GetVariableNode("x2").GetNonReference();
            CollectionAssert.AreEquivalent(generics, new[] { x2Res, xRes });

        }

        [Test]
        //[Ignore("Обобщенная константа без выхода")]
        public void LimitCall_ComplexEquations_TypesSolved()
        {
            //     0 2 1      3 5  4      6 8 7
            // r = x + y; i = y << 2; x = 3 / 2
            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("y", 1);
            graph.SetArith(0,1,2);
            graph.SetDef("r",2);

            graph.SetVar("y", 3);
            graph.SetIntConst(4, Primitive.U8);
            graph.SetBitShift(3, 4, 5);
            graph.SetDef("i", 5);

            graph.SetIntConst(6, Primitive.U8);
            graph.SetIntConst(7, Primitive.U8);
            graph.SetCall(Primitive.Real, 6,7,8);
            graph.SetDef("x", 8);

            var result = graph.Solve();
            result.AssertNamed(Primitive.Real, "x", "r");
            var generic = result.AssertAndGetSingleGeneric(Primitive.U24, Primitive.I96);

            result.AssertAreGenerics(generic, "y","i");
        }

        [Test]
        //[Ignore("Generic constants")]
        public void SummReducecByBitShift_AllTypesAreInt()
        {
            //  0 2 1  4 3
            //( x + y )<<3

            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("y", 1);
            graph.SetArith(0, 1, 2);

            graph.SetIntConst(3, Primitive.U8);

            graph.SetBitShift(2, 3, 4);
            graph.SetDef("out", 4);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(Primitive.U24, Primitive.I96);

            result.AssertAreGenerics(generic, "x", "y", "out");
        }

        [Test]
        //[Ignore("Generic constants")]
        public void ConcreteTypeOfArithmetical_ConstantsAreConcrete()
        {
            //0 4 1 3 2  
            //x<<(1 + 2)

            var graph = new GraphBuilder();

            graph.SetVar("x", 0);

            graph.SetIntConst(1, Primitive.U8);
            graph.SetIntConst(2, Primitive.U8);
            graph.SetArith(1,2,3);
            graph.SetBitShift(0, 3, 4);
            graph.SetDef("out", 4);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(Primitive.U24, Primitive.I96);

            result.AssertAreGenerics(generic, "x", "out");
        }


        [Test]
        public void TypeSpecified_PutHighterType_EquationSOlved()
        {
            //         1    0  
            //a:real;  a = 1:int32
            var graph = new GraphBuilder();
            graph.SetVarType("a", Primitive.Real);
            graph.SetConst(0, Primitive.I32);
            graph.SetDef("a",0);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Real,"a" );
        }

        [Test]
        public void TypeLimitSet_ThanChangedToLower_LowerLimitAccepted()
        {
            //    0            1
            //a = 1:int;  a = 1.0:int64
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.I32);
            graph.SetDef("a", 0);
            graph.SetConst(1, Primitive.I64);
            graph.SetDef("a", 1);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I64, "a");
        }

        [Test]
        public void TypeLimitSet_ThanChangedToHigher_LowerLimitAccepted()
        {
            //1   0          3   2
            //a = 1:int64;  a = 1.0:int32

            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.I64);
            graph.SetDef("a", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetDef("a", 1);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I64, "a");
        }


        [Test]
        public void EqualtyOnGenerics()
        {
            //     0  2  1     
            //y = 1.0 == x 

            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.Real);
            graph.SetVar("x",1);
            var generic = graph.SetEquality(0, 1, 2);
            graph.SetDef("y", 2);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Bool, "y");
            result.AssertNamed(Primitive.Real, "x");
            Assert.AreEqual(Primitive.Real, generic.GetNonReference());
        }
        [Test]
        public void EqualtyOnGenericsReversed()
        {
            //    0  2  1     
            //y = x == 1.0 

            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.Real);
            var generic = graph.SetEquality(0, 1, 2);
            graph.SetDef("y", 2);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Bool, "y");
            result.AssertNamed(Primitive.Real, "x");
            Assert.AreEqual(Primitive.Real, generic.GetNonReference());
        }



    }
}
