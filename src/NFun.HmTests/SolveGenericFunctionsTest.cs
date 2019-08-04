using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests
{
    public class SolveGenericFunctionsTest
    {
        private TiLanguageSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new TiLanguageSolver();
        }
        
        
        [Test]
        public void NonRecursiveConstFunction_solved()
        {
            //  1   0
            //f() = 1
            var tOut = solver.MakeGeneric();
            Assert.IsTrue(solver.SetVarType("f(0)", TiType.Fun(tOut)));

            solver.SetConst(0, TiType.Int32);
            solver.SetFunDefenition("f(0)", 1, 0).AssertSuccesfully();;
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Int32), res.GetVarType("f(0)"));
        }
        [Test]
        public void NonRecursiveProjectionFunction_solved()
        {
            //  1    0
            //f(a) = a
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetFunDefenition("f(1)", 1, 0).AssertSuccesfully();;
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Generic(0), TiType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Generic(0), res.GetVarType("f(1) a"));
        }
        [Test]
        public void NonRecursive_IfGenericFunction_SingleGenericFound()
        {
            //node |     4   3   0    1      2
            //expr |y(a,b) = if(true) a else b
            var tA = solver.SetNewVarOrThrow("a");
            var tB = solver.SetNewVarOrThrow("b");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("y(2)", TiType.Fun(tOut, tA, tB));

            solver.SetConst(0, TiType.Bool);
            solver.SetVar( 1,"a");
            solver.SetVar( 2,"b");
            solver.ApplyLcaIf(3, new[] {0}, new[] {1, 2});
            
            solver.SetFunDefenition("y(2)",4, 3);
            
            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(1, result.GenericsCount);
            
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("a"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("b"));
            Assert.AreEqual(TiType.Fun(TiType.Generic(0),TiType.Generic(0),TiType.Generic(0)), result.GetVarType("y(2)"));
        }
        
        [Test]
        public void NonRecursive_IfGenericFunctionWithToCases_SingleGenericFound()
        {
            //node |       6   5   0    1      2   3       4
            //expr |y(a,b,c) = if(true) a if(true) b  else c
            var tA = solver.SetNewVarOrThrow("a");
            var tB = solver.SetNewVarOrThrow("b");
            var tC = solver.SetNewVarOrThrow("c");

            var tOut = solver.MakeGeneric();
            solver.SetVarType("y(3)", TiType.Fun(tOut, tA, tB, tC));

            solver.SetConst(0, TiType.Bool);
            solver.SetVar( 1,"a");
            solver.SetConst(2, TiType.Bool);
            solver.SetVar( 3,"b");
            solver.SetVar( 4,"c");

            solver.ApplyLcaIf(5, new[] {0,2}, new[] {1, 3,4});
            
            solver.SetFunDefenition("y(3)",6, 5).AssertSuccesfully();;
            
            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(1, result.GenericsCount);
            var T0 = TiType.Generic(0);
            
            Assert.AreEqual(T0, result.GetVarType("a"));
            Assert.AreEqual(T0, result.GetVarType("b"));
            Assert.AreEqual(T0, result.GetVarType("c"));
            Assert.AreEqual(TiType.Fun(T0,T0,T0,T0), result.GetVarType("y(3)"));
        }

        [Test]
        public void NonRecursive_IfGenericFunctionForArrayArg_SingleGenericFound()
        {
            //node | 5      4   0        2  1       3
            //expr |y(a) = if(true) reverse(a) else a
            var tA = solver.SetNewVarOrThrow("a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("y(1)", TiType.Fun(tOut, tA));

            solver.SetConst(0, TiType.Bool);
            solver.SetVar( 1,"a");
            solver.SetCall(new CallDefenition(TiType.ArrayOf(TiType.Generic(0)), new[] {2, 1}));
            
            solver.SetVar( 3,"a");
            solver.ApplyLcaIf(4, new[] {0}, new[] {2, 3});
            
            solver.SetFunDefenition("y(1)",5, 4);
            
            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(1, result.GenericsCount);
            var tArr = TiType.ArrayOf(TiType.Generic(0));
            Assert.AreEqual(tArr, result.GetVarType("a"));
            Assert.AreEqual(TiType.Fun(tArr,tArr), result.GetVarType("y(1)"));
        }
        [Test]
        public void NonRecursive_IfGenericFunctionForArrayArgAndArrayInit_SingleGenericFound()
        {
            //node | 6       5   0        2  1       4 3
            //expr |y(a,b) = if(true) reverse(a) else [b]
            var tA = solver.SetNewVarOrThrow("a");
            var tB = solver.SetNewVarOrThrow("b");


            var tOut = solver.MakeGeneric();
            solver.SetVarType("y(2)", TiType.Fun(tOut, tA,tB));

            var tArr = TiType.ArrayOf(TiType.Generic(0));

            solver.SetConst(0, TiType.Bool);
            solver.SetVar( 1,"a");
            solver.SetCall(new CallDefenition(tArr, new[] {2, 1}));
            
            solver.SetVar( 3,"b");
            solver.SetArrayInit(4, 3);
            
            solver.ApplyLcaIf(5, new[] {0}, new[] {2, 4});
            
            solver.SetFunDefenition("y(2)",6, 5);
            
            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(tArr, result.GetVarType("a"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("b"));
            Assert.AreEqual(TiType.Fun(tArr,tArr,TiType.Generic(0)), result.GetVarType("y(2)"));
        }

        
        [Test]
        public void NonRecursive_IfGenericWithArrayFunction_SingleGenericFound()
        {
            //node |         8   7   0   3 1 2      6 4 5
            //expr |y(a,b,c,d) = if(true) [a,b] else [c,d]
            var tA = solver.SetNewVarOrThrow("a");
            var tB = solver.SetNewVarOrThrow("b");
            var tC = solver.SetNewVarOrThrow("c");
            var tD = solver.SetNewVarOrThrow("d");

            var tOut = solver.MakeGeneric();
            solver.SetVarType("y(4)", TiType.Fun(tOut, tA, tB, tC, tD));

            solver.SetConst(0, TiType.Bool);
            solver.SetVar( 1,"a");
            solver.SetVar( 2,"b");
            solver.SetArrayInit(3, 1, 2);
            
            solver.SetVar( 4,"c");
            solver.SetVar( 5,"d");
            solver.SetArrayInit(6, 4, 5);

            solver.ApplyLcaIf(7, new[] {0}, new[] {3, 6});
            
            solver.SetFunDefenition("y(4)",8, 7);
            
            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(1, result.GenericsCount);
            
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("a"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("b"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("c"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("d"));

            Assert.AreEqual(TiType.Fun(
                TiType.ArrayOf(TiType.Generic(0)),
                TiType.Generic(0),
                TiType.Generic(0),
                TiType.Generic(0),
                TiType.Generic(0)), result.GetVarType("y(4)"));
        }
        
        [Test]
        public void NonRecursive_IfGenericWithArrayAndSimpleFunction_SingleGenericFound()
        {
            //node |       6   5   0   3 1 2       4
            //expr |y(a,b,c) = if(true) [a,b] else с
            var tA = solver.SetNewVarOrThrow("a");
            var tB = solver.SetNewVarOrThrow("b");
            var tC = solver.SetNewVarOrThrow("c");

            var tOut = solver.MakeGeneric();
            solver.SetVarType("y(3)", TiType.Fun(tOut, tA, tB, tC));

            solver.SetConst(0, TiType.Bool);
            solver.SetVar( 1,"a");
            solver.SetVar( 2,"b");
            solver.SetArrayInit(3, 1, 2);
            
            solver.SetVar( 4,"c");

            solver.ApplyLcaIf(5, new[] {0}, new[] {3, 4});
            
            solver.SetFunDefenition("y(3)",6, 5);
            
            var result = solver.Solve();
            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(1, result.GenericsCount);
            var T0 = TiType.Generic(0);
            Assert.AreEqual(T0, result.GetVarType("a"));
            Assert.AreEqual(T0, result.GetVarType("b"));
            Assert.AreEqual(TiType.ArrayOf(T0), result.GetVarType("c"));

            Assert.AreEqual(TiType.Fun(
                TiType.ArrayOf(T0),T0,T0,TiType.ArrayOf(T0)), result.GetVarType("y(3)"));
        }
        [Test]
        public void NonRecursiveConcreteFunction_solved()
        {
            //  3    021
            //f(a) = a+1
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, TiType.Int32);
            solver.SetArithmeticalOp(2, 0, 1);
            solver.SetFunDefenition("f(1)", 3, 2).AssertSuccesfully();;
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Real, res.GetVarType("f(1) a"));
        }
        [Test]
        public void NonRecursiveConcreteFunction_WithSpecifiedOutputType()
        {
            //  3         021
            //f(a):long = a+1
            var type = solver.SetNewVarOrThrow("f(1) a");
            solver.SetVarType("f(1)", TiType.Fun(SolvingNode.CreateStrict(TiType.Int64), type));
            
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, TiType.Int32);
            solver.SetArithmeticalOp(2, 0, 1);
            solver.SetFunDefenition("f(1)", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Int64, TiType.Int64), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Int64, res.GetVarType("f(1) a"));
        }
        [Test]
        public void NonRecursiveGenericFunction_GenericsFound()
        {
            //  1    0
            //f(a) = a

            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetFunDefenition("f(1)", 1, 0);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Generic(0), TiType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Generic(0), res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void NonRecursiveGenericFunction_OutputSpecified_GenericsSolved()
        {
            //  1    0
            //f(a):long = a
            var tA = solver.SetNewVarOrThrow("f(1) a");
            solver.SetVarType("f(1)", TiType.Fun(SolvingNode.CreateStrict(TiType.Int64), tA));

            
            solver.SetVar(0, "f(1) a");
            solver.SetFunDefenition("f(1)", 1, 0);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Int64, TiType.Int64), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Int64, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void SimpleRecursiveGenericFunction_OutputSpecified_GenericsFound()
        {
            //  2         1 0
            //f(a):long = f(a)
            var tA = solver.SetNewVarOrThrow("f(1) a");
            solver.SetVarType("f(1)", TiType.Fun(SolvingNode.CreateStrict(TiType.Int64), tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetInvoke(1, "f(1)", new[] {0});
            solver.SetFunDefenition("f(1)", 2, 1);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Int64, TiType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Generic(0), res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void SimpleRecursiveGenericFunction_GenericsFound()
        {
            //  2    1 0
            //f(a) = f(a)
           
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetInvoke(1, "f(1)", new[] {0});
            solver.SetFunDefenition("f(1)", 2, 1);
            
            var res = solver.Solve();
            Assert.AreEqual(2,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Generic(1), TiType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Generic(0), res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void SimpleRecursiveFunction_Arithmetical_Solved()
        {
            //  4    1 0  3 2
            //f(a) = f(a) * 2
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");

            solver.SetInvoke(1, "f(1)", new[] {0});
            solver.SetConst(2,TiType.Int32);
            solver.SetArithmeticalOp(3, 1, 2);
            
            solver.SetFunDefenition("f(1)", 4, 3);
            
            var res = solver.Solve();
            Assert.AreEqual(1,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Generic(0)), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Generic(0), res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_ArithmeticalOnItsArg_Solved()
        {
            //  4    1 0  3 2
            //f(a) = f(a) * a
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");

            solver.SetInvoke(1, "f(1)", new[] {0});
            solver.SetVar(2, "f(1) a");

            solver.SetArithmeticalOp(3, 1, 2);
            
            solver.SetFunDefenition("f(1)", 4, 3);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Real, res.GetVarType("f(1) a"));
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
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, TiType.Bool);
            solver.SetCall(new CallDefenition(TiType.Bool, new[] {2, 0, 1}));
            
            solver.SetInvoke(3, "f(1)",new[]{2});
            solver.SetFunDefenition("f(1)", 4, 3);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(1,res.GenericsCount);

            Assert.AreEqual(TiType.Fun(TiType.Generic(0), TiType.Bool), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Bool, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_TextCallOnArg_Solved()
        {
            //  3    2 0  1
            //f(a) = f(a.reverseStr())
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetCall(new CallDefenition(TiType.Text, new[] {1, 0}));
            
            solver.SetInvoke(2, "f(1)",new[]{1});
            solver.SetFunDefenition("f(1)", 3, 2);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(1,res.GenericsCount);

            Assert.AreEqual(TiType.Fun(TiType.Generic(0), TiType.Text), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Text, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_strConcatOnArg_Solved()
        {
            //  4    3 0     2       1
            //f(a) = f(a.strConcat("hi"))
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, TiType.Text);
            solver.SetCall(new CallDefenition(new[]{TiType.Text,TiType.Text, TiType.Any}, new[] {2, 0,1}));
            
            solver.SetInvoke(3, "f(1)",new[]{2});
            solver.SetFunDefenition("f(1)", 4, 3);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(1,res.GenericsCount);

            Assert.AreEqual(TiType.Fun(TiType.Generic(0), TiType.Text), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Text, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_ArithmOpOnArg_Solved()
        {
            //  4    3 0  2  1
            //f(a) = f(a  +  1)
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, TiType.Int32);
            solver.SetArithmeticalOp(2, 0, 1);
            
            solver.SetInvoke(3, "f(1)" , new[]{2});
            solver.SetFunDefenition("f(1)", 4, 3);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(1,res.GenericsCount);

            Assert.AreEqual(TiType.Fun(TiType.Generic(0), TiType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Real, res.GetVarType("f(1) a"));
        }
        [Test]
        public void RecursiveFunction_BoolAndArithmOp_Solved()
        {
            //  6    1 0   5  2 4 3
            //f(a) = f(a) and a > 0
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetInvoke(1, "f(1)",new[]{0});
            
            solver.SetVar(2, "f(1) a");
            solver.SetConst(3, TiType.Int32); 
            solver.SetComparationOperator(4, 2, 3);
            
            solver.SetCall(new CallDefenition(TiType.Bool, new[] {5, 1, 4}));

            solver.SetFunDefenition("f(1)", 6, 5);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Bool, TiType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Real, res.GetVarType("f(1) a"));
        }
        
        [Test]
        public void RecursiveFunction_CompareAndBoolOp_Solved()
        {
            //  6    0 2 1 5   4 3
            //f(a) = a > 0 and f(a) 
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
           
            solver.SetVar(0, "f(1) a");
            solver.SetConst(1, TiType.Int32); 
            solver.SetComparationOperator(2, 0, 1);

            solver.SetVar(3, "f(1) a");
            solver.SetInvoke(4, "f(1)",new[]{3});
            
            solver.SetCall(new CallDefenition(TiType.Bool, new[] {5, 2, 4}));

            solver.SetFunDefenition("f(1)", 6, 5);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Bool, TiType.Real), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Real, res.GetVarType("f(1) a"));
        }
        [Test]
        public void RecursiveFunction_ArithmeticalOnItsCall_Solved()
        {
            //  5    1 0  4 3 2
            //f(a) = f(a) + f(2)
            var tA = solver.SetNewVarOrThrow("f(1) a");
            var tOut = solver.MakeGeneric();
            solver.SetVarType("f(1)", TiType.Fun(tOut, tA));
            
            solver.SetVar(0, "f(1) a");
            solver.SetInvoke(1, "f(1)",new[]{0});

            solver.SetConst(2, TiType.Int32);
            solver.SetInvoke(3, "f(1)",new[]{2});

            solver.SetArithmeticalOp(4, 1, 3);
            
            solver.SetFunDefenition("f(1)", 5, 4);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Int32), res.GetVarType("f(1)"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("f(1) a"));
        }
        
        
        [Test]
        public void TwoRecursiveFunctions_ArithmeticalOnItCall_Solved()
        {
            //   5     1 0  4  3 2
            //f1(a) = f2(a) + f2(2)
            //  10     7 6  9 8
            //f2(a) = f1(a) + 1 

            var tA1 = solver.SetNewVarOrThrow("f1(1) a");
            var tOut1 = solver.MakeGeneric();
            solver.SetVarType("f1(1)", TiType.Fun(tOut1, tA1));
            
            
            var tA2 = solver.SetNewVarOrThrow("f2(1) a");
            var tOut2 = solver.MakeGeneric();
            solver.SetVarType("f2(1)", TiType.Fun(tOut2, tA2));
            
            solver.SetVar(0, "f1(1) a");
            solver.SetInvoke(1, "f2(1)",new[]{0});
            solver.SetConst(2, TiType.Int32);
            solver.SetInvoke(3, "f2(1)",new[]{2});
            solver.SetArithmeticalOp(4, 1, 3);
            solver.SetFunDefenition("f1(1)", 5, 4);
            
            solver.SetVar(6, "f2(1) a");
            solver.SetInvoke(7, "f1(1)",new[]{6});
            solver.SetConst(8, TiType.Int32);
            solver.SetArithmeticalOp(9, 7, 8);
            solver.SetFunDefenition("f2(1)", 10, 9);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Int32), res.GetVarType("f1(1)"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Int32), res.GetVarType("f2(1)"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("f2(1) a"));
        }
        
        
        
        [Test]
        public void TwoRecursiveFunctions_SpecifyOuptut_ArithmeticalOnItCall_Solved()
        {
            //   5     1 0  4  3 2
            //f1(a) = f2(a) + f2(2)
            //  10     7 6  9 8
            //f2(a):int = f1(a) + 1 

            var tA1 = solver.SetNewVarOrThrow("f1(1) a");
            var tOut1 = solver.MakeGeneric();
            solver.SetVarType("f1(1)", TiType.Fun(tOut1, tA1));
            
            
            var tA2 = solver.SetNewVarOrThrow("f2(1) a");
            solver.SetVarType("f2(1)", TiType.Fun(SolvingNode.CreateStrict(TiType.Int32), tA2));
            
            solver.SetVar(0, "f1(1) a");
            solver.SetInvoke(1, "f2(1)",new[]{0});
            solver.SetConst(2, TiType.Int32);
            solver.SetInvoke(3, "f2(1)",new[]{2});
            solver.SetArithmeticalOp(4, 1, 3);
            solver.SetFunDefenition("f1(1)", 5, 4);
            
            solver.SetVar(6, "f2(1) a");
            solver.SetInvoke(7, "f1(1)",new[]{6});
            solver.SetConst(8, TiType.Int32);
            solver.SetArithmeticalOp(9, 7, 8);
            solver.SetFunDefenition("f2(1)", 10, 9);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Int32, TiType.Int32), res.GetVarType("f1(1)"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(TiType.Fun(TiType.Int32, TiType.Int32), res.GetVarType("f2(1)"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("f2(1) a"));
        }
        
        [Test]
        public void TwoRecursiveFunctions_InputArgSpecified_ArithmeticalOnItCall_Solved()
        {
            //   5     1 0  4  3 2
            //f1(a) = f2(a) + f2(2)
            //  10     7 6  9 8
            //f2(a:real) = f1(a) + 1 

            var tA1 = solver.SetNewVarOrThrow("f1(1) a");
            var tOut1 = solver.MakeGeneric();
            solver.SetVarType("f1(1)", TiType.Fun(tOut1, tA1));
            
            
            solver.SetVarType("f2(1) a", TiType.Real);
            var tOut2 = solver.MakeGeneric();
            solver.SetVarType("f2(1)", TiType.Fun(tOut2, SolvingNode.CreateStrict(TiType.Real)));
            
            solver.SetVar(0, "f1(1) a");
            solver.SetInvoke(1, "f2(1)",new[]{0});
            solver.SetConst(2, TiType.Int32);
            solver.SetInvoke(3, "f2(1)",new[]{2});
            solver.SetArithmeticalOp(4, 1, 3);
            solver.SetFunDefenition("f1(1)", 5, 4);
            
            solver.SetVar(6, "f2(1) a");
            solver.SetInvoke(7, "f1(1)",new[]{6});
            solver.SetConst(8, TiType.Int32);
            solver.SetArithmeticalOp(9, 7, 8);
            solver.SetFunDefenition("f2(1)", 10, 9);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Real), res.GetVarType("f1(1)"));
            Assert.AreEqual(TiType.Real, res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Real), res.GetVarType("f2(1)"));
            Assert.AreEqual(TiType.Real, res.GetVarType("f2(1) a"));
        }
        
        [Test]
        public void TwoGenericRecursiveFunctions_GenericsFound()
        {
            //   2     1 0 
            //f1(a) = f2(a)
            //  5     4 3
            //f2(a) = f1(a)

            var tA1 = solver.SetNewVarOrThrow("f1(1) a");
            var tOut1 = solver.MakeGeneric();
            solver.SetVarType("f1(1)", TiType.Fun(tOut1, tA1));
            
            var tA2 = solver.SetNewVarOrThrow("f2(1) a");
            var tOut2 = solver.MakeGeneric();
            solver.SetVarType("f2(1)", TiType.Fun(tOut2, tA2));
            
            solver.SetVar(0, "f1(1) a");
            solver.SetInvoke(1, "f2(1)",new[]{0});
            solver.SetFunDefenition("f1(1)", 2, 1);
            
            solver.SetVar(3, "f2(1) a");
            solver.SetInvoke(4, "f1(1)",new[]{3});
            solver.SetFunDefenition("f2(1)", 5, 4);
            
            var res = solver.Solve();
            Assert.AreEqual(2,res.GenericsCount);
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(TiType.Fun(TiType.Generic(1), TiType.Generic(0)), res.GetVarType("f1(1)"));
            Assert.AreEqual(TiType.Generic(0), res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(TiType.Fun(TiType.Generic(1), TiType.Generic(0)), res.GetVarType("f2(1)"));
            Assert.AreEqual(TiType.Generic(0), res.GetVarType("f2(1) a"));
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
            
            
            var tA1 = solver.SetNewVarOrThrow("f1(1) a");
            var tOut1 = solver.MakeGeneric();
            solver.SetVarType("f1(1)", TiType.Fun(tOut1, tA1));
            
            var tA2 = solver.SetNewVarOrThrow("f2(1) a");
            var tOut2 = solver.MakeGeneric();
            solver.SetVarType("f2(1)", TiType.Fun(tOut2, tA2));

            var tA3 = solver.SetNewVarOrThrow("f3(1) a");
            var tOut3 = solver.MakeGeneric();
            solver.SetVarType("f3(1)", TiType.Fun(tOut3, tA3));


            #region first
            solver.SetVar(0, "f1(1) a");
            solver.SetInvoke(1, "f2(1)",new[]{0});
            solver.SetConst(2, TiType.Int32);
            solver.SetInvoke(3, "f2(1)",new[]{2});
            solver.SetArithmeticalOp(4, 1, 3);
            solver.SetFunDefenition("f1(1)", 5, 4);
            #endregion

            #region second
            solver.SetVar(6, "f2(1) a");
            solver.SetInvoke(7, "f1(1)",new[]{6});
            solver.SetConst(8, TiType.Int32);
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
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Int32), res.GetVarType("f1(1)"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("f1(1) a"));
            
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Int32), res.GetVarType("f2(1)"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("f2(1) a"));
            
            Assert.AreEqual(TiType.Fun(TiType.Real, TiType.Int32), res.GetVarType("f3(1)"));
            Assert.AreEqual(TiType.Int32, res.GetVarType("f3(1) a"));
        }
    }
}