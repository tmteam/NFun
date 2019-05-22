using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests
{
    public class SolveGenericsAdapterTest
    {
        private NsHumanizerSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new NsHumanizerSolver();
        }
        
        [Test(Description = "y = x")]
        public void OneSimpleGenerics()
        {
            //node |1   0  
            //expr |y = x; 
            
            solver.SetVar( 0,"x");
            solver.SetDefenition("y",1, 0);
            
            var result = solver.Solve();
            
            Assert.AreEqual(1, result.GenericsCount);
            
            Assert.AreEqual(FType.Generic(0), result.GetVarType("x"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("y"));
            

        }
        [Test(Description = "y = x; | y2 = x2")]
        public void TwoSimpleGenerics()
        {
            //node |1   0  | 3    2
            //expr |y = x; | y2 = x2
            
            solver.SetVar( 0,"x");
            solver.SetDefenition("y",1, 0);
            solver.SetVar( 2,"x2");
            solver.SetDefenition("y2",3, 2);
            
            var result = solver.Solve();
            
            Assert.AreEqual(2, result.GenericsCount);
            
            Assert.AreEqual(FType.Generic(0), result.GetVarType("x"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("y"));
            
            Assert.AreEqual(FType.Generic(1), result.GetVarType("x2"));
            Assert.AreEqual(FType.Generic(1), result.GetVarType("y2"));
        }
        
        [Test]
        public void TwoGenericsUniteByIf_SingleGenericFound()
        {
            //node |4   3   0    1      2
            //expr |y = if(true) a else b
            solver.SetConst(0, FType.Bool);
            
            solver.SetVar( 1,"a");
            solver.SetVar( 2,"b");
            solver.ApplyLcaIf(3, new[] {0}, new[] {1, 2});
            
            solver.SetDefenition("y",4, 3);
            
            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(1, result.GenericsCount);
            
            Assert.AreEqual(FType.Generic(0), result.GetVarType("a"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("b"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("y"));
        }
       
        [Test(Description = "y1 = x1; y2 = [x2]")]
        public void ArrayInit_TwoEquationsWithGeneric_GenericElementsFound()
        {
            //node |1    0   4    3 2
            //expr |y1 = x1; y2 = [x2]; 

            solver.SetVar(0, "x1");
            Assert.IsTrue(solver.SetDefenition("y1", 1, 0));

            solver.SetVar(2, "x2");
            Assert.IsTrue(solver.SetArrayInit(3, 2));
            Assert.IsTrue(solver.SetDefenition("y2", 4, 3));

            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, result.GenericsCount);

                Assert.AreEqual(FType.Generic(0), result.GetVarType("x1"));
                Assert.AreEqual(FType.Generic(0), result.GetVarType("y1"));

                Assert.AreEqual(FType.Generic(1), result.GetVarType("x2"));
                Assert.AreEqual(FType.ArrayOf(FType.Generic(1)), result.GetVarType("y2"));
            });
        }
        
        [Test(Description = "y1 = [x1]; y2 = [x2]; y3 = [x3];")]
        public void ArrayInit_ThreeEquations_GenericElementsFound()
        {
            //node |2   1 0     5   4 3    8   7 6
            //expr |y1 = [x1]; y2 = [x2]; y3 = [x3]       

            solver.SetVar(0, "x1");
            Assert.IsTrue(solver.SetArrayInit(1, 0));
            Assert.IsTrue(solver.SetDefenition("y1", 2, 1));

            solver.SetVar(3, "x2");
            Assert.IsTrue(solver.SetArrayInit(4, 3));
            Assert.IsTrue(solver.SetDefenition("y2", 5, 4));

            solver.SetVar(6, "x3");
            Assert.IsTrue(solver.SetArrayInit(7, 6));
            Assert.IsTrue(solver.SetDefenition("y3", 8, 7));

            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(3, result.GenericsCount);

                Assert.AreEqual(FType.Generic(0), result.GetVarType("x1"));
                Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), result.GetVarType("y1"));

                Assert.AreEqual(FType.Generic(1), result.GetVarType("x2"));
                Assert.AreEqual(FType.ArrayOf(FType.Generic(1)), result.GetVarType("y2"));

                Assert.AreEqual(FType.Generic(2), result.GetVarType("x3"));
                Assert.AreEqual(FType.ArrayOf(FType.Generic(2)), result.GetVarType("y3"));
            });
        }

        [Test]
        public void SimpliestGenericFunctionCall_SingleGenericFound()
        {
            // 3    2   0 1
            // y = rand(a,b) 
            solver.SetVar( 0,"a");
            solver.SetVar( 1,"b");
            solver.SetCall(new CallDef(FType.Generic(0), new[] {2,0, 1}));
            solver.SetDefenition("y", 3, 2);
            
            var solvation = solver.Solve();
            Assert.AreEqual(1, solvation.GenericsCount);
            Assert.AreEqual(FType.Generic(0), solvation.GetVarType("y"));
            Assert.AreEqual(FType.Generic(0), solvation.GetVarType("a"));
            Assert.AreEqual(FType.Generic(0), solvation.GetVarType("b"));
        }
        [Test]
        public void SimpliestGenericFunctionWithSingleResolvedArg_EquationSolved()
        {
            // 3    2   0 1
            // y = rand(a,1) 
            solver.SetVar( 0,"a");
            solver.SetConst( 1,FType.Int32);
            solver.SetCall(new CallDef(FType.Generic(0), new[] {2,0, 1}));
            solver.SetDefenition("y", 3, 2);
            
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.Int32, solvation.GetVarType("y"));
            Assert.AreEqual(FType.Int32, solvation.GetVarType("a"));
        }
        [Test]
        public void SimpliestGenericFunctionWithResolvedOutputArg_EquationSolved()
        {
            //  3             2  0 1
            // y(a,b):int = rand(a,b) 
            solver.SetVar( 0,"a");
            solver.SetVar( 1,"b");
            solver.SetCall(new CallDef(FType.Generic(0), new[] {2,0, 1}));
            solver.SetDefenition("y", 3, 2);
            solver.SetStrict(3, FType.Int32);
            
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.Int32, solvation.GetVarType("y"));
            Assert.AreEqual(FType.Int32, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Int32, solvation.GetVarType("b"));
        }
        [Test]
        public void SimpliestGenericFunctionWithSpecifiedOutputArg_EquationSolved()
        {
            //  3             2  0 1
            // y(a,b):int = rand(a,b) 
            solver.SetVarType("y", FType.Int32);
            solver.SetVar( 0,"a");
            solver.SetVar( 1,"b");
            solver.SetCall(new CallDef(FType.Generic(0), new[] {2,0, 1}));
            solver.SetDefenition("y", 3, 2);
            
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.Int32, solvation.GetVarType("y"));
            Assert.AreEqual(FType.Int32, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Int32, solvation.GetVarType("b"));
        }
        [Test]
        public void LimitCall_TwoGenericOperations_SingleGenericFound()
        {
            // 5          0  2 1  4  3
            // y(a,b,c) = a.or(b).or(c) 
            solver.SetVar( 0,"a");
            solver.SetCall(new CallDef(FType.Generic(0), new[] {2,0, 1}));
            solver.SetCall(new CallDef(FType.Generic(0), new[] {4, 2, 3}));
            solver.SetDefenition("y", 5, 4);
            
            var solvation = solver.Solve();
            Assert.AreEqual(1, solvation.GenericsCount);
            Assert.AreEqual(FType.Generic(0), solvation.GetVarType("y"));
        }
        
        [Test]
        public void LimitCall_ThreeGenericOperations_SingleGenericFound()
        {
            // 7          0  2 1  4  3   6 5
            // y(a,b,c) = a.or(b).or(c).or(d) 
            solver.SetVar( 0,"a");
            solver.SetCall(new CallDef(FType.Generic(0), new[] {2,0, 1}));
            solver.SetCall(new CallDef(FType.Generic(0), new[] {4, 2, 3}));
            solver.SetCall(new CallDef(FType.Generic(0), new[] {6, 4, 5}));

            solver.SetDefenition("y", 7, 6);
            
            var solvation = solver.Solve();
            Assert.AreEqual(1, solvation.GenericsCount);
            Assert.AreEqual(FType.Generic(0), solvation.GetVarType("y"));
        }
        
        [Test]
        public void GenericCalcFirst_ThenLimit_GenericSolved()
        {
            // 2   0  1  4  3  6   5
            //rnd( a, b) *  2; a = 1.0:real

            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            solver.SetCall(new CallDef(FType.Generic(0), new[] {2, 0, 1}));
            solver.SetConst(3, FType.Int32);
            solver.SetArithmeticalOp(4, 2, 3);
            solver.SetConst(5, FType.Real);
            Assert.True(solver.SetDefenition("a", 6, 5));
             
            var solvation = solver.Solve();
             
            Assert.AreEqual(FType.Real, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Real, solvation.GetVarType("b"));
            Assert.AreEqual(FType.Real, solvation.GetNodeType(2));
        }
        

        
        [Test]
        public void GenericCalcFirst_ThenMoreStrictLimit_GenericSolved()
        {
            // 2   0  1  4  3  6   5
            //rnd( a, b) *  2.0; a = 1:int

            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            solver.SetCall(new CallDef(FType.Generic(0), new[] {2, 0, 1}));
            solver.SetConst(2, FType.Real);
            solver.SetArithmeticalOp(4, 2, 3);
            solver.SetConst(5, FType.Int32);
            Assert.True(solver.SetDefenition("a", 6, 5));
             
            var solvation = solver.Solve();
             
            Assert.AreEqual(FType.Real, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Real, solvation.GetVarType("b"));
        }
        
        [Test]
        public void Generic_callOfTwoTypes_GenericTypeEqualToBaseType()
        {
            //3     2  0  1
            //a = rnd( 0.1, 1) 

            solver.SetConst(0,FType.Real);
            solver.SetConst(1,FType.Int32);
            solver.SetCall(new CallDef(FType.Generic(0), new[] {2, 0, 1}));
            Assert.True(solver.SetDefenition("a", 3, 2));
             
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);             
            Assert.IsTrue(solvation.IsSolved);             
            Assert.AreEqual(FType.Real, solvation.GetVarType("a"));
        }
        
        
        [Test]
        public void Generic_callOfTwoTypesReversed_GenericTypeEqualToBaseType()
        {
            //3     2  0  1
            //a = rnd(1,0.1) 

            solver.SetConst(0,FType.Int32);
            solver.SetConst(1,FType.Real);
            Assert.True(solver.SetCall(new CallDef(FType.Generic(0), new[] {2, 0, 1})));
            Assert.True(solver.SetDefenition("a", 3, 2));
             
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);             
            Assert.IsTrue(solvation.IsSolved);             

            Assert.AreEqual(FType.Real, solvation.GetVarType("a"));
        }
        
        
        
        [Test]
        public void GenericFirst_LimitSetAfter_OriginGenericSolved()
        {
            // 2   0  1    4   3   
            //rnd( a, b) ; a = 1:int

            solver.SetVar(0,"a");
            solver.SetVar(1,"b");
            solver.SetCall(new CallDef(FType.Generic(0), new[] {2, 0, 1}));
            solver.SetConst(3, FType.Int32);
            
            Assert.True(solver.SetDefenition("a", 4, 3));
             
            var solvation = solver.Solve();
             
            Assert.AreEqual(FType.Int32, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Int32, solvation.GetVarType("b"));
            Assert.AreEqual(FType.Int32, solvation.GetNodeType(2));
        }
        
        [Test]
        public void GenericSet_LimitChangedTwiceAfterGeneric_OriginGenericSolved()
        {
            //1   0       4   2  3    6    5
            //a = 1:int; rnd( a, b) ; a = 1.0:real

            solver.SetConst(0, FType.Int32);
            Assert.True(solver.SetDefenition("a", 1,0));
            solver.SetVar(2,"a");
            solver.SetVar(3,"b");
            solver.SetCall(new CallDef(FType.Generic(0), new[] {4, 2, 3}));
            solver.SetConst(5, FType.Real);
            
            Assert.True(solver.SetDefenition("a", 6, 5));
             
            var solvation = solver.Solve();
             
            Assert.AreEqual(FType.Real, solvation.GetVarType("a"));
            Assert.AreEqual(FType.Real, solvation.GetVarType("b"));
            Assert.AreEqual(FType.Real, solvation.GetNodeType(2));
        }
        
        

 
    }
}