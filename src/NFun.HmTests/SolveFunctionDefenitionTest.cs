using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests
{
    public class SolveFunctionDefenitionTest
    {
        private HmHumanizerSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new HmHumanizerSolver();
        }

        [Test]
        public void ConcatWithFullFunctionalTypeSpecification_EquationSolved()
        {
            var arrayOfInt = FType.ArrayOf(FType.Int32);
            // 3                  0    2   1
            // y(a:int[]):int[] = a.concat(a)
            solver.SetVarType("a", arrayOfInt);
            solver.SetVarType("y()", FType.Fun(arrayOfInt, arrayOfInt));

            Assert.IsTrue(solver.SetVar(0, "a"));
            Assert.IsTrue(solver.SetVar(1, "a"));
            Assert.IsTrue(solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1})));

            solver.SetFunDefenition("y()", 3, 2).AssertSuccesfully();

            var solvation = solver.Solve();
            Assert.IsTrue(solvation.IsSolved);
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(arrayOfInt, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Fun(arrayOfInt, arrayOfInt), solvation.GetVarType("y()"));
        }
        
        
        [Test]
        public void ConcatWithFullFunctionalTypeSpecification_SetDefAtStart_EquationSolved()
        {
            var arrayOfInt = FType.ArrayOf(FType.Int32);
            // 3                  0    2   1
            // y(a:int[]):int[] = a.concat(a)
            solver.SetVarType("a", arrayOfInt);
            solver.SetVarType("y()", FType.Fun(arrayOfInt, arrayOfInt));

            solver.SetFunDefenition("y()", 3, 2).AssertSuccesfully();
            
            Assert.IsTrue(solver.SetVar(0, "a"));
            Assert.IsTrue(solver.SetVar(1, "a"));
            Assert.IsTrue(solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1})));

            var solvation = solver.Solve();
            Assert.IsTrue(solvation.IsSolved);
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(arrayOfInt, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Fun(arrayOfInt, arrayOfInt), solvation.GetVarType("y()"));
        }
        
        [Test]
        public void ReverseWithFullFunctionalTypeSpecification_SetDefAtStart_EquationSolved()
        {
            var arrayOfInt = FType.ArrayOf(FType.Int32);
            // 2                      1   0
            // y(a:int[]):int[] = reverse(a)
            solver.SetVarType("a", arrayOfInt);
            solver.SetVarType("y()", FType.Fun(arrayOfInt, arrayOfInt));

            solver.SetFunDefenition("y()", 2, 1).AssertSuccesfully();;
            
            Assert.IsTrue(solver.SetVar(0, "a"));
            Assert.IsTrue(solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {1, 0})));

            var solvation = solver.Solve();
            Assert.IsTrue(solvation.IsSolved);
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(arrayOfInt, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Fun(arrayOfInt, arrayOfInt), solvation.GetVarType("y()"));
        }
        
        [Test]
        public void SelfWithFullFunctionalTypeSpecification_SetDefAtStart_EquationSolved()
        {
            var arrayOfInt = FType.ArrayOf(FType.Int32);
            // 2                   1   0
            // y(a:int[]):int[] = self(a)
            solver.SetVarType("a", arrayOfInt);
            solver.SetVarType("y()", FType.Fun(arrayOfInt, arrayOfInt));

            solver.SetFunDefenition("y()", 2, 1).AssertSuccesfully();;
            
            Assert.IsTrue(solver.SetVar(0, "a"));
            Assert.IsTrue(solver.SetCall(new CallDef(FType.Generic(0), new[] {1, 0})));

            var solvation = solver.Solve();
            Assert.IsTrue(solvation.IsSolved);
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(arrayOfInt, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Fun(arrayOfInt, arrayOfInt), solvation.GetVarType("y()"));
        }
        
        [Test]
        public void DefaultWithFullFunctionalTypeSpecificationOfArray_SetDefAtStart_EquationSolved()
        {
            var arrayOfInt = FType.ArrayOf(FType.Int32);

            // 1              0
            // y():int[] = default()
            solver.SetVarType("y()", FType.Fun(arrayOfInt));

            solver.SetFunDefenition("y()", 1, 0).AssertSuccesfully();;
            Assert.IsTrue(solver.SetCall(new CallDef(FType.Generic(0), new[] {0})));

            var solvation = solver.Solve();
            Assert.IsTrue(solvation.IsSolved);
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.Fun(arrayOfInt), solvation.GetVarType("y()"));
        }
        
        [Test]
        public void DefaultWithFullFunctionalTypeSpecificationOfPrimitive_SetDefAtStart_EquationSolved()
        {
            // 1              0
            // y():int = default()
            solver.SetVarType("y()", FType.Fun(FType.Int32));

            solver.SetFunDefenition("y()", 1, 0).AssertSuccesfully();;
            Assert.IsTrue(solver.SetCall(new CallDef(FType.Generic(0), new[] {0})));

            var solvation = solver.Solve();
            Assert.IsTrue(solvation.IsSolved);
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.Fun(FType.Int32), solvation.GetVarType("y()"));
        }
        [Test]
        public void ImplicitCast_EquationSolved()
        {
            //     1           0
            // y(i:int):real = i
            solver.SetVarType("i", FType.Int32);
            solver.SetVarType("y()", FType.Fun(FType.Real, FType.Int32));

            Assert.IsTrue(solver.SetVar(0, "i"));

            solver.SetFunDefenition("y()", 1, 0).AssertSuccesfully();

            var solvation = solver.Solve();
            Assert.IsTrue(solvation.IsSolved);
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.Int32, solvation.GetVarType("i"));
            Assert.AreEqual(FType.Fun(FType.Real, FType.Int32), solvation.GetVarType("y()"));
        }
    }
}