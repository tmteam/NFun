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
        [TestCase("y = 2",PrimitiveVarType.IntType)]
        [TestCase("y = 2*3",PrimitiveVarType.IntType)]
        [TestCase("y = 2^3",PrimitiveVarType.IntType)]
        [TestCase("y = 2%3",PrimitiveVarType.IntType)]

        [TestCase("y = 4/3",PrimitiveVarType.RealType)]
        [TestCase("y = 4- 3",PrimitiveVarType.IntType)]
        [TestCase("y = 4+ 3",PrimitiveVarType.IntType)]
        [TestCase("z = 1 + 4/3 + 3 +2*3 -1", PrimitiveVarType.RealType)]
        [TestCase("y = -2*-4",PrimitiveVarType.IntType)]
        [TestCase("y = -2*(-4+2)",PrimitiveVarType.IntType)]
        [TestCase("y = -(-(-1))",PrimitiveVarType.IntType)]

        [TestCase("y = 0.2",PrimitiveVarType.RealType)]
        [TestCase("y = 1.1_11  ",PrimitiveVarType.RealType)]
        [TestCase("y = 4*0.2",PrimitiveVarType.RealType)]
        [TestCase("y = 4/0.2",PrimitiveVarType.RealType)]
        [TestCase("y = 4/0.2",PrimitiveVarType.RealType)]
        [TestCase("y = 2^0.3",PrimitiveVarType.RealType)]
        [TestCase("y = 0.2^2",PrimitiveVarType.RealType)]
        [TestCase("y = 0.2%2",PrimitiveVarType.RealType)]
        [TestCase("y = 3%0.2",PrimitiveVarType.RealType)]

        [TestCase("y = 0xfF  ",PrimitiveVarType.IntType)]
        [TestCase("y = 0x00_Ff  ",PrimitiveVarType.IntType)]
        [TestCase("y = 0b001  ",PrimitiveVarType.IntType)]
        [TestCase("y = 0b11  ",PrimitiveVarType.IntType)]
        [TestCase("y = 0x_1",PrimitiveVarType.IntType)]
        
        [TestCase("y = 1==1",PrimitiveVarType.BoolType)]
        [TestCase("y = 1==0",PrimitiveVarType.BoolType)]
        [TestCase("y = true==true",PrimitiveVarType.BoolType)]
        [TestCase("y = 1<>0",PrimitiveVarType.BoolType)]
        [TestCase("y = 0<>1",PrimitiveVarType.BoolType)]
        [TestCase("y = 5<>5",PrimitiveVarType.BoolType)]
        [TestCase("y = 5>3", PrimitiveVarType.BoolType)]
        [TestCase("y = 5>6", PrimitiveVarType.BoolType)]
        [TestCase("y = 5>=3", PrimitiveVarType.BoolType)]
        [TestCase("y = 5>=6", PrimitiveVarType.BoolType)]
        [TestCase("y = 5<=5", PrimitiveVarType.BoolType)]
        [TestCase("y = 5<=3", PrimitiveVarType.BoolType)]
        [TestCase("y = true and true", PrimitiveVarType.BoolType)]
        [TestCase("y = true or true", PrimitiveVarType.BoolType)]
        [TestCase("y = true xor true", PrimitiveVarType.BoolType)]

        [TestCase("y=\"\"", PrimitiveVarType.TextType)]
        [TestCase("y=''", PrimitiveVarType.TextType)]
        [TestCase("y='hi world'", PrimitiveVarType.TextType)]
        [TestCase("y='hi world'+5", PrimitiveVarType.TextType)]
        [TestCase("y='hi world'+true", PrimitiveVarType.TextType)]
        [TestCase("y='hi world'+true+5", PrimitiveVarType.TextType)]
        [TestCase("y=''+true+5", PrimitiveVarType.TextType)]
        [TestCase("y='hi'+'world'", PrimitiveVarType.TextType)]
        public void SingleEquation_OutputTypeCalculatesCorrect(string expr, PrimitiveVarType type)
        {
            
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(type, res.Results.First().Type);
        }
        
        [TestCase("y = 1\rz=2",PrimitiveVarType.IntType, PrimitiveVarType.IntType)]
        [TestCase("y = 2.0\rz=2",PrimitiveVarType.RealType, PrimitiveVarType.IntType)]
        [TestCase("y = true\rz=false",PrimitiveVarType.BoolType, PrimitiveVarType.BoolType)]

        [TestCase("y = 1\rz=y",PrimitiveVarType.IntType, PrimitiveVarType.IntType)]
        [TestCase("y = z\rz=2",PrimitiveVarType.IntType, PrimitiveVarType.IntType)]

        [TestCase("y = z/2\rz=2",PrimitiveVarType.RealType, PrimitiveVarType.IntType)]
        [TestCase("y = 2\rz=y/2",PrimitiveVarType.IntType, PrimitiveVarType.RealType)]

        [TestCase("y = 2.0\rz=y",PrimitiveVarType.RealType, PrimitiveVarType.RealType)]
        [TestCase("y = z\rz=2.0",PrimitiveVarType.RealType, PrimitiveVarType.RealType)]

        [TestCase("y = true\rz=y",PrimitiveVarType.BoolType, PrimitiveVarType.BoolType)]
        [TestCase("y = z\rz=true",PrimitiveVarType.BoolType, PrimitiveVarType.BoolType)]

        
        [TestCase("y = 2\rz=y>1",PrimitiveVarType.IntType, PrimitiveVarType.BoolType)]
        [TestCase("y = z>1\rz=2",PrimitiveVarType.BoolType, PrimitiveVarType.IntType)]

        [TestCase("y = 2.0\rz=y>1",PrimitiveVarType.RealType, PrimitiveVarType.BoolType)]
        [TestCase("y = z>1\rz=2.0",PrimitiveVarType.BoolType, PrimitiveVarType.RealType)]
        [TestCase("y = 'hi'\rz=y",PrimitiveVarType.TextType, PrimitiveVarType.TextType)]
        [TestCase("y = 'hi'\rz=y+ 'lala'",PrimitiveVarType.TextType, PrimitiveVarType.TextType)]
        [TestCase("y = true\rz='lala'+y",PrimitiveVarType.BoolType, PrimitiveVarType.TextType)]

        public void TwinEquations_OutputTypesCalculateCorrect(string expr, PrimitiveVarType ytype,PrimitiveVarType ztype)
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