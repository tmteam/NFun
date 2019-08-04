using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests
{
    public class SolveAdapterAnonymFunctionsTest
    {
        private TiLanguageSolver _solver;

        [SetUp]
        public void Init()
        {
            _solver = new TiLanguageSolver();
        }
        [Test]
        public void FilterFunction_Resolved()
        {
            //6      0    5     4 132
            //y = [0,2].filter(x->x>0)
            _solver.SetConst(0, TiType.ArrayOf(TiType.Int32));
            
            var xGeneric =  _solver.SetNewVarOrThrow("4:x");
            _solver.InitLambda(4, 3, new[] {xGeneric}).AssertSuccesfully();
            
            _solver.SetVar(1, "4:x");
            _solver.SetConst(2, TiType.Int32);
            _solver.SetCall(new CallDefenition(new[] {TiType.Bool, TiType.Int32, TiType.Int32}, new []{3,1,2}));
            

            _solver.SetCall(new CallDefenition(new[]
            {
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.Fun(TiType.Bool,TiType.Generic(0)),
            }, new[] {5, 0, 4}));
            
            _solver.SetDefenition("y", 6, 5);
            
            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32),         res.GetVarType("y"));
            Assert.AreEqual(TiType.Fun(TiType.Bool,TiType.Int32), res.GetNodeType(4));
            Assert.AreEqual(TiType.Int32, res.GetVarType("4:x"));
            
        }

        [Test]
        public void MultiSolvingWithConcreteMap_Resolved()
        {
            //6      0    5  4 132
            //y = [0,2].map(x->x>0)
            _solver.SetConst(0, TiType.ArrayOf(TiType.Int32));
            
            var xGeneric =  _solver.SetNewVarOrThrow("4:x");
            _solver.InitLambda(4, 3, new[] {xGeneric}).AssertSuccesfully();
            
            _solver.SetVar(1, "4:x");
            _solver.SetConst(2, TiType.Int32);
            _solver.SetCall(new CallDefenition(new[] {TiType.Bool, TiType.Int32, TiType.Int32}, new []{3,1,2}));
            

            _solver.SetCall(new CallDefenition(new[]
            {
                TiType.ArrayOf(TiType.Generic(1)),
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.Fun(TiType.Generic(1),TiType.Generic(0)),
            }, new[] {5, 0, 4}));
            
            _solver.SetDefenition("y", 6, 5);
            
            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Bool),        res.GetVarType("y"));
            Assert.AreEqual(TiType.Fun(TiType.Bool,TiType.Int32), res.GetNodeType(4));
            Assert.AreEqual(TiType.Int32, res.GetVarType("4:x"));
            
        }

        [Test]
        public void MultiSolvingWithComparationMap_Resolved()
        {
            //6      0    5  4 132
            //y = [0,2].map(x->x>0)
            _solver.SetConst(0, TiType.ArrayOf(TiType.Int32));

            var xGeneric = _solver.SetNewVarOrThrow("4:x");
            _solver.InitLambda(4, 3, new[] { xGeneric }).AssertSuccesfully();

            _solver.SetVar(1, "4:x");
            _solver.SetConst(2, TiType.Int32);
            _solver.SetComparationOperator(3, 1, 2);


            _solver.SetCall(new CallDefenition(new[]
            {
                TiType.ArrayOf(TiType.Generic(1)),
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.Fun(TiType.Generic(1),TiType.Generic(0)),
            }, new[] { 5, 0, 4 }));

            _solver.SetDefenition("y", 6, 5);

            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0, res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Bool), res.GetVarType("y"));
            Assert.AreEqual(TiType.Fun(TiType.Bool, TiType.Int32), res.GetNodeType(4));
            Assert.AreEqual(TiType.Int32, res.GetVarType("4:x"));

        }

        [Test]
        public void MultiSolvingWithFilterAndComparation_Resolved()
        {
            //6      0    5    4  132
            //y = [0,2].filter(x->x>0)
            _solver.SetConst(0, TiType.ArrayOf(TiType.Int32));

            var xGeneric = _solver.SetNewVarOrThrow("4:x");
            _solver.InitLambda(4, 3, new[] { xGeneric }).AssertSuccesfully();

            _solver.SetVar(1, "4:x");
            _solver.SetConst(2, TiType.Int32);
            _solver.SetComparationOperator(3, 1, 2);

            _solver.SetCall(new CallDefenition(new[]
            {
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.Fun(TiType.Bool,TiType.Generic(0)),
            }, new[] { 5, 0, 4 }));

            _solver.SetDefenition("y", 6, 5);

            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0, res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), res.GetVarType("y"));
            Assert.AreEqual(TiType.Fun(TiType.Bool, TiType.Int32), res.GetNodeType(4));
            Assert.AreEqual(TiType.Int32, res.GetVarType("4:x"));
        }

        [Test]
        public void MultiSolvingWithFakeAndComparation_Resolved()
        {
            //6   0       5    4  132
            //y = 1.returnSelf(x->x>0) //returns sameType in any case
            _solver.SetConst(0, TiType.Int32);

            var xGeneric = _solver.SetNewVarOrThrow("4:x");
            _solver.InitLambda(4, 3, new[] { xGeneric }).AssertSuccesfully();

            _solver.SetVar(1, "4:x");
            _solver.SetConst(2, TiType.Int32);
            _solver.SetComparationOperator(3, 1, 2);

            _solver.SetCall(new CallDefenition(new[]
            {
                TiType.Generic(0),
                TiType.Generic(0),
                TiType.Fun(TiType.Bool,TiType.Generic(0)),
            }, new[] { 5, 0, 4 }));

            _solver.SetDefenition("y", 6, 5);

            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0, res.GenericsCount);
            Assert.AreEqual(TiType.Int32, res.GetVarType("y"));
            Assert.AreEqual(TiType.Fun(TiType.Bool, TiType.Int32), res.GetNodeType(4));
            Assert.AreEqual(TiType.Int32, res.GetVarType("4:x"));
        }

        [Test]
        public void MultiSolvingWithMapAndClosure_Resolved()
        {
            //6   0  5  4  132
            //y = a.Map(x->x*input)
            _solver.SetVar(0,"a");
            
            var xGeneric =  _solver.SetNewVarOrThrow("4:x");
            _solver.InitLambda(4, 3, new[] {xGeneric}).AssertSuccesfully();
            
            _solver.SetVar(1, "4:x");
            _solver.SetVar(2, "input");
            _solver.SetArithmeticalOp(3, 1, 2).AssertSuccesfully();

            _solver.SetCall(new CallDefenition(new[]
            {
                TiType.ArrayOf(TiType.Generic(1)),
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.Fun(TiType.Generic(1),TiType.Generic(0)),
            }, new[] {5, 0, 4}));
            
            _solver.SetDefenition("y", 6, 5);
            
            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Real),        res.GetVarType("y"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Real),        res.GetVarType("a"));
            Assert.AreEqual(TiType.Real,        res.GetVarType("input"));
            Assert.AreEqual(TiType.Fun(TiType.Real,TiType.Real), res.GetNodeType(4));
            Assert.AreEqual(TiType.Real, res.GetVarType("4:x"));
        }
        
        
        [Test]
        public void MultiSolvingWithMapAndClosure_argTypeSpecified_Resolved()
        {
            //6   0  5      4  132
            //y = a.Map(x:int->x*x)
            _solver.SetVar(0,"a");
            
            _solver.SetVarType("4:x", TiType.Int32);
            var xType = _solver.GetOrCreate("4:x");
            _solver.InitLambda(4, 3, new[] {xType}).AssertSuccesfully();
            
            _solver.SetVar(1, "4:x");
            _solver.SetVar(2, "4:x");
            _solver.SetArithmeticalOp(3, 1, 2).AssertSuccesfully();

            _solver.SetCall(new CallDefenition(new[]
            {
                TiType.ArrayOf(TiType.Generic(1)),
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.Fun(TiType.Generic(1),TiType.Generic(0)),
            }, new[] {5, 0, 4}));
            
            _solver.SetDefenition("y", 6, 5);
            
            var res = _solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32),        res.GetVarType("y"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32),        res.GetVarType("a"));
            Assert.AreEqual(TiType.Fun(TiType.Int32,TiType.Int32), res.GetNodeType(4));
            Assert.AreEqual(TiType.Int32, res.GetVarType("4:x"));
        }
    }
}