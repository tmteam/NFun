using System;
using System.Linq;
using Funny.Runtime;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    public class TypizationTest
    {
        [Test]
        public void VoidTest()
        {
            Assert.Pass();
        }
        [TestCase("y = 2",PrimitiveVarType.Int)]
        [TestCase("y = 2*3",PrimitiveVarType.Int)]
        [TestCase("y = 2^3",PrimitiveVarType.Int)]
        [TestCase("y = 2%3",PrimitiveVarType.Int)]

        [TestCase("y = 4/3",PrimitiveVarType.Real)]
        [TestCase("y = 4- 3",PrimitiveVarType.Int)]
        [TestCase("y = 4+ 3",PrimitiveVarType.Int)]
        [TestCase("z = 1 + 4/3 + 3 +2*3 -1", PrimitiveVarType.Real)]
        [TestCase("y = -2*-4",PrimitiveVarType.Int)]
        [TestCase("y = -2*(-4+2)",PrimitiveVarType.Int)]
        [TestCase("y = -(-(-1))",PrimitiveVarType.Int)]

        [TestCase("y = 0.2",PrimitiveVarType.Real)]
        [TestCase("y = 1.1_11  ",PrimitiveVarType.Real)]
        [TestCase("y = 4*0.2",PrimitiveVarType.Real)]
        [TestCase("y = 4/0.2",PrimitiveVarType.Real)]
        [TestCase("y = 4/0.2",PrimitiveVarType.Real)]
        [TestCase("y = 2^0.3",PrimitiveVarType.Real)]
        [TestCase("y = 0.2^2",PrimitiveVarType.Real)]
        [TestCase("y = 0.2%2",PrimitiveVarType.Real)]
        [TestCase("y = 3%0.2",PrimitiveVarType.Real)]

        [TestCase("y = 0xfF  ",PrimitiveVarType.Int)]
        [TestCase("y = 0x00_Ff  ",PrimitiveVarType.Int)]
        [TestCase("y = 0b001  ",PrimitiveVarType.Int)]
        [TestCase("y = 0b11  ",PrimitiveVarType.Int)]
        [TestCase("y = 0x_1",PrimitiveVarType.Int)]
        
        [TestCase("y = 1==1",PrimitiveVarType.Bool)]
        [TestCase("y = 1==0",PrimitiveVarType.Bool)]
        [TestCase("y = true==true",PrimitiveVarType.Bool)]
        [TestCase("y = 1<>0",PrimitiveVarType.Bool)]
        [TestCase("y = 0<>1",PrimitiveVarType.Bool)]
        [TestCase("y = 5<>5",PrimitiveVarType.Bool)]
        [TestCase("y = 5>3", PrimitiveVarType.Bool)]
        [TestCase("y = 5>6", PrimitiveVarType.Bool)]
        [TestCase("y = 5>=3", PrimitiveVarType.Bool)]
        [TestCase("y = 5>=6", PrimitiveVarType.Bool)]
        [TestCase("y = 5<=5", PrimitiveVarType.Bool)]
        [TestCase("y = 5<=3", PrimitiveVarType.Bool)]
        [TestCase("y = true and true", PrimitiveVarType.Bool)]
        [TestCase("y = true or true", PrimitiveVarType.Bool)]
        [TestCase("y = true xor true", PrimitiveVarType.Bool)]

        [TestCase("y=\"\"", PrimitiveVarType.Text)]
        [TestCase("y=''", PrimitiveVarType.Text)]
        [TestCase("y='hi world'", PrimitiveVarType.Text)]
        [TestCase("y='hi world'+5", PrimitiveVarType.Text)]
        [TestCase("y='hi world'+true", PrimitiveVarType.Text)]
        [TestCase("y='hi world'+true+5", PrimitiveVarType.Text)]
        [TestCase("y=''+true+5", PrimitiveVarType.Text)]
        [TestCase("y='hi'+'world'", PrimitiveVarType.Text)]
        public void SingleEquation_OutputTypeCalculatesCorrect(string expr, PrimitiveVarType type)
        {
            
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(VarType.PrimitiveOf(type), res.Results.First().Type);
        }
        
        [TestCase("y = 1\rz=2",PrimitiveVarType.Int, PrimitiveVarType.Int)]
        [TestCase("y = 2.0\rz=2",PrimitiveVarType.Real, PrimitiveVarType.Int)]
        [TestCase("y = true\rz=false",PrimitiveVarType.Bool, PrimitiveVarType.Bool)]

        [TestCase("y = 1\rz=y",PrimitiveVarType.Int, PrimitiveVarType.Int)]
        [TestCase("y = z\rz=2",PrimitiveVarType.Int, PrimitiveVarType.Int)]

        [TestCase("y = z/2\rz=2",PrimitiveVarType.Real, PrimitiveVarType.Int)]
        [TestCase("y = 2\rz=y/2",PrimitiveVarType.Int, PrimitiveVarType.Real)]

        [TestCase("y = 2.0\rz=y",PrimitiveVarType.Real, PrimitiveVarType.Real)]
        [TestCase("y = z\rz=2.0",PrimitiveVarType.Real, PrimitiveVarType.Real)]

        [TestCase("y = true\rz=y",PrimitiveVarType.Bool, PrimitiveVarType.Bool)]
        [TestCase("y = z\rz=true",PrimitiveVarType.Bool, PrimitiveVarType.Bool)]

        
        [TestCase("y = 2\rz=y>1",PrimitiveVarType.Int, PrimitiveVarType.Bool)]
        [TestCase("y = z>1\rz=2",PrimitiveVarType.Bool, PrimitiveVarType.Int)]

        [TestCase("y = 2.0\rz=y>1",PrimitiveVarType.Real, PrimitiveVarType.Bool)]
        [TestCase("y = z>1\rz=2.0",PrimitiveVarType.Bool, PrimitiveVarType.Real)]
        [TestCase("y = 'hi'\rz=y",PrimitiveVarType.Text, PrimitiveVarType.Text)]
        [TestCase("y = 'hi'\rz=y+ 'lala'",PrimitiveVarType.Text, PrimitiveVarType.Text)]
        [TestCase("y = true\rz='lala'+y",PrimitiveVarType.Bool, PrimitiveVarType.Text)]

        public void TwinEquations_OutputTypesCalculateCorrect(string expr, PrimitiveVarType ytype,PrimitiveVarType ztype)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            var y = res.Get("y");
            Assert.AreEqual(y.Type,VarType.PrimitiveOf(ytype),"y");
            var z = res.Get("z");
            Assert.AreEqual(z.Type,VarType.PrimitiveOf(ztype),"z");
        }
        
        [TestCase("y=5+'hi'")]
        public void ObviouslyFailsWithOutputCast(string expr) =>
            Assert.Throws<OutpuCastParseException>(
                ()=> Interpreter.BuildOrThrow(expr));
        
        [TestCase("x:foo\r y= x and true")]        
        [TestCase("x::foo\r y= x and true")]       
        [TestCase("x:real[\r y= x")]        
        [TestCase("x:foo[]\r y= x")]        
        [TestCase("x:real]\r y= x")]        
        [TestCase("x:real[][\r y= x")]        
        [TestCase("x:real[]]\r y= x")]        
        [TestCase("x:real[[]\r y= x")]        
        [TestCase("x:real][]\r y= x")]        
        public void ObviouslyFailsWithParse(string expr) =>
            Assert.Throws<ParseException>(
                ()=> Interpreter.BuildOrThrow(expr));
        
        [TestCase(new []{1,2},    "x:int[]\r y= x", new []{1,2})]        
        [TestCase(new []{1,2},    "x:int[]\r y= x::x", new []{1,2,1,2})]
        [TestCase(new []{"1","2"},    "x:text[]\r y= x", new []{"1","2"})]        
        [TestCase(new []{"1","2"},    "x:text[]\r y= x::x", new []{"1","2","1","2"})]
        [TestCase(new []{1.0,2.0},    "x:real[]\r y= x", new []{1.0,2.0})]        
        
        [TestCase(new []{1.0,2.0},    "x:real[]\r y= x::x", new []{1.0,2.0,1.0,2.0})]        
        [TestCase(1.0, "x:real\r y= x+1", 2.0)]        
        [TestCase(1,    "x:int\r y= x+1", 2)]        
        [TestCase("1", "x:text\r y= x+1", "11")]        
        [TestCase(true, "x:bool\r y= x and true", true)]        
        public void SingleInputTypedEquation(object x,  string expr, object y)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate(Var.New("x", x));
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(y, res.Results.First().Value);
        }
        
    }
}