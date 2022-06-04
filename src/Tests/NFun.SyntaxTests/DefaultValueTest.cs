using System;
using System.Collections.Generic;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests; 

public class DefaultValueTest {
    [TestCase("y:bool = default", default(bool))]
    [TestCase("y:byte = default", default(byte))]
    [TestCase("y:uint16 = default", default(UInt16))]
    [TestCase("y:uint32 = default", default(UInt32))]
    [TestCase("y:uint64 = default", default(UInt64))]
    [TestCase("y:int16 = default", default(Int16))]
    [TestCase("y:int32 = default", default(Int32))]
    [TestCase("y:int64 = default", default(Int64))]
    [TestCase("y:real = default", default(double))]
    [TestCase("y:text = default", "")]

    [TestCase("d():bool   = default; y:bool = d()", default(bool))]
    [TestCase("d():byte   = default; y:byte = d()", default(byte))]
    [TestCase("d():uint16 = default; y:uint16 = d()", default(UInt16))]
    [TestCase("d():uint32 = default; y:uint32 = d()", default(UInt32))]
    [TestCase("d():uint64 = default; y:uint64 = d()", default(UInt64))]
    [TestCase("d():int16  = default; y:int16 = d()", default(Int16))]
    [TestCase("d():int32  = default; y:int32 = d()", default(Int32))]
    [TestCase("d():int64  = default; y:int64 = d()", default(Int64))]
    [TestCase("d():real   = default; y:real = d()", default(double))]
    [TestCase("d():text   = default; y:text = d()", "")]

    [TestCase("d() = default; y:bool = d()", default(bool))]
    [TestCase("d() = default; y:byte = d()", default(byte))]
    [TestCase("d() = default; y:uint16 = d()", default(UInt16))]
    [TestCase("d() = default; y:uint32 = d()", default(UInt32))]
    [TestCase("d() = default; y:uint64 = d()", default(UInt64))]
    [TestCase("d() = default; y:int16 = d()", default(Int16))]
    [TestCase("d() = default; y:int32 = d()", default(Int32))]
    [TestCase("d() = default; y:int64 = d()", default(Int64))]
    [TestCase("d() = default; y:real = d()", default(double))]
    [TestCase("d() = default; y:text = d()", "")]
    
    [TestCase("d(a,b) = default(a,b); y:bool   = d(1,'a')", default(bool))]
    [TestCase("d(a,b) = default(a,b); y:byte   = d(1,'a')", default(byte))]
    [TestCase("d(a,b) = default(a,b); y:uint16 = d(1,'a')", default(UInt16))]
    [TestCase("d(a,b) = default(a,b); y:uint32 = d(1,'a')", default(UInt32))]
    [TestCase("d(a,b) = default(a,b); y:uint64 = d(1,'a')", default(UInt64))]
    [TestCase("d(a,b) = default(a,b); y:int16  = d(1,'a')", default(Int16))]
    [TestCase("d(a,b) = default(a,b); y:int32  = d(1,'a')", default(Int32))]
    [TestCase("d(a,b) = default(a,b); y:int64  = d(1,'a')", default(Int64))]
    [TestCase("d(a,b) = default(a,b); y:real   = d(1,'a')", default(double))]
    [TestCase("d(a,b) = default(a,b); y:text   = d(1,'a')", "")]
    
    [TestCase("d()  = if(true) default else rule(x,y) = x+y; y = d()(1,2)", default(int))]
    [TestCase("d(a) = if(true) default else rule(x,y) = a; y = d('test')(1,2)", "")]
    
    [TestCase("d() = if(false) default+1 else default; y:uint32 = d()", default(UInt32))]
    [TestCase("d() = if(false) 1+default else default; y:uint64 = d()", default(UInt64))]
    
    [TestCase("d() = if(true) default+1 else default; y:uint32 = d()", (UInt32)1)]
    [TestCase("d() = if(true) 1+default else default; y:uint64 = d()", (UInt64)1)]
    
    [TestCase("d() = if(true) -default else default; y:int32 = d()", default(Int32))]
    [TestCase("d() = if(true) -default else default; y:int64 = d()", default(Int64))]
    [TestCase("d() = if(true) -default else default; y:real = d()", default(double))]

