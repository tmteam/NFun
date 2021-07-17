using System;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests
{
    // <summary>
    // Confirmation of betta syntax document examples
    // </summary>
    [TestFixture]
    public class SpecificationTests
    {
        [TestCase(10, "y = x+1  #addition", "y", 11)]
        [TestCase(10, "y = x-1  #subtraction", "y", 9)]
        [TestCase(10, "y = x*2  #multiplication", "y", 20)]
        [TestCase(10.0, "y = x/2  #division", "y", 5.0)]
        [TestCase(10, "y = x.rema(3)  #rema", "y", 1)]
        [TestCase(10.0, "y = x**2.0  #exponentiation", "y", 100.0)]
        [TestCase(10, "y = 10*x +1", "y", 101)]
        [TestCase(10.0, "10*x +1", "out", 101)]
        [TestCase(0.0, "y = cos(x)", "y", 1.0)]
        [TestCase(0.0, "y = x.cos()", "y", 1.0)]
       // [TestCase(0.0, "y = x.cos().tan() .abs() .round()", "y", 2)]
        [TestCase(5,   "y:int = x.add(3)", "y", 8)]
        [TestCase(1.0, "y = x == 0.0", "y", false)]
        [TestCase(0, "x==0", "out", true)]
        [TestCase(1, "y:bool = x != 0", "y", true)]
        [TestCase(1, "y = not (x == 0)", "y", true)]
        [TestCase(1, "if (x == 0) 0 else 1", "out", 1)]
        [TestCase(55, "y = if (x < 0) 0 else x", "y", 55)]
        [TestCase(-42, @"
y = if (x < 0) -1 
if (x == 0)  0
else 1", "y", -1)]
        [TestCase(-42.2, @"
if (x < 0) -1 
if (x ==0.0)  0
else 1.0", "out", -1.0)]

        [TestCase(321, @"
if (x < 0) -1 
else if (x > 0) 1
else if (x ==0) 0
else 123
", "out", 1)]
        [TestCase(-400, @"
# if это выражение 
y = 1+ 15 *  if (x < 0 ) -1
		  if (x > 0)  1
		  else 0", "y", -14)]

        [TestCase(4, @"
   y =  if (x ==1) 'one'
        if (x ==2) 'two'
        if (x ==3) 'three'
        if (x ==4) 'four'
        if (x ==5) 'five'
        if (x ==6) 'six'
        if (x ==7) 'seven'
        if (x ==8) 'eight'
        if (x ==9) 'nine'
        if (x > 9) 'ten or more' 
        if (x.cos()>0)  'cos is positive' 
        else 'negative'", "y", "four")]

        [TestCase(3, @"
tostring(v:int):text =
            if (v == 0) 'zero'
			if (v == 1) 'one'
			if (v == 2) 'two'
			else 'not supported' 
x:int
y = tostring(x)", "y", "not supported")]
        [TestCase(2.5, "y = [1.0,2.0,3.0].filter(fun it<x).max()", "y", 2.0)]
        [TestCase(2.5, "x:real \r y = [1.0,2.0,3.0].filter(fun it<x).max()", "y", 2.0)]
        public void Real_SingleEquationWithSingleInput(object xVal, string expression, string outputName,
            object outputValue)
        {
            var runtime = Funny.Hardcore.Build(expression);
            runtime.AssertInputsCount(1);
            runtime.AssertOutputsCount(1);
            runtime
                .Calc("x", xVal)
                .AssertReturns(outputName, outputValue);
        }

        [TestCase("y = 1", "y", 1)]
        [TestCase("1", "out", 1)]
        [TestCase("y = 1      #1, int", "y", 1)]
        [TestCase("y = 0xf 	#15, int", "y", 15)]
        [TestCase("y = 0b101  #5, int", "y", 5)]
        [TestCase("y = 1.0    #1, real", "y", 1.0)]
        [TestCase("y = 1.51   #1.51, real", "y", 1.51)]
        [TestCase("y = 123_321 #123321 int", "y", 123321)]
        [TestCase("y = 123_321_000 #123321000 int", "y", 123321000)]
        [TestCase("y = 12_32_1.1 #12321.1, real", "y", 12321.1)]
        [TestCase("y = 0x123_321 #много, int", "y", 1192737)]
        [TestCase("y = 'string constant'", "y", "string constant")]
        [TestCase("y = ['a','b','foo']# ['a','b','foo'] type: text[]","y", new[] {"a", "b", "foo"})]
        [TestCase("y:int[] = [1,2,3,4]#Int[]", "y", new[] {1, 2, 3, 4})]
        [TestCase("y:int[] = [1..4] #[1,2,3,4]", "y", new[] {1, 2, 3, 4})]
      //  [TestCase("y:int[] = [1..7..2]  #[1,3,5,7]", "y", new[] {1, 3, 5, 7})]
       // [TestCase("y = [1..2.5..0.5]  #[1.0,1.5,2.0,2.5]", "y", new[] {1.0, 1.5, 2.0, 2.5})]
        [TestCase("y = [1.0,2.0, 3.5] #Real[]", "y", new[] {1.0, 2.0, 3.5})]
        [TestCase("y:int[] = [1,2,3,4] .concat( [3,4,5,6])  #Concat [1,2,3,4,3,4,5,6]", "y", new[] {1, 2, 3, 4, 3, 4, 5, 6})]
        [TestCase("y = 1 in [1,2,3,4]# true", "y", true)]
        [TestCase("y = 0 in [1,2,3,4] # false", "y", false)]
        [TestCase("y:int[] = [1,2,3,4].intersect([3,4,5,6])  #[3,4]", "y", new[] {3, 4})]
        [TestCase("y:int[] = [1,2,3,4].except([3,4,5,6])  #[1,2]", "y", new[] {1, 2})]
        [TestCase("y:int[] = [1,2,3,4].unite([3,4,5,6])  #[1,2,3,4,5,6]", "y", new[] {1, 2, 3, 4, 5, 6})]
        [TestCase("y:int[] = [1,2,3,4].unique([3,4,5,6])  # [1,2,5,6]", "y", new[] {1, 2, 5, 6})]
        [TestCase("y:int[] = [1,2,3,4].take(2)  # [1,2]", "y", new[] {1, 2})]
        [TestCase("y:int[] = [1,2,3,4].skip(2)  # [3,4]", "y", new[] {3, 4})]
        [TestCase("y:int = [1,2,3,4].max()  # 4", "y", 4)]
        [TestCase("y:int = [1,2,3,4].min()  # 1", "y", 1)]
        [TestCase("y:int = [1,2,3,4].median()  # 2", "y", 2)]
        [TestCase("y = [1,2,3,4].avg()  # 2.5", "y", 2.5)]
        [TestCase("y:int = [1,2,3,4].sum()  # 10", "y", 10)]
        [TestCase("y:int = [1,2,3,4].count() # 4", "y", 4)]
        [TestCase("y = [1,2,3,4].any() # true", "y", true)]
        [TestCase("y:int[] = [3,1,2,3,4].sort() # [1,2,3,3,4]", "y", new[] {1, 2, 3, 3, 4})]
        [TestCase("y:int[] = [1,2,3,4].reverse() #[4,3,2,1]", "y", new[] {4, 3, 2, 1})]
        [TestCase("y:int[] = [0..6].set(3, 42) #[0,1,2,42,4,5,6]", "y", new[] {0, 1, 2, 42, 4, 5, 6})]
        [TestCase("y = [].any() # false", "y", false)]
        [TestCase("y:int[] = 1.repeat(3) # [1,1,1]", "y", new[] {1, 1, 1})]
        [TestCase("y = ['foo','bar'].repeat(3).flat()#['foo','bar','foo','bar','foo','bar'] "
            , "y", new[] {"foo", "bar", "foo", "bar", "foo", "bar"})]
        [TestCase("y:int = [0..10][0]  #0", "y", 0)]
        [TestCase("y:int = [0..10][1]  #1", "y", 1)]
        [TestCase("y:int[] = [0..10][1:3] #[1,2,3]", "y", new[] {1, 2, 3})]
        [TestCase("y:int[] = [0..10][7:] #[7,8,9,10]", "y", new[] {7, 8, 9, 10})]
        [TestCase("y:int[] = [0..10][:2] #[0,1,2]", "y", new[] {0, 1, 2})]
        [TestCase("y = [1..4].map(fun it/2)#[0.5,1.0,1.5,2.0]", "y", new[] {0.5, 1.0, 1.5, 2.0})]
        [TestCase("y = [1..4].any(fun it>0)#true", "y", true)]
        [TestCase("y = [1..4].all(fun it>2)#false", "y", false)]
        public void Constant(string expr, string outputName, object val)
        {
            var runtime = expr.Build();
            runtime.AssertInputsCount(0);
            runtime.AssertOutputsCount(1);
            runtime.Calc().AssertReturns(outputName, val);
        }

        [TestCase("a=1; b=2; c=3;",                             new[]{"a","b","c"}, new object[]{1, 2, 3})]
        [TestCase("a = 1; b = 2; c = 3",                        new[]{"a","b","c"}, new object[]{1, 2, 3})]
        [TestCase("a=1; b = if (a==1) 'one' else 'foo'; c=45;", new[]{"a","b","c"}, new object[]{1, "one", 45})]
        [TestCase("a=1; b = if (a == 0) 0 else 1; c = 1",       new[]{"a","b","c"}, new object[]{1, 1, 1})]
        [TestCase("a=0; b = cos(a); c = sin(a)",                new[]{"a","b","c"}, new object[]{0.0, 1.0, 0.0})]
        [TestCase("a = [1,2,3,4].max(); b = [1,2,3,4].min()",   new[]{"a","b"},     new object[]{4, 1})]
        [TestCase("a =[0..10][1]; b=[0..5][2]; c=[0..5][3];",   new[]{"a","b","c"}, new object[]{1, 2, 3})]
        public void SomeConstantInExpression(string expr, string[] outputNames, object[] constantValues)
        {
            var calculateResult = expr.Calc();
            for (var item=0; item < outputNames.Length; item++)
            {
                var val = constantValues[item];
                var name = outputNames[item];
                calculateResult.AssertResultHas(name,val);
            }
        }
        
        [Test]
        public void Multiline()
        {
            var expr = @"

            y = 10.0
    *

x-
    10

";
            var runtime = expr.Build();
            runtime.AssertInputsCount(1);
            runtime.AssertOutputsCount(1);
            runtime.Calc("x", 2.5).AssertReturns("y", 15.0);
        }
 

        [Test]
        public void Multiple_equations()
        {
            var expr = @"
sum = x1+x2
dif = x1-x2";
            var runtime = expr.Build();
            runtime.AssertInputsCount(2);
            runtime.AssertOutputsCount(2);
            runtime.Calc(("x1", 10.0),("x2", 2.5)).AssertReturns(("sum", 12.5), ("dif", 7.5));
        }

        [Test]
        public void Multiple_DiscreeteEquations()
        {
            var expr =
                @"
x1: bool
x2: bool
x3: bool
y1 = x1 and x2
y2 = x1 and true # == x1
y3 = x1 == false 
y4 = not(x1 and x2 or x3)
";
            var runtime = expr.Build();
            runtime.AssertInputsCount(3);
            runtime.AssertOutputsCount(4);
            runtime.Calc(
                    ("x1", true),
                    ("x2", false),
                    ("x3", true))
                .AssertReturns(("y1", false),
                    ("y2", true),
                    ("y3", false),
                    ("y4", false));
        }

        [TestCase("y = a / b", BaseFunnyType.Real)]
        [TestCase("y = 0.0", BaseFunnyType.Real)]
        [TestCase("y = false #bool", BaseFunnyType.Bool)]
        //[TestCase("y = 'hi' #text", BaseVarType.)]
        //  [TestCase("y = 'hi'.strConcat(a) #text", BaseVarType.Text)]
        //  [TestCase("y = 'hi'.strConcat(a) #text", BaseVarType.Text)]
        [TestCase("y = [1,2,3]  #int[]", BaseFunnyType.ArrayOf)]
        [TestCase("y = ['1','2','3']  #text[]", BaseFunnyType.ArrayOf)]
      //  [TestCase("y = 'hi '.strConcat(u) #text", BaseVarType.Text)]
        public void Single_Equation_OutputTypeTest(string expression, BaseFunnyType primitiveType)
        {
            var runtime = expression.Build();
            runtime.AssertOutputsCount(1);
            var output = runtime["y"];
            Assert.AreEqual(primitiveType, output.Type.BaseType);
        }

        [Test]
        public void CalculationWithAttributes() =>
            @"
yprivate   = 0.1 * xpublic 
yPublic   = yprivate + xpublic"
                .Calc("xpublic", 10.0).AssertReturns(("yprivate", 1.0), ("yPublic", 11.0));

        [TestCase(" y1 = [1,’2’,3,4]      # error")]
        [TestCase(" x:real[] \r y = x.filter(x => x< 2 ) # error.")]
        public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();

        [TestCase(" y:int = convert('string')")]
        [TestCase(" y:int = convert('string')")]
        [TestCase(" y = [1,2,3][4]")]
        public void ObviousFailsOnRuntime(string expr) => Assert.Throws<FunRuntimeException>(() => expr.Calc());

        class MyIn
        {
            public int Count { get; set; }
            public string Name { get; set; }
        }
        class MyOut
        {
            public int Id { get; set; }
            public bool Flag { get; set; }
        }
        
        [Test]
        public void CheckExamplesInReadme()
        {
            //Let's make some fun
            object a  = Funny.Calc("42*(3-1)"); // 84 
            Assert.AreEqual(84,a);
            int[]  e  = Funny.Calc<int[]>("[1,3,5].reverse()"); //int[]{5,3,1}
            CollectionAssert.AreEquivalent(new[]{5,3,1},e);
            
            string h  = Funny.Calc<MyIn, string>(
                "if (count>0) name.repeat(count).flat() else 'none'", 
                new MyIn{Count = 3, Name = "bar"}); //"barbarbar"
            Assert.AreEqual("barbarbar", h);
            
            var f = Funny.ForCalcMany<MyIn,MyOut>().Build(@"
                            id = count - 1
                            flag = name != 'test'");
            MyOut result = f(new MyIn{Count = 100, Name = "kat"}); //MyOut{Id = 99; Flag = true}
            TestTools.TestHelper.AreSame(new MyOut { Id = 99, Flag = true }, result);

            // Hardcore mode 
            var runtime = Funny.Hardcore.Build(@"
                    out1 = in1-1
                    out2:int = in2.filter(fun it>out1).map(fun it*it)[1]");

            runtime["in1"].Value = 2;
            runtime["in2"].Value = new[] { 0, 1, 2, 3, 4 };
            runtime.Run();

            foreach (var v in runtime.Variables)
                Console.WriteLine($"Variable '{v.Name}'' of type {v.Type} equals {v.Value}");
        }
    }
}