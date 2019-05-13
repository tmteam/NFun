using System;
using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace TysoTake2.TypeSolvingNodes.Tests
{
    public class SolveRecursiveFunctionsTest
    {
        private NsHumanizerSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new NsHumanizerSolver();
        }
        [Test]
        public void SimpleNonRecursiveConcreteFunction_solved()
        {
            //  3    021
            //f(a) = a+1
            solver.SetVarType("f(1)", FType.Fun(FType.Generic(0), FType.Generic(1)));
            solver.SetNewVar("f(1) a");
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, FType.Int32);
            solver.SetArithmeticalOp(2, 0, 1);
            solver.SetFunDefenition("f(1)", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("a"));
        }
        [Test]
        public void SimpleNonRecursiveConcreteFunction_WithSpecifiedOutputType()
        {
            //  3         021
            //f(a):long = a+1
            solver.SetVarType("f(1)", FType.Fun(FType.Int64, FType.Generic(0)));
            solver.SetNewVar("f(1) a");
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, FType.Int32);
            solver.SetArithmeticalOp(2, 0, 1);
            solver.SetFunDefenition("f(1)", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("a"));
        }
        [Test]
        public void SimpleNonRecursiveGenericFunction_GenericsFound()
        {
            //  1    0
            //f(a) = a
            solver.SetVarType("f(1)", FType.Fun(FType.Generic(0),FType.Generic(1)));
            solver.SetNewVar("f(1) a");
            solver.SetVar(0, "f(1) a");
            solver.SetFunDefenition("f(1)", 1, 0);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Generic(0), FType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Generic(0), res.GetVarType("a"));
        }
        
        [Test]
        public void SimpleNonRecursiveGenericFunction_OutputSpecified_GenericsSolved()
        {
            //  1    0
            //f(a):long = a
            solver.SetVarType("f(1)", FType.Fun(FType.Int64, FType.Generic(0)));
            solver.SetNewVar("f(1) a");
            
            solver.SetVar(0, "f(1) a");
            solver.SetFunDefenition("f(1)", 1, 0);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Int64, FType.Int64), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Int64, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void SimpleRecursiveGenericFunction_OutputSpecified_GenericsFound()
        {
            //  3          2  1 0
            //f(a):long = inv(f,a)
            solver.SetVarType("f(1)", FType.Fun(FType.Int64, FType.Generic(0)));
            solver.SetNewVar("f(1) a");

            solver.SetVar(0, "f(1) a");
            solver.SetVar(1, "f(1)");

            solver.SetInvoke(2, 1, new[] {0});
            
            solver.SetFunDefenition("f(1)", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Int64, FType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Generic(0), res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void SimpleRecursiveGenericFunction_GenericsFound()
        {
            //  3     2  1 0
            //f(a) = inv(f,a)
            solver.SetVarType("f(1)", FType.Fun(FType.Generic(0), FType.Generic(1)));
            solver.SetNewVar("f(1) a");

            solver.SetVar(0, "f(1) a");
            solver.SetVar(1, "f(1)");

            solver.SetInvoke(2, 1, new[] {0});
            
            solver.SetFunDefenition("f(1)", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(2,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Generic(0), FType.Generic(1)), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Generic(1), res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void SimpleRecursiveFunction_Arithmetical_Solved()
        {
            //  5     2  1 0  4 3
            //f(a) = inv(f,a) * 2
            solver.SetVarType("f(1)", FType.Fun(FType.Generic(0), FType.Generic(1)));
            solver.SetNewVar("f(1) a");

            solver.SetVar(0, "f(1) a");
            solver.SetVar(1, "f(1)");

            solver.SetInvoke(2, 1, new[] {0});

            solver.SetConst(3,FType.Int32);
            solver.SetArithmeticalOp(4, 2, 3);
            
            solver.SetFunDefenition("f(1)", 5, 4);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_ArithmeticalOnItsArg_Solved()
        {
            //  5     2  1 0  4 3
            //f(a) = inv(f,a) * a
            solver.SetVarType("f(1)", FType.Fun(FType.Generic(0), FType.Generic(1)));
            solver.SetNewVar("f(1) a");

            solver.SetVar(0, "f(1) a");
            solver.SetVar(1, "f(1)");

            solver.SetInvoke(2, 1, new[] {0});

            solver.SetVar(3, "f(1) a");
            solver.SetArithmeticalOp(4, 2, 3);
            
            solver.SetFunDefenition("f(1)", 5, 4);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_ArithmeticalOnItsCall_Solved()
        {
            //  5    1 0  4 3 2
            //f(a) = f(a) + f(2)
            solver.SetVarType("f(1)", FType.Fun(FType.Generic(0), FType.Generic(1)));
            solver.SetNewVar("f(1) a");

            solver.SetVar(0, "f(1) a");
            solver.SetInvoke(1, "f(1)",new[]{0});

            solver.SetConst(2, FType.Int32);
            solver.SetInvoke(3, "f(1)",new[]{2});

            solver.SetArithmeticalOp(4, 1, 3);
            
            solver.SetFunDefenition("f(1)", 5, 4);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("f(1) a"));
        }
        
        
        [Test]
        public void TwoRecursiveFunctions_ArithmeticalOnItCall_Solved()
        {
            //   5     1 0  4  3 2
            //f1(a) = f2(a) + f2(2)
            //  10     7 6  9 8
            //f2(a) = f1(a) + 1 

            solver.SetVarType("f1(1)", FType.Fun(FType.Generic(0), FType.Generic(1)));
            solver.SetNewVar("f1(1) a");

            solver.SetVarType("f2(1)", FType.Fun(FType.Generic(0), FType.Generic(1)));
            solver.SetNewVar("f2(1) a");
            
            solver.SetVar(0, "f1(1) a");
            solver.SetInvoke(1, "f2(1)",new[]{0});
            solver.SetConst(2, FType.Int32);
            solver.SetInvoke(3, "f2(1)",new[]{2});
            solver.SetArithmeticalOp(4, 1, 3);
            solver.SetFunDefenition("f1(1)", 5, 4);
            
            solver.SetVar(6, "f2(1) a");
            solver.SetInvoke(7, "f1(1)",new[]{6});
            solver.SetConst(8, FType.Int32);
            solver.SetArithmeticalOp(9, 7, 8);
            solver.SetFunDefenition("f2(1)", 10, 9);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Real), res.GetVarType("f1(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(FType.Fun(FType.Real, FType.Real), res.GetVarType("f2(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("f2(1) a"));
        }
        
        [Test]
        public void TwoGenericRecursiveFunctions_GenericsFound()
        {
            //   2     1 0 
            //f1(a) = f2(a)
            //  5     4 3
            //f2(a) = f1(a)

            solver.SetVarType("f1(1)", FType.Fun(FType.Generic(0), FType.Generic(1)));
            solver.SetNewVar("f1(1) a");

            solver.SetVarType("f2(1)", FType.Fun(FType.Generic(0), FType.Generic(1)));
            solver.SetNewVar("f2(1) a");
            
            solver.SetVar(0, "f1(1) a");
            solver.SetInvoke(1, "f2(1)",new[]{0});
            solver.SetFunDefenition("f1(1)", 2, 1);
            
            solver.SetVar(3, "f2(1) a");
            solver.SetInvoke(4, "f1(1)",new[]{3});
            solver.SetFunDefenition("f2(1)", 5, 4);
            
            var res = solver.Solve();
            Assert.AreEqual(2,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Generic(0), FType.Generic(1)), res.GetVarType("f1(1)"));
            Assert.AreEqual(FType.Generic(1), res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(FType.Fun(FType.Generic(0), FType.Generic(1)), res.GetVarType("f2(1)"));
            Assert.AreEqual(FType.Generic(1), res.GetVarType("f2(1) a"));
        }
    }
}