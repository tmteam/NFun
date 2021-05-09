using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests.UserFunctions
{
    [TestFixture]
    public class GenericUserFunctionsTest
    {
        [TestCase("fake(a) = a\r y = fake(1.0)",1.0)]
        [TestCase("fake(a) = a\r y = fake(fake(true))",true)]
        [TestCase("fake(a) = a\r y = 'test'.fake().fake().fake()","test")]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,true)",1.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,false)",2.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2.0,true)",1.0)]
        [TestCase(@"choise(a,b,c,d,con1,con2) = 
                          if(con1) 
                                if(con2) a 
                                else     b
                          else 
                                if(con2) c
                                else     d

                    y:int = choise(1,2,3,4,false,true)",3)]
        
        [TestCase(@"choise(a,b,c,d,con1,con2) = 
                          if(con1) 
                                if(con2) a[0] 
                                else     b
                          else 
                                if(con2) c
                                else     d[0]

                    y:int = choise([1],2,3,[4,5],false,false)", 4)]
        
        [TestCase("mkarr(a,b,c,d,takefirst) = if(takefirst) [a,b] else [c,d]\r y = mkarr(1,2,3,4,false)",new[]{3.0,4.0})]
        [TestCase("repeat(a) = a.concat(a)\r y = [1,2,3].repeat()",new[]{1.0,2.0,3.0,1.0,2.0,3.0})]
        [TestCase("repeat(a) = a.concat(a)\r y = ['a','b'].repeat().repeat()",new[]{"a","b","a","b","a","b","a","b"})]
        [TestCase("first(a) = a[0]\r y = [5,4,3].first()",5.0)]
        [TestCase("first(a) = a[0]\r y = [[5,4],[3,2],[1]].first()",new[]{5.0,4.0})]
        [TestCase("first(a) = a[0]\r y = [[5.0,4.0],[3.0,2.0],[1.0]].first().first()",5.0)]
        [TestCase("first(a, f) = a.filter(f)[0] \r y = [1,2,3].first{it>1}", 2.0)]
        [TestCase("first(a, f) = a.filter(f)[0] \r y = [1.0,2.0,3.0].first{it>1}", 2.0)]
        [TestCase("filtrepeat(a, f) = a.concat(a).filter(f) \r y = [1.0,2.0,3.0].filtrepeat{it>1}", new[]{2.0,3.0,2.0,3.0})]
        [TestCase("concat(a, b,c) = a.concat(b).concat(c) \r y:int[] = concat([1,2],[3,4],[5,6])", new[]{1,2,3,4,5,6})]
        [TestCase(@"car1(g) = g(2); my(x)=x-1; y =  car1(my)   ", 1.0)]
        [TestCase(@"car1(g) = g(2,3,4); my(a,b,c)=a+b+c; y = car1(my)   ", 9.0)]
        [TestCase(@"choose(f1, f2,  selector, arg1, arg2) = if(selector) f1(arg1,arg2) else f2(arg1,arg2); 
                   y =  choose(max, min, true, 1,2)", 2.0)]
        [TestCase(@"car0(g) = g(2,4); y = car0(max)    ", 4.0)]
        [TestCase(@"car2(g) = g(2,4); y = car2(min)    ", 2.0)]
        public void ConstantEquation(string expr, object expected)
        {
            var runtime = FunBuilder.Build(expr);
            runtime.Calculate().AssertReturns(VarVal.New("y", expected));
        }

        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(0x1,2.0,true)",1.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(0x1,2.0,false)",2.0)]

        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,false,true)",(object)1.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,false,false)",(object)false)]
        //todo         [Ignore("complex lca")]
        //[TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,[1,2],true)",(object)1.0)]
        //[TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,[1,2],false)",new double[]{1,2})]
        //[TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:anything = choise(1,[1,2],true)",(object)1.0)]
        //[TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:anything = choise(1,[1,2],false)",new double[]{1,2})]
        
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:anything = choise(0x1,2.0,true)",1)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:anything = choise(0x1,2.0,false)",2.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:anything = choise(1,false,true)",(object)1.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:anything = choise(1,false,false)",(object)false)]
        
        public void ConstantEquationWithUpcast(string expr, object expected)
        {
            var runtime = FunBuilder.Build(expr);
            var result = runtime.Calculate().GetValueOf("y");
            Assert.IsTrue(TypeHelper.AreEqual(result, expected), $"result: {result} expected: {expected}");
        }
        
        [TestCase("repeat(a) = a.concat(a); " +
                  "a = [1.0].repeat().repeat();" +
                  "b = ['a'].repeat().repeat();",new double[]{1,1,1,1},new[]{"a", "a" , "a" , "a" })]
        
        [TestCase("sum(a,b,c) = a+b+c; " +
                  "a:real = sum(1,2,3);" +
                  "b:int  = sum(1,2,3);", 6.0, 6)]
        public void ConstantEquationWithTwoUsesOfGenerics(string expr, object expectedA, object expectedB)
        {
            var runtime = FunBuilder.Build(expr);
            runtime.Calculate()
                .AssertHas(VarVal.New("a", expectedA))
                .AssertHas(VarVal.New("b", expectedB));
        }

        [Test]
        public void SelectOverload()
        {
            var expr =
                @"  
                #custom user function max(r r r) overloads
                #built in function max(r r)
                max(i, j, k) = i.max(j).max(k)
  
                userfun = max(1, 2, 3)
                builtin = max(1, 2)";
            FunBuilder.Build(expr).Calculate()
                .AssertReturns(VarVal.New("userfun", 3.0), VarVal.New("builtin", 2.0));
        }

        [Test]
        public void GenericRecursive()
        {
            var expr =
                @"fact(n) = if (n==0) 0
                            if (n == 1) 1
                            else fact(n - 1) * n

                res = [0..4].map(fact)";
            FunBuilder.Build(expr).Calculate()
                .AssertHas(VarVal.New("res", new[] { 0.0, 1.0, 2, 6, 24 }));

        }

        [Test]
        public void TwinGenericFunCall()
        {
            var expr = @"maxOfArray(t) = t.fold(max)

           maxOfMatrix(t) = t.map(maxOfArray).maxOfArray()

  origin = [
              [12,05,06],
              [42,33,12],
              [01,15,18]
             ] 

  res:int = origin.maxOfMatrix()";
            FunBuilder.Build(expr).Calculate()
                .AssertHas(VarVal.New("res", 42));
        }

        [Ignore("UB")]
        [Test]
        public void TwinGenericWrongOrderFunCall()
        {
            var expr = @"

           maxOfMatrix(t) = t.map(maxOfArray).maxOfArray()
            
            maxOfArray(t) = t.fold(max)

  origin = [
              [12,05,06],
              [42,33,12],
              [01,15,18]
             ] 

  res:int = origin.maxOfMatrix()";
            FunBuilder.Build(expr).Calculate()
                .AssertHas(VarVal.New("res", 42));
        }

        [Test]
        public void GenericBubbleSort()
        {
            var expr = @"twiceSet(arr,i,j,ival,jval)
  	                        = arr.set(i,ival).set(j,jval)

                          swap(arr, i, j) 
                            = arr.twiceSet(i,j,arr[j], arr[i])
                          
                          swapIfNotSorted(c, i)
  	                        =	if   (c[i]<c[i+1]) c
  		                        else c.swap(i, i+1)

                          # run thru array 
                          # and swap every unsorted values
                          onelineSort(input) =  
  	                        [0..input.count()-2].fold(input, swapIfNotSorted)		

                          bubbleSort(input)= [0..input.count()-1].fold(input){onelineSort(it1)}

                          i:int[]  = [1,4,3,2,5].bubbleSort()
                          r:real[] = [1,4,3,2,5].bubbleSort()
";


            FunBuilder.Build(expr).Calculate()
                .AssertReturns(
                    VarVal.New("i", new[] { 1, 2, 3, 4, 5 }),
                    VarVal.New("r", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }));

        }
    }
}