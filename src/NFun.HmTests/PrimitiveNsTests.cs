using System.Linq;
using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace TysoTake2.TypeSolvingNodes.Tests
{
    public class PrimitiveNsTests
    {
        private FSolver _f;

        private CallDef FunInvoke(int nodeId, int funId, int[] argsId) =>
            new CallDef(
                new[]
                {
                    FType.Generic(0),
                    FType.GenericFun(argsId.Length),
                }.Concat(Enumerable.Range(1, argsId.Length).Select(FType.Generic)).ToArray()
                ,new[] {nodeId,funId}.Concat(argsId).ToArray()
            );
        private CallDef ArrayIndex(int nodeId, int arrayId, int indexId)
            =>new CallDef(
                new[]
                {
                    FType.Generic(0),
                    FType.ArrayOf(FType.Generic(0)),
                    FType.Int32 
                },new[] {nodeId, arrayId, indexId}
            );

        [SetUp]
        public void Setup()
        {    
            _f = new FSolver();
        }
       
        
        
        [Test]
        public void ArrayConcat_GenericFound()
        {
            //node|   2    0 1
            //expr| concat(a,b)
            _f.SetVar(0, "a");
            _f.SetVar(1, "b");
            _f.SetCall(new CallDef(
                    FType.ArrayOf(FType.Generic(0)),new[] {0, 1, 2}
            ));
            var result = _f.Solve();

            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), result.GetNodeType(0));
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), result.GetNodeType(1));
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), result.GetNodeType(2));
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), result.GetVarType("a"));
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), result.GetVarType("b"));
        }
        [Test]
        public void CallWithAnonymousFunction_GenericSolved()
        {
            //node|          8 32 0 1  7  6  4 5
            //expr| y(a,b,f) =  f(a,b) + get(a,b) 
            _f.SetVar(0, "a");
            _f.SetVar(1, "b");
            _f.SetVar(2, "f");
            _f.SetCall(FunInvoke(3, 2, new[] {0, 1}));
            

            
            _f.SetVar(4, "a");
            _f.SetVar(5, "b");
            _f.SetCall(ArrayIndex(6, 4, 5));
            
            _f.SetCall(new CallDef(FType.Int32, new[] {3, 7, 6}));
            _f.Unite(8, 7);
            
            var result = _f.Solve();

            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.Fun(FType.Int32, FType.ArrayOf(FType.Int32), FType.Int32), result.GetVarType("f"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), result.GetVarType("a"));
            Assert.AreEqual(FType.Int32, result.GetVarType("b"));
            Assert.AreEqual(FType.Int32, result.GetNodeType(8));

        }
        [Test]
        public void CallWithAnonymousFunction_GenericTypesAreCorrect()
        {
            //node|          3  2 0 1 
            //expr| y(a,b,f) =  f(a,b) 
            _f.SetVar(0, "a");
            _f.SetVar(1, "b");
            _f.SetVar(2, "f");
                
            _f.SetCall(FunInvoke(3,2,new[]{0,1}));
            
            var result = _f.Solve();

            Assert.AreEqual(3, result.GenericsCount);
            Assert.AreEqual(FType.Fun(FType.Generic(2),FType.Generic(0), FType.Generic(1)), result.GetVarType("f"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("a"));
            Assert.AreEqual(FType.Generic(1), result.GetVarType("b"));
            Assert.AreEqual(FType.Generic(2), result.GetNodeType(3));

        }
        
        [Test]
        public void ArrayGet_GenericSolved()
        {
            //node|  2  0 1  4 3
            //expr| get(a,0) + 1
            _f.SetVar(0, "a");
            Assert.IsTrue(_f.SetStrict(1, FType.Int32));
            Assert.IsTrue(_f.SetCall(ArrayIndex(2, 0, 1)));
            
            Assert.IsTrue(_f.SetCall(new CallDef(FType.Int32, new[] {2, 3, 4})));
            
            var result = _f.Solve();
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), result.GetVarType("a"));
        }
        
      


        
        [Test(Description = "y = 1 + 2 * x")]
        public void SolvingIntWithSingleVar()
        {
            //node |5   0 4 1 3 2
            //expr |y = 1 + 2 * x;

            _f.SetStrict(0, FType.Int32);
            _f.SetStrict(1, FType.Int32);
            _f.SetVar(2, "x");
            _f.SetStrict(2, FType.Int32);
            _f.SetStrict(3, FType.Int32);
            _f.SetStrict(4, FType.Int32);
            _f.Unite(5,4);
            _f.SetVar(5,"y");

            var result = _f.Solve();
            
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.Int32, result.GetVarType("x"));
            Assert.AreEqual(FType.Int32, result.GetVarType("y"));
        }

        [Test]
        public void SolvingSimpleCallForArray()
        {
            //node |2   1   0
            //expr |y = sum(a)
            
            _f.SetVar(0, "a");  
            _f.SetStrict(0, FType.ArrayOf(FType.Int32));
            _f.SetStrict(1, FType.Int32);
            _f.Unite(2, 1);
            _f.SetVar(2, "y");
            var result = _f.Solve();
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), result.GetVarType("a"));
            Assert.AreEqual(FType.Int32, result.GetVarType("y"));
        }
        
        [Test]
        public void SolvingSimpleCallForArrayThatReturnsArray()
        {
            //node |2   1       0
            //expr |y = reverse(a)
            
            _f.SetVar(0, "a");  
            _f.SetStrict(0, FType.ArrayOf(FType.Int32));
            _f.SetStrict(1, FType.ArrayOf(FType.Int32));
            _f.Unite(2, 1);
            _f.SetVar(2, "y");
            var result = _f.Solve();
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), result.GetVarType("a"));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), result.GetVarType("y"));
        }

        [Test(Description = "y = if a: 1 else 0")]
        public void SolvingSimpleCaseWithIfs()
        {
            //node |6 5  2 1  0  4   3 
            //expr |y = if a: 1 else 0;

            _f.SetStrict(0, FType.Int32);
            _f.SetVar(1, "a");  
            _f.SetStrict(1, FType.Bool);
            _f.Unite(2,0);
            _f.SetStrict(3, FType.Int32);
            _f.Unite(4,2);
            _f.Unite(5,2);
            _f.Unite(6,5);
            _f.SetVar(6,"y");
            var result = _f.Solve();

            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.Bool, result.GetVarType("a"));
            Assert.AreEqual(FType.Int32, result.GetVarType("y"));

        }


        [Test(Description = "y=if a:x; else z+1;")]
        public void SolvingCaseWithIfs()
        {
            //node |5  6    1  0        2 4 3
            //expr |y = if (a) x; else (z + 1);

            _f.SetVar(0, "x");
            _f.SetVar(1, "a");
            _f.SetStrict(1, FType.Bool);
            _f.SetVar(2, "z");
            _f.SetStrict(2, FType.Int32);
            _f.SetStrict(3, FType.Int32);
            _f.SetStrict(4, FType.Int32);
            _f.Unite(0, 4);
            _f.Unite(0, 6);
            _f.SetVar(5, "y");
            _f.Unite(5, 6);

            var result = _f.Solve();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, result.GenericsCount);
                Assert.AreEqual(FType.Bool, result.GetVarType("a"),"a");
                Assert.AreEqual(FType.Int32, result.GetVarType("x"),"x");
                Assert.AreEqual(FType.Int32, result.GetVarType("z"),"z");
                Assert.AreEqual(FType.Int32, result.GetVarType("y"),"y");
            });
        }

        [Test(Description = "y = if (a) x else z ")]
        public void CleanGenericOnIfs()
        {
            //node |6 5  2    1   0  4   3 
            //expr |y = if (true) x else z 

            _f.SetVar(0, "x");  
            _f.SetStrict(1, FType.Bool);
            _f.Unite(2,0);
            _f.SetVar(3, "z");
            _f.Unite(4,3);
            _f.Unite(4,2);
            _f.Unite(5,4);
            _f.Unite(6,5);
            _f.SetVar(6,"y");

            var result = _f.Solve();
            
            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(FType.Generic(0), result.GetVarType("x"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("z"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("y"));
        }
        
        [Test]
        public void OutputEqualsInput_simpleGeneric()
        {
            //node |1   0
            //expr |y = x 

            _f.SetVar(0, "x");
            _f.Unite(1,0);
            _f.SetVar(1, "y");

            var result = _f.Solve();
            
            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(FType.Generic(0), result.GetVarType("x"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("y"));
        }
        
        [Test]
        public void InputRepeats_simpleGeneric()
        {
            //node |3   0 2 1 
            //expr |y = x + x 
            _f.SetVar(0, "x");
            _f.SetVar(1, "x");

            _f.SetCall(new CallDef(
                new[] {FType.Int32, FType.Int32, FType.Int32},
                new[] {2, 0, 1}));

            _f.SetVar(3,"y");
            _f.Unite(3,2);

            var result = _f.Solve();
            
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.Int32, result.GetVarType("x"));
            Assert.AreEqual(FType.Int32, result.GetVarType("y"));
        }
        
        [Test(Description = "y = x; | y2 = x2")]
        public void TwoSimpleGenerics()
        {
            //node |1   0  | 3    2
            //expr |y = x; | y2 = x2

            _f.SetVar(0, "x");
            _f.Unite(1,0);
            _f.SetVar(1, "y");

            _f.SetVar(2, "x2");
            _f.Unite(3,2);
            _f.SetVar(3, "y2");
            
            var result = _f.Solve();
            
            Assert.AreEqual(2, result.GenericsCount);
            
            Assert.AreEqual(FType.Generic(0), result.GetVarType("x"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("y"));
            
            Assert.AreEqual(FType.Generic(1), result.GetVarType("x2"));
            Assert.AreEqual(FType.Generic(1), result.GetVarType("y2"));
            
        }

        [Test]
        public void InvalidTypes_SetEqualityReturnsFalse()
        {
            _f.SetStrict(0, FType.Int32);
            _f.SetStrict(1, FType.Bool);
            Assert.IsFalse(_f.Unite(0, 1));
        }
        [Test]
        public void CallWithAnonymousFunctionOnIf_GenericTypesAreCorrect()
        {
            //node|                  6  3  2 0 1   4      5
            //expr| max(cmp, a,b) =  if (cmp(a,b)) a else b 
            _f.SetVar(0, "a");
            _f.SetVar(1, "b");
            _f.SetVar(2, "cmp");
            _f.SetCall(FunInvoke(3, 2, new[] {0, 1}));

            _f.SetStrict(3, FType.Bool);
            _f.SetVar(4, "a");
            _f.SetVar(5, "b");
            _f.Unite(6, 4);
            _f.Unite(6, 5);


            var result = _f.Solve();

            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(FType.Fun(FType.Bool, FType.Generic(0), FType.Generic(0)), result.GetVarType("cmp"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("a"));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("b"));
            Assert.AreEqual(FType.Generic(0), result.GetNodeType(6));

        }
    }
}