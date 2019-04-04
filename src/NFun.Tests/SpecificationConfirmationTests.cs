using NFun;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    // <summary>
    // Confirmation of betta syntax document examples
    // </summary>
    [TestFixture]
    public class SpecificationConfirmationTests
    {
        [TestCase(10.0, "y = x+1  #Суммирование", "y", 11.0)]
        [TestCase(10.0, "y = x-1  #Вычитание", "y", 9.0)]
        [TestCase(10.0, "y = x*2  #Умножение", "y", 20.0)]
        [TestCase(10.0, "y = x/2  #Деление", "y", 5.0)]
        [TestCase(10.0, "y = x%3  #Остаток от деления", "y", 1.0)]
        [TestCase(10.0, "y = x**2  #Cтепень", "y", 100.0)]
        [TestCase(10.0, "y = 10*x +1", "y", 101.0)]
        [TestCase(10.0, "10*x +1", "out", 101.0)]
        [TestCase(0.0, "y = cos(x)", "y", 1.0)]
        [TestCase(0.0, "y = x.cos()", "y", 1.0)]
        [TestCase(0.0, "y = x.cos().tan() .abs() .round()", "y", 2)]
        [TestCase(5.0, "y = x.sum(3)", "y", 8.0)]
        [TestCase(1.0, "y = x == 0","y", false)]
        [TestCase(0.0, "x==0","out", true)]
        [TestCase(0.1, "y = x != 0","y", true)]
        [TestCase(1.0, "y = not (x == 0)","y", true)]
        [TestCase(1.0, "if x == 0 then 0 else 1","out", 1)]
        [TestCase(55.5, "y = if x < 0 then 0 else x","y", 55.5)]
        [TestCase(-42.2, @"
y = if x < 0 then -1 
if x == 0 then 0
else 1","y", -1)]
        [TestCase(-42.2, @"
if x < 0 then -1 
if x ==0 then 0
else 1","out", -1)]

        [TestCase(321.0, @"
if x < 0 then -1 
else if x > 0 then 1
else if x ==0 then 0
else 123
","out", 1)]
        [TestCase(-400, @"
# if это выражение 
y = 1+ 15 *  if x < 0 then -1
		  if x > 0 then 1
		  else 0","y", -14)]
        [TestCase(1.0, 
"y = 'value is ' + (if x < 0 then 'less than'"+
        "if x > 0 then 'more than'"+
        "else 'equals to') + ' zero'","y", "value is more than zero")]
        
        [TestCase(4.0, @"
   y =  if x ==1 then 'one'
        if x ==2 then 'two'
        if x ==3 then 'three'
        if x ==4 then 'four'
        if x ==5 then 'five'
        if x ==6 then 'six'
        if x ==7 then 'seven'
        if x ==8 then 'eight'
        if x ==9 then 'nine'
        if x > 9 then 'ten or more' 
        if x.cos()>0 then 'cos is positive' 
        else 'negative'","y", "four")]
        
        [TestCase(3, @"
tostring(v:int):text =
            if v == 0 then 'zero'
			if v == 1 then 'one'
			if v == 2 then 'two'
			else 'not supported' 
x:int
y = tostring(x)","y", "not supported")]
       // [TestCase(1.0, "","y", false)]

        [TestCase(0.43,"y = 'Welcome to fun.Version is '+ x+'.Next version is '+ (x+1)"
            , "y", "Welcome to fun.Version is 0.43.Next version is 1.43")]

        public void Real_SingleEquationWithSingleInput(object xVal, string expression, string outputName, object outputValue)
        {
            var runtime = FunBuilder.BuildDefault(expression);
            Assert.AreEqual(1, runtime.Inputs.Length);
            Assert.AreEqual(1, runtime.Outputs.Length);
            runtime.Calculate(Var.New("x", xVal))
                .AssertReturns(Var.New(outputName,outputValue));
        }
        [TestCase("y = 1", "y", 1)]
        [TestCase("y = 1", "y", 1)]
        [TestCase("1", "out", 1)]
        [TestCase("y = 1      #1, int","y",1)]
        [TestCase("y = 0xf 	#15, int","y",15)]
        [TestCase("y = 0b101  #5, int","y",5)]
        [TestCase("y = 1.0    #1, real","y",1.0)]
        [TestCase("y = 1.51   #1.51, real","y",1.51)]
        [TestCase("y = 123_321 #123321 int","y",123321)]
        [TestCase("y = 123_321_000 #123321000 int","y",123321000)]
        [TestCase("y = 12_32_1.1 #12321.1, real","y",12321.1)]
        [TestCase("y = 0x123_321 #много, int","y",1192737)]
        [TestCase("y = 'string constant'", "y", "string constant")]
        [TestCase("y = [1,2,3,4]#Int[]", "y", new []{1,2,3,4})]
        [TestCase("y = [1..4] #[1,2,3,4]", "y", new []{1,2,3,4})]
        [TestCase("y = [1..7..2]  #[1,3,5,7]", "y", new []{1,3,5,7})]
        [TestCase("y = [1..2.5..0.5]  #[1.0,1.5,2.0,2.5]", "y", new []{1.0,1.5,2.0,2.5})]
        [TestCase("y = [1.0,2.0, 3.5] #Real[]", "y", new []{1.0,2.0, 3.5})]
        [TestCase("y = [1,2,3,4] @ [3,4,5,6]  #Concat [1,2,3,4,3,4,5,6]", "y", new []{1,2,3,4,3,4,5,6})]
        [TestCase("y = 1 in [1,2,3,4]# true", "y", true)]
        [TestCase("y = 0 in [1,2,3,4] # false", "y", false)]
        [TestCase("y = [1,2] in [1,2,3,4] # true", "y", true)]
        [TestCase("y = [1,2,3,4].intersect([3,4,5,6])  #[3,4]", "y", new []{3,4})]
        [TestCase("y = [1,2,3,4].except([3,4,5,6])  #[1,2]", "y", new []{1,2})]
        [TestCase("y = [1,2,3,4].unite([3,4,5,6])  #[1,2,3,4,5,6]", "y", new []{1,2,3,4,5,6})]
        [TestCase("y = [1,2,3,4].unique([3,4,5,6])  # [1,2,5,6]", "y", new []{1,2,5,6})]
        [TestCase("y = [1,2,3,4].take(2)  # [1,2]", "y", new []{1,2})]
        [TestCase("y = [1,2,3,4].skip(2)  # [3,4]", "y", new []{3,4})]
        [TestCase("y = [1,2,3,4].max()  # 4", "y", 4)]
        [TestCase("y = [1,2,3,4].min()  # 1", "y", 1)]
        [TestCase("y = [1,2,3,4].median()  # 2", "y", 2)]
        [TestCase("y = [1,2,3,4].avg()  # 2.5", "y", 2.5)]
        [TestCase("y = [1,2,3,4].sum()  # 10", "y", 10)]
        [TestCase("y = [1,2,3,4].count() # 4", "y", 4)]
        [TestCase("y = [1,2,3,4].any() # true", "y", true)]
        [TestCase("y = [3,1,2,3,4].sort() # [1,2,3,3,4]", "y", new []{1,2,3,3,4})]
        [TestCase("y = [1,2,3,4].reverse() #[4,3,2,1]", "y", new []{4,3,2,1})]
        [TestCase("y = [0..6].set(3, 42) #[0,1,2,42,4,5,6]", "y", new []{0,1,2,42,4,5,6})]
        [TestCase("y = [].any() # false", "y", false)]
        [TestCase("y = 1.repeat(3) # [1,1,1]", "y", new []{1,1,1})]
        [TestCase("y = ['foo','bar'].reiterate(3)#['foo','bar','foo','bar','foo','bar'] "
            , "y", new []{"foo","bar","foo","bar","foo","bar"})]
        [TestCase("y = [0..10][0]  #0", "y", 0)]
        [TestCase("y = [0..10][1]  #1", "y", 1)]
        [TestCase("y = [0..10][1:3] #[1,2,3]", "y", new []{1,2,3})]
        [TestCase("y = [0..10][7:] #[7,8,9,10]", "y", new []{7,8,9,10})]
        [TestCase("y = [0..10][:2] #[0,1,2]", "y", new []{0,1,2})]
        [TestCase("y = [1..4].map(i:int=> i/2)#[0.5,1.0,1.5,2.0]", "y", new []{0.5,1.0,1.5,2.0})]
        [TestCase("y = [1..4].any(i:int => i>0)#true", "y", true)]
        [TestCase("y = [1..4].all(i:int => i>2)#false", "y", false)]
        [TestCase("y = [1..4].fold((i:int,j:int)=>i+j)# 10.Аналог sum", "y", 10)]
        [TestCase("y = [1..4].fold((i:int,j:int)=>if i>j then i else j)#4.Аналог max", "y", 4)]
        public void Constant(string expr, string ouputName, object val)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            Assert.AreEqual(0, runtime.Inputs.Length);
            Assert.AreEqual(1, runtime.Outputs.Length);
            runtime.Calculate()
                .AssertReturns(Var.New(ouputName,val));
        }
        
        [Test]
        public void Multiline()
        {
            var expr = @"

            y = 10
    *

x-
    10

";
            var runtime = FunBuilder.BuildDefault(expr);
            Assert.AreEqual(1, runtime.Inputs.Length);
            Assert.AreEqual(1, runtime.Outputs.Length);
            runtime.Calculate(Var.New("x", 2.5))
                .AssertReturns(Var.New("y",15.0));
        }
        
        
        [Test]
        public void Multiple_equations()
        {
            var expr = @"
sum = x1+x2
dif = x1-x2";
            var runtime = FunBuilder.BuildDefault(expr);
            Assert.AreEqual(2, runtime.Inputs.Length);
            Assert.AreEqual(2, runtime.Outputs.Length);
            runtime.Calculate(Var.New("x1",10.0),Var.New("x2",2.5))
                .AssertReturns(Var.New("sum",12.5),Var.New("dif",7.5));
        }
        [Test]
        public void Multiple_unorderedEquations()
        {
            var expr = @"
y1 = (-b + d**0.5) /2*a #Используется d
d  = b**2 - 4*a*c
y2 = (-b - d**0.5) /2*a #используется d";
            var runtime = FunBuilder.BuildDefault(expr);
            Assert.AreEqual(3, runtime.Inputs.Length);
            Assert.AreEqual(3, runtime.Outputs.Length);
            runtime.Calculate(
                    Var.New("a",1.0),Var.New("b",-8.0),Var.New("c",12.0))
                .AssertReturns(Var.New("d",16.0),
                    Var.New("y1",6.0),
                    Var.New("y2",2.0));
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
            var runtime = FunBuilder.BuildDefault(expr);
            Assert.AreEqual(3, runtime.Inputs.Length);
            Assert.AreEqual(4, runtime.Outputs.Length);
            runtime.Calculate(
                    Var.New("x1",true),
                    Var.New("x2",false),
                    Var.New("x3",true))
                .AssertReturns(Var.New("y1",false),
                    Var.New("y2",true),
                    Var.New("y3",false),
                    Var.New("y4",false));
        }

        [TestCase("y = a / b",BaseVarType.Real)]
        [TestCase("y = 0.0", BaseVarType.Real)]
        [TestCase("y = false #bool", BaseVarType.Bool)]
        [TestCase("y = 'hi' #text", BaseVarType.Text)]
        [TestCase("y = 'hi' + a #text", BaseVarType.Text)]
        [TestCase("y = 'hi' + a #text", BaseVarType.Text)]
        [TestCase("y = [1,2,3]  #int[]", BaseVarType.ArrayOf)]
        [TestCase("y = ['1','2','3']  #text[]", BaseVarType.ArrayOf)]
        [TestCase("y = 'hi '+ u #text", BaseVarType.Text)]
        public void Single_Equation_OutputTypeTest(string expression, BaseVarType primitiveType)
        {
            var runtime = FunBuilder.BuildDefault(expression);
            Assert.AreEqual(1, runtime.Outputs.Length);
            var output = runtime.Outputs[0];
            Assert.AreEqual(primitiveType, output.Type.BaseType);
        }
    }
}