using System;
using System.Linq;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests
{
    class NfunUpdateTest
    {
        [TestCase("y = 2*x", 3, 6)]
        [TestCase("y = 2*x", 3.5, 7.0)]
        [TestCase("y = 4/x", 2, 2)]
        [TestCase("y = x/4", 10, 2.5)]
        [TestCase("y = 4- x", 3, 1)]
        [TestCase("y = x- x", 3, 0)]
        [TestCase("y = 4+ x", 3, 7)]
        [TestCase("y = (x + 4/x)", 2, 4)]
        [TestCase("y = x**3", 2, 8)]
        [TestCase("y = x.rema(3)", 2, 2)]
        [TestCase("y = x.rema(4)", 5, 1)]
        [TestCase("y = x.rema(-4)", 5, 1)]
        [TestCase("y = x.rema(4)", -5, -1)]
        [TestCase("y = x.rema(-4)", -5, -1)]
        [TestCase("y = -x ", 0.3, -0.3)]
        [TestCase("y = -(-(-x))", 2, -2)]
        [TestCase("y = x/0.2", 1, 5)]

        public void SingleVariableEquation(string expr, double arg, double expected)
        {
            var runtime = expr.Build();
            var ySource = runtime.GetAllVariableSources().First(vs => vs.IsOutput && vs.Name == "y");
            var xSource = runtime.GetAllVariableSources().First(vs => !vs.IsOutput && vs.Name == "x");
            xSource.FunnyValue = arg;
            runtime.Update();
            Assert.AreEqual(expected, ySource.FunnyValue);
        }
        
        
        [TestCase("y = 2*x", 0.0)]
        [TestCase("y:real = 2*x", 0.0)]
        [TestCase("y:int64 = 2*x", (Int64)0)]
        [TestCase("y:int32 = 2*x", (Int32)0)]
        [TestCase("y:int16 = 2&x", (Int16)0)]
        [TestCase("y:uint64 = 2*x", (UInt64)0)]
        [TestCase("y:uint32 = 2*x", (UInt32)0)]
        [TestCase("y:uint16 = 2&x", (UInt16)0)]
        [TestCase("y:byte   = 2&x", (byte)0)]
        [TestCase("y:byte   = 2|x", (byte)2)]
        [TestCase("y:byte   = x1|x2|x3", (byte)0)]
        [TestCase("y = x/4", 0.0)]
        [TestCase("y = (x+1)/4", 0.25)]
        [TestCase("y = true or x", true)]
        [TestCase("x:bool; y = x or x", false)]
        [TestCase("x:text; y = x", "")]
        [TestCase("x:text; y = x.count()", 0)]
        [TestCase("x:text; y = x.reverse()", "")]
        [TestCase("x:int[]; y = x.count()", 0)]
        [TestCase("x:int[][]; y = x.count()", 0)]
        [TestCase("x:real[][]; y = x.count()", 0)]
        [TestCase("x:text[][]; y = x.count()", 0)]
        [TestCase("y = -(-(-x))", 0.0)]
        public void InputNotSet_SingleVariableEquation(string expr, object expected)
        {
            var runtime = expr.Build();
            var ySource = runtime.GetAllVariableSources().First(vs => vs.IsOutput && vs.Name == "y");
            runtime.Update();
            Assert.AreEqual(expected, ySource.FunnyValue);
        }
    }
}
