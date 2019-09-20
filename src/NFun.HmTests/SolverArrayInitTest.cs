using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests
{
    [TestFixture]
    public class SolverArrayInitTest
    {
        private TiLanguageSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new TiLanguageSolver();
        }
        [Test]
        public void EmptyArrayInit()
        {
            //1    0
            //y = []
            solver.SetArrayInit(0).AssertSuccesfully();
            solver.SetDefenition("y", 1, 0);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(0)), res.GetVarType("y"));
        }
        [Test]
        public void ArrayOfVariables_Solved()
        {
            //       3   20 1
            //a:int; y = [a,b]
            solver.SetVarType("a", TiType.Int32);
            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            solver.SetArrayInit(2,0,1).AssertSuccesfully();
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.Int32, res.GetVarType("a"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("b"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), res.GetVarType("y"));
        }
        
        [Test]
        public void ArrayOfVariables_GenericFound()
        {
            //3   20 1
            //y = [a,b]
            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            solver.SetArrayInit(2,0,1).AssertSuccesfully();
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(1,res.GenericsCount);
            Assert.AreEqual(TiType.Generic(0), res.GetVarType("a"));
            Assert.AreEqual(TiType.Generic(0), res.GetVarType("b"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(0)), res.GetVarType("y"));
        }
        [Test(Description = "y = [x]")]
        public void ArrayInit_GenericElement()
        {
            //node |2  1 0  
            //expr |y = [x]; 
            
            solver.SetVar( 0,"x");
            solver.SetArrayInit(1, 0).AssertSuccesfully();
            solver.SetDefenition("y",2, 1).AssertSuccesfully();
            
            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(1, result.GenericsCount);
            
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("x"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(0)), result.GetVarType("y"));
            

        }
        [Test(Description = "y = [x,-x]")]
        public void ArrayInit_WithNegativeInputs_Solved()
        {
            //node |3 2  10 
            //expr | [x, -x]
            
            solver.SetVar( 0,"x");
            solver.SetNegateOp(1,0).AssertSuccesfully();
            solver.SetVar( 2,"x");
            solver.SetArrayInit(3,1,2).AssertSuccesfully();
            
            var result = solver.Solve();
            
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(0, result.GenericsCount);
            
            Assert.AreEqual(TiType.Real, result.GetVarType("x"));
        }
        [Test(Description = "y = [-x,x]")]
        public void ArrayInit_WithNegativeInputs2_Solved()
        {
            //node |3 21  0 
            //expr | [-x, x]
            
            solver.SetVar( 0,"x");
            solver.SetVar( 1,"x");
            solver.SetNegateOp(2,1).AssertSuccesfully();
            solver.SetArrayInit(3,2,0).AssertSuccesfully();
            
            var result = solver.Solve();
            
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(0, result.GenericsCount);
            
            Assert.AreEqual(TiType.Real, result.GetVarType("x"));
        }

        [Test(Description = "y = [-x,x]")]
        public void ArrayInit_WithNegativeInputs3_Solved()
        {
            //node |3 0  21
            //expr | [x, -x]

            solver.SetVar( 0,"x");

            solver.SetVar( 1,"x");
            solver.SetNegateOp(2,1).AssertSuccesfully();

            solver.SetArrayInit(3,0,2).AssertSuccesfully();
            
            var result = solver.Solve();
            
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(0, result.GenericsCount);
            
            Assert.AreEqual(TiType.Real, result.GetVarType("x"));
        }

        [Test(Description = "y = [x]; a = [b,c]")]
        public void ArrayInit_twoComplexEquations_GenericElementsFound()
        {
            //node |2  1 0         6  5 3 4
            //expr |y = [x]    ;   a = [b,c]; 
            
            solver.SetVar( 0,"x");
            solver.SetArrayInit(1, 0).AssertSuccesfully();
            solver.SetDefenition("y",2, 1).AssertSuccesfully();

            
            solver.SetVar( 3,"b");
            solver.SetVar( 4,"c");
            solver.SetArrayInit(5, 3,4).AssertSuccesfully();
            solver.SetDefenition("a",6, 5).AssertSuccesfully();

            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(2, result.GenericsCount);
            
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("x"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(0)), result.GetVarType("y"));
            Assert.AreEqual(TiType.Generic(1), result.GetVarType("b"));
            Assert.AreEqual(TiType.Generic(1), result.GetVarType("c"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(1)), result.GetVarType("a"));
        }

        
        [Test]
        public void ArrayWithIntValuesSolvesWell()
        {
            //4   30 1 2 
            //y = [1,2,3]
            
            solver.SetConst(0, TiType.Int32);
            solver.SetConst(1, TiType.Int32);
            solver.SetConst(2, TiType.Int32);
            solver.SetArrayInit(3, 0, 1, 2).AssertSuccesfully();
            solver.SetDefenition("y", 4, 3);
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);

            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), res.GetVarType("y"));
        }
        
        [Test]
        public void VarInArray_VarTypeSolved()
        {
            //5   40 1 2 3
            //y = [1,2,3,x]
            solver.SetConst(0, TiType.Int32);
            solver.SetConst(1, TiType.Int32);
            solver.SetConst(2, TiType.Int32);
            solver.SetVar(3, "x");

            solver.SetArrayInit(4, 0, 1, 2,3);
            solver.SetDefenition("y", 5, 4);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.Int32, res.GetVarType("x"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), res.GetVarType("y"));
        }
        
        [Test]
        public void ProcArrayOfVariables_Solved()
        {
            //3   20  1
            //y = [a..b]
            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            Assert.IsTrue(solver.SetProcArrayInit(2,0,1));
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.Int32, res.GetVarType("a"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("b"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), res.GetVarType("y"));
        }
        [Test]
        public void ProcArrayOfVariablesWithStep_SolvedAsReal()
        {
            //4   30  1  2
            //y = [a..b..c]
            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            solver.SetVar(2,"c");

            Assert.IsTrue(solver.SetProcArrayInit(3,0,1,2));
            solver.SetDefenition("y", 4, 3);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.Real, res.GetVarType("a"));
            Assert.AreEqual(TiType.Real, res.GetVarType("b"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Real), res.GetVarType("y"));
        }
        [Test]
        public void ProcArrayOfConstantsWithStep_SolvedAsInt()
        {
            //4   30  1  2
            //y = [1..5..2]
            solver.SetConst(0,TiType.Int32);
            solver.SetConst(1,TiType.Int32);
            solver.SetConst(2,TiType.Int32);

            Assert.IsTrue(solver.SetProcArrayInit(3,0,1,2));
            solver.SetDefenition("y", 4, 3);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), res.GetVarType("y"));
        }
       
        [Test]
        public void ProcArrayOfConstantsWithStep_SolvedAsReal()
        {
            //4   30  1    2
            //y = [1..5.0..2]
            solver.SetConst(0,TiType.Int32);
            solver.SetConst(1,TiType.Real);
            solver.SetConst(2,TiType.Int32);

            solver.SetProcArrayInit(3,0,1,2);
            solver.SetDefenition("y", 4, 3);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Real), res.GetVarType("y"));
        }
        
        [Test]
        public void EqualsAsGeneric_TwoConstantsArray_ReturnsTrue()
        {
            //5    1 0  4  3 2
            //y = [ 1 ] == [ 1 ]
            solver.SetConst(0,TiType.Int32);
            solver.SetArrayInit(1, 0);
            solver.SetConst(2,TiType.Int32);
            solver.SetArrayInit(3, 2);

            Assert.IsTrue(solver.SetCall(new CallDefenition(new[] {TiType.Bool, TiType.Generic(0), TiType.Generic(0)}, new[] {4, 1, 3})));
            solver.SetDefenition("y", 5, 4).AssertSuccesfully();
            
            var res = solver.Solve();
            
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.Bool, res.GetVarType("y"));
        }
        
        
        [Test]
        public void EqualsAsObjects_TwoConstantsArray_ReturnsTrue()
        {
            //5    1 0  4  3 2
            //y = [ 1 ] == [ 1 ]
            solver.SetConst(0,TiType.Int32);
            solver.SetArrayInit(1, 0);
            solver.SetConst(2,TiType.Int32);
            solver.SetArrayInit(3, 2);

            Assert.IsTrue(solver.SetCall(new CallDefenition(new[] {TiType.Bool, TiType.Any, TiType.Any}, new[] {4, 1, 3})));
            solver.SetDefenition("y", 5, 4).AssertSuccesfully();
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(TiType.Bool, res.GetVarType("y"));
        }

    }
}