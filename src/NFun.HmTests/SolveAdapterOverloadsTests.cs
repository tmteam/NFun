using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests
{
    [TestFixture]
    public class SolveAdapterOverloadsTests
    {
        private NsHumanizerSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new NsHumanizerSolver();
        }
        [Test]
        public void ToStrOverload_TypeSpecified_equationSolved()
        {
            //ToStr(...)//int, real, bool, arrayof(T)
            //2    1    0   4   3
            //y = ToStr(x); x = 2
            
            solver.SetVar(0, "x");
            solver.SetOverloadCall(ToStrOverloads, 1, 0);
            solver.SetDefenition("y", 2, 1);
            
            solver.SetConst(3, FType.Int32);
            solver.SetDefenition("x", 4, 3);
            
            var res = solver.Solve();
            
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Int32, res.GetVarType("x"));
            Assert.AreEqual(FType.Text, res.GetVarType("y"));
        }
        [Test]
        public void SumOverloads_EquationSolved()
        {
            //5     2  0 1 4 3 
            //y = Summ(a,b)<<2;
            
            solver.SetVar(0, "a");
            solver.SetVar(1, "b");
            
            solver.SetOverloadCall(SummOverloads,2,  0,1);
            solver.SetBitShiftOperator(4, 2, 3);
            
            solver.SetDefenition("y", 5, 4);
            
            var res = solver.Solve();
            
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Int32, res.GetVarType("y"));
            Assert.AreEqual(FType.Int32, res.GetVarType("a"));
            Assert.AreEqual(FType.Int32, res.GetVarType("b"));
        }
        
        [Test]
        public void SumOverloads_SpecifiedByArithmetical_EquationSolved()
        {
            //5     2  0 1 4 3 
            //y = Summ(a,b) + 2;
            
            solver.SetVar(0, "a");
            solver.SetVar(1, "b");
            
            solver.SetOverloadCall(SummOverloads,2,  0,1);
            solver.SetArithmeticalOp(4, 2, 3);
            
            solver.SetDefenition("y", 5, 4);
            
            var res = solver.Solve();
            
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Real, res.GetVarType("y"));
            Assert.AreEqual(FType.Real, res.GetVarType("a"));
            Assert.AreEqual(FType.Real, res.GetVarType("b"));
        }
        
        [Test]
        public void SumOverloads_SomeArgsAreKnown_EquationSolved()
        {
            //5     2  0 1 4 3  7   6
            //y = Summ(a,b)<<2; a = 1
            
            solver.SetVar(0, "a");
            solver.SetVar(1, "b");
            
            solver.SetOverloadCall(SummOverloads,2,  0,1);
            solver.SetBitShiftOperator(4, 2, 3);
            
            solver.SetDefenition("y", 5, 4);
            
            solver.SetConst(6,FType.Int32);
            solver.SetDefenition("a", 7, 6);
            
            var res = solver.Solve();
            
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Int32, res.GetVarType("y"));
            Assert.AreEqual(FType.Int32, res.GetVarType("a"));
            Assert.AreEqual(FType.Int32, res.GetVarType("b"));
        }
        [Test]
        public void SumOverloads_SpecfiedWithArithmetical_SomeArgsAreKnown_EquationSolved()
        {
            //5     2  0 1 4 3  7   6
            //y = Summ(a,b)+ 2; a = 1
            
            solver.SetVar(0, "a");
            solver.SetVar(1, "b");
            
            solver.SetOverloadCall(SummOverloads,2,  0,1);
            solver.SetArithmeticalOp(4, 2, 3);
            
            solver.SetDefenition("y", 5, 4);
            
            solver.SetConst(6,FType.Int32);
            solver.SetDefenition("a", 7, 6);
            
            var res = solver.Solve();
            
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Real, res.GetVarType("y"));
            Assert.AreEqual(FType.Int32, res.GetVarType("a"));
            Assert.AreEqual(FType.Int32, res.GetVarType("b"));
        }
        [Test]
        public void SumOverloads_TypeNotSolved()
        {
            //3     2  0 1
            //y = Summ(a,b);
            
            solver.SetVar(0, "a");
            solver.SetVar(1, "b");
            
            solver.SetOverloadCall(SummOverloads,2,  0,1);
            
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.Real, res.GetVarType("y"));
            Assert.AreEqual(FType.Real, res.GetVarType("a"));
            Assert.AreEqual(FType.Real, res.GetVarType("b"));        }
        
        [Test]
        public void SingleOveload_calculatesWell()
        {
            //3     2  0 1
            //y = someFun(a,b);
            
            solver.SetVar(0, "a");
            solver.SetVar(1, "b");
            
            solver.SetOverloadCall(new []{new FunSignature(FType.Int64, FType.Bool,FType.Any)},2,  0,1);
            
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);

            Assert.AreEqual(FType.Int64, res.GetVarType("y"));
            Assert.AreEqual(FType.Bool,  res.GetVarType("a"));
            Assert.AreEqual(FType.Any,   res.GetVarType("b"));
        }
        
        private FunSignature[] SummOverloads => new[]
        {
            new FunSignature(FType.Int32, FType.Int32,FType.Int32),
            new FunSignature(FType.Int64, FType.Int64,FType.Int64),
            new FunSignature(FType.Real, FType.Real,FType.Real),

        };
        private FunSignature[] ToStrOverloads => new[]
        {
            new FunSignature(FType.Text, FType.Int32),
            new FunSignature(FType.Text, FType.Int64),
            new FunSignature(FType.Text, FType.Real),
            new FunSignature(FType.Text, FType.ArrayOf(FType.Any)),
            new FunSignature(FType.Text, FType.Any)
        };
    }
}