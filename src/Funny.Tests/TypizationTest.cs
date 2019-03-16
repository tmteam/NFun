using System.Linq;
using Funny.Runtime;
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
        [TestCase("y = 2",VarType.IntType)]
        [TestCase("y = 2*3",VarType.IntType)]
        [TestCase("y = 2^3",VarType.IntType)]
        [TestCase("y = 2%3",VarType.IntType)]

        [TestCase("y = 4/3",VarType.RealType)]
        [TestCase("y = 4- 3",VarType.IntType)]
        [TestCase("y = 4+ 3",VarType.IntType)]
        [TestCase("z = 1 + 4/3 + 3 +2*3 -1", VarType.RealType)]
        [TestCase("y = -2*-4",VarType.IntType)]
        [TestCase("y = -2*(-4+2)",VarType.IntType)]
        [TestCase("y = -(-(-1))",VarType.IntType)]

        [TestCase("y = 0.2",VarType.RealType)]
        [TestCase("y = 1.1_11  ",VarType.RealType)]
        [TestCase("y = 4*0.2",VarType.RealType)]
        [TestCase("y = 4/0.2",VarType.RealType)]
        [TestCase("y = 4/0.2",VarType.RealType)]
        [TestCase("y = 2^0.3",VarType.RealType)]
        [TestCase("y = 0.2^2",VarType.RealType)]
        [TestCase("y = 0.2%2",VarType.RealType)]
        [TestCase("y = 3%0.2",VarType.RealType)]

        [TestCase("y = 0xfF  ",VarType.IntType)]
        [TestCase("y = 0x00_Ff  ",VarType.IntType)]
        [TestCase("y = 0b001  ",VarType.IntType)]
        [TestCase("y = 0b11  ",VarType.IntType)]
        [TestCase("y = 0x_1",VarType.IntType)]
        
        [TestCase("y = 1==1",VarType.BoolType)]
        [TestCase("y = 1==0",VarType.BoolType)]
        [TestCase("y = true==true",VarType.BoolType)]
        [TestCase("y = 1<>0",VarType.BoolType)]
        [TestCase("y = 0<>1",VarType.BoolType)]
        [TestCase("y = 5<>5",VarType.BoolType)]
        [TestCase("y = 5>3", VarType.BoolType)]
        [TestCase("y = 5>6", VarType.BoolType)]
        [TestCase("y = 5>=3", VarType.BoolType)]
        [TestCase("y = 5>=6", VarType.BoolType)]
        [TestCase("y = 5<=5", VarType.BoolType)]
        [TestCase("y = 5<=3", VarType.BoolType)]
        [TestCase("y = true and true", VarType.BoolType)]
        [TestCase("y = true or true", VarType.BoolType)]
        [TestCase("y = true xor true", VarType.BoolType)]

        [TestCase("y=\"\"", VarType.TextType)]
        [TestCase("y=''", VarType.TextType)]
        [TestCase("y='hi world'", VarType.TextType)]
        [TestCase("y='hi world'+5", VarType.TextType)]
        [TestCase("y='hi world'+true", VarType.TextType)]
        [TestCase("y='hi world'+true+5", VarType.TextType)]
        [TestCase("y=''+true+5", VarType.TextType)]
        [TestCase("y='hi'+'world'", VarType.TextType)]
        public void SingleEquation_OutputTypeCalculatesCorrect(string expr, VarType type)
        {
            
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(type, res.Results.First().Type);
        }
        
        [TestCase("y = 1\rz=2",VarType.IntType, VarType.IntType)]
        [TestCase("y = 2.0\rz=2",VarType.RealType, VarType.IntType)]
        [TestCase("y = true\rz=false",VarType.BoolType, VarType.BoolType)]

        [TestCase("y = 1\rz=y",VarType.IntType, VarType.IntType)]
        [TestCase("y = z\rz=2",VarType.IntType, VarType.IntType)]

        [TestCase("y = z/2\rz=2",VarType.RealType, VarType.IntType)]
        [TestCase("y = 2\rz=y/2",VarType.IntType, VarType.RealType)]

        [TestCase("y = 2.0\rz=y",VarType.RealType, VarType.RealType)]
        [TestCase("y = z\rz=2.0",VarType.RealType, VarType.RealType)]

        [TestCase("y = true\rz=y",VarType.BoolType, VarType.BoolType)]
        [TestCase("y = z\rz=true",VarType.BoolType, VarType.BoolType)]

        
        [TestCase("y = 2\rz=y>1",VarType.IntType, VarType.BoolType)]
        [TestCase("y = z>1\rz=2",VarType.BoolType, VarType.IntType)]

        [TestCase("y = 2.0\rz=y>1",VarType.RealType, VarType.BoolType)]
        [TestCase("y = z>1\rz=2.0",VarType.BoolType, VarType.RealType)]
        [TestCase("y = 'hi'\rz=y",VarType.TextType, VarType.TextType)]
        [TestCase("y = 'hi'\rz=y+ 'lala'",VarType.TextType, VarType.TextType)]
        [TestCase("y = true\rz='lala'+y",VarType.BoolType, VarType.TextType)]

        public void TwinEquations_OutputTypesCalculateCorrect(string expr, VarType ytype,VarType ztype)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            var y = res.Get("y");
            Assert.AreEqual(y.Type,ytype,"y");
            var z = res.Get("z");
            Assert.AreEqual(z.Type,ztype,"z");
        }
        
        [TestCase("y=5+'hi'")]
        public void ObviouslyFailsWithOutputCast(string expr) =>
            Assert.Throws<OutpuCastParseException>(
                ()=> Interpreter.BuildOrThrow(expr));
        
        
        [TestCase("x:foo\r y= x and true")]        
        [TestCase("x::foo\r y= x and true")]        
        public void ObviouslyFailsWithParse(string expr) =>
            Assert.Throws<ParseException>(
                ()=> Interpreter.BuildOrThrow(expr));

        
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