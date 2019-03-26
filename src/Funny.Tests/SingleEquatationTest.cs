using System.Linq;
using Funny.Runtime;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class SingleEquationTest
    {
        [TestCase("y = 2",2)]
        [TestCase("y = 2*3",6)]
        [TestCase("y = 4/2",2)]
        [TestCase("y = 4- 3",1)]
        [TestCase("y = 4+ 3",7)]
        [TestCase("z = 1 + 4/2 + 3 +2*3 -1", 11)]
        [TestCase("z = 1 + (1 + 4)/2 - (3 +2)*(3 -1)",-6.5)]
        [TestCase("y = 2 ",2)]
        [TestCase("y = 2  ",2)]
        [TestCase("y = -1",-1)]
        [TestCase("y = -1 ",-1)]
        [TestCase("y = -1+2",1)]
        [TestCase("y = -(1+2)",-3)]
        [TestCase("y = -2*-4",8)]
        [TestCase("y = -2*(-4+2)",4)]
        [TestCase("y = -(-(-1))",-1)]
        [TestCase("y = -(-1)",1)]
        [TestCase("y = 0.2",0.2)]
        [TestCase("y = 11.222  ",11.222)]
        [TestCase("y = 11_11  ",1111)]
        [TestCase("y = 1.1_11  ",1.111)]
        [TestCase("y = 0xfF  ",255)]
        [TestCase("y = 0x00_Ff  ",255)]
        [TestCase("y = 0b001  ",1)]
        [TestCase("y = 0b11  ",3)]
        [TestCase("y = 0x_1",1)]
        [TestCase("y = 1 & 1",1)]
        [TestCase("y = 1 & 2",0)]
        [TestCase("y = 1 & 3",1)]
        [TestCase("y = 0 | 2",2)]
        [TestCase("y = 1 | 2",3)]
        [TestCase("y = 1 | 4",5)]
        [TestCase("y = 1 ^ 0",1)]
        [TestCase("y = 1 ^ 1",0)]
        [TestCase("y = 1 ^ 1",0)]
        [TestCase("y = 1 << 3",8)]
        [TestCase("y = 8 >> 3",1)]
        public void NumbersConstantEquation(string expr, object expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(expected, res.Results.First().Value);
        }
        
        [TestCase("y = ''", "")]
        [TestCase("y = 'hi'", "hi")]
        [TestCase("y = 'World'", "World")]
        [TestCase("y = 'hi'+5", "hi5")]
        [TestCase("y = ''+10", "10")]
        [TestCase("y = ''+true", "True")]
        [TestCase("y = 'hi'+5+true", "hi5True")]
        [TestCase("y = 'hi'+' '+'world'", "hi world")]
        public void TextConstantEquation(string expr, string expected)
        {
            
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(expected, res.Results.First().Value);
        }
        
        [TestCase("y = true", true)]
        [TestCase("y = false", false)]
        [TestCase("y = 1==1.0",true)]
        [TestCase("y = 0==0.0",true)]
        [TestCase("y = 0<>0.5",true)]
        [TestCase("y = 0<>0.0",false)]
        [TestCase("y = 1==1",true)]
        [TestCase("y = 1==0",false)]
        [TestCase("y = true==true",true)]
        [TestCase("y = true==false",false)]
        [TestCase("y = 1<>0",true)]
        [TestCase("y = 0<>1",true)]
        [TestCase("y = 5<>5",false)]
        [TestCase("y = 5>5", false)]
        [TestCase("y = 5>3", true)]
        [TestCase("y = 5>6", false)]
        [TestCase("y = 5>=5", true)]
        [TestCase("y = 5>=3", true)]
        [TestCase("y = 5>=6", false)]
        [TestCase("y = 5<=5", true)]
        [TestCase("y = 5<=3", false)]
        [TestCase("y = 5<=6", true)]
        [TestCase("y = true and true", true)]
        [TestCase("y = true and false", false)]
        [TestCase("y = false and false", false)]
        [TestCase("y = true or true", true)]
        [TestCase("y = true or false", true)]
        [TestCase("y = false or false", false)]
        [TestCase("y = true xor true", false)]
        [TestCase("y = true xor false", true)]
        [TestCase("y = false xor false", false)]
        public void DiscreeteConstantEquataion(string expr, bool expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(new Var("y", expected, VarType.Bool));
        }
        

        [TestCase("y = x",2,2)]
        [TestCase("y = 2*x",3,6)]
        [TestCase("y = 4/x",2,2)]
        [TestCase("y = x/4",10,2.5)]
        [TestCase("y = 4- x",3,1)]
        [TestCase("y = x- x",3,0)]
        [TestCase("y = 4+ x",3,7)]
        [TestCase("y = (x + 4/x)",2,4)]
        [TestCase("y = x**3", 2,8)]
        [TestCase("y = x%3", 2,2)]
        [TestCase("y = x%4", 5,1)]
        [TestCase("y = x%-4", 5,1)]
        [TestCase("y = x%4", -5,-1)]
        [TestCase("y = x%-4", -5,-1)]
        [TestCase("y = x%4", -5,-1)]
        [TestCase("y = x%2", -5.2,-1.2)]
        [TestCase("y = 5%x", 2.2,0.6)]
        [TestCase("y = -x ",0.3,-0.3)]
        [TestCase("y = -(-(-x))",2,-2)]
        [TestCase("y = x/0.2",1,5)]
        public void SingleVariableEquation(string expr, double arg, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate(Var.New("x",arg))
                .AssertReturns(0.00001, Var.New("y", expected));
        }

        [TestCase("y = ()")]
        [TestCase("y = )")]
        [TestCase("y = )*2")]
        [TestCase("y = )2")]
        [TestCase("y = (")]
        [TestCase("y = (*2")]
        [TestCase("y = *2")]
        [TestCase("y = /2")]
        [TestCase("y = ^2")]
        [TestCase("y = (2")]
        [TestCase("y = ((2)")]
        [TestCase("y = 2)")]
        [TestCase("y = 2++")]
        [TestCase("y = ++2")]
        [TestCase("y = 2--")]
        [TestCase("y = --2")]
        [TestCase("y = 2^^")]
        [TestCase("y = ^^2")]
        [TestCase("y = 2%%")]
        [TestCase("y = %%2")]
        [TestCase("y = 2a")]
        [TestCase("y = =a")]
        [TestCase("y = 2+ 3 + 4 +")]
        [TestCase("y = x()")]
        [TestCase("y = x*((2)")]
        [TestCase("y = 2*x)")]
        [TestCase("y = 2++x")]
        [TestCase("y = x++2")]
        [TestCase("y = 2--x")]
        [TestCase("y = x--2")]
        [TestCase("y = *2a")]
        [TestCase("y = =a")]
        [TestCase("y = x+2+ 3 + 4 +")]
        [TestCase("y = \"")]
        [TestCase("y = '")]
        [TestCase("y = (")]
        [TestCase("y = -")]
        [TestCase("y = ~")]
        [TestCase("~y=3")]
        [TestCase("y = 0x")]
        [TestCase("y = 0..2")]
        [TestCase("y = .2")]
        [TestCase("y = 0bx2")]
        [TestCase("y = 02.")]
        [TestCase("y = 0x2.3")]
        [TestCase("y = 0x99GG")]
        [TestCase("y = 0bFF")]
        [TestCase("y='hell")]
        [TestCase("y=hell'")]
        [TestCase("y='")]
        [TestCase("y=0.")]
        [TestCase("y=0.*1")]
        public void ObviouslyFails(string expr) =>
            Assert.Throws<FunParseException>(
                ()=> Interpreter.BuildOrThrow(expr));

        [TestCase("y = x1+x2",2,3,5)]
        [TestCase("y = 2*x1*x2",3,6, 36)]
        [TestCase("y = x1*4/x2",2,2,4)]
        [TestCase("y = (x1+x2)/4",2,2,1)]
        public void TwoVariablesEquation(string expr, double arg1, double arg2, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate(
                Var.New("x1", arg1),
                Var.New("x2", arg2));

            Assert.AreEqual(expected, res.Results.First().Value);
        }
     
        [TestCase("y = 1", new string[0])]        
        [TestCase("y = x", new []{"x"})]
        [TestCase("y = x/2",new []{"x"})]
        [TestCase("y = in1/2+ in2",new []{"in1","in2"})]
        [TestCase("y = in1/2 + (in2*in3)",new []{"in1","in2", "in3"})]
        public void InputVarablesListIsCorrect(string expr, string[] inputNames)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            CollectionAssert.AreEquivalent(inputNames, runtime.Variables);
        }

        
    }
}