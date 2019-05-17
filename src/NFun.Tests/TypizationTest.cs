using System;
using System.Linq;
using NFun;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Types;
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
        [TestCase("y = 2",BaseVarType.Int32)]
        [TestCase("y = 2*3",BaseVarType.Int32)]
        [TestCase("y = 2**3",BaseVarType.Real)]
        [TestCase("y = 2%3",BaseVarType.Int32)]

        [TestCase("y = 4/3",BaseVarType.Real)]
        [TestCase("y = 4- 3",BaseVarType.Int32)]
        [TestCase("y = 4+ 3",BaseVarType.Int32)]
        [TestCase("z = 1 + 4/3 + 3 +2*3 -1", BaseVarType.Real)]
        [TestCase("y = -2*-4",BaseVarType.Int32)]
        [TestCase("y = -2*(-4+2)",BaseVarType.Int32)]
        [TestCase("y = -(-(-1))",BaseVarType.Int32)]

        [TestCase("y = 0.2",BaseVarType.Real)]
        [TestCase("y = 1.1_11  ",BaseVarType.Real)]
        [TestCase("y = 4*0.2",BaseVarType.Real)]
        [TestCase("y = 4/0.2",BaseVarType.Real)]
        [TestCase("y = 4/0.2",BaseVarType.Real)]
        [TestCase("y = 2**0.3",BaseVarType.Real)]
        [TestCase("y = 0.2**2",BaseVarType.Real)]
        [TestCase("y = 0.2%2",BaseVarType.Real)]
        [TestCase("y = 3%0.2",BaseVarType.Real)]

        [TestCase("y = 0xfF  ",BaseVarType.Int32)]
        [TestCase("y = 0x00_Ff  ",BaseVarType.Int32)]
        [TestCase("y = 0b001  ",BaseVarType.Int32)]
        [TestCase("y = 0b11  ",BaseVarType.Int32)]
        [TestCase("y = 0x_1",BaseVarType.Int32)]
        
        [TestCase("y = 1==1",BaseVarType.Bool)]
        [TestCase("y = 1==0",BaseVarType.Bool)]
        [TestCase("y = true==true",BaseVarType.Bool)]
        [TestCase("y = 1!=0",BaseVarType.Bool)]
        [TestCase("y = 0!=1",BaseVarType.Bool)]
        [TestCase("y = 5!=5",BaseVarType.Bool)]
        [TestCase("y = 5>3", BaseVarType.Bool)]
        [TestCase("y = 5>6", BaseVarType.Bool)]
        [TestCase("y = 5>=3", BaseVarType.Bool)]
        [TestCase("y = 5>=6", BaseVarType.Bool)]
        [TestCase("y = 5<=5", BaseVarType.Bool)]
        [TestCase("y = 5<=3", BaseVarType.Bool)]
        [TestCase("y = true and true", BaseVarType.Bool)]
        [TestCase("y = true or true", BaseVarType.Bool)]
        [TestCase("y = true xor true", BaseVarType.Bool)]
        [TestCase("y=\"\"", BaseVarType.Text)]
        [TestCase("y=''", BaseVarType.Text)]
        [TestCase("y='hi world'", BaseVarType.Text)]
        [TestCase("y='hi world'.strConcat(5)", BaseVarType.Text)]
        [TestCase("y='hi world'.strConcat(true)", BaseVarType.Text)]
        [TestCase("y='hi world'.strConcat(true).strConcat(5)", BaseVarType.Text)]
        [TestCase("y=''.strConcat(true).strConcat(5)", BaseVarType.Text)]
        [TestCase("y='hi'.strConcat('world')", BaseVarType.Text)]
        [TestCase("y = 1<<2", BaseVarType.Int32)]
        [TestCase("y = 8>>2", BaseVarType.Int32)]
        [TestCase("y = 3|2", BaseVarType.Int32)]
        [TestCase("y = 3^2", BaseVarType.Int32)]
        [TestCase("y = 4&2", BaseVarType.Int32)]

        public void SingleEquation_OutputTypeCalculatesCorrect(string expr, BaseVarType type)
        {
            
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(VarType.PrimitiveOf(type), res.Results.First().Type);
        }
        
        [TestCase("y = 1\rz=2",BaseVarType.Int32, BaseVarType.Int32)]
        [TestCase("y = 2.0\rz=2",BaseVarType.Real, BaseVarType.Int32)]
        [TestCase("y = true\rz=false",BaseVarType.Bool, BaseVarType.Bool)]

        [TestCase("y = 1\rz=y",BaseVarType.Int32, BaseVarType.Int32)]
        [TestCase("z=2 \r y = z",BaseVarType.Int32, BaseVarType.Int32)]

        [TestCase("z=2 \r y = z/2",BaseVarType.Real, BaseVarType.Int32)]
        [TestCase("y = 2\rz=y/2",BaseVarType.Int32, BaseVarType.Real)]

        [TestCase("y = 2.0\rz=y",BaseVarType.Real, BaseVarType.Real)]
        [TestCase("z=2.0 \ry = z",BaseVarType.Real, BaseVarType.Real)]

        [TestCase("y = true\rz=y",BaseVarType.Bool, BaseVarType.Bool)]
        [TestCase("z=true \r y = z",BaseVarType.Bool, BaseVarType.Bool)]

        
        [TestCase("y = 2\r z=y>1",BaseVarType.Int32, BaseVarType.Bool)]
        [TestCase("z=2 \r y = z>1",BaseVarType.Bool, BaseVarType.Int32)]

        [TestCase("y = 2.0\rz=y>1",BaseVarType.Real, BaseVarType.Bool)]
        [TestCase("z=2.0 \r y = z>1",BaseVarType.Bool, BaseVarType.Real)]
        [TestCase("y = 'hi'\rz=y",BaseVarType.Text, BaseVarType.Text)]
        [TestCase("y = 'hi'\rz=y.strConcat('lala')",BaseVarType.Text, BaseVarType.Text)]
        [TestCase("y = true\rz='lala'.strConcat(y)",BaseVarType.Bool, BaseVarType.Text)]

        public void TwinEquations_OutputTypesCalculateCorrect(string expr, BaseVarType ytype,BaseVarType ztype)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate();
            var y = res.Get("y");
            Assert.AreEqual(y.Type,VarType.PrimitiveOf(ytype),"y");
            var z = res.Get("z");
            Assert.AreEqual(z.Type,VarType.PrimitiveOf(ztype),"z");
        }
        
        
        [TestCase("x:foo\r y= x and true")]        
        [TestCase("x::foo\r y= x and true")]       
        [TestCase("x:real[\r y= x")]        
        [TestCase("x:foo[]\r y= x")]        
        [TestCase("x:real]\r y= x")]        
        [TestCase("x:real[][\r y= x")]        
        [TestCase("x:real[]]\r y= x")]        
        [TestCase("x:real[[]\r y= x")]        
        [TestCase("x:real][]\r y= x")]    
        [TestCase("y=5+'hi'")]
        [TestCase("x:real \r y = [1..x][0]")]
        [TestCase("x:real \r y = [x..10][0]")]
        [TestCase("x:real \r y = [1..x..10][0]")]
        [TestCase("x:real \r y = [1..10][x]")]
        [TestCase("x:real \r y = [1..10][:x]")]
        [TestCase("x:real \r y = [1..10][:x:]")]
        [TestCase("x:real \r y = [1..10][::x]")]
        [TestCase("y = x \r x:real ")]
        [TestCase("z:real \r  y = x+z \r x:real ")]
        public void ObviouslyFailsWithParse(string expr) =>
            Assert.Throws<FunParseException>(
                ()=> FunBuilder.BuildDefault(expr));
        
        [TestCase(new []{1,2},    "x:int[]\r y= x", new []{1,2})]        
        [TestCase(new []{1,2},    "x:int[]\r y= x.concat(x)", new []{1,2,1,2})]
        [TestCase(new []{"1","2"},    "x:text[]\r y= x", new []{"1","2"})]        
        [TestCase(new []{"1","2"},    "x:text[]\r y= x.concat(x)", new []{"1","2","1","2"})]
        [TestCase(new []{1.0,2.0},    "x:real[]\r y= x", new []{1.0,2.0})]        
        
        [TestCase(new []{1.0,2.0},    "x:real[]\r y= x.concat(x)", new []{1.0,2.0,1.0,2.0})]        
        [TestCase(1.0, "x:real\r y= x+1", 2.0)]        
        [TestCase(1,    "x:int\r y= x+1", 2)]        
        [TestCase("1", "y= x.strConcat(1)", "11")]        
        [TestCase(true, "x:bool\r y= x and true", true)]    

        public void SingleInputTypedEquation(object x,  string expr, object y)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate(Var.New("x", x));
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(y, res.Results.First().Value);
        }

        [TestCase("int", 1, BaseVarType.Int32)]
        [TestCase("int32", 1, BaseVarType.Int32)]
        [TestCase("int64", (long)1, BaseVarType.Int64)]
        [TestCase("real",  1.0,BaseVarType.Real)]
        [TestCase("text", "1",BaseVarType.Text)]
        [TestCase("int[]", new []{1,2,3},BaseVarType.ArrayOf)]
        [TestCase("int64[]", new long[]{1,2,3},BaseVarType.ArrayOf)]
        public void OutputEqualsInput(string type, object expected, BaseVarType baseVarType)
        {
            var runtime = FunBuilder.BuildDefault($"x:{type}\r  y = x");
            var res = runtime.Calculate(Var.New("x", expected));
               res.AssertReturns(Var.New("y", expected));
            Assert.AreEqual(baseVarType, res.Get("y").Type.BaseType);
        }
    }
}