    [TestCase("d(a,b) = if(a>b) a else default; y:uint32 = d(0,42)", default(UInt32))]
    [TestCase("d(a,b) = if(a>b) a else default; y:uint64 = d(0,42)", default(UInt64))]
    [TestCase("d(a,b) = if(a>b) a else default; y:int32  = d(0,42)", default(Int32))]
    [TestCase("d(a,b) = if(a>b) a else default; y:int64  = d(0,42)", default(Int64))]
    [TestCase("d(a,b) = if(a>b) a else default; y:real   = d(0,42)", default(double))]
    [TestCase("d(a,b) = if(a>b) a else default; y:text   = d('ab','cd')", "")]

    [TestCase("d(a,b) = if(a>b) a else default*1; y:uint32 = d(0,42)", default(UInt32))]
    [TestCase("d(a,b) = if(a>b) a else default*1; y:uint64 = d(0,42)", default(UInt64))]
    [TestCase("d(a,b) = if(a>b) a else default*1; y:int32  = d(0,42)", default(Int32))]
    [TestCase("d(a,b) = if(a>b) a else default*1; y:int64  = d(0,42)", default(Int64))]
    [TestCase("d(a,b) = if(a>b) a else default*1; y:real   = d(0,42)", default(double))]

    [TestCase("d(a,b) = if(a>b) a else default; y:uint32 = d(42,0)", (UInt32)42)]
    [TestCase("d(a,b) = if(a>b) a else default; y:uint64 = d(42,0)", (UInt64)42)]
    [TestCase("d(a,b) = if(a>b) a else default; y:int32  = d(42,0)", (Int32)42)]
    [TestCase("d(a,b) = if(a>b) a else default; y:int64  = d(42,0)", (Int64)42)]
    [TestCase("d(a,b) = if(a>b) a else default; y:real   = d(42,0)", (double)42)]
    [TestCase("d(a,b) = if(a>b) a else default; y:text   = d('cd','a')", "cd")]
    [TestCase("y = default+1", 1)]
    [TestCase("y:text = default.concat('test')", "test")]
    [TestCase("y:text = default.reverse()", "")]
    [TestCase("y = default.count()", 0)]
    public void ConstantCalc(string expression, object expected) =>
        expression.AssertReturns("y", expected);

    [Test]
    public void DefaultOfAnyConstantTest() =>
        Assert.IsInstanceOf<object>("default".Calc().Get("out"));

    [Test]
    public void ArrayOfIntConstantTest() =>
        "if(true) default else [1,2,3]".AssertReturns(Array.Empty<Int32>());

    [Test]
    public void ArrayOfArrayOfRealConstantTest() =>
        "if(true) default else [[1.0,2,3]]".AssertReturns(Array.Empty<double[]>());

    [Test]
    public void ArrayOfArrayOfBoolConstantTest() =>
        "if(true) default else [[[true]]]".AssertReturns(Array.Empty<bool[][]>());

    [Test]
    public void ArrayOfArrayOfTextConstantTest() =>
        "if(true) default else [[['foo']]]".AssertReturns(Array.Empty<string[][]>());
    
    [Test]
    public void StructDefConstantTest() =>
        "if(true) default else {name = 'a', age = 42, car = {name = 'tesla'}, ids = [1,2,3]}"
            .AssertReturns(new {
                name =  "",
                age =  0,
                car = new { name = "" },
                ids = Array.Empty<int>()
            });
    
    [Test]
    public void ArrayOfStructsDefConstantTest() =>
        "if(true) default else [{name = 'a', age = 42, car = {name = 'tesla'}, ids = [1,2,3]}]"
            .AssertReturns(Array.Empty<Dictionary<string, object>>());
    
    [TestCase("default[0]")]
    public void ObviousFailsOnRuntime(string expression) => expression.AssertObviousFailsOnRuntime();
    
    [TestCase("vasa.default")]
    [TestCase("(default+default)[0]")]
    public void ObviousFailsOnParse(string expression) => expression.AssertObviousFailsOnParse();
}