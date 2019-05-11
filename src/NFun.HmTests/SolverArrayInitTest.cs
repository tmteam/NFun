using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace TysoTake2.TypeSolvingNodes.Tests
{
    [TestFixture]
    public class SolverArrayInitTest
    {
        private NsHumanizerSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new NsHumanizerSolver();
        }
        [Test]
        public void EmptyArrayInit()
        {
            //1    0
            //y = []
            solver.SetArrayInit(0);
            solver.SetDefenition("y", 1, 0);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), res.GetVarType("y"));
        }
        [Test]
        public void ArrayOfVariables_Solved()
        {
            //       3   20 1
            //a:int; y = [a,b]
            solver.SetVarType("a", FType.Int32);
            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            solver.SetArrayInit(2,0,1);
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Int32, res.GetVarType("a"));
            Assert.AreEqual(FType.Int32, res.GetVarType("b"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), res.GetVarType("y"));
        }
        
        [Test]
        public void ArrayOfVariables_GenericFound()
        {
            //3   20 1
            //y = [a,b]
            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            solver.SetArrayInit(2,0,1);
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.AreEqual(FType.Generic(0), res.GetVarType("a"));
            Assert.AreEqual(FType.Generic(0), res.GetVarType("b"));
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), res.GetVarType("y"));
        }
        [Test]
        public void ArrayWithIntValuesSolvesWell()
        {
            //4   30 1 2 
            //y = [1,2,3]
            solver.SetConst(0, FType.Int32);
            solver.SetConst(1, FType.Int32);
            solver.SetConst(2, FType.Int32);
            solver.SetArrayInit(3, 0, 1, 2);
            solver.SetDefenition("y", 4, 3);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), res.GetVarType("y"));
        }
        
        [Test]
        public void VarInArray_VarTypeSolved()
        {
            //5   40 1 2 3
            //y = [1,2,3,x]
            solver.SetConst(0, FType.Int32);
            solver.SetConst(1, FType.Int32);
            solver.SetConst(2, FType.Int32);
            solver.SetVar(3, "x");

            solver.SetArrayInit(4, 0, 1, 2,3);
            solver.SetDefenition("y", 5, 4);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Int32, res.GetVarType("x"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), res.GetVarType("y"));
        }
        
        [Test]
        public void ProcArrayOfVariables_Solved()
        {
            //3   20  1
            //y = [a..b]
            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            solver.SetProcArrayInit(2,0,1);
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Int32, res.GetVarType("a"));
            Assert.AreEqual(FType.Int32, res.GetVarType("b"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), res.GetVarType("y"));
        }
        [Test]
        public void ProcArrayOfVariablesWithStep_SolvedAsReal()
        {
            //4   30  1  2
            //y = [a..b..c]
            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            solver.SetVar(2,"c");

            solver.SetProcArrayInit(3,0,1,2);
            solver.SetDefenition("y", 4, 3);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Real, res.GetVarType("a"));
            Assert.AreEqual(FType.Real, res.GetVarType("b"));
            Assert.AreEqual(FType.ArrayOf(FType.Real), res.GetVarType("y"));
        }
        [Test]
        public void ProcArrayOfConstantsWithStep_SolvedAsInt()
        {
            //4   30  1  2
            //y = [1..5..2]
            solver.SetConst(0,FType.Int32);
            solver.SetConst(1,FType.Int32);
            solver.SetConst(2,FType.Int32);

            solver.SetProcArrayInit(3,0,1,2);
            solver.SetDefenition("y", 4, 3);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), res.GetVarType("y"));
        }
       
        [Test]
        public void ProcArrayOfConstantsWithStep_SolvedAsReal()
        {
            //4   30  1    2
            //y = [1..5.0..2]
            solver.SetConst(0,FType.Int32);
            solver.SetConst(1,FType.Real);
            solver.SetConst(2,FType.Int32);

            solver.SetProcArrayInit(3,0,1,2);
            solver.SetDefenition("y", 4, 3);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Real), res.GetVarType("y"));
        }
        
        [Test]
        public void EqualsAsGeneric_TwoConstantsArray_ReturnsTrue()
        {
            //5    1 0  4  3 2
            //y = [ 1 ] == [ 1 ]
            solver.SetConst(0,FType.Int32);
            solver.SetArrayInit(1, 0);
            solver.SetConst(2,FType.Int32);
            solver.SetArrayInit(3, 2);

            Assert.IsTrue(solver.SetCall(new CallDef(new[] {FType.Bool, FType.Generic(0), FType.Generic(0)}, new[] {4, 1, 3})));
            Assert.IsTrue(solver.SetDefenition("y", 5, 4));
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Bool, res.GetVarType("y"));
        }
        
        
        [Test]
        public void EqualsAsObjects_TwoConstantsArray_ReturnsTrue()
        {
            //5    1 0  4  3 2
            //y = [ 1 ] == [ 1 ]
            solver.SetConst(0,FType.Int32);
            solver.SetArrayInit(1, 0);
            solver.SetConst(2,FType.Int32);
            solver.SetArrayInit(3, 2);

            Assert.IsTrue(solver.SetCall(new CallDef(new[] {FType.Bool, FType.Any, FType.Any}, new[] {4, 1, 3})));
            Assert.IsTrue(solver.SetDefenition("y", 5, 4));
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Bool, res.GetVarType("y"));
        }

    }
}