namespace NFun.SyntaxTests.Functions;

using Exceptions;
using TestTools;
using NUnit.Framework;

[TestFixture]
public class ConcreteUserFunctionsTest {
    [TestCase("myor(a:bool, b:bool):bool = a or b \r y = myor(true,false)", true)]
    [TestCase("mysum(a:int, b:int):int = a + b \r    y = mysum(1,2)", 3)]
    [TestCase("mysum(a:real, b:real):real = a + b \r y = mysum(1,2)", 3.0)]
    [TestCase("mysum(a:int, b:real):real = a + b \r  y = mysum(1,2)", 3.0)]
    [TestCase("mysum(a:int, b:real):real = a + b \r  y = mysum(1,2.0)", 3.0)]
    [TestCase("mysum(a:real, b:int):real = a + b \r  y = mysum(1,2)", 3.0)]
    [TestCase("conv(x:int):real = x; y = conv(2);", 2.0)]
    [TestCase("myconcat(a:text, b:text):text = a.concat(b) \r  y = myconcat(\"my\",\"test\")", "mytest")]
    [TestCase("myconcat(a:text, b:text):text = a.concat(b) \r  y = myconcat(1.toText(),\"test\")", "1test")]
    [TestCase("myconcat(a:text, b:text):text = a.concat(b) \r  y = myconcat(1.toText(),2.toText())", "12")]
    [TestCase("myconcat(a:text, b):text = a.concat(b)\r  y = myconcat(1.toText(), 2.5.toText())", "12.5")]
    [TestCase("arr(a:real[]):real[] = a    \r  y = arr([1.0,2.0])", new[] { 1.0, 2.0 })]
    [TestCase("arr(a:real[]):real[] = a.concat(a) \r  y = arr([1.0,2.0])", new[] { 1.0, 2.0, 1.0, 2.0 })]
    [TestCase("arr(a:int[]):int[] = a \r  y = arr([1,2])", new[] { 1, 2 })]
    // Narrow signed type in concrete function signature (round-trips through
    // SubstituteConcreteTypes / SearchMaxGenericTypeId without throwing).
    [TestCase("f(x:int8):int8 = x\r y = f(5)", (sbyte)5)]
    [TestCase("f(x:int8):int8 = -x\r y = f(5)", (sbyte)(-5))]
    [TestCase("arr(a:text[]):text[] = a.concat(a) \r  y = arr(['qwe','rty'])", new[] { "qwe", "rty", "qwe", "rty" })]
    [TestCase(@"car2(g):real = g(2.0,4.0); y = car2(max)    ", 4.0)]
    [TestCase(@"
            f(x:int) = x
            f(x:int, y:int) = y
            y = f(1) + f(1,2)
        ", 3)]
    [TestCase(@"
            max(x:int) = x
            max(x:int, y:int) = y
            y = max(1) + max(3,2) # user functions return 1 + 2 as a result
        ", 3)]
    public void TypedConstantEquation_NonRecursiveFunction(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // Float32 in user function signatures — requires FloatFamily opt-in.
    [TestCase("f(x:float32):float32 = x\r y = f(1.5)",  1.5f)]
    [TestCase("f(x:float32):float32 = -x\r y = f(1.5)", -1.5f)]
    [TestCase("f(a:float32, b:float32):float32 = a + b\r y = f(1.5, 2.5)", 4.0f)]
    public void Float32_TypedFunction(string expr, object expected) =>
        expr.BuildWithFloats().Calc().AssertReturns("y", expected);

    [TestCase("_inc(a) = a+1\r y = _inc(2)", 3)]
    [TestCase("_inc(y) = y+1\r y = _inc(2)", 3)]
    [TestCase("mult2(a,b) = a*b \r y = mult2(3,4)+1", 13)]
    [TestCase("div2(a,b) = a/b  \r mult2(a,b) = a*b         \r y = mult2(3,4)+div2(4,2)", 14.0)]
    public void ConstantEquation_NonRecursiveGenericFunction(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("_inc(a) = a+1.0\r y = _inc(2.0)", 3.0)]
    [TestCase("div2(a,b) = a/b  \r div3(a,b,c) = div2(a,b)/c\r y = div3(16,4,2)", 2)]
    public void ConstantEquation_NonRecursiveFunction(string expr, double expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("plus3(a,b,c) = plus2(a,b)+c \r plus2(a,b) = a+b  \r y = plus3(16,4,2)", 22)]
    public void ConstantEquation_ReversedImplementationsOfFunctions(string expr, int expected) =>
        expr.AssertReturns("y", expected);

    [Test]
    public void BubbleSortConcrete() {
        var expr = @"twiceSet(arr:int[],i:int,j:int,ival:int,jval:int):int[]
  	                        = arr.set(i,ival).set(j,jval)

                          swap(arr:int[], i:int, j:int):int[]
                            = arr.twiceSet(i,j,arr[j], arr[i])

                          swapIfNotSorted(c:int[], i:int):int[]
  	                        =	if   (c[i]<c[i+1]) c
  		                        else c.swap(i, i+1)

                          # run thru array
                          # and swap every unsorted values
                          onelineSort(input:int[]):int[] =
  	                        [0..input.count()-2].fold(input, swapIfNotSorted)

                          bubbleSort(input:int[]):int[]=
  	                        [0..input.count()-1]
  		                        .fold(
  			                        input,
  			                        rule onelineSort(it1))


                          i:int[]  = [1,4,3,2,5].bubbleSort()";
        expr.AssertReturns("i", new[] { 1, 2, 3, 4, 5 });
    }


    [TestCase(
    @"
        f(x) = false
        not true
    ", false)]
    [TestCase(
    @"
        f(x) = false
        not f(x)
    ", true)]
    public void AnonymEquationAfterUserFunction(string expr, object value) => expr.AssertReturns(value);


    // Mutual recursion `f → g → f` and `f → g → l → f` were previously rejected
    // at parse time (FU822 'Complex recursion'). Since the SCC-grouped solver
    // landed, mutual recursion is a first-class feature — those cases now parse
    // successfully and run, bounded by the shared-depth guard.
    [TestCase("y(1)=1")]
    [TestCase("y(x,y=1")]
    [TestCase("y(x y)=1")]
    [TestCase("y(x, l) x+l")]
    [TestCase("y(x,  l) ==x+l")]
    [TestCase("ma(x, l)) =x+l")]
    [TestCase("ma(x)) =x")]
    [TestCase("ma((x, l) =x+l")]
    [TestCase("ma((x) =x")]
    [TestCase("y(x, x) =x")]
    [TestCase("y(x, x) =1.0")]
    [TestCase("y(x:int, x:int):int =1")]
    [TestCase("1y(x, l) =x+l")]
    [TestCase("(tom(x, l)) =x+l")]
    [TestCase("(tom(x)) =x")]
    [TestCase("(xtom()) =default")]
    [TestCase("((tom(x, l))) =x+l")]
    [TestCase("((tom(x))) =x")]
    [TestCase("((tom())) =42")]
    [TestCase("((tom(x, l)),z) =x+l")]
    [TestCase("((tom(x)),z) =x")]
    [TestCase("((tom()),z) =42")]
    [TestCase("(y(x, l)) =x+g(c)=12")]
    [TestCase("y(x, l) = y(x)")]
    [TestCase("y(x, l) = y(1,2")]
    [TestCase("y(x, l) = (1,2)")]
    [TestCase("y(x, l) = 1,2")]
    [TestCase("y(x, l) = 1,2*3")]
    [TestCase("y(x, l) = 4*(1,2)")]
    [TestCase("y(, l) = 1")]
    [TestCase("y(x, (l)) = 1.0")]
    [TestCase("y((x)) = x*2")]
    [TestCase("y((a,b)) = 42")]
    [TestCase("y(,) = 2")]
    [TestCase("y(()) = 2")]
    [TestCase("foo()) = 2")]
    [TestCase("foo(() = 2")]
    [TestCase("foo) = 2")]
    [TestCase("foo( = 2")]
    [TestCase("foo) = 2")]
    [TestCase("foo)( = 2")]
    [TestCase("foo(( = 2")]
    [TestCase("foo)) = 2")]
    [TestCase("y(()) = ()2")]
    [TestCase("y(()) = 2()")]
    [TestCase("y(x) = 2*z")]
    [TestCase("y(x) = 2*y")]
    [TestCase("y(x:int):int = 2*z")]
    [TestCase("y(x:int):int = 2*y")]
    [TestCase("y(x)=")]
    [TestCase("y(x)-1")]
    [TestCase("y:int(x)-1")]
    [TestCase("y(x):foo=x")]
    [TestCase("y(x+1)=x")]
    [TestCase("y(,x)=x")]
    [TestCase("y(,)=42")]
    [TestCase("y(x,1)=x")]
    [TestCase("y(1)=x")]
    [TestCase("y(x:foo)=x")]
    [TestCase("y(x:int)= x+\"vasa\"")]
    [TestCase("y(x:int)= x+1.0\n out = y(\"test\")")]
    [TestCase("y(x:real[)= x")]
    [TestCase("y(x:foo[])= x")]
    [TestCase("y(x:real])= x")]
    [TestCase("y(x):real]= x")]
    [TestCase("y(x):real[= x")]
    [TestCase("a(x)=x\r a(y)=y\r")]
    [TestCase("(x)=x\r y = out(x)\r")]
    [TestCase("f(i,j,k) = 12.0 \r y = f(((1,2),3)->i+j)")]
    [TestCase("f((i,j),k) = 12.0 \r y = f(((1,2),3)->i+j)")]
    [TestCase("f(x*2) = 12.0 \r y = f(3)")]
    [TestCase("f(x*2) = 12.0")]
    [TestCase("y(x):real= 'vasa'")]
    [TestCase("j = 1 y(x)= x+1")]
    [TestCase("y:real(x)= 1")]
    [TestCase("y:real(x:real)= 1")]
    [TestCase("y:real(x):real= 1")]
    [TestCase("f(x):real= 1; f(x):int = 2; out = 1")]
    [TestCase("f(x):real= 1; f(x):int = 2; out = f(1)")]
    [TestCase("f(x):real= 1; F(x):int = 2; out = 1")]
    [TestCase("F(x):real= 1; f(x):int = 2; out = f(1)")]
    [TestCase("f(x):real= 1; f(x):int = 2; out = F(1)")]
    public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();

    [TestCase("sum(a,b) = a + b\r y = sum(3,5)", 8, Description = "Different arity: works")]
    [TestCase("max(a,b) = if(a>b) a else b\r y = max(3,5)", 5, Description = "max(2 args) shadows builtin max(2 args)")]
    public void UserFunction_SameNameAsBuiltin_DifferentArityOrNonRecursive_Works(string expr, int expected) =>
        expr.AssertResultHas("y", expected);

    #region Float32AndFloat64 dialect
    // Concrete-typed user functions with float32 in signature.

    // f(x:float32, y:int):float32 = x + y (mixed args, int widens to f32).
    [Test]
    public void Float32_MixedArgs_IntWidensToF32() {
        var rt = "f(x:float32, y:int):float32 = x + y\r out = f(1.5, 2)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3.5f, rt["out"].Value);
    }

    // f(x:float32):real = x — widen in return.
    [Test]
    public void Float32_ReturnWidenedToReal() {
        var rt = "f(x:float32):real = x\r out = f(1.5)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(1.5, rt["out"].Value);
    }

    // f(x:real):float32 = x — narrowing return, must fail.
    [Test]
    public void Float32_NarrowingReturn_RealToF32_ParseError() =>
        Assert.Throws<Exceptions.FunnyParseException>(() =>
            "f(x:real):float32 = x\r out = f(1.5)".BuildWithFloats());

    // Multiple f32 params, arithmetic composition.
    [Test]
    public void Float32_ThreeArgs_Arithmetic() {
        var rt = "f(a:float32,b:float32,c:float32):float32 = a + b*c\r out = f(1.0, 2.0, 3.0)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(7.0f, rt["out"].Value);
    }

    // f32 array param.
    [Test]
    public void Float32_ArrayParam_ReturnsElement() {
        var rt = "f(a:float32[]):float32 = a[0]\r out = f([1.5,2.5,3.5])".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    // f32 array param, return array.
    [Test]
    public void Float32_ArrayParam_ArrayReturn_Concat() {
        var rt = "f(a:float32[]):float32[] = a.concat(a)\r out = f([1.5,2.5])".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(new[] { 1.5f, 2.5f, 1.5f, 2.5f }, rt["out"].Value);
    }

    // Type-mismatched literal at call site — int literal narrows to f32.
    [Test]
    public void Float32_ConcreteFunc_IntLiteralAtCallSite() {
        var rt = "f(x:float32):float32 = x\r out = f(42)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(42.0f, rt["out"].Value);
    }

    // Recursive f32 function (halving toward base case).
    [Test]
    public void Float32_RecursiveHalving() {
        var rt = "f(x:float32):float32 = if(x < 0.01) 1.0 else f(x/2.0)\r out = f(0.5)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.0f, rt["out"].Value);
    }

    // Two arrows: f32 arg passed to text-returning function.
    [Test]
    public void Float32_PassedToToText() {
        var rt = "f(x:float32):text = x.toText()\r out = f(3.5)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("3.5", rt["out"].Value);
    }

    // ReturnType float32? not tested here — that requires optional dialect
    // (covered in OptionalTypeTest.cs).

    // Higher-order: pass f32→f32 function as argument.
    [Test]
    public void Float32_HigherOrderFunction_MapWithF32Lambda() {
        var rt = "twice(x:float32):float32 = x + x\r arr:float32[]=[1.0,2.0,3.0]\r out = arr.map(twice)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(new[] { 2.0f, 4.0f, 6.0f }, rt["out"].Value);
    }

    // Explicit function reference used in fold.
    [Test]
    public void Float32_HigherOrderFunction_FoldWithF32Lambda() {
        var rt = "add(a:float32,b:float32):float32 = a + b\r arr:float32[]=[1.0,2.0,3.0]\r out = arr.fold(add)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(6.0f, rt["out"].Value);
    }
    #endregion

    #region Regression pins for TIC workarounds

    // WO2 — StateFun solved-shortcut in DeepCloneNode. Without it: "Circular ancestor 0" crash.
    [Test]
    public void UserFunctionCall_ConcreteReturn_NoCircularAncestor() =>
        "f(x) = x+1\r y = f(2)".Calc().AssertResultHas("y", 3);

    // WO5 / Round 6 #75 — DestructionFunctions incompatibility guard. If-branches with
    // structurally incompatible types (Text vs Int) must surface as a parse error, not
    // silently coerce int → char[] via ToText.
    [TestCase("f(x) = if(x==0) 'a' else x\ry = f(0)")]
    [TestCase("f(x) = if (x==0) 'a' else x; out = f(5)")]
    [TestCase("f(x) = if (false) 'a' else x; out = f(42)")]
    public void UserFunctionReturn_IncompatibleIfBranches_ParseError(string script) =>
        Assert.Throws<FunnyParseException>(() => script.Calc());

    #endregion
}
