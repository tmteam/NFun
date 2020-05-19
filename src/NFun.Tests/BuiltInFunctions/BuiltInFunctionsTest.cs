using NFun;
using NFun.ParseErrors;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests.BuiltInFunctions
{
    [TestFixture]

    public class ConvertFunctionsTest
    {
        [TestCase("int", (int)-123, "int16", (short)-123)]
        [TestCase("int", (int)123, "int16", (short)123)]
        [TestCase("int16", (short)-123, "int16", (short)-123)]
        [TestCase("int32", (int)-123, "int16", (short)-123)]
        [TestCase("int64", (long)-123, "int16", (short)-123)]
        [TestCase("uint8", (byte)123, "int16", (short)123)]
        [TestCase("uint16", (ushort)123, "int16", (short)123)]
        [TestCase("uint32", (uint)123, "int16", (short)123)]
        [TestCase("uint64", (ulong)123, "int16", (short)123)]

        [TestCase("int", (int)-123, "int", -123)]
        [TestCase("int", (int)123, "int", 123)]
        [TestCase("int16", (short)-123, "int", -123)]
        [TestCase("int32", (int)-123, "int", -123)]
        [TestCase("int64", (long)-123, "int", -123)]
        [TestCase("uint8", (byte)123, "int", 123)]
        [TestCase("uint16", (ushort)123, "int", 123)]
        [TestCase("uint32", (uint)123, "int", 123)]
        [TestCase("uint64", (ulong)123, "int", 123)]

        [TestCase("int", (int)-123, "int64", (long)-123)]
        [TestCase("int", (int)123, "int64", (long)123)]
        [TestCase("int16", (short)-123, "int64", (long)-123)]
        [TestCase("int32", (int)-123, "int64", (long)-123)]
        [TestCase("int64", (long)-123, "int64", (long)-123)]
        [TestCase("uint8", (byte)123, "int64", (long)123)]
        [TestCase("uint16", (ushort)123, "int64", (long)123)]
        [TestCase("uint32", (uint)123, "int64", (long)123)]
        [TestCase("uint64", (ulong)123, "int64", (long)123)]

        [TestCase("int", (int)123, "byte", (byte)123)]
        [TestCase("int16", (short)123, "byte", (byte)123)]
        [TestCase("int64", (long)123, "byte", (byte)123)]
        [TestCase("uint8", (byte)123, "byte", (byte)123)]
        [TestCase("uint16", (ushort)123, "byte", (byte)123)]
        [TestCase("uint32", (uint)123, "byte", (byte)123)]
        [TestCase("uint64", (ulong)123, "byte", (byte)123)]

        [TestCase("int", (int)123, "uint16", (ushort)123)]
        [TestCase("int16", (short)123, "uint16", (ushort)123)]
        [TestCase("int64", (long)123, "uint16", (ushort)123)]
        [TestCase("uint8", (byte)123, "uint16", (ushort)123)]
        [TestCase("uint16", (ushort)123, "uint16", (ushort)123)]
        [TestCase("uint32", (uint)123, "uint16", (ushort)123)]
        [TestCase("uint64", (ulong)123, "uint16", (ushort)123)]

        [TestCase("int", (int)123, "uint32", (uint)123)]
        [TestCase("int16", (short)123, "uint32", (uint)123)]
        [TestCase("int64", (long)123, "uint32", (uint)123)]
        [TestCase("uint8", (byte)123, "uint32", (uint)123)]
        [TestCase("uint16", (ushort)123, "uint32", (uint)123)]
        [TestCase("uint32", (uint)123, "uint32", (uint)123)]
        [TestCase("uint64", (ulong)123, "uint32", (uint)123)]

        [TestCase("int", (int)123, "uint64", (ulong)123)]
        [TestCase("int16", (short)123, "uint64", (ulong)123)]
        [TestCase("int64", (long)123, "uint64", (ulong)123)]
        [TestCase("uint8", (byte)123, "uint64", (ulong)123)]
        [TestCase("uint16", (ushort)123, "uint64", (ulong)123)]
        [TestCase("uint32", (uint)123, "uint64", (ulong)123)]
        [TestCase("uint64", (ulong)123, "uint64", (ulong)123)]
        public void ConvertIntegersFunctionsTest(string inputType, object inputValue, string outputType, object expectedOutput)
        {
            var expr = $"x:{inputType}; y:{outputType} = convert(x)";
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate(VarVal.New("x", inputValue))
                .AssertReturns(VarVal.New("y", expectedOutput));
        }

        [Ignore("converts")]
        [TestCase("toInt(1.2)", 1)]
        [TestCase("toInt(-1.2)", -1)]
        [TestCase("toInt('1')", 1)]
        [TestCase("toInt('-123')", -123)]
        [TestCase("toInt([0x21,0x33,0x12])", 1_192_737)]
        [TestCase("toInt([0x21,0x33,0x12,0x00])", 1_192_737)]
        [TestCase("toInt([0x21,0x00,0x00,0x00])", 0x21)]
        [TestCase("toInt([0x21,0x00,0x00,0x00])", 0x21)]
        [TestCase("toInt([0x21])", 0x21)]

        [TestCase("toReal('1')", 1.0)]
        [TestCase("toReal('1.1')", 1.1)]
        [TestCase("toReal('-0.123')", -0.123)]
        [TestCase("toReal(1)", 1.0)]
        [TestCase("toReal(-1)", -1.0)]
        [TestCase("toBits(123)", new[]
        {
            true, true, false,true, true, true, true ,false,
            false,false,false,false,false,false,false,false,
            false,false,false,false,false,false,false,false,
            false,false,false,false,false,false,false,false,
        })]
        [TestCase("toBytes(123)", new[] { 123, 0, 0, 0 })]
        [TestCase("toBytes(1_192_737)", new[] { 0x21, 0x33, 0x12, 0 })]
        [TestCase("toUnicode('hi there')", new[] { 0x68, 00, 0x69, 00, 0x20, 00, 0x74, 00, 0x68, 00, 0x65, 00, 0x72, 00, 0x65, 00 })]
        [TestCase("toUtf8('hi there')", new[] { 0x68, 0x69, 0x20, 0x74, 0x68, 0x65, 0x72, 0x65 })]
        public void ConstantConvertTest(string expr, object expected)
            => FunBuilder.BuildDefault(expr).Calculate().AssertOutEquals(expected);


        [TestCase("y:int= convert(1.2)", 1)]
        [TestCase("y:int= convert(-1.2)", -1)]
        [TestCase("y:int= convert('1')", 1)]
        [TestCase("y:int= convert('-123')", -123)]
        [TestCase("x:byte[]=[0x21,0x33,0x12];  y:int= x.convert()", 1_192_737)]
        [TestCase("x:byte[]=[0x21,0x33,0x12,0x00]; y:int= convert(x)", 1_192_737)]
        [TestCase("x:byte[]=[0x21,0x00,0x00,0x00]; y:int= convert(x)", 0x21)]
        [TestCase("x:byte[]=[0x21,0x00,0x00,0x00]; y:int= convert(x)", 0x21)]
        [TestCase("x:byte[]=[0x21]; y:int= convert(x)", 0x21)]

        [TestCase("y:real = convert('1')", 1.0)]
        [TestCase("y:real = convert('1.1')", 1.1)]
        [TestCase("y:real = convert('-0.123')", -0.123)]
        [TestCase("y:real = convert(1)", 1.0)]
        [TestCase("y:real = convert(-1)", -1.0)]
        [TestCase("y:bool[] = convert(0b1111011)", new[]
        {
            true, true, false,true, true, true, true ,false,
            false,false,false,false,false,false,false,false,
            false,false,false,false,false,false,false,false,
            false,false,false,false,false,false,false,false,
        })]
        [TestCase("y:byte[]=convert(0x123)", new byte[] { 35, 1, 0, 0 })]
        [TestCase("y:byte[]=convert(0xFA00FA)", new byte[] { 250, 0, 250, 0 })]
        [TestCase("y:byte[]=convert('hi there')", new byte[] { 0x68, 00, 0x69, 00, 0x20, 00, 0x74, 00, 0x68, 00, 0x65, 00, 0x72, 00, 0x65, 00 })]
        public void ConstantConvertFunctionTest(string expr, object expected)
            => FunBuilder.BuildDefault(expr).Calculate().AssertHas(VarVal.New("y", expected));
    }
    [TestFixture]
    public class BuiltInFunctionsTest
    {
       


        [TestCase("toText([1,2,3])", "[1,2,3]")]
        [TestCase("toText(-1)", "-1")]
        [TestCase("toText(-0.123)", "-0.123")]
       
        
        [TestCase("abs(0x1)", 1)]
        [TestCase("abs(-0x1)", 1)]
        [TestCase("abs(1.0)",1.0)]
        [TestCase("abs(-1.0)",1.0)]
        [TestCase("add(0x1,2)",3)]
        [TestCase("add(add(1,2),add(0x3,4))",10)]
        [TestCase("abs(0x1-0x4)", 3)]
        [TestCase("15 - add(abs(1-4), 0x7)",5)]
        //[TestCase("pi()",Math.PI)]
        //[TestCase("e()",Math.E)]
        [TestCase("sqrt(0x0)", 0.0)]
        [TestCase("sqrt(1.0)", 1.0)]
        [TestCase("sqrt(4.0)", 2.0)]
        [TestCase("cos(0)", 1.0)]
        [TestCase("sin(0)", 0.0)]
        [TestCase("acos(1)", 0.0)]
        [TestCase("asin(0)", 0.0)]
        [TestCase("atan(0)", 0.0)]
        [TestCase("tan(0)", 0.0)]
        [TestCase("exp(0)", 1.0)] 
        [TestCase("log(1,10)", 0.0)]
        [TestCase("log(1)", 0.0)]
        [TestCase("log10(1)", 0.0)]
        //todo
        //[TestCase("ceil(7.03)",  8)]
        //[TestCase("ceil(7.64)",  8)]
        //[TestCase("ceil(0.12)",  1)]
        //[TestCase("ceil(-0.12)", 0)]
        //[TestCase("ceil(-7.1)", -7)]
        //[TestCase("ceil(-7.6)", -7)]
        //[TestCase("floor(7.03)",  7)]
        //[TestCase("floor(7.64)",  7)]
        //[TestCase("floor(0.12)",  0)]
        //[TestCase("floor(-0.12)", -1)]
        //[TestCase("floor(-7.1)", -8)]
        //[TestCase("floor(-7.6)", -8)]
        //[TestCase("round(1.66666,1)", 1.7)]
        //[TestCase("round(1.222,2)", 1.22)]
        //[TestCase("round(1.66666)", 2)]
        //[TestCase("round(1.2)", 1)]


        //[TestCase("sign(-5)", -1)]
        //[TestCase("sign(-5.0)", -1)]
        //[TestCase("sign(5)", 1)]
        //[TestCase("sign(5.2)", 1)]
        [TestCase("min(0.5, 1)", 0.5)]
        [TestCase("[1,2,3].count()", 3)]
        [TestCase("['1','2','3'].count()", 3)]
        [TestCase("[1..10].filter{it>3}.count()", 7)]
        [TestCase("[].count()", 0)]

        [TestCase("count([1,2,3])",3)]
        [TestCase("count([])",0)]
        [TestCase("count([1.0,2.0,3.0])",3)]
        [TestCase("count([[1,2],[3,4]])",2)]
        [TestCase("avg([1,2,3])",2.0)]
        [TestCase("avg([1.0,2.0,6.0])",3.0)]
        [TestCase("sum([1.0,2,3])",6.0)]
        [TestCase("sum([1.0,2.5,6.0])", 9.5)]
        [TestCase("max([1.0,10.5,6.0])",10.5)]
        [TestCase("max([1,-10,0.0])",1.0)]
        [TestCase("max(1.0,3.4)",3.4)]
        [TestCase("max(0x4,3)",4)]
        [TestCase("min([1.0,10.5,6.0])",1.0)]
        [TestCase("min([0x1,-10,0])",-10)]
        [TestCase("min(1.0,3.4)",1.0)]
        [TestCase("min(4,0x3)",3)]
        [TestCase("median([1.0,10.5,6.0])",6.0)]
        [TestCase("median([1,-10,0])",0.0)]        
        [TestCase("[1.0,2.0,3.0].any()",true)]
        [TestCase("['a'].any()", true)]
        [TestCase("[1..10].filter{it>3}.any()", true)]
        [TestCase("[1..10].filter{it>10}.any()", false)]
        [TestCase("[1,2,3,4].fold{it1+it2}", 10.0)]
        [TestCase("[1,2,3,4].fold(0,{it1+it2})", 10.0)]
        [TestCase("[1,2,3,4].fold(-10,{it1+it2})", 0.0)]
        [TestCase("[1,2,3,4].fold(''){'{it1}{it2}'}", "1234")]
        [TestCase("any([])",false)]
        [TestCase("[0x4,0x3,0x5,0x1].sort()",new []{1,3,4,5})]
        [TestCase("[4.0,3.0,5.0,1.0].sort()",new []{1.0,3.0,4.0,5.0})]
        [TestCase("['4.0','3.0','5.0','1.0'].sort()",new []{"1.0","3.0","4.0","5.0"})]
        [TestCase("range(0,5)",new []{0.0,1,2,3,4,5})]
        [TestCase("range(7,10)",new []{7.0,8,9,10})]
        [TestCase("range(1,10,2.0)",new []{1.0,3.0,5.0,7.0,9.0})]
        public void ConstantEquationWithPredefinedFunction(string expr, object expected)
            =>FunBuilder.BuildDefault(expr).Calculate().AssertOutEquals(0.00001, expected);
        
        [TestCase((long)42, "x:int64\r y = x.add(1)", (long)43)]
        [TestCase((long)42, "x:int64\r y = max(1,x)", (long)42)]
        [TestCase((long)42, "x:int64\r y = min(1,x)", (long)1)]
        [TestCase((long)42, "x:int64\r y = min(100,x)", (long)42)]
        public void SingleVariableEquation(object input, string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate(VarVal.New("x", input))
                .AssertReturns(VarVal.New("y", expected));
        }
    

        
        [TestCase("y = abs(x)",1.0,1.0)]

        [TestCase("y = abs(-x)",-1.0,1.0)]
        [TestCase("y = add(x,0x2)",1,3)]
        [TestCase("y = add(0x1,x)",2,3)]
        [TestCase("y = add(add(x,x),add(x,x))",1.0,4.0)]
        [TestCase("y = abs(x-4.0)",1.0,3.0)]
        //todo
        // [TestCase("x:int; y = abs(toInt(x)-toInt(4))",1,3)]
        [TestCase("y = abs(x-4)",1.0,3.0)]
        //todo
       //[TestCase("y = abs(toInt(x)-toInt(4))",1,3)]
        //[TestCase("y = abs(x-toInt(4))",1,3)]
        public void EquationWithPredefinedFunction(string expr, object arg, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate(VarVal.New("x", arg))
                .AssertReturns(0.00001, VarVal.New("y", expected));
        }
        [TestCase("y = pi(")]
        [TestCase("y = pi(1)")]
        [TestCase("y = abs(")]
        [TestCase("y = abs)")]
        [TestCase("y = abs()")]
        [TestCase("y = abs(1,)")]
        [TestCase("y = abs(1,,2)")]
        [TestCase("y = abs(,,2)")]
        [TestCase("y = abs(,,)")]
        [TestCase("y = abs(2,)")]
        [TestCase("y = abs(1,2)")]
        [TestCase("y = abs(1 2)")]
        [TestCase("y = add(")]
        [TestCase("y = add()")]
        [TestCase("y = add(1)")]
        [TestCase("y = add 1")]
        [TestCase("y = add(1,2,3)")]
        [TestCase("y = avg(['1','2','3'])")]
       // [TestCase("y= max([])")]
        [TestCase("y= max(1,2,3)")]
        [TestCase("y= max(1,true)")]
        [TestCase("y= max(1,(j)->j)")]
        [TestCase("y = [1,2] in [1,2,3,4]")]
        public void ObviouslyFails(string expr) =>
            Assert.Throws<FunParseException>(
                () => FunBuilder.BuildDefault(expr));

    
    }
}