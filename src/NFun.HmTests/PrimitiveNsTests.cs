using System.Linq;
using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests
{
    public class PrimitiveNsTests
    {
        private TiSolver _ti;

        private CallDefenition FunInvoke(int nodeId, int funId, int[] argsId) =>
            new CallDefenition(
                new[]
                {
                    TiType.Generic(0),
                    TiType.GenericFun(argsId.Length),
                }.Concat(Enumerable.Range(1, argsId.Length).Select(TiType.Generic)).ToArray()
                ,new[] {nodeId,funId}.Concat(argsId).ToArray()
            );
        private CallDefenition ArrayIndex(int nodeId, int arrayId, int indexId)
            =>new CallDefenition(
                new[]
                {
                    TiType.Generic(0),
                    TiType.ArrayOf(TiType.Generic(0)),
                    TiType.Int32 
                },new[] {nodeId, arrayId, indexId}
            );

        [SetUp]
        public void Setup()
        {    
            _ti = new TiSolver();
        }
       
        
        
        [Test]
        public void ArrayConcat_GenericFound()
        {
            //node|   2    0 1
            //expr| concat(a,b)
            _ti.SetVar(0, "a");
            _ti.SetVar(1, "b");
            _ti.SetCall(new CallDefenition(
                    TiType.ArrayOf(TiType.Generic(0)),new[] {0, 1, 2}
            ));
            var result = _ti.Solve();

            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(0)), result.GetNodeType(0));
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(0)), result.GetNodeType(1));
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(0)), result.GetNodeType(2));
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(0)), result.GetVarType("a"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(0)), result.GetVarType("b"));
        }
        [Test]
        public void CallWithAnonymousFunction_GenericSolved()
        {
            //node|          8 32 0 1  7  6  4 5
            //expr| y(a,b,f) =  f(a,b) + get(a,b) 
            _ti.SetVar(0, "a");
            _ti.SetVar(1, "b");
            _ti.SetVar(2, "f");
            _ti.SetCall(FunInvoke(3, 2, new[] {0, 1}));
            

            
            _ti.SetVar(4, "a");
            _ti.SetVar(5, "b");
            _ti.SetCall(ArrayIndex(6, 4, 5));
            
            _ti.SetCall(new CallDefenition(TiType.Int32, new[] {3, 7, 6}));
            _ti.Unite(8, 7);
            
            var result = _ti.Solve();

            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(TiType.Fun(TiType.Int32, TiType.ArrayOf(TiType.Int32), TiType.Int32), result.GetVarType("f"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), result.GetVarType("a"));
            Assert.AreEqual(TiType.Int32, result.GetVarType("b"));
            Assert.AreEqual(TiType.Int32, result.GetNodeType(8));

        }
        [Test]
        public void CallWithAnonymousFunction_GenericTypesAreCorrect()
        {
            //node|          3  2 0 1 
            //expr| y(a,b,f) =  f(a,b) 
            _ti.SetVar(0, "a");
            _ti.SetVar(1, "b");
            _ti.SetVar(2, "f");
                
            _ti.SetCall(FunInvoke(3,2,new[]{0,1}));
            
            var result = _ti.Solve();

            Assert.AreEqual(3, result.GenericsCount);
            Assert.AreEqual(TiType.Fun(TiType.Generic(2),TiType.Generic(0), TiType.Generic(1)), result.GetVarType("f"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("a"));
            Assert.AreEqual(TiType.Generic(1), result.GetVarType("b"));
            Assert.AreEqual(TiType.Generic(2), result.GetNodeType(3));

        }
        
        [Test]
        public void ArrayGet_GenericSolved()
        {
            //node|  2  0 1  4 3
            //expr| get(a,0) + 1
            _ti.SetVar(0, "a");
            Assert.IsTrue(_ti.SetStrict(1, TiType.Int32));
            Assert.IsTrue(_ti.SetCall(ArrayIndex(2, 0, 1)));
            
            Assert.IsTrue(_ti.SetCall(new CallDefenition(TiType.Int32, new[] {2, 3, 4})));
            
            var result = _ti.Solve();
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), result.GetVarType("a"));
        }
        
      


        
        [Test(Description = "y = 1 + 2 * x")]
        public void SolvingIntWithSingleVar()
        {
            //node |5   0 4 1 3 2
            //expr |y = 1 + 2 * x;

            _ti.SetStrict(0, TiType.Int32);
            _ti.SetStrict(1, TiType.Int32);
            _ti.SetVar(2, "x");
            _ti.SetStrict(2, TiType.Int32);
            _ti.SetStrict(3, TiType.Int32);
            _ti.SetStrict(4, TiType.Int32);
            _ti.Unite(5,4);
            _ti.SetVar(5,"y");

            var result = _ti.Solve();
            
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(TiType.Int32, result.GetVarType("x"));
            Assert.AreEqual(TiType.Int32, result.GetVarType("y"));
        }

        [Test]
        public void SolvingSimpleCallForArray()
        {
            //node |2   1   0
            //expr |y = sum(a)
            
            _ti.SetVar(0, "a");  
            _ti.SetStrict(0, TiType.ArrayOf(TiType.Int32));
            _ti.SetStrict(1, TiType.Int32);
            _ti.Unite(2, 1);
            _ti.SetVar(2, "y");
            var result = _ti.Solve();
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), result.GetVarType("a"));
            Assert.AreEqual(TiType.Int32, result.GetVarType("y"));
        }
        
        [Test]
        public void SolvingSimpleCallForArrayThatReturnsArray()
        {
            //node |2   1       0
            //expr |y = reverse(a)
            
            _ti.SetVar(0, "a");  
            _ti.SetStrict(0, TiType.ArrayOf(TiType.Int32));
            _ti.SetStrict(1, TiType.ArrayOf(TiType.Int32));
            _ti.Unite(2, 1);
            _ti.SetVar(2, "y");
            var result = _ti.Solve();
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), result.GetVarType("a"));
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), result.GetVarType("y"));
        }

        [Test(Description = "y = if a: 1 else 0")]
        public void SolvingSimpleCaseWithIfs()
        {
            //node |6 5  2 1  0  4   3 
            //expr |y = if a: 1 else 0;

            _ti.SetStrict(0, TiType.Int32);
            _ti.SetVar(1, "a");  
            _ti.SetStrict(1, TiType.Bool);
            _ti.Unite(2,0);
            _ti.SetStrict(3, TiType.Int32);
            _ti.Unite(4,2);
            _ti.Unite(5,2);
            _ti.Unite(6,5);
            _ti.SetVar(6,"y");
            var result = _ti.Solve();

            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(TiType.Bool, result.GetVarType("a"));
            Assert.AreEqual(TiType.Int32, result.GetVarType("y"));

        }


        [Test(Description = "y=if a:x; else z+1;")]
        public void SolvingCaseWithIfs()
        {
            //node |5  6    1  0        2 4 3
            //expr |y = if (a) x; else (z + 1);

            _ti.SetVar(0, "x");
            _ti.SetVar(1, "a");
            _ti.SetStrict(1, TiType.Bool);
            _ti.SetVar(2, "z");
            _ti.SetStrict(2, TiType.Int32);
            _ti.SetStrict(3, TiType.Int32);
            _ti.SetStrict(4, TiType.Int32);
            _ti.Unite(0, 4);
            _ti.Unite(0, 6);
            _ti.SetVar(5, "y");
            _ti.Unite(5, 6);

            var result = _ti.Solve();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, result.GenericsCount);
                Assert.AreEqual(TiType.Bool, result.GetVarType("a"),"a");
                Assert.AreEqual(TiType.Int32, result.GetVarType("x"),"x");
                Assert.AreEqual(TiType.Int32, result.GetVarType("z"),"z");
                Assert.AreEqual(TiType.Int32, result.GetVarType("y"),"y");
            });
        }

        [Test(Description = "y = if (a) x else z ")]
        public void CleanGenericOnIfs()
        {
            //node |6 5  2    1   0  4   3 
            //expr |y = if (true) x else z 

            _ti.SetVar(0, "x");  
            _ti.SetStrict(1, TiType.Bool);
            _ti.Unite(2,0);
            _ti.SetVar(3, "z");
            _ti.Unite(4,3);
            _ti.Unite(4,2);
            _ti.Unite(5,4);
            _ti.Unite(6,5);
            _ti.SetVar(6,"y");

            var result = _ti.Solve();
            
            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("x"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("z"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("y"));
        }
        
        [Test]
        public void OutputEqualsInput_simpleGeneric()
        {
            //node |1   0
            //expr |y = x 

            _ti.SetVar(0, "x");
            _ti.Unite(1,0);
            _ti.SetVar(1, "y");

            var result = _ti.Solve();
            
            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("x"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("y"));
        }
        
        [Test]
        public void InputRepeats_simpleGeneric()
        {
            //node |3   0 2 1 
            //expr |y = x + x 
            _ti.SetVar(0, "x");
            _ti.SetVar(1, "x");

            _ti.SetCall(new CallDefenition(
                new[] {TiType.Int32, TiType.Int32, TiType.Int32},
                new[] {2, 0, 1}));

            _ti.SetVar(3,"y");
            _ti.Unite(3,2);

            var result = _ti.Solve();
            
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(TiType.Int32, result.GetVarType("x"));
            Assert.AreEqual(TiType.Int32, result.GetVarType("y"));
        }
        
        [Test(Description = "y = x; | y2 = x2")]
        public void TwoSimpleGenerics()
        {
            //node |1   0  | 3    2
            //expr |y = x; | y2 = x2

            _ti.SetVar(0, "x");
            _ti.Unite(1,0);
            _ti.SetVar(1, "y");

            _ti.SetVar(2, "x2");
            _ti.Unite(3,2);
            _ti.SetVar(3, "y2");
            
            var result = _ti.Solve();
            
            Assert.AreEqual(2, result.GenericsCount);
            
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("x"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("y"));
            
            Assert.AreEqual(TiType.Generic(1), result.GetVarType("x2"));
            Assert.AreEqual(TiType.Generic(1), result.GetVarType("y2"));
            
        }

        [Test]
        public void InvalidTypes_SetEqualityReturnsFalse()
        {
            _ti.SetStrict(0, TiType.Int32);
            _ti.SetStrict(1, TiType.Bool);
            Assert.IsFalse(_ti.Unite(0, 1));
        }
        [Test]
        public void CallWithAnonymousFunctionOnIf_GenericTypesAreCorrect()
        {
            //node|                  6  3  2 0 1   4      5
            //expr| max(cmp, a,b) =  if (cmp(a,b)) a else b 
            _ti.SetVar(0, "a");
            _ti.SetVar(1, "b");
            _ti.SetVar(2, "cmp");
            _ti.SetCall(FunInvoke(3, 2, new[] {0, 1}));

            _ti.SetStrict(3, TiType.Bool);
            _ti.SetVar(4, "a");
            _ti.SetVar(5, "b");
            _ti.Unite(6, 4);
            _ti.Unite(6, 5);


            var result = _ti.Solve();

            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(TiType.Fun(TiType.Bool, TiType.Generic(0), TiType.Generic(0)), result.GetVarType("cmp"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("a"));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("b"));
            Assert.AreEqual(TiType.Generic(0), result.GetNodeType(6));

        }
    }
}