using System;
using System.Net.WebSockets;
using NFun;
using NFun.BuiltInFunctions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class BuiltInFunctionsTest
    {
        [TestCase("int",   (int)-123,     "toInt16", (short)-123)]
        [TestCase("int",   (int)123,      "toInt16", (short) 123)]
        [TestCase("int16", (short)-123,   "toInt16", (short)-123)]
        [TestCase("int32", (int)-123,     "toInt16", (short)-123)]
        [TestCase("int64", (long)-123,    "toInt16", (short)-123)]
        [TestCase("uint8", (byte)123,     "toInt16", (short) 123)]
        [TestCase("uint16",(ushort)123,   "toInt16", (short) 123)]
        [TestCase("uint32",(uint)123,     "toInt16", (short) 123)]
        [TestCase("uint64",(ulong)123,    "toInt16", (short) 123)]
        
        [TestCase("int",   (int)-123,     "toInt", -123)]
        [TestCase("int",   (int)123,      "toInt",  123)]
        [TestCase("int16", (short)-123,   "toInt", -123)]
        [TestCase("int32", (int)-123,     "toInt", -123)]
        [TestCase("int64", (long)-123,    "toInt", -123)]
        [TestCase("uint8", (byte)123,     "toInt",  123)]
        [TestCase("uint16",(ushort)123,   "toInt",  123)]
        [TestCase("uint32",(uint)123,     "toInt",  123)]
        [TestCase("uint64",(ulong)123,    "toInt",  123)]

        [TestCase("int",   (int)-123,     "toInt", -123)]
        [TestCase("int",   (int)123,      "toInt",  123)]
        [TestCase("int16", (short)-123,   "toInt", -123)]
        [TestCase("int32", (int)-123,     "toInt", -123)]
        [TestCase("int64", (long)-123,    "toInt", -123)]
        [TestCase("uint8", (byte)123,     "toInt",  123)]
        [TestCase("uint16",(ushort)123,   "toInt",  123)]
        [TestCase("uint32",(uint)123,     "toInt",  123)]
        [TestCase("uint64",(ulong)123,    "toInt",  123)]
        
        [TestCase("int",   (int)-123,     "toInt32", -123)]
        [TestCase("int",   (int)123,      "toInt32",  123)]
        [TestCase("int16", (short)-123,   "toInt32", -123)]
        [TestCase("int32", (int)-123,     "toInt32", -123)]
        [TestCase("int64", (long)-123,    "toInt32", -123)]
        [TestCase("uint8", (byte)123,     "toInt32",  123)]
        [TestCase("uint16",(ushort)123,   "toInt32",  123)]
        [TestCase("uint32",(uint)123,     "toInt32",  123)]
        [TestCase("uint64",(ulong)123,    "toInt32",  123)]
        
        [TestCase("int",   (int)-123,     "toInt64", (long)-123)]
        [TestCase("int",   (int)123,      "toInt64", (long) 123)]
        [TestCase("int16", (short)-123,   "toInt64", (long)-123)]
        [TestCase("int32", (int)-123,     "toInt64", (long)-123)]
        [TestCase("int64", (long)-123,    "toInt64", (long)-123)]
        [TestCase("uint8", (byte)123,     "toInt64", (long) 123)]
        [TestCase("uint16",(ushort)123,   "toInt64", (long) 123)]
        [TestCase("uint32",(uint)123,     "toInt64", (long) 123)]
        [TestCase("uint64",(ulong)123,    "toInt64", (long) 123)]
        
        [TestCase("int",   (int)123,      "toUint8", (byte) 123)]
        [TestCase("int16", (short)123,    "toUint8", (byte) 123)]
        [TestCase("int64", (long)123,     "toUint8", (byte) 123)]
        [TestCase("uint8", (byte)123,     "toUint8", (byte) 123)]
        [TestCase("uint16",(ushort)123,   "toUint8", (byte) 123)]
        [TestCase("uint32",(uint)123,     "toUint8", (byte) 123)]
        [TestCase("uint64",(ulong)123,    "toUint8", (byte) 123)]
        
        [TestCase("int",   (int)123,      "toByte", (byte) 123)]
        [TestCase("int16", (short)123,    "toByte", (byte) 123)]
        [TestCase("int64", (long)123,     "toByte", (byte) 123)]
        [TestCase("uint8", (byte)123,     "toByte", (byte) 123)]
        [TestCase("uint16",(ushort)123,   "toByte", (byte) 123)]
        [TestCase("uint32",(uint)123,     "toByte", (byte) 123)]
        [TestCase("uint64",(ulong)123,    "toByte", (byte) 123)]
        
        [TestCase("int",   (int)123,      "toUint16", (ushort) 123)]
        [TestCase("int16", (short)123,    "toUint16", (ushort) 123)]
        [TestCase("int64", (long)123,     "toUint16", (ushort) 123)]
        [TestCase("uint8", (byte)123,     "toUint16", (ushort) 123)]
        [TestCase("uint16",(ushort)123,   "toUint16", (ushort) 123)]
        [TestCase("uint32",(uint)123,     "toUint16", (ushort) 123)]
        [TestCase("uint64",(ulong)123,    "toUint16", (ushort) 123)]
        
        [TestCase("int",   (int)123,      "toUint32", (uint) 123)]
        [TestCase("int16", (short)123,    "toUint32", (uint) 123)]
        [TestCase("int64", (long)123,     "toUint32", (uint) 123)]
        [TestCase("uint8", (byte)123,     "toUint32", (uint) 123)]
        [TestCase("uint16",(ushort)123,   "toUint32", (uint) 123)]
        [TestCase("uint32",(uint)123,     "toUint32", (uint) 123)]
        [TestCase("uint64",(ulong)123,    "toUint32", (uint) 123)]
        
        [TestCase("int",   (int)123,      "toUint64", (ulong) 123)]
        [TestCase("int16", (short)123,    "toUint64", (ulong) 123)]
        [TestCase("int64", (long)123,     "toUint64", (ulong) 123)]
        [TestCase("uint8", (byte)123,     "toUint64", (ulong) 123)]
        [TestCase("uint16",(ushort)123,   "toUint64", (ulong) 123)]
        [TestCase("uint32",(uint)123,     "toUint64", (ulong) 123)]
        [TestCase("uint64",(ulong)123,    "toUint64", (ulong) 123)]
        public void ConvertIntegersFunctionsTest(string inputType, object inputValue,  string converter, object expectedOutput)
        {
            var expr = $"x:{inputType}; y = {converter}(x)";
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate(Var.New("x", inputValue))
                .AssertReturns(Var.New("y", expectedOutput));
        }
        
        [TestCase( "toInt(1.2)",  1)]
        [TestCase( "toInt(-1.2)", -1)]
        [TestCase( "toInt('1')", 1)]
        [TestCase( "toInt('-123')", -123)]
        [TestCase("toInt([0x21,0x33,0x12])",1_192_737)]
        [TestCase("toInt([0x21,0x33,0x12,0x00])",1_192_737)]
        [TestCase("toInt([0x21,0x00,0x00,0x00])",0x21)]
        [TestCase("toInt([0x21,0x00,0x00,0x00])",0x21)]
        [TestCase("toInt([0x21])",0x21)]

        [TestCase("toReal('1')", 1.0)]
        [TestCase("toReal('1.1')", 1.1)]
        [TestCase("toReal('-0.123')", -0.123)]
        [TestCase("toReal(1)", 1.0)]
        [TestCase("toReal(-1)", -1.0)]
        [TestCase("toText([1,2,3])", "[1,2,3]")]
        [TestCase("toText(-1)", "-1")]
        [TestCase("toText(-0.123)", "-0.123")]
        [TestCase("toBits(123)", new[]
        {
            true, true, false,true, true, true, true ,false,
            false,false,false,false,false,false,false,false,
            false,false,false,false,false,false,false,false,
            false,false,false,false,false,false,false,false,
        })]
        [TestCase("toBytes(123)", new[]{123,0,0,0})]
        [TestCase("toBytes(1_192_737)", new[]{0x21,0x33,0x12,0})]
        [TestCase("toUnicode('hi there')", new[]{0x68,00,0x69,00,0x20,00,0x74,00,0x68,00,0x65,00,0x72,00,0x65,00})]
        [TestCase("toUtf8('hi there')", new[]{0x68,0x69,0x20,0x74,0x68,0x65,0x72,0x65})]

        
        [TestCase("abs(1)",1)]
        [TestCase("abs(-1)",1)]
        [TestCase("abs(1.0)",1.0)]
        [TestCase("abs(-1.0)",1.0)]
        [TestCase("sum(1,2)",3)]
        [TestCase("sum(sum(1,2),sum(3,4))",10)]
        [TestCase("abs(1-4)",3)]
        [TestCase("15 - sum(abs(1-4), 7)",5)]
        [TestCase("pi()",Math.PI)]
        [TestCase("e()",Math.E)]
        
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

        [TestCase("ceil(7.03)",  8)]
        [TestCase("ceil(7.64)",  8)]
        [TestCase("ceil(0.12)",  1)]
        [TestCase("ceil(-0.12)", 0)]
        [TestCase("ceil(-7.1)", -7)]
        [TestCase("ceil(-7.6)", -7)]
        [TestCase("floor(7.03)",  7)]
        [TestCase("floor(7.64)",  7)]
        [TestCase("floor(0.12)",  0)]
        [TestCase("floor(-0.12)", -1)]
        [TestCase("floor(-7.1)", -8)]
        [TestCase("floor(-7.6)", -8)]
        [TestCase("round(1.66666,1)", 1.7)]
        [TestCase("round(1.222,2)", 1.22)]
        [TestCase("round(1.66666)", 2)]
        [TestCase("round(1.2)", 1)]

        
        [TestCase("sign(-5)", -1)]
        [TestCase("sign(-5.0)", -1)]
        [TestCase("sign(5)", 1)]
        [TestCase("sign(5.2)", 1)]
        [TestCase("min(0.5, 1)", 0.5)]
        [TestCase("count([1,2,3])",3)]
        [TestCase("count([])",0)]
        [TestCase("count([1.0,2.0,3.0])",3)]
        [TestCase("count([[1,2],[3,4]])",2)]
        [TestCase("avg([1,2,3])",2.0)]
        [TestCase("avg([1.0,2.0,6.0])",3.0)]
        [TestCase("sum([1,2,3])",6)]
        [TestCase("sum([1.0,2.5,6.0])",9.5)]
        [TestCase("max([1.0,10.5,6.0])",10.5)]
        [TestCase("max([1,-10,0])",1)]
        [TestCase("max(1.0,3.4)",3.4)]
        [TestCase("max(4,3)",4)]
        [TestCase("min([1.0,10.5,6.0])",1.0)]
        [TestCase("min([1,-10,0])",-10)]
        [TestCase("min(1.0,3.4)",1.0)]
        [TestCase("min(4,3)",3)]
        [TestCase("median([1.0,10.5,6.0])",6.0)]
        [TestCase("median([1,-10,0])",0)]        
        [TestCase("[1.0,2.0,3.0].any()",true)]
        [TestCase("any([])",false)]
        [TestCase("[4,3,5,1].sort()",new []{1,3,4,5})]
        [TestCase("[4.0,3.0,5.0,1.0].sort()",new []{1.0,3.0,4.0,5.0})]
        [TestCase("['4.0','3.0','5.0','1.0'].sort()",new []{"1.0","3.0","4.0","5.0"})]
        [TestCase("range(0,5)",new []{0,1,2,3,4,5})]
        [TestCase("range(7,10)",new []{7,8,9,10})]
        [TestCase("range(1,10,2)",new []{1,3,5,7,9})]
        public void ConstantEquationWithPredefinedFunction(string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate()
                .AssertReturns(0.00001, Var.New("out", expected));
        }
        
        [TestCase((long)42, "x:int64\r y = x.sum(1)", (long)43)]
        [TestCase((long)42, "x:int64\r y = max(1,x)", (long)42)]
        [TestCase((long)42, "x:int64\r y = min(1,x)", (long)1)]
        [TestCase((long)42, "x:int64\r y = min(100,x)", (long)42)]
        public void SingleVariableEquation(object input, string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate(Var.New("x", input))
                .AssertReturns(Var.New("y", expected));
        }
    
        
        //todo hi fun overloads selector
        //[TestCase("y = [0,7,1,2,3] . fold(max)", 7)]
        //[TestCase("y = [0,7,1,2,3] . fold(sum)", 7)]
        //[TestCase("y = [0.0,7.0,1.0,2.0,3.0] . fold(sum)", 7.0)]

        [TestCase("mysum(x:int, y:int):int = x+y \r" +
                  "y = [0,7,1,2,3].reduce(mysum)", 13)]
        [TestCase( @"rr(x:real):bool = x>10
                     y = filter([11.0,20.0,1.0,2.0],rr)",new[]{11.0,20.0})]
        [TestCase( @"ii(x:int):bool = x>10
                     y = filter([11,20,1,2],ii)",new[]{11,20})]
        [TestCase( @"ii(x:int):int = x*x
                     y = map([1,2,3],ii)",new[]{1,4,9})]
        [TestCase( @"ii(x:int):real = x/2
                     y = map([1,2,3],ii)",new[]{0.5,1.0,1.5})]
        [TestCase( @"isodd(x:int):bool = (x%2) == 0
                     y = map([1,2,3],isodd)",new[]{false, true,false})]
        [TestCase( @"toS1(t:text, x:int):text = t.strConcat(x)
                     y = reduce([1,2,3], ':', toS1)",":123")]
        [TestCase( @"toS2(t:text, x:int):text = t.strConcat(x)
                     y = reduce([1], '', toS2)","1")]
        [TestCase( @"toS3(t:text, x:int):text = t.strConcat(x)
                     y = reduce([1][1:1], '', toS3)","")]
        [TestCase( @"toR(r:real, x:int):real = r+x
                     y = reduce([1,2,3], 0.5, toR)",6.5)]
        [TestCase( @"iSum(r:int, x:int):int = r+x
                     y = reduce([1,2,3], iSum)",6)]
        [TestCase( @"iSum(r:int, x:int):int = r+x
                     y = reduce([100], iSum)",100)]
        public void HiOrderFunConstantEquatation(string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate()
                .AssertReturns(Var.New("y", expected));
        }
        
        [TestCase("y = take([1,2,3,4,5],3)",new []{1,2,3})]        
        [TestCase("y = take([1.0,2.0,3.0,4.0,5.0],4)",new []{1.0,2.0,3.0,4.0})]        
        [TestCase("y = take([1.0,2.0,3.0],20)",new []{1.0,2.0,3.0})]        
        [TestCase("y = take([1.0,2.0,3.0],0)",new double[0])]        
        [TestCase("y = take(skip([1.0,2.0,3.0],1),1)",new []{2.0})]        
    
        [TestCase("y = skip([1,2,3,4,5],3)",new []{4,5})]        
        [TestCase("y = skip(['1','2','3','4','5'],3)",new []{"4","5"})]        
        [TestCase("y = skip([1.0,2.0,3.0,4.0,5.0],4)",new []{5.0})]        
        [TestCase("y = skip([1.0,2.0,3.0],20)",new double[0])]        
        [TestCase("y = skip([1.0,2.0,3.0],0)",new []{1.0,2.0,3.0})]        
        
        [TestCase("y = repeat('abc',3)",new []{"abc","abc","abc"})]        
        [TestCase("y = repeat('abc',0)",new string[0])]        
        
        [TestCase("mypage(x:int[]):int[] = take(skip(x,1),1) \r y = mypage([1,2,3]) ",new []{2})]        

        [TestCase("y = [1,2,3]. reverse()",new[]{3,2,1})]
        [TestCase("y = [1,2,3]. reverse() . reverse()",new[]{1,2,3})]
        [TestCase("y = []. reverse()",new object[0])]
        
        [TestCase("y = [1,2,3].get(1)", 2)]
        [TestCase("y = [1,2,3].get(0)", 1)]
        
        [TestCase("y = [1.0,2.0].concat([3.0,4.0])", new []{1.0,2.0,3.0,4.0})]
        [TestCase("y = [1.0].concat([2.0]).concat([3.0,4.0])", new []{1.0,2.0,3.0,4.0})]
        [TestCase("y = [].concat([])", new object[0])]

        [TestCase("y = [1,2,3].set(1,42)", new[]{1,42,3})]
        
        [TestCase("y = [1.0] . reiterate(3)", new[]{1.0,1.0,1.0})]
        [TestCase("y = [] . reiterate(3)", new object[0])]
        [TestCase("y = ['a','b'] . reiterate(3)", new []{"a","b","a","b","a","b"})]
        [TestCase("y = ['a','b'] . reiterate(0)", new string[0])]
        [TestCase("y = ['a','b'] . reiterate(1)", new []{"a","b"})]
        
        [TestCase("y = [1.0,2.0] . unite([3.0,4.0])", new []{1.0,2.0,3.0,4.0})]
        [TestCase("y = [1.0,2.0,3.0]. unite([3.0,4.0])", new []{1.0,2.0,3.0,4.0})]

        [TestCase("y = []. intersect([])", new object[0])]
        [TestCase("y = [1.0,4.0,2.0,3.0] .intersect([3.0,4.0])", new []{4.0,3.0})]
        [TestCase("y = [1.0,4.0,2.0,3.0,4.0] .intersect([3.0,4.0])", new []{4.0,3.0})]
        [TestCase("y = []. unite([])", new object[0])]

        [TestCase("y = [1.0,2.0].unique([3.0,4.0])", new []{1.0,2.0,3.0,4.0})]
        [TestCase("y = [1.0,2.0,3.0].unique([3.0,4.0])", new []{1.0,2.0,4.0})]
        [TestCase("y = [3.0,4.0].unique([3.0,4.0])", new double[0])]
        [TestCase("y = [].unique([])", new object[0])]

        [TestCase("y = []. except([])", new object[0])]
        [TestCase("y = [1.0,2.0] . except([3.0,4.0])", new []{1.0,2.0})]
        [TestCase("y = [1.0,2.0,3.0].except([3.0,4.0])", new []{1.0,2.0})]
        
        [TestCase("y = find([1,2,3], 2)", 1)]
        [TestCase("y = find([1,2,3], 4)", -1)]
        [TestCase("y = find([1,2,-4], -4)", 2)]
        [TestCase("y = find([[1,2],[3,4],[5,6]], [3,4])", 1)]
        [TestCase("y = find([[1,2],[3,4],[5,6]], [3,5])", -1)]
        [TestCase("y = find(['la','LALA','pipi'], 'pipi')", 2)]
        [TestCase("y = find(['la','LALA','pipi'], 'pIpi')", -1)]
        
        [TestCase("y = [[1],[2,3],[4,5,6]].flat()", new []{1,2,3,4,5,6})]
        [TestCase("y = [[1]][1:1].flat()", new int[0])]
        [TestCase("y = [[1][1:1]].flat()", new int[0])]
        [TestCase("y = flat([['1'],['2','3'],['4','5','6']])", new []{"1","2","3","4","5","6"})]
        [TestCase("y = flat([['1']][1:1])", new string[0])]
        [TestCase("y = flat([['1'][1:1]])", new string[0])]

        [TestCase("y = [0..100].chunk(10)[0] == [0..9]",true)]
        [TestCase("y = [0..100].chunk(10)[1] == [10..19]",true)]
        [TestCase("y = [0..100].chunk(10)[9] == [90..99]",true)]
        [TestCase("y = [0..100].chunk(10)[10] == [100]",true)]
        [TestCase("y = [0..100].chunk(10)[0] == [0..2]",false)]
        [TestCase("y = [0..100].chunk(10).flat() == [0..100]",true)]
        [TestCase("y = [0..100].chunk(7).flat() == [0..100]",true)]
        [TestCase("y = [0..100].chunk(1).flat() == [0..100]",true)]
        [TestCase("y = [0..1].chunk(7).flat() == [0,1]",true)]
        [TestCase("y = [0..1].chunk(7) == [[0,1]]",true)]
        [TestCase("y = [0..6].chunk(2) == [[0,1],[2,3],[4,5],[6]]",true)]
        [TestCase("y = [3..7].chunk(1) == [[3],[4],[5],[6],[7]]",true)]
        public void ConstantEquationWithGenericPredefinedFunction(string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate() 
                .AssertReturns(0.00001, Var.New("y", expected));
        }
        
        [TestCase("y = abs(x)",1.0,1.0)]

        [TestCase("y = abs(-x)",-1.0,1.0)]
        [TestCase("y = sum(x,2)",1,3)]
        [TestCase("y = sum(1,x)",2,3)]
        [TestCase("y = sum(sum(x,x),sum(x,x))",1.0,4.0)]
        [TestCase("y = abs(x-4.0)",1.0,3.0)]
        [TestCase("x:int; y = abs(toInt(x)-toInt(4))",1,3)]
        [TestCase("y = abs(x-4)",1.0,3.0)]
        //Check, when (and if) TI will be rewritten
        //[TestCase("y = abs(toInt(x)-toInt(4))",1,3)]
        //[TestCase("y = abs(x-toInt(4))",1,3)]
        public void EquationWithPredefinedFunction(string expr, object arg, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate(Var.New("x", arg))
                .AssertReturns(0.00001, Var.New("y", expected));
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
        [TestCase("y= max([])")]
        [TestCase("y= max(['a','b'])")]
        [TestCase("y= max('a','b')")]
        [TestCase("y= max(1,2,3)")]
        [TestCase("y= max(1,true)")]
        [TestCase("y= max(1,(j)->j)")]
        public void ObviouslyFails(string expr) =>
            Assert.Throws<FunParseException>(
                ()=> FunBuilder.BuildDefault(expr));

        [TestCase("y = [1..100].chunk(-1)")]
        [TestCase("y = [1..100].chunk(0)")]
        [TestCase(@"iSum(r:int, x:int):int = r+x
                     y = reduce([100][1:1], iSum)")]
        public void FailsOnRuntime(string expr)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            Assert.Throws<FunRuntimeException>(
                () => runtime.Calculate());
            byte a = 1;
        }
    }
}