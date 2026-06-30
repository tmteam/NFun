namespace NFun.SyntaxTests.Functions;

using TestTools;
using NUnit.Framework;

[TestFixture]
public class GenericUserFunctionsTest {
    [TestCase("fake(a) = a\r y = fake(1.0)", 1.0)]
    [TestCase("fake(a) = a\r y = fake(fake(true))", true)]
    [TestCase("fake(a) = a\r y = 'test'.fake().fake().fake()", "test")]
    // Generic monomorphization preserves narrow primitive types (Int8 reachable
    // via SearchMaxGenericTypeId + SubstituteConcreteTypes).
    [TestCase("id(a) = a\r y:int8 = id(5)", (sbyte)5)]
    [TestCase("id(a) = a\r y:byte = id(5)", (byte)5)]
    [TestCase("id(a) = a\r y:int16 = id(5)", (short)5)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,true)", 1)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,false)", 2)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2.0,true)", 1.0)]
    [TestCase(
        @"choise(a,b,c,d,con1,con2) =
                          if(con1)
                                if(con2) a
                                else     b
                          else
                                if(con2) c
                                else     d

                    y:int = choise(1,2,3,4,false,true)", 3)]
    [TestCase(
        @"choise(a,b,c,d,con1,con2) =
                          if(con1)
                                if(con2) a[0]
                                else     b
                          else
                                if(con2) c
                                else     d[0]

                    y:int = choise([1],2,3,[4,5],false,false)", 4)]
    [TestCase("mkarr(a,b,c,d,takefirst) = if(takefirst) [a,b] else [c,d]\r y = mkarr(1,2,3,4,false)", new[] { 3, 4 })]
    [TestCase("repeat(a) = a.concat(a)\r y = [1,2,3].repeat()", new[] { 1, 2, 3, 1, 2, 3 })]
    [TestCase(
        "repeat(a) = a.concat(a)\r y = ['a','b'].repeat().repeat()",
        new[] { "a", "b", "a", "b", "a", "b", "a", "b" })]
    [TestCase("first(a) = a[0]\r y = [5,4,3].first()", 5)]
    [TestCase("first(a) = a[0]\r y = [[5,4],[3,2],[1]].first()", new[] { 5, 4 })]
    [TestCase("first(a) = a[0]\r y = [[5.0,4.0],[3.0,2.0],[1.0]].first().first()", 5.0)]
    [TestCase("first(a, f) = a.filter(f)[0] \r y = [1,2,3].first(rule it>1)", 2)]
    [TestCase("first(a, f) = a.filter(f)[0] \r y = [1.0,2.0,3.0].first(rule it>1)", 2.0)]
    [TestCase(
        "filtrepeat(a, f) = a.concat(a).filter(f) \r y = [1.0,2.0,3.0].filtrepeat(rule it>1)",
        new[] { 2.0, 3.0, 2.0, 3.0 })]
    [TestCase(
        "concat(a, b,c) = a.concat(b).concat(c) \r y:int[] = concat([1,2],[3,4],[5,6])",
        new[] { 1, 2, 3, 4, 5, 6 })]
    [TestCase(@"car1(g) = g(2); my(x)=x-1; y =  car1(my)   ", 1)]
    [TestCase(@"car1(g) = g(2,3,4); my(a,b,c)=a+b+c; y = car1(my)   ", 9)]
    [TestCase(
        @"choose(f1, f2,  selector, arg1, arg2) = if(selector) f1(arg1,arg2) else f2(arg1,arg2);
                   y =  choose(max, min, true, 1,2)", 2)]
    [TestCase(@"car0(g) = g(2,4); y = car0(max)    ", 4)]
    [TestCase(@"car2(g) = g(2,4); y = car2(min)    ", 2)]
    public void ConstantEquation(string expr, object expected) => expr.AssertReturns("y", expected);

    // Generic monomorphisation to Float32 — requires FloatFamily dialect opt-in.
    [TestCase("id(a) = a\r y:float32 = id(1.5)",  1.5f)]
    [TestCase("id(a) = a\r y:float32 = id(5)",    5.0f)]
    public void Float32_GenericMonomorphisation(string expr, object expected) =>
        expr.BuildWithFloats().Calc().AssertReturns("y", expected);

    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(0x1,2.0,true)", 1.0)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(0x1,2.0,false)", 2.0)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,false,true)", 1)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,false,false)", false)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,[2,3],true)",1)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(0,[1,2],false)",new[]{1,2})]

    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:any = choise(3.0,[4,5],true)",3.0)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:any = choise(6,[7.0,8],false)",new[]{7.0,8.0})]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:any = choise(0x1,2.0,true)", 1)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:any = choise(0x1,2.0,false)", 2.0)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:any = choise(1,false,true)", 1)]
    [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y:any = choise(1,false,false)", false)]
    public void ConstantEquationWithUpcast(string expr, object expected) {
        var result = expr.Calc().Get("y");
        FunnyAssert.AreSame(result, expected, $"result: {result} expected: {expected}");
    }

    [TestCase(
        "repeat(a) = a.concat(a); " +
        "a = [1.0].repeat().repeat();" +
        "b = ['a'].repeat().repeat();", new double[] { 1, 1, 1, 1 }, new[] { "a", "a", "a", "a" })]
    [TestCase(
        "sum(a,b,c) = a+b+c; " +
        "a:real = sum(1,2,3);" +
        "b:int  = sum(1,2,3);", 6.0, 6)]
    public void ConstantEquationWithTwoUsesOfGenerics(string expr, object expectedA, object expectedB) =>
        expr.AssertResultHas("a", expectedA).AssertResultHas("b", expectedB);

    [Test]
    public void SelectOverload() =>
        @"
                #custom user function max(r r r) overloads
                #built in function max(r r)
                max(i, j, k) = i.max(j).max(k)

                userfun = max(1, 2, 3)
                builtin = max(1, 2)"
            .AssertReturns(("userfun", 3), ("builtin", 2));

    [Test]
    public void GenericRecursive() =>
        @"fact(n) = if (n==0) 0
                            if (n == 1) 1
                            else fact(n - 1) * n

                res = [0..4].map(fact)"
            .AssertResultHas("res", new[] { 0, 1, 2, 6, 24 });

    [Test]
    public void TwinGenericFunCall() =>
        @"maxOfArray(t) = t.fold(max)

           maxOfMatrix(t) = t.map(maxOfArray).maxOfArray()

  origin = [
              [12,05,06],
              [42,33,12],
              [01,15,18]
             ]

  res:int = origin.maxOfMatrix()".AssertResultHas("res", 42);

    [Test]
    public void TwinGenericWrongOrderFunCall() {
        var expr = @"

           maxOfMatrix(t) = t.map(maxOfArray).maxOfArray()

            maxOfArray(t) = t.fold(max)

  origin = [
              [12,05,06],
              [42,33,12],
              [01,15,18]
             ]

  res:int = origin.maxOfMatrix()";
        expr.AssertResultHas("res", 42);
    }

    [Test]
    public void GenericBubbleSort() =>
        @"twiceSet(arr,i,j,ival,jval)
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

                          bubbleSort(input)= [0..input.count()-1].fold(input, rule onelineSort(it1))

                          i:int[]  = [1,4,3,2,5].bubbleSort()
                          r:real[] = [1,4,3,2,5].bubbleSort()"
            .AssertReturns(("i", new[] { 1, 2, 3, 4, 5 }), ("r", new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }));


    [TestCase("f(x)= 1; f(x):int = 2; out = 1")]
    [TestCase("f(x)= 1; f(x) = x; out = 1")]
    [TestCase("f(x:int)= 1; f(x:real) = 2; out = 1")]
    [TestCase("f(x)= 1; out = 1; f(x):int = 2; ")]
    [TestCase("f(x)= 1; out = 1; f(x) = x; ")]
    [TestCase("f(x:int)= 1; out = 1; f(x:real) = 2; ")]
    [TestCase("f(x)= 1; f(x):int = 2; out = f(1)")]
    [TestCase("f(x)= 1; f(x) = x; out = f(1)")]
    [TestCase("f(x:int)= 1; f(x:real) = 2; out = f(1)")]
    [TestCase("F(x)= 1; out = f(1); f(x):int = 2; ")]
    [TestCase("out = f(1); F(x)= 1; f(x) = x; ")]
    [TestCase("f(x:int)= 1; out = f(1); F(x:real) = 2; ")]
    [TestCase("f(x)= 1; F(x):int = 2; out = 1")]
    [TestCase("F(x)= 1; f(x) = x; out = 1")]
    [TestCase("f(x:int)= 1; F(x:real) = 2; out = 1")]
    [TestCase("F(x)= 1; out = 1; f(x):int = 2; ")]
    [TestCase("f(x)= 1; out = 1; F(x) = x; ")]
    [TestCase("f(x:int)= 1; out = 1; F(x:real) = 2; ")]
    [TestCase("f(x)= 1; F(x):int = 2; out = f(1)")]
    [TestCase("F(x)= 1; f(x) = x; out = f(1)")]
    [TestCase("f(x:int)= 1; F(x:real) = 2; out = f(1)")]
    [TestCase("f(x)= 1; out = f(1); F(x):int = 2; ")]
    [TestCase("out = f(1); F(x)= 1; f(x) = x; ")]
    [TestCase("F(x:int)= 1; out = f(1); f(x:real) = 2; ")]
    public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();

    #region Float32AndFloat64 dialect
    // Generic user function monomorphisation to float32.

    // pair(a,b) = a + b — inferred to F32.
    [Test]
    public void Float32_Generic_PairSum_InferredF32() {
        var rt = "pair(a,b) = a + b\r y:float32 = pair(1.0, 2.0)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3.0f, rt["y"].Value);
    }

    // first(arr) = arr[0] — with concrete f32[].
    [Test]
    public void Float32_Generic_FirstOfF32Array() {
        var rt = "first(arr) = arr[0]\r arr:float32[]=[1.5,2.5,3.5]\r y:float32 = first(arr)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.5f, rt["y"].Value);
    }

    // last(arr) = arr[arr.count()-1].
    [Test]
    public void Float32_Generic_LastOfF32Array() {
        var rt = "last(arr) = arr[arr.count()-1]\r arr:float32[]=[1.5,2.5,3.5]\r y:float32 = last(arr)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3.5f, rt["y"].Value);
    }

    // Twice a generic ID call.
    [Test]
    public void Float32_Generic_IdComposed() {
        var rt = "id(x) = x\r y:float32 = id(id(1.5))".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.5f, rt["y"].Value);
    }

    // Sum of two f32 arrays with generic zip-style function.
    [Test]
    public void Float32_Generic_MapOverF32Array() {
        var rt = "double(x) = x * 2.0\r arr:float32[] = [1.0,2.0,3.0]\r y = arr.map(rule double(it))".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(new[] { 2.0f, 4.0f, 6.0f }, rt["y"].Value);
    }

    // Generic function with two type params — one f32, one int, target real.
    [Test]
    public void Float32_Generic_TwoDifferentTypesAtCallSite() {
        var rt = "pair(a,b) = a + b\r x:float32=1.5\r y = pair(x, 2)".BuildWithFloats();
        rt.Run();
        // Result narrows to Float32 since f32 is the dominant type in the call.
        Assert.AreEqual(3.5f, rt["y"].Value);
    }

    // Generic function target-narrowed to f32.
    [Test]
    public void Float32_Generic_TwoDifferentTypes_TargetF32() {
        var rt = "pair(a,b) = a + b\r x:float32=1.5\r y:float32 = pair(x, 2)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3.5f, rt["y"].Value);
    }

    // Choise-style with f32 branch.
    [Test]
    public void Float32_Generic_Choise_WithF32() {
        var rt = "pick(a,b,take) = if(take) a else b\r y:float32 = pick(1.5, 2.5, true)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.5f, rt["y"].Value);
    }

    // Nested generic with f32.
    [Test]
    public void Float32_Generic_ComposedGenerics() {
        var rt = "id(x) = x\r pair(a,b)=a+b\r y:float32 = pair(id(1.0), id(2.0))".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3.0f, rt["y"].Value);
    }

    // Recursive-ish user function with f32.
    [Test]
    public void Float32_Generic_UserFunctionCallingItself() {
        var rt = "f(x:float32):float32 = if(x < 0.1) 1.0 else f(x/2.0)\r y = f(1.0)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["y"].Type.ToString());
        Assert.AreEqual(1.0f, rt["y"].Value);
    }
    #endregion
}
