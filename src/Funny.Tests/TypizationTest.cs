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

        public void OutputType(string expr, VarType type)
        {
            
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(type, res.Results.First().Type);
        }

    }
}