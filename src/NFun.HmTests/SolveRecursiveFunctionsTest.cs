using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests
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
        public void NonRecursiveConstFunction_solved()
        {
            //  1   0
            //f() = 1
            var tOut = solver.MakeGeneric();
            Assert.IsTrue(solver.SetVarType("f(0)", FType.Fun(tOut)));

            solver.SetConst(0, FType.Int32);
            Assert.IsTrue(solver.SetFunDefenition("f(0)", 1, 0));
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Int32), res.GetVarType("f(0)"));
        }
        [Test]
        public void NonRecursiveProjectionFunction_solved()
        {
            //  1    0
            //f(a) = a
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            Assert.IsTrue(solver.SetFunDefenition("f(1)", 1, 0));
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Generic(0), FType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Generic(0), res.GetVarType("f(1) a"));
        }
        [Test]
        public void NonRecursiveConcreteFunction_solved()
        {
            //  3    021
            //f(a) = a+1
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, FType.Int32);
            solver.SetArithmeticalOp(2, 0, 1);
            Assert.IsTrue(solver.SetFunDefenition("f(1)", 3, 2));
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("f(1) a"));
        }
        [Test]
        public void NonRecursiveConcreteFunction_WithSpecifiedOutputType()
        {
            //  3         021
            //f(a):long = a+1
            var type = solver.SetNewVar("f(1) a");
            solver.SetVarType("f(1)", FType.Fun(SolvingNode.CreateStrict(FType.Int64), type));
            
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, FType.Int32);
            solver.SetArithmeticalOp(2, 0, 1);
            solver.SetFunDefenition("f(1)", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Int64, FType.Int64), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Int64, res.GetVarType("f(1) a"));
        }
        [Test]
        public void NonRecursiveGenericFunction_GenericsFound()
        {
            //  1    0
            //f(a) = a

            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetFunDefenition("f(1)", 1, 0);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Generic(0), FType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Generic(0), res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void NonRecursiveGenericFunction_OutputSpecified_GenericsSolved()
        {
            //  1    0
            //f(a):long = a
            var tA = solver.SetNewVar("f(1) a");
            solver.SetVarType("f(1)", FType.Fun(SolvingNode.CreateStrict(FType.Int64), tA));

            
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
            //  2         1 0
            //f(a):long = f(a)
            var tA = solver.SetNewVar("f(1) a");
            solver.SetVarType("f(1)", FType.Fun(SolvingNode.CreateStrict(FType.Int64), tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetInvoke(1, "f(1)", new[] {0});
            solver.SetFunDefenition("f(1)", 2, 1);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Int64, FType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Generic(0), res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void SimpleRecursiveGenericFunction_GenericsFound()
        {
            //  2    1 0
            //f(a) = f(a)
           
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetInvoke(1, "f(1)", new[] {0});
            solver.SetFunDefenition("f(1)", 2, 1);
            
            var res = solver.Solve();
            Assert.AreEqual(2,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Generic(1), FType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Generic(0), res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void SimpleRecursiveFunction_Arithmetical_Solved()
        {
            //  4    1 0  3 2
            //f(a) = f(a) * 2
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");

            solver.SetInvoke(1, "f(1)", new[] {0});
            solver.SetConst(2,FType.Int32);
            solver.SetArithmeticalOp(3, 1, 2);
            
            solver.SetFunDefenition("f(1)", 4, 3);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Generic(0), res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_ArithmeticalOnItsArg_Solved()
        {
            //  4    1 0  3 2
            //f(a) = f(a) * a
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");

            solver.SetInvoke(1, "f(1)", new[] {0});
            solver.SetVar(2, "f(1) a");

            solver.SetArithmeticalOp(3, 1, 2);
            
            solver.SetFunDefenition("f(1)", 4, 3);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("f(1) a"));
        }
        /*
         *
         * [TestCase( "f(n, iter) = f(n, iter+1).strConcat(n >iter)")]
        [TestCase( "f1(n, iter) = f1(n+1, iter).strConcat(n >iter)")]
        [TestCase( "f2(n, iter) = n > iter and f2(n,iter)")]
        [TestCase( "f3(n, iter) = n > iter and f3(n,iter+1)")]
        [TestCase( "f4(n, iter) = f4(n,iter) and (n > iter)")]
        [TestCase( "f8(n) = n==0 and f8(n)")]
         
         * 
         */
        
        [Test]
        public void RecursiveFunction_BoolOpOnArg_Solved()
        {
            //  4    3 0  2  1
            //f(a) = f(a and true)
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, FType.Bool);
            solver.SetCall(new CallDef(FType.Bool, new[] {2, 0, 1}));
            
            solver.SetInvoke(3, "f(1)",new[]{2});
            solver.SetFunDefenition("f(1)", 4, 3);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(1,res.GenericsCount);

            Assert.AreEqual(FType.Fun(FType.Generic(0), FType.Bool), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Bool, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_TextCallOnArg_Solved()
        {
            //  3    2 0  1
            //f(a) = f(a.reverseStr())
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetCall(new CallDef(FType.Text, new[] {1, 0}));
            
            solver.SetInvoke(2, "f(1)",new[]{1});
            solver.SetFunDefenition("f(1)", 3, 2);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(1,res.GenericsCount);

            Assert.AreEqual(FType.Fun(FType.Generic(0), FType.Text), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Text, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_strConcatOnArg_Solved()
        {
            //  4    3 0     2       1
            //f(a) = f(a.strConcat("hi"))
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, FType.Text);
            solver.SetCall(new CallDef(new[]{FType.Text,FType.Text, FType.Any}, new[] {2, 0,1}));
            
            solver.SetInvoke(3, "f(1)",new[]{2});
            solver.SetFunDefenition("f(1)", 4, 3);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(1,res.GenericsCount);

            Assert.AreEqual(FType.Fun(FType.Generic(0), FType.Text), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Text, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_ArithmOpOnArg_Solved()
        {
            //  4    3 0  2  1
            //f(a) = f(a  +  1)
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, FType.Int32);
            solver.SetArithmeticalOp(2, 0, 1);
            
            solver.SetInvoke(3, "f(1)" , new[]{2});
            solver.SetFunDefenition("f(1)", 4, 3);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(1,res.GenericsCount);

            Assert.AreEqual(FType.Fun(FType.Generic(0), FType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("f(1) a"));
        }
        [Test]
        public void RecursiveFunction_BoolAndArithmOp_Solved()
        {
            //  6    1 0   5  2 4 3
            //f(a) = f(a) and a > 0
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetInvoke(1, "f(1)",new[]{0});
            
            solver.SetVar(2, "f(1) a");
            solver.SetConst(3, FType.Int32); 
            solver.SetComparationOperator(4, 2, 3);
            
            solver.SetCall(new CallDef(FType.Bool, new[] {5, 1, 4}));

            solver.SetFunDefenition("f(1)", 6, 5);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Bool, FType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_CompareAndBoolOp_Solved()
        {
            //  6    0 2 1 5   4 3
            //f(a) = a > 0 and f(a) 
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
           
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, FType.Int32); 
            solver.SetComparationOperator(2, 0, 1);

            solver.SetVar(3, "f(1) a");
            solver.SetInvoke(4, "f(1)",new[]{3});
            
            solver.SetCall(new CallDef(FType.Bool, new[] {5, 2, 4}));

            solver.SetFunDefenition("f(1)", 6, 5);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Bool, FType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Real, res.GetVarType("f(1) a"));
        }
        [Test]
        public void RecursiveFunction_ArithmeticalOnItsCall_Solved()
        {
            //  5    1 0  4 3 2
            //f(a) = f(a) + f(2)
            var tA = solver.SetNewVar("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", FType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetInvoke(1, "f(1)",new[]{0});

            solver.SetConst(2, FType.Int32);
            solver.SetInvoke(3, "f(1)",new[]{2});

            solver.SetArithmeticalOp(4, 1, 3);
            
            solver.SetFunDefenition("f(1)", 5, 4);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Int32), res.GetVarType("f(1)"));
            Assert.AreEqual(FType.Int32, res.GetVarType("f(1) a"));
        }
        
        
        [Test]
        public void TwoRecursiveFunctions_ArithmeticalOnItCall_Solved()
        {
            //   5     1 0  4  3 2
            //f1(a) = f2(a) + f2(2)
            //  10     7 6  9 8
            //f2(a) = f1(a) + 1 

            var tA1 = solver.SetNewVar("f1(1) a");
            var tOut1 = solver.MakeGeneric();
            solver.SetVarType("f1(1)", FType.Fun(tOut1, tA1));
            
            
            var tA2 = solver.SetNewVar("f2(1) a");
            var tOut2 = solver.MakeGeneric();
            solver.SetVarType("f2(1)", FType.Fun(tOut2, tA2));
            
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
            Assert.AreEqual(FType.Fun(FType.Real, FType.Int32), res.GetVarType("f1(1)"));
            Assert.AreEqual(FType.Int32, res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(FType.Fun(FType.Real, FType.Int32), res.GetVarType("f2(1)"));
            Assert.AreEqual(FType.Int32, res.GetVarType("f2(1) a"));
        }
        
        
        
        [Test]
        public void TwoRecursiveFunctions_SpecifyOuptut_ArithmeticalOnItCall_Solved()
        {
            //   5     1 0  4  3 2
            //f1(a) = f2(a) + f2(2)
            //  10     7 6  9 8
            //f2(a):int = f1(a) + 1 

            var tA1 = solver.SetNewVar("f1(1) a");
            var tOut1 = solver.MakeGeneric();
            solver.SetVarType("f1(1)", FType.Fun(tOut1, tA1));
            
            
            var tA2 = solver.SetNewVar("f2(1) a");
            solver.SetVarType("f2(1)", FType.Fun(SolvingNode.CreateStrict(FType.Int32), tA2));
            
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
            Assert.AreEqual(FType.Fun(FType.Int32, FType.Int32), res.GetVarType("f1(1)"));
            Assert.AreEqual(FType.Int32, res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(FType.Fun(FType.Int32, FType.Int32), res.GetVarType("f2(1)"));
            Assert.AreEqual(FType.Int32, res.GetVarType("f2(1) a"));
        }
        
        [Test]
        public void TwoRecursiveFunctions_InputArgSpecified_ArithmeticalOnItCall_Solved()
        {
            //   5     1 0  4  3 2
            //f1(a) = f2(a) + f2(2)
            //  10     7 6  9 8
            //f2(a:real) = f1(a) + 1 

            var tA1 = solver.SetNewVar("f1(1) a");
            var tOut1 = solver.MakeGeneric();
            solver.SetVarType("f1(1)", FType.Fun(tOut1, tA1));
            
            
            solver.SetVarType("f2(1) a", FType.Real);
            var tOut2 = solver.MakeGeneric();
            solver.SetVarType("f2(1)", FType.Fun(tOut2, SolvingNode.CreateStrict(FType.Real)));
            
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

            var tA1 = solver.SetNewVar("f1(1) a");
            var tOut1 = solver.MakeGeneric();
            solver.SetVarType("f1(1)", FType.Fun(tOut1, tA1));
            
            var tA2 = solver.SetNewVar("f2(1) a");
            var tOut2 = solver.MakeGeneric();
            solver.SetVarType("f2(1)", FType.Fun(tOut2, tA2));
            
            solver.SetVar(0, "f1(1) a");
            solver.SetInvoke(1, "f2(1)",new[]{0});
            solver.SetFunDefenition("f1(1)", 2, 1);
            
            solver.SetVar(3, "f2(1) a");
            solver.SetInvoke(4, "f1(1)",new[]{3});
            solver.SetFunDefenition("f2(1)", 5, 4);
            
            var res = solver.Solve();
            Assert.AreEqual(2,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Generic(1), FType.Generic(0)), res.GetVarType("f1(1)"));
            Assert.AreEqual(FType.Generic(0), res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(FType.Fun(FType.Generic(1), FType.Generic(0)), res.GetVarType("f2(1)"));
            Assert.AreEqual(FType.Generic(0), res.GetVarType("f2(1) a"));
        }
        
        
        [Test]
        public void ThreeRecursiveFunctions_ArithmeticalOnItCall_Solved()
        {
            //   5     1 0  4  3 2
            //f1(a) = f2(a) + f2(2)
            //  10     7 6  9 8
            //f2(a) = f1(a) + f3(a)
            //   13   12 11 
            //f3(a) = f1(a) 
            
            
            var tA1 = solver.SetNewVar("f1(1) a");
            var tOut1 = solver.MakeGeneric();
            solver.SetVarType("f1(1)", FType.Fun(tOut1, tA1));
            
            var tA2 = solver.SetNewVar("f2(1) a");
            var tOut2 = solver.MakeGeneric();
            solver.SetVarType("f2(1)", FType.Fun(tOut2, tA2));

            var tA3 = solver.SetNewVar("f3(1) a");
            var tOut3 = solver.MakeGeneric();
            solver.SetVarType("f3(1)", FType.Fun(tOut3, tA3));


            #region first
            solver.SetVar(0, "f1(1) a");
            solver.SetInvoke(1, "f2(1)",new[]{0});
            solver.SetConst(2, FType.Int32);
            solver.SetInvoke(3, "f2(1)",new[]{2});
            solver.SetArithmeticalOp(4, 1, 3);
            solver.SetFunDefenition("f1(1)", 5, 4);
            #endregion

            #region second
            solver.SetVar(6, "f2(1) a");
            solver.SetInvoke(7, "f1(1)",new[]{6});
            solver.SetConst(8, FType.Int32);
            solver.SetArithmeticalOp(9, 7, 8);
            solver.SetFunDefenition("f2(1)", 10, 9);
            #endregion

            #region third
            solver.SetVar(11, "f3(1) a");
            solver.SetInvoke(12, "f1(1)",new[]{11});
            solver.SetFunDefenition("f3(1)", 13, 12);
            #endregion

            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(FType.Fun(FType.Real, FType.Int32), res.GetVarType("f1(1)"));
            Assert.AreEqual(FType.Int32, res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(FType.Fun(FType.Real, FType.Int32), res.GetVarType("f2(1)"));
            Assert.AreEqual(FType.Int32, res.GetVarType("f2(1) a"));
            
            Assert.AreEqual(FType.Fun(FType.Real, FType.Int32), res.GetVarType("f3(1)"));
            Assert.AreEqual(FType.Int32, res.GetVarType("f3(1) a"));
        }
    }
}