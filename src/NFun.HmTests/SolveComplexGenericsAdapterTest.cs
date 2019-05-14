using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests
{
    public class SolveComplexGenericsAdapterTest
    {
        private NsHumanizerSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new NsHumanizerSolver();
        }

        [Test]
        public void GetArrElement_EquationSolved()
        {
            // 2  1 0
            //get(a,i)
            solver.SetVar(0, "i");
            solver.SetVar(1, "a");
            
            solver.SetCall(new CallDef(new[]{
                    FType.Generic(0),
                    FType.ArrayOf(FType.Generic(0)), 
                    FType.Int32}, new[] {2,1,0}));
            var solvation = solver.Solve();
            Assert.AreEqual(1, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), solvation.GetVarType("a"));
            Assert.AreEqual(FType.Int32, solvation.GetVarType("i"));
            Assert.AreEqual(FType.Generic(0),  solvation.GetNodeType(2));

        }
        
        [Test]
        public void SingleArgGenericFunction_SingleGenericFound()
        {
            // 2        1     0
            // y(a) = reverse(a)
            solver.SetVar( 0,"a");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {1,0}));
            solver.SetDefenition("y", 2, 1);
            
            var solvation = solver.Solve();
            Assert.AreEqual(1, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), solvation.GetVarType("y"));
        }
        
        [Test]
        public void SingleNestedArgGenericFunction_SingleGenericFound()
        {
            // 4        3      2  1 0
            // y(a) = reverse(get(a,0))
            solver.SetVar( 1,"a");
            solver.SetConst(0,FType.Int32);
            solver.SetCall(new CallDef(
                new[]{
                    FType.Generic(0), 
                    FType.ArrayOf(FType.Generic(0)), 
                    FType.Int32}, 
                new[] {2,1,0}));

            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {3,2}));
            solver.SetDefenition("y", 4, 3);
            
            var solvation = solver.Solve();
            Assert.AreEqual(1, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), solvation.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.ArrayOf(FType.Generic(0))), solvation.GetVarType("a"));
        }
        
        [Test]
        public void LimitCall_TwoGenericArrayOperations_SingleGenericFound()
        {
            // 5          0  2     1     4   3
            // y(a,b,c) = a.concat(b).concat(c)
            solver.SetVar( 0,"a");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2,0, 1}));
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {4, 2, 3}));
            solver.SetDefenition("y", 5, 4);
            
            var solvation = solver.Solve();
            Assert.AreEqual(1, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), solvation.GetVarType("y"));
        }
    
        
        [Test]
        public void LimitArrayOperationsWithConcreteArgument_EquationSolved()
        {
            // 3             0      2    1
            // y(a,b,c) = [0,1,2].concat(b)
            solver.SetConst( 0,FType.ArrayOf(FType.Int32));
            solver.SetVar(1, "b");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1}));
            solver.SetDefenition("y", 3, 2);
            
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("b"));
        }
        [Test]
        public void LimitTwoArrayOperationsWithLastConcreteArgument_EquationSolved()
        {
            // 5          0     2  1   4        3
            // y(a,b,c) = a.concat(b).concat([0,1,2])
            solver.SetVar( 0,"a");
            solver.SetVar( 1,"b");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1}));
            solver.SetConst(3,FType.ArrayOf(FType.Int32));
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {4, 2, 3}));
            solver.SetDefenition("y", 5, 4);
            
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("b"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("a"));

        }
        [Test]
        public void TwoGenericArrayOperations_SingleGenericFound()
        {
            // 5          0  2     1     4   3
            // y(a,b,c) = a.concat(b).concat(c)
            solver.SetVar( 0,"a");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1}));
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {4, 2, 3}));
            solver.SetDefenition("y", 5, 4);
            
            var solvation = solver.Solve();
            Assert.AreEqual(1, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), solvation.GetVarType("y"));
        }
        
        [Test]
        public void ThreeGenericArrayOperations_SingleGenericFound()
        {
            // 7            0  2     1     4   3     6   5
            // y(a,b,c,d) = a.concat(b).concat(c).concat(d)
            solver.SetVar( 0,"a");
            solver.SetVar( 1,"b");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1}));
            solver.SetVar( 3,"c");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {4, 2, 3}));
            solver.SetVar( 5,"d");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {6, 4, 5}));

            solver.SetDefenition("y", 7, 6);
            
            var solvation = solver.Solve();
            Assert.AreEqual(1, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), solvation.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), solvation.GetVarType("a"));
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), solvation.GetVarType("b"));
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), solvation.GetVarType("c"));
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), solvation.GetVarType("d"));
        }
        [Test]
        public void ThreeGenericArrayOperations_MiddleArgSpecified_EquationSolved()
        {
            // 7          0  2     1     4       3     6   5
            // y(a,b,d) = a.concat(b).concat([1,2]).concat(d)
            solver.SetVar( 0,"a");
            solver.SetVar( 1,"b");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1}));
            solver.SetConst(3, FType.ArrayOf(FType.Int32));
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {4, 2, 3}));
            solver.SetVar( 5,"d");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {6, 4, 5}));

            solver.SetDefenition("y", 7, 6);
            
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("a"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("b"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("d"));
        }
        
        [Test]
        public void ThreeGenericArrayOperations_ArgsHasDifferentTypes_EquationSolved()
        {
            // 7                                 0  2     1     4   3     6   5
            // y(a,b:real[],c:int64[],d:int[]) = a.concat(b).concat(c).concat(d)

            solver.SetVarType("b", FType.ArrayOf(FType.Real));
            solver.SetVarType("c", FType.ArrayOf(FType.Int64));
            solver.SetVarType("d", FType.ArrayOf(FType.Int32));

            solver.SetVar( 0,"a");
            solver.SetVar( 1,"b");
            Assert.IsTrue(solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1})));
            Assert.IsTrue(solver.SetVar( 3,"c"));
            Assert.IsTrue(solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {4, 2, 3})));
            Assert.IsTrue(solver.SetVar( 5,"d"));
            Assert.IsTrue(solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {6, 4, 5})));
            
            Assert.IsTrue(solver.SetDefenition("y", 7, 6));
            
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Real), solvation.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Real), solvation.GetVarType("a"));
            Assert.AreEqual(FType.ArrayOf(FType.Real), solvation.GetVarType("b"));
            Assert.AreEqual(FType.ArrayOf(FType.Int64), solvation.GetVarType("c"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("d"));
        }
        
        [Test]
        public void ThreeGenericArrayOperationsWithWrongOutputSpecified_ReturnsError()
        {
            // 7               0  2     1     4       3     6   5
            // y(a,b,d):real = a.concat(b).concat([1,2]).concat(d)
            solver.SetVarType("y",FType.Real);
            solver.SetVar( 0,"a");
            solver.SetVar( 1,"b");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1}));
            solver.SetConst(3, FType.ArrayOf(FType.Int32));
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {4, 2, 3}));
            solver.SetVar( 5,"d");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {6, 4, 5}));

            Assert.IsFalse(solver.SetDefenition("y", 7, 6));
        }

        [Test]
        public void ArrayOperationsWithConcreteArgument_EquationSolved()
        {
            // 3             0      2    1
            // y(a,b,c) = [0,1,2].concat(b)
            solver.SetConst( 0,FType.ArrayOf(FType.Int32));
            solver.SetVar(1, "b");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1}));
            solver.SetDefenition("y", 3, 2);
            
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("b"));
        }
        [Test]
        public void TwoArrayOperationsWithLastConcreteArgument_EquationSolved()
        {
            // 5          0     2  1   4        3
            // y(a,b,c) = a.concat(b).concat([0,1,2])
            solver.SetVar( 0,"a");
            solver.SetVar( 1,"b");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1}));
            solver.SetConst(3,FType.ArrayOf(FType.Int32));
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {4, 2, 3}));
            solver.SetDefenition("y", 5, 4);
            
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("b"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("a"));

        }
        [Test]
        public void TwoArrayOperationsWithSpecifiedArg_EquationSolved()
        {
            // 5                0   2    1   4     3
            // y(a,b,c):int[] = a.concat(b).concat(c)
            solver.SetVarType("y", FType.ArrayOf(FType.Int32));
            solver.SetVar( 0,"a");
            solver.SetVar( 1,"b");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {2, 0, 1}));
            solver.SetVar(3,"c");
            solver.SetCall(new CallDef(FType.ArrayOf(FType.Generic(0)), new[] {4, 2, 3}));
            solver.SetDefenition("y", 5, 4);
            
            var solvation = solver.Solve();
            Assert.AreEqual(0, solvation.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("b"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solvation.GetVarType("a"));

        }
    }
}