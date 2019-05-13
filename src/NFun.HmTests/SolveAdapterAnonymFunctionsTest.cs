using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace TysoTake2.TypeSolvingNodes.Tests
{
    public class SolveAdapterAnonymFunctionsTest
    {
        private NsHumanizerSolver _solver;

        [SetUp]
        public void Init()
        {
            _solver = new NsHumanizerSolver();
        }
        [Test]
        public void FilterFunction_Resolved()
        {
            //6      0    5     4 132
            //y = [0,2].filter(x=>x>0)
            _solver.SetConst(0, FType.ArrayOf(FType.Int32));
            
            var xGeneric =  _solver.SetNewVar("4:x");
            Assert.IsTrue(_solver.InitLambda(4, 3, new[] {xGeneric}));
            
            _solver.SetVar(1, "4:x");
            _solver.SetConst(2, FType.Int32);
            _solver.SetCall(new CallDef(new[] {FType.Bool, FType.Int32, FType.Int32}, new []{3,1,2}));
            

            _solver.SetCall(new CallDef(new[]
            {
                FType.ArrayOf(FType.Generic(0)),
                FType.ArrayOf(FType.Generic(0)),
                FType.Fun(FType.Bool,FType.Generic(0)),
            }, new[] {5, 0, 4}));
            
            _solver.SetDefenition("y", 6, 5);
            
            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32),         res.GetVarType("y"));
            Assert.AreEqual(FType.Fun(FType.Bool,FType.Int32), res.GetNodeType(4));
            Assert.AreEqual(FType.Int32, res.GetVarType("4:x"));
            
        }

        [Test]
        public void MultiSolvingWithMap_Resolved()
        {
            //6      0    5  4 132
            //y = [0,2].map(x=>x>0)
            _solver.SetConst(0, FType.ArrayOf(FType.Int32));
            
            var xGeneric =  _solver.SetNewVar("4:x");
            Assert.IsTrue(_solver.InitLambda(4, 3, new[] {xGeneric}));
            
            _solver.SetVar(1, "4:x");
            _solver.SetConst(2, FType.Int32);
            _solver.SetCall(new CallDef(new[] {FType.Bool, FType.Int32, FType.Int32}, new []{3,1,2}));
            

            _solver.SetCall(new CallDef(new[]
            {
                FType.ArrayOf(FType.Generic(1)),
                FType.ArrayOf(FType.Generic(0)),
                FType.Fun(FType.Generic(1),FType.Generic(0)),
            }, new[] {5, 0, 4}));
            
            _solver.SetDefenition("y", 6, 5);
            
            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Bool),        res.GetVarType("y"));
            Assert.AreEqual(FType.Fun(FType.Bool,FType.Int32), res.GetNodeType(4));
            Assert.AreEqual(FType.Int32, res.GetVarType("4:x"));
            
        }

        [Test]
        public void MultiSolvingWithMapAndClosure_Resolved()
        {
            //6   0  5  4  132
            //y = a.Map(x=>x*input)
            _solver.SetVar(0,"a");
            
            var xGeneric =  _solver.SetNewVar("4:x");
            Assert.IsTrue(_solver.InitLambda(4, 3, new[] {xGeneric}));
            
            _solver.SetVar(1, "4:x");
            _solver.SetVar(2, "input");
            _solver.SetArithmeticalOp(3, 1, 2);

            _solver.SetCall(new CallDef(new[]
            {
                FType.ArrayOf(FType.Generic(1)),
                FType.ArrayOf(FType.Generic(0)),
                FType.Fun(FType.Generic(1),FType.Generic(0)),
            }, new[] {5, 0, 4}));
            
            _solver.SetDefenition("y", 6, 5);
            
            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Real),        res.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Real),        res.GetVarType("a"));
            Assert.AreEqual(FType.Real,        res.GetVarType("input"));
            Assert.AreEqual(FType.Fun(FType.Real,FType.Real), res.GetNodeType(4));
            Assert.AreEqual(FType.Real, res.GetVarType("4:x"));
        }
        
        
        [Test]
        public void MultiSolvingWithMapAndClosure_argTypeSpecified_Resolved()
        {
            //6   0  5      4  132
            //y = a.Map(x:int=>x*x)
            _solver.SetVar(0,"a");
            
            _solver.SetVarType("4:x", FType.Int32);
            var xType = _solver.GetByVar("4:x");
            Assert.IsTrue(_solver.InitLambda(4, 3, new[] {xType}));
            
            _solver.SetVar(1, "4:x");
            _solver.SetVar(2, "4:x");
            _solver.SetArithmeticalOp(3, 1, 2);

            _solver.SetCall(new CallDef(new[]
            {
                FType.ArrayOf(FType.Generic(1)),
                FType.ArrayOf(FType.Generic(0)),
                FType.Fun(FType.Generic(1),FType.Generic(0)),
            }, new[] {5, 0, 4}));
            
            _solver.SetDefenition("y", 6, 5);
            
            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32),        res.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32),        res.GetVarType("a"));
            Assert.AreEqual(FType.Fun(FType.Int32,FType.Int32), res.GetNodeType(4));
            Assert.AreEqual(FType.Int32, res.GetVarType("4:x"));
        }
    }
}