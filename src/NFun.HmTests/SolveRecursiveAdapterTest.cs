using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests
{
    public class SolveRecursiveAdapterTest
    {
        private HmHumanizerSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new HmHumanizerSolver();
        }
        [Test]
        public void OutEqualsToItself_SingleGenericFound()
        {
            //1   0
            //y = y
            solver.SetVar(0, "y");
            solver.SetDefenition("y", 1,0);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.AreEqual(FType.Generic(0),         res.GetVarType("y"));
        }
        [Test]
        public void OutEqualsToItself_TypeSpecified_EquationSolved()
        {
            //        1   0
            //y:bool; y = y
            solver.SetVarType("y", FType.Bool);
            solver.SetVar(0, "y");
            solver.SetDefenition("y", 1,0);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Bool, res.GetVarType("y"));
        }
        [Test]
        public void OutEqualsToItself_TypeLimitedAfter_EquationSolved()
        {
            // 1   0  3   2
            // y = y; y = 1;
            solver.SetVar(0, "y");
            solver.SetDefenition("y", 1,0);
            solver.SetConst(2,FType.Int32);
            solver.SetDefenition("y", 3,2);

            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Int32, res.GetVarType("y"));
        }
        [Test]
        public void OutEqualsToItself_TypeLimitedBefore_EquationSolved()
        {
            // 1   0  3   2
            // y = 1; y = y; 
            solver.SetConst(0,FType.Int32);
            solver.SetDefenition("y", 1,0);

            solver.SetVar(2, "y");
            solver.SetDefenition("y", 3,2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Int32, res.GetVarType("y"));
        }

        [Test]
        public void CircularDependenciesWithEquation_SingleGenericFound()
        {
            //3   021  7   465   
            //a = b*c; b = c*a; 
            solver.SetVar(0, "b");
            solver.SetVar(1, "c");
            solver.SetArithmeticalOp(2, 0, 1);
            solver.SetDefenition("a", 3, 2);

            solver.SetVar(4, "c");
            solver.SetVar(5, "a");
            solver.SetArithmeticalOp(6, 4, 5);
            solver.SetDefenition("b", 4, 5);

            var res = solver.Solve();

            Assert.AreEqual(0, res.GenericsCount);
            Assert.AreEqual(FType.Real, res.GetVarType("a"));
            Assert.AreEqual(FType.Real, res.GetVarType("b"));
            Assert.AreEqual(FType.Real, res.GetVarType("c"));
        }

        
        [Test]
        public void CircularDependencies_AllTypesSpecified_EquationSolved()
        {
            //1   0  3   2  5   4
            //a = b; b = c; c = a
            solver.SetVarType("a", FType.Bool);
            solver.SetVarType("b", FType.Bool);
            solver.SetVarType("c", FType.Bool);

            solver.SetVar(0, "b");
            solver.SetDefenition("a", 1,0);
            
            solver.SetVar(2, "c");
            solver.SetDefenition("a", 3,2);

            solver.SetVar(4, "a");
            solver.SetDefenition("a", 5,4);

            var res = solver.Solve();
            
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Bool,         res.GetVarType("a"));
            Assert.AreEqual(FType.Bool,         res.GetVarType("b"));
            Assert.AreEqual(FType.Bool,         res.GetVarType("c"));
        }
        
        [Test]
        public void CircularDependencies_SingleGenericFound()
        {
            //1   0  3   2  5   4
            //a = b; b = c; c = a
            solver.SetVar(0, "b");
            solver.SetDefenition("a", 1,0);
            
            solver.SetVar(2, "c");
            solver.SetDefenition("a", 3,2);

            solver.SetVar(4, "a");
            solver.SetDefenition("a", 5,4);

            var res = solver.Solve();
            
            Assert.AreEqual(1,res.GenericsCount);
            Assert.AreEqual(FType.Generic(0),         res.GetVarType("a"));
            Assert.AreEqual(FType.Generic(0),         res.GetVarType("b"));
            Assert.AreEqual(FType.Generic(0),         res.GetVarType("c"));
        }
        
        [Test]
        public void OutDependsOnItself_Resolved()
        {
            //3   0 2 1
            //y = y + 2
            solver.SetVar(0, "y");
            solver.SetConst(1,FType.Int32);
            solver.SetArithmeticalOp(2, 0, 1);
            
            solver.SetDefenition("y", 3,2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Real,         res.GetVarType("y"));
        }
    }
}