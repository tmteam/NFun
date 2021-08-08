using System;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Operators
{
    public class ArithmeticalOperatorsTest
    {
        [TestCase("y = 2*3", 6)]
        [TestCase("y = -2*-4", 8)]
        [TestCase("y = -2.5*4", -10.0)]
        [TestCase("y = 1.5*-3", -4.5)]
        
        [TestCase("y:real = 2*3", 6.0)]
        [TestCase("y:real = -2*-4", 8.0)]
        [TestCase("y:real = -2.5*4", -10.0)]
        [TestCase("y:real = 1.5*-3", -4.5)]
        [TestCase("y:real = 1.5*0", 0.0)]
        [TestCase("y:real = -1.5*0", 0.0)]
        [TestCase("y:real = 0*1.5", 0.0)]
        [TestCase("y:real = 0*-1.5", 0.0)]
        
        [TestCase("y:int64 = 2*3",    (Int64) 6)]
        [TestCase("y:int64 = -2*-4",  (Int64) 8)]
        [TestCase("y:int64 = -2*5",  (Int64) (-10))]
        [TestCase("y:int64 = 2*-6",  (Int64)(-12))]
        [TestCase("y:int64 = 100*0",  (Int64)(0))]
        [TestCase("y:int64 = 0*100",  (Int64)(0))]
        [TestCase("y:int64 = -100*0",  (Int64)(0))]
        [TestCase("y:int64 = 0*-100",  (Int64)(0))]

        [TestCase("y:int32 = 2*3",     (Int32) 6)]
        [TestCase("y:int32 = -2*-4",   (Int32) 8)]
        [TestCase("y:int32 = -2*5",    (Int32) (-10))]
        [TestCase("y:int32 = 2*-6",    (Int32)(-12))]
        [TestCase("y:int32 = -100*0",  (Int32)(0))]
        [TestCase("y:int32 = 0*-100",  (Int32)(0))]

        [TestCase("y:uint64 = 1*1",     (UInt64) 1)]
        [TestCase("y:uint64 = 2*1",     (UInt64) 2)]
        [TestCase("y:uint64 = 2*3",     (UInt64) 6)]
        [TestCase("y:uint64 = 100*0",   (UInt64)(0))]
        [TestCase("y:uint64 = 0*100",   (UInt64)(0))]
        [TestCase("y:uint64 = 0*0",     (UInt64)(0))]

        [TestCase("y:uint32 = 1*1",     (UInt32) 1)]
        [TestCase("y:uint32 = 2*1",     (UInt32) 2)]
        [TestCase("y:uint32 = 2*3",     (UInt32) 6)]
        [TestCase("y:uint32 = 100*0",   (UInt32)(0))]
        [TestCase("y:uint32 = 0*100",   (UInt32)(0))]
        [TestCase("y:uint32 = 0*0",     (UInt32)(0))]
        public void ConstantMultiply(string expression, object expected) 
            => expression.AssertReturns("y",expected);

        [TestCase("y = x*3.0", 2.5, 7.5)]
        [TestCase("y = -2.0*x",1.0, -2.0)]
        [TestCase("y = x*-2",0, 0)]
        [TestCase("y = x*-2.0",1.5, -3.0)]

        [TestCase("y:int64  = 2*x",  (Int64) 3,  (Int64) 6)]
        [TestCase("y:int32  = x*2",  (Int32) 1,  (Int32) 2)]
        [TestCase("y:uint64 = x*3",  (UInt64) 4,  (UInt64) 12)]
        [TestCase("y:uint32 = x*3",  (UInt32) 4,  (UInt32) 12)]
        public void VarMultiply(string expression,object input,  object expected) 
            => expression.Calc("x",input).AssertResultHas("y",expected);

        [TestCase("y = 2+3", 5)]
        [TestCase("y = -2+-4", -6)]
        [TestCase("y = -2.5+4", 1.5)]
        [TestCase("y = 1.5+-3", -1.5)]
        
        [TestCase("y:real = 2+3", 5.0)]
        [TestCase("y:real = -2+-4", -6.0)]
        [TestCase("y:real = -2.5+4", 1.5)]
        [TestCase("y:real = 1.5+-3", -1.5)]
        [TestCase("y:real = 1.5+0", 1.5)]
        [TestCase("y:real = -1.5+0", -1.5)]
        [TestCase("y:real = 0+1.5", 1.5)]
        [TestCase("y:real = 0+-1.5", -1.5)]
        
        [TestCase("y:int64 = 2+3",    (Int64) 5)]
        [TestCase("y:int64 = -2+-4",  (Int64) (-6))]
        [TestCase("y:int64 = -2+5",  (Int64) (3))]
        [TestCase("y:int64 = 2+-6",  (Int64)(-4))]
        [TestCase("y:int64 = 100+0",  (Int64)(100))]
        [TestCase("y:int64 = 0+100",  (Int64)(100))]
        [TestCase("y:int64 = -100+0",  (Int64)(-100))]
        [TestCase("y:int64 = 0+-100",  (Int64)(-100))]

        [TestCase("y:int32 = 2+3",     (Int32) (5))]
        [TestCase("y:int32 = -2+-4",   (Int32) (-6))]
        [TestCase("y:int32 = -2+5",    (Int32) (3))]
        [TestCase("y:int32 = 2+-6",    (Int32)(-4))]
        [TestCase("y:int32 = -100+0",  (Int32)(-100))]
        [TestCase("y:int32 = 0+-100",  (Int32)(-100))]

        [TestCase("y:uint64 = 2+1",     (UInt64) 3)]
        [TestCase("y:uint64 = 2+3",     (UInt64) 5)]
        [TestCase("y:uint64 = 100+0",   (UInt64)(100))]
        [TestCase("y:uint64 = 0+100",   (UInt64)(100))]
        [TestCase("y:uint64 = 0+0",     (UInt64)(0))]
        
        [TestCase("y:uint32 = 2+1",     (UInt32) 3)]
        [TestCase("y:uint32 = 2+3",     (UInt32) 5)]
        [TestCase("y:uint32 = 100+0",   (UInt32)(100))]
        [TestCase("y:uint32 = 0+100",   (UInt32)(100))]
        [TestCase("y:uint32 = 0+0",     (UInt32)(0))]
        [TestCase("y:int32 = 2+(-2147483647)",              -2147483645)]
        [TestCase("y:int64 = 2+(-9223372036854775808)",     -9223372036854775806)]
        public void ConstantAddition(string expression, object expected)
            => expression.AssertReturns("y",expected);

        [TestCase("y = x+3", 2, 5)]
        [TestCase("y = -2+x",1, -1)]
        [TestCase("y = x+-2",0, -2)]
        [TestCase("y = x+-2",1, -1)]
        [TestCase("y:int64  = 2+x",  (Int64) 3,  (Int64) 5)]
        [TestCase("y:int32  = x+2",  (Int32) 1,  (Int32) 3)]
        [TestCase("y:uint64 = x+3",  (UInt64) 4,  (UInt64) 7)]
        [TestCase("y:uint32 = x+3",  (UInt32) 4,  (UInt32) 7)]
        public void VarAddition(string expression,object input,  object expected) 
            => expression.Calc("x",input).AssertResultHas("y",expected);

        
        [TestCase("y = 2-3", -1)]
        [TestCase("y = -2.5-4", -6.5)]
        
        [TestCase("y:real = 2-3", -1.0)]
        [TestCase("y:real = -2.5-4", -6.5)]
        [TestCase("y:real = 1.5-0", 1.5)]
        [TestCase("y:real = -1.5-0", -1.5)]
        [TestCase("y:real = 0-1.5", -1.5)]
        
        [TestCase("y:int64 = 2-3",    (Int64) (-1))]
        [TestCase("y:int64 = -2-5",  (Int64) (-7))]
        [TestCase("y:int64 = 100-0",  (Int64)(100))]
        [TestCase("y:int64 = 0-100",  (Int64)(-100))]
        [TestCase("y:int64 = -100-0",  (Int64)(-100))]

        [TestCase("y:int32 = 2-3",     (Int32) (-1))]
        [TestCase("y:int32 = -2-5",    (Int32) (-7))]
        [TestCase("y:int32 = -100-0",  (Int32)(-100))]
       
        [TestCase("y:uint64 = 2-1",     (UInt64) 1)]
        [TestCase("y:uint64 = 100-0",   (UInt64)(100))]
        [TestCase("y:uint64 = 0-0",     (UInt64)(0))]
        
        [TestCase("y:uint32 = 2-1",     (UInt32) 1)]
        [TestCase("y:uint32 = 100-0",   (UInt32)(100))]
        [TestCase("y:uint32 = 0-0",     (UInt32)(0))]

        [TestCase("y:int32 = 2-2147483646",     (Int32)(-2147483644))]
        [TestCase("y:int64 = 2-9223372036854775807",     (Int64)(-9223372036854775805))]

        [TestCase("y:int64 = 9223372036854775807-9223372036854775807",     (Int64)(0))]
        public void ConstantSubstraction(string expression, object expected)
            => expression.AssertReturns("y",expected);

        [TestCase("y = x-3.0", 2.5, -0.5)]
        [TestCase("y = -2.0-x",1.0, -3.0)]
        [TestCase("y = x-2.0",0.0, -2.0)]
        [TestCase("y:real = x-3", 2.5, -0.5)]
        [TestCase("y:real = -2-x",1.0, -3.0)]
        [TestCase("y:real = x-2",0.0, -2.0)]

        [TestCase("y:int64  = 2-x",  (Int64) 3,  (Int64) (-1))]
        [TestCase("y:int32  = x-2",  (Int32) 1,  (Int32) (-1))]
        [TestCase("y:uint64 = x-3",  (UInt64) 4,  (UInt64) 1)]
        [TestCase("y:uint32 = x-3",  (UInt32) 4,  (UInt32) 1)]
        public void VarSubstract(string expression,object input,  object expected) 
            => expression.Calc("x",input).AssertResultHas("y",expected);
        
        [TestCase("y = 4/2",2.0)]
        [TestCase("y = 2/4",0.5)]
        [TestCase("y = -2/4",-0.5)]
        [TestCase("y = -2/-4",0.5)]
        [TestCase("y = 2/-4",-0.5)]
        [TestCase("y = 0/4",0.0)]
        [TestCase("y = 3/1.5",2.0)]
        [TestCase("y = 4.5/1.5",3.0)]
        public void ConstantDivision(string expression, object expected)
            => expression.AssertReturns("y",expected);
        
        [TestCase("y = x/3", 1.5, 0.5)]
        [TestCase("y = x/3",   -3.0, -1.0)]
        [TestCase("y = x/-3",  -3.0,  1.0)]
        [TestCase("y = -x/-3", -3.0, -1.0)]
        [TestCase("y = -2/x",1.0, -2.0)]
        [TestCase("y = -2/x",-2.0, 1.0)]
        [TestCase("y = -2/-x",-2.0, -1.0)]
        public void VarDivision(string expression,object input,  object expected) 
            => expression.Calc("x",input).AssertResultHas("y",expected);
        
        [TestCase("y = 4**2",16.0)]
        [TestCase("y = 2**4",16.0)]
        [TestCase("y = 0**4",0.0)]
        [TestCase("y = 0**0",1.0)]
        [TestCase("y = 2**0",1.0)]
        [TestCase("y = 0.1**0",1.0)]
        [TestCase("y = 1.5**2",2.25)]
        [TestCase("y = 4.5**1.5",9.5459415460183923)]
        public void ConstantPow(string expression, object expected)
            => expression.AssertReturns("y",expected);
        
        [TestCase("y = -2**x",2.0, 4.0)]
        [TestCase("y = -2**-x",1.0, -0.5)]
        [TestCase("y = x**2",1.0, 1.0)]
        [TestCase("y = x**2",0.0, 0.0)]
        [TestCase("y = x**2",2.0, 4.0)]
        [TestCase("y = x**-1",1.0, 1.0)]
        [TestCase("y = x**x",0.0, 1.0)]
        public void VarPow(string expression,object input,  object expected) 
            => expression.Calc("x",input).AssertResultHas("y",expected);
        
        [TestCase("y = -1",-1)]
        [TestCase("y:real  = -(1)",(double)(-1.0))]
        [TestCase("y:int64 = -(1)",(Int64)(-1.0))]
        [TestCase("y:int32 = -(1)",(Int32)(-1.0))]
        [TestCase("y:int16 = -(1)",(Int16)(-1.0))]
        [TestCase("y = -0x1 ",-1)]
        [TestCase("y = -(-1)", 1)]
        [TestCase("y = -(-(-1))",-1)]
    
        public void ConstantNegate(string expression, object expected)
            => expression.AssertReturns("y",expected);

        [TestCase("y = 1 + 4/2 + 3 +2*3 -1", 11.0)]
        [TestCase("y = 1 + (1 + 4)/2 - (3 +2)*(3 -1)",-6.5)]
        [TestCase("y = -(1+2)",-3)]
        [TestCase("y = -2*(-4+2)", 4)]
        public void ConstantExpression(string expression, object expected)
            => expression.AssertReturns("y",expected);

        [TestCase("y = x%3", 2,2)]
        [TestCase("y = x%4", 5,1)]
        [TestCase("y = x%-4", 5,1)]
        [TestCase("y = x%4", -5,-1)]
        [TestCase("y = x%-4", -5,-1)]
        [TestCase("y = x%4", -5,-1)]
        [TestCase("y = -(-(-x))",2.0,-2.0)]
        public void SingleIntVariableEquation(string expr, object arg, object expected) => 
            expr.Calc("x",arg).AssertReturns("y", expected);
        
        
        [TestCase("y = (x + 4.0/x)",2,4)]
        [TestCase("y = x % 3.0", 2,2)]
        [TestCase("y = x % 4.0", 5,1)]
        [TestCase("y = x % -4.0", 5,1)]
        [TestCase("y = x % 4.0", -5,-1)]
        [TestCase("y = x % -4.0", -5,-1)]
        [TestCase("y = x % 4.0", -5,-1)]
        [TestCase("y = x % 2.0", -5.2,-1.2)]
        [TestCase("y = 5.0 % x", 2.2,0.6)]
        [TestCase("y:real = -x ",0.3,-0.3)]
        [TestCase("y = -(-(-1.0*x))",2,-2)]
        public void SingleRealVariableEquation(string expr, double arg, double expected) => 
            expr.Calc("x",arg).AssertReturns("y", expected);

        [TestCase("y = x1+x2",2.0,3.0,5.0)]
        [TestCase("y = 2*x1*x2",3,6, 36)]
        [TestCase("y = x1*4/x2",2.0,2.0,4.0)]
        [TestCase("y = (x1+x2)/4",2.0,2.0,1.0)]
        public void TwoVariablesEquation(string expr, object arg1, object arg2, object expected) => 
            expr.Calc(("x1",arg1),("x2",arg2)).AssertResultHas("y",expected);

        [Ignore("TODO Arithmetical oops")]
        [TestCase("y:uint64 = 2-3")]
        [TestCase("y:uint64 = 0-100")]
        [TestCase("y:uint32 = 2-3")]
        [TestCase("y = 2/0")]
        [TestCase("y = 0/0")]
        [TestCase("y:uint32 = 10+-30")]
        [TestCase("y:uint32 = 0-100")]
        public void Oops(string expression) => Assert.Throws<FunnyRuntimeException>(() => expression.Calc());

        [TestCase("y = /2")]
        [TestCase("y = )*2")]
        [TestCase("y = (*2")]
        [TestCase("y = *2")]
        [TestCase("y = 2++")]
        [TestCase("y = ++2")]
        [TestCase("y = 2--")]
        [TestCase("y = --2")]
        [TestCase("y = 2a")]
        [TestCase("y = 2+ 3 + 4 +")]
        [TestCase("y = x*((2)")]
        [TestCase("y = 2*x)")]
        [TestCase("y = 2++x")]
        [TestCase("y = x++2")]
        [TestCase("y = 2--x")]
        [TestCase("y = x--2")]
        [TestCase("y = *2a")]
        [TestCase("y = x+2+ 3 + 4 +")]
        [TestCase("y=0.*1")]
        [TestCase("x*2 \rx*3")]
        [TestCase("y:real = -2--4")]
        [TestCase("y:real = 1.5--3")]
        [TestCase("y:real = 0--1.5")]
        [TestCase("y:int64 = -2--4")]
        [TestCase("y:int64 = 2--6")]
        [TestCase("y:int32 = -2--4")]
        [TestCase("y:int32 = 2--6")]
        [TestCase("y:int32 = 0--100")]
        [TestCase("y:int64 = 0--100")]
        [TestCase("y = ()")]
        [TestCase("y = )")]
        [TestCase("y = )2")]
        [TestCase("y = (")]
        [TestCase("y = (2")]
        [TestCase("y = ((2)")]
        [TestCase("y = 2)")]
        [TestCase("y = 2%%")]
        [TestCase("y = %%2")]
        [TestCase("y = =a")]
        [TestCase("y = x()")]
        [TestCase("y = =a")]
        [TestCase("y = \"")]
        [TestCase("y = (")]
        [TestCase("y = 0x")]
        [TestCase("y = 0..2")]
        [TestCase("1 2")]
        [TestCase("1 \r2")]
        [TestCase("=x*2")]
        [TestCase("y = y")]
        [TestCase("y = y+x")]
        [TestCase("a: int a=4")]
        public void ObviouslyFails(string expr) => expr.AssertObviousFailsOnParse();
    }
}