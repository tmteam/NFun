using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests
{
    [TestFixture]
    public class SolveAdapterOverloadsTests
    {
        private TiLanguageSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new TiLanguageSolver();
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
            
            solver.SetConst(3, TiType.Int32);
            solver.SetDefenition("x", 4, 3);
            
            var res = solver.Solve();
            
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.Int32, res.GetVarType("x"));
            Assert.AreEqual(TiType.Text, res.GetVarType("y"));
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
            Assert.AreEqual(TiType.Int32, res.GetVarType("y"), "y");
            Assert.AreEqual(TiType.Int32, res.GetVarType("a"), "a");
            Assert.AreEqual(TiType.Int32, res.GetVarType("b"), "b");
        }
        
        [Test]
        public void SumOverloads_SpecifiedByArithmetical_EquationSolved()
        {
            //5     2  0 1 4 3 
            //y = Summ(a,b) + 2;
            
            solver.SetVar(0, "a");
            solver.SetVar(1, "b");
            
            solver.SetOverloadCall(SummOverloads,2,  0,1);
            solver.SetArithmeticalOp(4, 2, 3).AssertSuccesfully();
            
            solver.SetDefenition("y", 5, 4);
            
            var res = solver.Solve();
            
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.Real, res.GetVarType("y"));
            Assert.AreEqual(TiType.Real, res.GetVarType("a"));
            Assert.AreEqual(TiType.Real, res.GetVarType("b"));
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
            
            solver.SetConst(6,TiType.Int32);
            solver.SetDefenition("a", 7, 6);
            
            var res = solver.Solve();
            
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.Int32, res.GetVarType("y"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("a"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("b"));
        }
        [Test]
        public void SumOverloads_SpecfiedWithArithmetical_SomeArgsAreKnown_EquationSolved()
        {
            //5     2  0 1 4 3  7   6
            //y = Summ(a,b)+ 2; a = 1
            
            solver.SetVar(0, "a");
            solver.SetVar(1, "b");
            
            solver.SetOverloadCall(SummOverloads,2,  0,1);
            solver.SetArithmeticalOp(4, 2, 3).AssertSuccesfully();
            
            solver.SetDefenition("y", 5, 4);
            
            solver.SetConst(6,TiType.Int32);
            solver.SetDefenition("a", 7, 6);
            
            var res = solver.Solve();
            
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.Real, res.GetVarType("y"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("a"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("b"));
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
            Assert.AreEqual(TiType.Real, res.GetVarType("y"));
            Assert.AreEqual(TiType.Real, res.GetVarType("a"));
            Assert.AreEqual(TiType.Real, res.GetVarType("b"));        }
        
        [Test]
        public void SingleOveload_calculatesWell()
        {
            //3     2  0 1
            //y = someFun(a,b);
            
            solver.SetVar(0, "a");
            solver.SetVar(1, "b");
            
            solver.SetOverloadCall(new []{new TiFunctionSignature(TiType.Int64, TiType.Bool,TiType.Any)},2,  0,1);
            
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);

            Assert.AreEqual(TiType.Int64, res.GetVarType("y"));
            Assert.AreEqual(TiType.Bool,  res.GetVarType("a"));
            Assert.AreEqual(TiType.Any,   res.GetVarType("b"));
        }
        [Test(Description = "y = -x; | y2 = -x")]
        public void TwoOutputsEqualToNegativeInputViaOverloads()
        {
            //node |2   1 0 | 5   4 3
            //expr |y = -x; | y2 = -x
            
            solver.SetVar( 0,"x");
            solver.SetOverloadCall(InvertOverloads, 1, 0);
            solver.SetDefenition("y",2, 1);
            
            
            solver.SetVar( 3,"x");
            solver.SetOverloadCall(InvertOverloads, 4, 3);
            solver.SetDefenition("y2",5, 4);
            
            var result = solver.Solve();
            
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(0, result.GenericsCount);
            
            Assert.AreEqual(TiType.Real, result.GetVarType("x"));
            Assert.AreEqual(TiType.Real, result.GetVarType("y"));
            Assert.AreEqual(TiType.Real, result.GetVarType("y2"));
        }
        private TiFunctionSignature[] InvertOverloads =>new[]
        {
            new TiFunctionSignature(TiType.Int32, TiType.Int32),
            new TiFunctionSignature(TiType.Int64, TiType.Int64),
            new TiFunctionSignature(TiType.Real, TiType.Real),

        };
        private TiFunctionSignature[] SummOverloads => new[]
        {
            new TiFunctionSignature(TiType.Int32, TiType.Int32,TiType.Int32),
            new TiFunctionSignature(TiType.Int64, TiType.Int64,TiType.Int64),
            new TiFunctionSignature(TiType.Real, TiType.Real,TiType.Real),

        };
        private TiFunctionSignature[] ToStrOverloads => new[]
        {
            new TiFunctionSignature(TiType.Text, TiType.Int32),
            new TiFunctionSignature(TiType.Text, TiType.Int64),
            new TiFunctionSignature(TiType.Text, TiType.Real),
            new TiFunctionSignature(TiType.Text, TiType.ArrayOf(TiType.Any)),
            new TiFunctionSignature(TiType.Text, TiType.Any)
        };
    }
}