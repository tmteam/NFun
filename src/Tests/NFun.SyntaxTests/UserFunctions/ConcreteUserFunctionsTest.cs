using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.UserFunctions;

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


    [TestCase("y = f(1)\r f(x) = g(x) \r g(x) = f(x)")]
    [TestCase("y = f(1)\r f(x) = g(x) \r g(x) = l(x)\r l(x) = f(x)")]
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
}
