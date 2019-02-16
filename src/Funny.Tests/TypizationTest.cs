using System.Linq;
using Funny.Runtime;
using NUnit.Framework;

namespace Funny.Tests
{
    public class TypizationTest
    {
        [TestCase("y = 2",VarType.IntType)]
        [TestCase("y = 2*3",VarType.IntType)]
        [TestCase("y = 2^3",VarType.IntType)]
        [TestCase("y = 2%3",VarType.IntType)]

        [TestCase("y = 4/3",VarType.NumberType)]
        [TestCase("y = 4- 3",VarType.IntType)]
        [TestCase("y = 4+ 3",VarType.IntType)]
        [TestCase("z = 1 + 4/3 + 3 +2*3 -1", VarType.NumberType)]
        [TestCase("y = -2*-4",VarType.IntType)]
        [TestCase("y = -2*(-4+2)",VarType.IntType)]
        [TestCase("y = -(-(-1))",VarType.IntType)]

        [TestCase("y = 0.2",VarType.NumberType)]
        [TestCase("y = 1.1_11  ",VarType.NumberType)]
        [TestCase("y = 4*0.2",VarType.NumberType)]
        [TestCase("y = 4/0.2",VarType.NumberType)]
        [TestCase("y = 4/0.2",VarType.NumberType)]
        [TestCase("y = 2^0.3",VarType.NumberType)]
        [TestCase("y = 0.2^2",VarType.NumberType)]
        [TestCase("y = 0.2%2",VarType.NumberType)]
        [TestCase("y = 3%0.2",VarType.NumberType)]

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

        public void SingleEquation_OutputTypeCalculatesCorrect(string expr, VarType type)
        {
            
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(type, res.Results.First().Type);
        }

        [TestCase("y = 1\rz=2",VarType.IntType, VarType.IntType)]
        [TestCase("y = 2.0\rz=2",VarType.NumberType, VarType.IntType)]
        [TestCase("y = true\rz=false",VarType.BoolType, VarType.BoolType)]

        [TestCase("y = 1\rz=y",VarType.IntType, VarType.IntType)]
        [TestCase("y = z\rz=2",VarType.IntType, VarType.IntType)]

        [TestCase("y = z/2\rz=2",VarType.NumberType, VarType.IntType)]
        [TestCase("y = 2\rz=y/2",VarType.IntType, VarType.NumberType)]

        [TestCase("y = 2.0\rz=y",VarType.NumberType, VarType.NumberType)]
        [TestCase("y = z\rz=2.0",VarType.NumberType, VarType.NumberType)]

        [TestCase("y = true\rz=y",VarType.BoolType, VarType.BoolType)]
        [TestCase("y = z\rz=true",VarType.BoolType, VarType.BoolType)]

        
        [TestCase("y = 2\rz=y>1",VarType.IntType, VarType.BoolType)]
        [TestCase("y = z>1\rz=2",VarType.BoolType, VarType.IntType)]

        [TestCase("y = 2.0\rz=y>1",VarType.NumberType, VarType.BoolType)]
        [TestCase("y = z>1\rz=2.0",VarType.BoolType, VarType.NumberType)]
        public void TwinEquations_OutputTypesCalculateCorrect(string expr, VarType ytype,VarType ztype)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            var y = res.Get("y");
            Assert.AreEqual(y.Type,ytype,"y");
            var z = res.Get("z");
            Assert.AreEqual(z.Type,ztype,"z");
        }
    }
}