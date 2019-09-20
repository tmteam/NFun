using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests
{
    public class SolveAdapterAnonymFunctionsTestOLD
    
    {
        private TiLanguageSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new TiLanguageSolver();
        }
        [Test]
        public void FilterFunction_Resolved()
        {
            //3      0    2       1
            //y = [0,2].filter(x=>x>0)
            solver.SetConst(0, TiType.ArrayOf(TiType.Int32));
            solver.SetStrict(1, TiType.Fun(TiType.Generic(0), TiType.Bool));
            solver.SetCall(new CallDefenition(new[]
            {
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.Fun(TiType.Generic(0), TiType.Bool),
            }, new[] {2, 0, 1}));
            
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32),         res.GetVarType("y"));
            Assert.AreEqual(TiType.Fun(TiType.Int32, TiType.Bool), res.GetNodeType(1));
        }
        [Test]
        public void MultiSolvingWithMapAndClosure_Resolved()
        {
            //3   0  2    1
            //y = a.Map(x=>x*input)

            solver.SetVar(0, "a");
            //######### SOLVING ANONYMOUS ################
            var anonymFunSolver = new TiLanguageSolver();
            // 3    021
            //out = x>input
            anonymFunSolver.SetVar(0, "x");
            var closured = solver.GetOrCreate("input");

            anonymFunSolver.SetNode(1,closured);
            anonymFunSolver.SetArithmeticalOp(2, 0, 1).AssertSuccesfully();

            var anonymSolve = anonymFunSolver.Solve();
            var anonymFunDef =  anonymSolve.MakeFunDefenition();
            //###############################################
            solver.SetStrict(1, anonymFunDef);
            Assert.IsTrue(solver.SetCall(new CallDefenition(new[]
            {
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.ArrayOf(TiType.Generic(1)),
                TiType.Fun(TiType.Generic(0),TiType.Generic(1)), 
            }, new[] {2, 0, 1})));
            
            solver.SetDefenition("y", 3, 2).AssertSuccesfully();
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Real),         res.GetVarType("y"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Real),         res.GetVarType("a"));
            Assert.AreEqual(TiType.Real,         res.GetVarType("input"));
        }
    }
}