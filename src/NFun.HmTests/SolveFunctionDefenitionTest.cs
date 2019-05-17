using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests
{
    public class SolveFunctionDefenitionTest
    {
        private NsHumanizerSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new NsHumanizerSolver();
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

            Assert.IsTrue(solver.SetFunDefenition("y()", 3, 2));

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

            Assert.IsTrue(solver.SetFunDefenition("y()", 3, 2));
            
            Assert.IsTrue(solver.SetVar(0, "a"));
            Assert.IsTrue(solver.SetVar(1, "a"));
            Assert.IsTrue(solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1})));


            var solvation = solver.Solve();
            Assert.IsTrue(solvation.IsSolved);
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(arrayOfInt, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Fun(arrayOfInt, arrayOfInt), solvation.GetVarType("y()"));
        }
    }
}