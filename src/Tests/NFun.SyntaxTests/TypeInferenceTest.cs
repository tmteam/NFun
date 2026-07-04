using System;
using System.Linq;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Full-pipeline type inference tests: parse → TIC → resolve → runtime/build.
/// Verifies that the OUTPUT FunnyType of each variable matches expectation across
/// a broad sample of expressions (literals, arithmetic, operators, generics,
/// typed annotations, etc.). Distinct from TIC unit tests — these go through the
/// entire compilation pipeline. There is some overlap with Operators/, Constants,
/// and ImplicitCast tests; targeted dedupe is left as ongoing cleanup work.
/// </summary>
public class TypeInferenceTest {

    [TestCase("y = 0x2", BaseFunnyType.Int32)]
    [TestCase("y = 0x2*3", BaseFunnyType.Int32)]
    [TestCase("y = 2**3", BaseFunnyType.Int32)]
    [TestCase("y = 0x2 % 3", BaseFunnyType.Int32)]
    [TestCase("y = 4/3", BaseFunnyType.Real)]
    [TestCase("y = 0x4- 3", BaseFunnyType.Int32)]
    [TestCase("y = 0x4+ 3", BaseFunnyType.Int32)]
    [TestCase("z = 1 + 4/3 + 3 +2*3 -1", BaseFunnyType.Real)]
    [TestCase("y = -0x2*-4", BaseFunnyType.Int32)]
    [TestCase("y = -2*(-4+0x2)", BaseFunnyType.Int32)]
    [TestCase("y = -(-(-1.0))", BaseFunnyType.Real)]
    [TestCase("y = 0.2", BaseFunnyType.Real)]
    [TestCase("y = 1.1_11  ", BaseFunnyType.Real)]
    [TestCase("y = 4*0.2", BaseFunnyType.Real)]
    [TestCase("y = 4/0.2", BaseFunnyType.Real)]
    [TestCase("y = 4/0.2", BaseFunnyType.Real)]
    [TestCase("y = 2**0.3", BaseFunnyType.Real)]
    [TestCase("y = 0.2**2", BaseFunnyType.Real)]
    [TestCase("y = 0.2 % 2", BaseFunnyType.Real)]
    [TestCase("y = 3 % 0.2", BaseFunnyType.Real)]
    [TestCase("y = 0xfF  ", BaseFunnyType.Int32)]
    [TestCase("y = 0x00_Ff  ", BaseFunnyType.Int32)]
    [TestCase("y = 0b001  ", BaseFunnyType.Int32)]
    [TestCase("y = 0b11  ", BaseFunnyType.Int32)]
    [TestCase("y = 0x_1", BaseFunnyType.Int32)]
    [TestCase("y = 1==1", BaseFunnyType.Bool)]
    [TestCase("y = 1==0", BaseFunnyType.Bool)]
    [TestCase("y = true==true", BaseFunnyType.Bool)]
    [TestCase("y = 1!=0", BaseFunnyType.Bool)]
    [TestCase("y = 0!=1", BaseFunnyType.Bool)]
    [TestCase("y = 5!=5", BaseFunnyType.Bool)]
    [TestCase("y = 5>3", BaseFunnyType.Bool)]
    [TestCase("y = 5>6", BaseFunnyType.Bool)]
    [TestCase("y = 5>=3", BaseFunnyType.Bool)]
    [TestCase("y = 5>=6", BaseFunnyType.Bool)]
    [TestCase("y = 5<=5", BaseFunnyType.Bool)]
    [TestCase("y = 5<=3", BaseFunnyType.Bool)]
    [TestCase("y = true and true", BaseFunnyType.Bool)]
    [TestCase("y = true or true", BaseFunnyType.Bool)]
    [TestCase("y = true xor true", BaseFunnyType.Bool)]
    [TestCase("y = 1<<2", BaseFunnyType.Int32)]
    [TestCase("y = 8>>2", BaseFunnyType.Int32)]
    [TestCase("y = 3|2", BaseFunnyType.Int32)]
    [TestCase("y = 3^2", BaseFunnyType.Int32)]
    [TestCase("y = 4&2", BaseFunnyType.Int32)]
    [TestCase(
        @"fibrec(n, iter, p1,p2) =
                          if (n >iter)
                                fibrec(n, iter+1, p1+p2, p1)
                          else
                                p1+p2
          fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)

          y:int = fib(1)", BaseFunnyType.Int32)]
    [TestCase(
        @"fibrec(n:int, iter, p1,p2) =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2

                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y:int = fib(1)", BaseFunnyType.Int32)]
    [TestCase(
        @"fibrec(n, iter, p1,p2):int =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2

                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(1)", BaseFunnyType.Int32)]
    [TestCase(
        @"y = [1..7]
                        .map(rule it+1)
                        .sum()", BaseFunnyType.Int32)]
    [TestCase(
        @"y = [1..8]
                        .map(rule [it,1].sum())
                        .sum()", BaseFunnyType.Int32)]
    [TestCase(
        @"y = [1..9]
                        .map(rule [1,it].sum())
                        .sum()", BaseFunnyType.Int32)]
    [TestCase(
        @"y = [1..10]
                        .map(rule [1..it].sum())
                        .sum()", BaseFunnyType.Int32)]
    [TestCase(
        @"y = [1..11]
                        .map(rule [1..it].sum())
                        .sum()", BaseFunnyType.Int32)]
    [TestCase(
        @"y = [1..12]
                        .map(rule [1..it]
                                .map(rule 2600/it)
                                .sum())
                        .sum()", BaseFunnyType.Real)]
    [TestCase(
        @"y = [1..13]
                        .map(rule [1..10]
                                .map(rule 2600/it)
                                .sum())
                        .sum()", BaseFunnyType.Real)]
    [TestCase(
        @"y = [1..14]
                        .map(rule it/2)
                        .sum()", BaseFunnyType.Real)]
    [TestCase(
        @"div10(x) = 2600/x
            y = [1..20].map(div10).sum()", BaseFunnyType.Real)]
    [TestCase(
        @"div11(x) = 2600/x
            supsum(n) = [1..n].map(div11).sum()
            y = [1..20].map(supsum).sum()", BaseFunnyType.Real)]
    //round() function not available
    //[TestCase(
    //    @"div12(x) = 2600/x
    //        supsum(n) = [1..n].map(div12).sum()
    //        y = [1..20].map(supsum).sum().round()", BaseFunnyType.Int32)]
    public void SingleEquation_Runtime_OutputTypeCalculatesCorrect(string expr, BaseFunnyType type) {
        var clrtype = FunnyConverter.RealIsDouble.GetOutputConverterFor(FunnyType.PrimitiveOf(type)).ClrType;

        expr.Calc().AssertResultIs(clrtype);
    }

    [TestCase(
        @"someRec(n, iter, p1,p2) =
                          if (n >iter)
                                someRec(n, iter+1, p1+p2, p1)
                          else
                                p1+p2
          y = someRec(9,2,1,1)", BaseFunnyType.Int32)]
    [TestCase(
        @"someRec2(n, iter) =
                          if (n >iter)
                                someRec2(n, iter+1)
                          else
                                1
          y:int = someRec2(0x9,0x2)", BaseFunnyType.Int32)]
    [TestCase("(if(true) [1,2] else [])[0]", BaseFunnyType.Int32)]
    [TestCase("(if(false) [] else [1,2])[0]", BaseFunnyType.Int32)]
    public void SingleEquations_Parsing_OutputTypesCalculateCorrect(string expr, BaseFunnyType type) =>
        Assert.AreEqual(type, expr.Build().Variables.Single(v => v.IsOutput).Type.BaseType);

    [TestCase("f(n, iter)  = f(n, iter+1).concat((n >iter).toText())")]
    [TestCase("f1(n, iter) = f1(n+1, iter).concat((n >iter).toText())")]
    [TestCase("f2(n, iter) = n > iter and f2(n,iter)")]
    [TestCase("f3(n, iter) = n > iter and f3(n,iter+1)")]
    [TestCase("f4(n, iter) = f4(n,iter) and (n > iter)")]
    [TestCase("f5(n) = f5(n) and n>0")]
    [TestCase("f6(n) = n>0 and f6(n)")]
    [TestCase("f7(n) = n>0 and true")]
    [TestCase("f8(n) = n==0 and f8(n)")]
    [TestCase("f9(n) = f9(n and true)")]
    [TestCase("fa(n) = fa(n+1)")]
    [TestCase("fb(n) = n.concat('').fb()")]
    [TestCase("[a].map(rule it)")]
    [TestCase("[a].filter(rule it>2)")]
    [TestCase("[a].reverse()")]
    [TestCase("[a]")]
    [TestCase("y = [-x].all(rule it<0.0)")]
    [TestCase("y = [x,x].all(rule it<0.0)")]
    [TestCase("y = [-x,x].all(rule it<0.0 )")]
    [TestCase("y = [1,-x].all( rule it<0.0 )")]
    [TestCase("y = [x,2.0,3.0].all(rule it >1.0)")]
    [TestCase("y = [1..11].map(rule [1..n].sum())")]
    [TestCase("y = [1..12].map(rule [1..n].sum()).sum()")]
    [TestCase("y = [1..11].map(rule [1..i].sum())")]
    [TestCase("y = [1..12].map(rule [1..i].sum()).sum()")]
    [TestCase("dsum7(x) = x+x")]
    [TestCase(
        @"div9(x) = 2600/x
            y = [1..20].map(div9)")]
    [TestCase(
        @"div10(x) = 2600/x
                    y(n) = [1..n].map(div10).sum()")]
    [TestCase(
        @"dsum11(x:int):int = x+x
            y = [1..20].map(dsum11)")]
    [TestCase(
        @"dsum12(x:real):real = x+x
            y = [1..20].map(dsum12)")]
    [TestCase(
        @"div13(x:int):real = 2600/x
                    y(n) = [1..n].map(div13).sum()")]
    [TestCase(
        @"div14(x:real):real = 2600/x
                    y(n) = [1..n].map(div14).sum()")]
    [TestCase(
        @"input:int[]
            dsame15(x:real):real = x
            y = input.map(dsame15)")]
    [TestCase("y = x * -x")]
    [TestCase("y = -x * x")]
    [TestCase("y = [-x,x]")]
    [TestCase("y = [-x]")]
    [TestCase("y1 = -x \r y2 = -x")]
    [TestCase("y1 = x  \r y2 = -x")]
    [TestCase("y = [x,-x].all(rule it<0.0)")]
    [TestCase("y = [-x,-x].all(rule it<0.0)")]
    [TestCase("z = [-x,-x,-x] \r  y = z.all(rule it < 0.0)")]
    [TestCase("y = [x, -x]")]
    [TestCase("y = [-x,-x,-x].all(rule it < 0.0)")]
    [TestCase("[x, -x]")]
    public void EquationTypes_SolvesSomehow(string expr) => Assert.DoesNotThrow(() => expr.Build());

    [TestCase("y:int = 1\rz:int=2", BaseFunnyType.Int32, BaseFunnyType.Int32)]
    [TestCase("y = 2.0\rz:int=2", BaseFunnyType.Real, BaseFunnyType.Int32)]
    [TestCase("y = true\rz=false", BaseFunnyType.Bool, BaseFunnyType.Bool)]
    [TestCase("y:int = 1\rz=y", BaseFunnyType.Int32, BaseFunnyType.Int32)]
    [TestCase("z:int=2 \r y = z", BaseFunnyType.Int32, BaseFunnyType.Int32)]
    [TestCase("z:int=2 \r y = z/2", BaseFunnyType.Real, BaseFunnyType.Int32)]
    [TestCase("y:int = 2\rz=y/2", BaseFunnyType.Int32, BaseFunnyType.Real)]
    [TestCase("y = 2.0\rz=y", BaseFunnyType.Real, BaseFunnyType.Real)]
    [TestCase("z=2.0 \ry = z", BaseFunnyType.Real, BaseFunnyType.Real)]
    [TestCase("y = true\rz=y", BaseFunnyType.Bool, BaseFunnyType.Bool)]
    [TestCase("z=true \r y = z", BaseFunnyType.Bool, BaseFunnyType.Bool)]
    [TestCase("y:int = 2\r z=y>1", BaseFunnyType.Int32, BaseFunnyType.Bool)]
    [TestCase("z:int=2 \r y = z>1", BaseFunnyType.Bool, BaseFunnyType.Int32)]
    [TestCase("y = 2.0\rz=y>1", BaseFunnyType.Real, BaseFunnyType.Bool)]
    [TestCase("z=2.0 \r y = z>1", BaseFunnyType.Bool, BaseFunnyType.Real)]
    public void TwinEquations_Runtime_OutputTypesCalculateCorrect(
        string expr, BaseFunnyType ytype, BaseFunnyType ztype) {
        var res = expr.Calc();
        var y = res.Get("y");
        Assert.AreEqual(y.GetType(),
            FunnyConverter.RealIsDouble.GetOutputConverterFor(FunnyType.PrimitiveOf(ytype)).ClrType);
        var z = res.Get("z");
        Assert.AreEqual(z.GetType(),
            FunnyConverter.RealIsDouble.GetOutputConverterFor(FunnyType.PrimitiveOf(ztype)).ClrType);
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
    [TestCase("x:real \r y = [1..10][x]")]
    [TestCase("x:real \r y = [1..10][:x]")]
    [TestCase("x:real \r y = [1..10][:x:]")]
    [TestCase("x:real \r y = [1..10][::x]")]
    [TestCase("y = x \r x:real ")]
    [TestCase("z:real \r  y = x+z \r x:real ")]
    [TestCase("a:int \r a=4")]
    [TestCase("a:int a=4")]
    [TestCase("a:real =false")]
    [TestCase("a:real =false")]
    [TestCase("x:bool; a:real =x")]
    public void ObviouslyFailsWithParse(string expr) => expr.AssertObviousFailsOnParse();


    [TestCase(new[] { 1, 2 }, "x:int[]\r y= x", new[] { 1, 2 })]
    [TestCase(new[] { 1, 2 }, "x:int[]\r y= x.concat(x)", new[] { 1, 2, 1, 2 })]
    [TestCase(new[] { "1", "2" }, "x:text[]\r y= x", new[] { "1", "2" })]
    [TestCase(new[] { "1", "2" }, "x:text[]\r y= x.concat(x)", new[] { "1", "2", "1", "2" })]
    [TestCase(new[] { 1.0, 2.0 }, "x:real[]\r y= x", new[] { 1.0, 2.0 })]
    [TestCase(new[] { 1.0, 2.0 }, "x:real[]\r y= x.concat(x)", new[] { 1.0, 2.0, 1.0, 2.0 })]
    [TestCase(1.0, "x:real\r y= x+1", 2.0)]
    [TestCase(1, "x:int\r y= x+1", 2)]
    [TestCase(1, "y= x+1", 2)]
    [TestCase(2, "y= x*1", 2)]
    [TestCase(1, "y= x-1", 0)]
    [TestCase(1, "y= 1+x", 2)]
    [TestCase(2, "y= 1*x", 2)]
    [TestCase(1, "y= 1-x", 0)]
    [TestCase(true, "x:bool\r y= x and true", true)]
    public void SingleInputTypedEquation(object x, string expr, object y) =>
        expr.Calc("x", x).AssertReturns(y);

    [Test]
    public void DifferentTypeUsageOfInputVariable() =>
        Funny.Hardcore.Build("i64:int64 = x+1; i32:int32 = x+1")
            .AssertContains("i32", FunnyType.Int32)
            .AssertContains("i64", FunnyType.Int64)
            .AssertContains("x", FunnyType.Int32)
            .Calc("x", 42)
            .AssertResultHas("i64", 43L)
            .AssertResultHas("i32", 43);

    [TestCase("y:int[]= [1,2,3].map(rule it*it)", new[] { 1, 4, 9 })]
    [TestCase("y:int[]= [1,2,3].map(rule it)", new[] { 1, 2, 3 })]
    [TestCase("y:int[]= [1,2,3].map(rule 1)", new[] { 1, 1, 1 })]
    [TestCase("y= [1,2,3].map(rule 'hi')", new[] { "hi", "hi", "hi" })]
    [TestCase("y= [true,true,false].map(rule 'hi')", new[] { "hi", "hi", "hi" })]
    [TestCase("y:int[]= [1,2,3].filter (rule it>2)", new[] { 3 })]
    [TestCase("y:int= [1,2,3].fold(rule it1+it2)", 6)]
    [TestCase("y:int= [1,2,3].fold(rule 1)", 1)]
    [TestCase("y:int= [1,2,3].fold(rule it1)", 1)]
    [TestCase("y:int= [1,2,3].fold(rule it1+1)", 3)]
    public void ConstantTypedEquation(string expr, object y) =>
        expr.Calc().AssertReturns(y);


    [TestCase("y(x) = y(x)")]
    [TestCase("y(x):int = y(x)")]
    [TestCase("y(x:int) = y(x)")]
    [TestCase("y(x:int):int = y(x)")]
    [TestCase("y(x) = y(x)+1")]
    [TestCase("y(x:int) = y(x)+1")]
    [TestCase("y(x) = y(x-1)+y(x-2)")]
    [TestCase("fib(x) = if(x<3) 1 else fib(x-1)+fib(x-2)")]
    public void RecFunction_TypeSolved(string expr) =>
        Assert.DoesNotThrow(() => expr.Build());

    [TestCase("byte", (byte)1, BaseFunnyType.UInt8)]
    [TestCase("uint8", (byte)1, BaseFunnyType.UInt8)]
    [TestCase("uint16", (UInt16)1, BaseFunnyType.UInt16)]
    [TestCase("uint32", (UInt32)1, BaseFunnyType.UInt32)]
    [TestCase("uint64", (UInt64)1, BaseFunnyType.UInt64)]
    [TestCase("int8", (sbyte)1, BaseFunnyType.Int8)]
    [TestCase("sbyte", (sbyte)1, BaseFunnyType.Int8)]
    // Float32/Float64 input-output round-trip. Requires FloatFamily dialect opt-in
    // via Funny.Hardcore.WithDialect — this parametric uses default dialect, so
    // these cases are kept Ignore'd. Equivalent coverage in BuiltInFunctionsTest
    // (Float32_GenericMonomorphisation, Float32_TypedFunction etc.).
    [TestCase("float32", 1.0f, BaseFunnyType.Float32, Ignore = "Default dialect rejects float32 keyword; covered by Float32_* tests using BuildWithFloats")]
    [TestCase("float64", 1.0,  BaseFunnyType.Real,    Ignore = "Default dialect rejects float64 keyword; covered by Default_Float64Keyword_BuildsRealVariable")]
    [TestCase("int16", (Int16)1, BaseFunnyType.Int16)]
    [TestCase("int", (int)1, BaseFunnyType.Int32)]
    [TestCase("int32", (int)1, BaseFunnyType.Int32)]
    [TestCase("int64", (long)1, BaseFunnyType.Int64)]
    [TestCase("real", 1.0, BaseFunnyType.Real)]
    [TestCase("bool", true, BaseFunnyType.Bool)]
    public void OutputEqualsInput(string type, object expected, BaseFunnyType baseFunnyType) {
        var res = $"x:{type}\r  y = x".Calc("x", expected);
        res.AssertReturns(expected);
        res.AssertResultIs(("y", GetClrType(FunnyType.PrimitiveOf(baseFunnyType))));
    }

    // sbyte ≡ int8: alias is interchangeable in type annotations.
    [Test]
    public void SbyteAlias_InterchangeableWithInt8() {
        var rt = Funny.Hardcore.Build("x:sbyte=5\r y:int8=x\r out=y");
        rt.Run();
        Assert.AreEqual("Int8", rt["out"].Type.ToString());
        Assert.AreEqual((sbyte)5, rt["out"].Value);
    }

    [Test]
    public void SbyteAlias_InFunctionSignature() {
        var rt = Funny.Hardcore.Build("f(x:sbyte):sbyte = -x\r out = f(5)");
        rt.Run();
        Assert.AreEqual("Int8", rt["out"].Type.ToString());
        Assert.AreEqual((sbyte)-5, rt["out"].Value);
    }

    [TestCase("int[]", new[] { 1, 2, 3 }, BaseFunnyType.Int32)]
    [TestCase("int64[]", new long[] { 1, 2, 3 }, BaseFunnyType.Int64)]
    public void OutputEqualsInputArray(string type, object expected, BaseFunnyType arrayType) {
        var res = $"x:{type}\r  y = x".Calc("x", expected);
        res.AssertReturns(expected);
        res.AssertResultIs(("y", GetClrType(FunnyType.ArrayOf(FunnyType.PrimitiveOf(arrayType)))));
    }

    [Test]
    public void OutputEqualsTextInput() {
        var res = "x:text;  y = x".Calc("x", "1");
        res.AssertReturns("y", "1");
        res.AssertResultIs(("y", typeof(string)));
    }

    [TestCase("byte", "&", BaseFunnyType.UInt8)]
    [TestCase("uint8", "&", BaseFunnyType.UInt8)]
    [TestCase("uint16", "&", BaseFunnyType.UInt16)]
    [TestCase("uint32", "&", BaseFunnyType.UInt32)]
    [TestCase("uint64", "&", BaseFunnyType.UInt64)]
    [TestCase("int8", "&", BaseFunnyType.Int8)]
    [TestCase("int16", "&", BaseFunnyType.Int16)]
    [TestCase("int", "&", BaseFunnyType.Int32)]
    [TestCase("int32", "&", BaseFunnyType.Int32)]
    [TestCase("int64", "&", BaseFunnyType.Int64)]
    [TestCase("byte", "|", BaseFunnyType.UInt8)]
    [TestCase("uint8", "|", BaseFunnyType.UInt8)]
    [TestCase("uint16", "|", BaseFunnyType.UInt16)]
    [TestCase("uint32", "|", BaseFunnyType.UInt32)]
    [TestCase("uint64", "|", BaseFunnyType.UInt64)]
    [TestCase("int8", "|", BaseFunnyType.Int8)]
    [TestCase("int16", "|", BaseFunnyType.Int16)]
    [TestCase("int", "|", BaseFunnyType.Int32)]
    [TestCase("int32", "|", BaseFunnyType.Int32)]
    [TestCase("int64", "|", BaseFunnyType.Int64)]
    [TestCase("byte", "^", BaseFunnyType.UInt8)]
    [TestCase("uint8", "^", BaseFunnyType.UInt8)]
    [TestCase("uint16", "^", BaseFunnyType.UInt16)]
    [TestCase("uint32", "^", BaseFunnyType.UInt32)]
    [TestCase("uint64", "^", BaseFunnyType.UInt64)]
    [TestCase("int8", "^", BaseFunnyType.Int8)]
    [TestCase("int16", "^", BaseFunnyType.Int16)]
    [TestCase("int", "^", BaseFunnyType.Int32)]
    [TestCase("int32", "^", BaseFunnyType.Int32)]
    [TestCase("int64", "^", BaseFunnyType.Int64)]
    public void IntegersBitwiseOperatorTest(string inputTypes, string function, BaseFunnyType expectedOutputType) {
        var runtime = $"a:{inputTypes}; b:{inputTypes}; c=a{function}b;".Build();
        Assert.AreEqual(expectedOutputType, runtime.Variables.Single(v => v.IsOutput).Type.BaseType);
    }

    [TestCase("byte", BaseFunnyType.UInt8)]
    [TestCase("uint8", BaseFunnyType.UInt8)]
    [TestCase("uint16", BaseFunnyType.UInt16)]
    [TestCase("uint32", BaseFunnyType.UInt32)]
    [TestCase("uint64", BaseFunnyType.UInt64)]
    [TestCase("int8", BaseFunnyType.Int8)]
    [TestCase("int16", BaseFunnyType.Int16)]
    [TestCase("int", BaseFunnyType.Int32)]
    [TestCase("int32", BaseFunnyType.Int32)]
    [TestCase("int64", BaseFunnyType.Int64)]
    public void IntegersBitwiseInvertTest(string inputTypes, BaseFunnyType expectedOutputType) {
        var runtime = $"a:{inputTypes}; b:{inputTypes}; c= ~a".Build();
        Assert.AreEqual(expectedOutputType, runtime.Variables.Single(v => v.IsOutput).Type.BaseType);
    }

    [TestCase("int", BaseFunnyType.Int32)]
    [TestCase("int32", BaseFunnyType.Int32)]
    [TestCase("int64", BaseFunnyType.Int64)]
    public void SummOfTwoIntegersTest(string inputTypes, BaseFunnyType expectedOutputType) {
        var runtime = $"a:{inputTypes}; b:{inputTypes}; y = a + b".Build();
        Assert.AreEqual(expectedOutputType, runtime.Variables.Single(v => v.IsOutput && v.Name == "y").Type.BaseType);
    }

    [TestCase("int", BaseFunnyType.Int32)]
    [TestCase("int32", BaseFunnyType.Int32)]
    [TestCase("int64", BaseFunnyType.Int64)]
    public void DifferenceOfTwoIntegersTest(string inputTypes, BaseFunnyType expectedOutputType) {
        var runtime = $"a:{inputTypes}; b:{inputTypes}; y = a - b".Build();
        Assert.AreEqual(expectedOutputType, runtime.Variables.Single(o => o.Name == "y").Type.BaseType);
    }

    [TestCase("int", BaseFunnyType.Int32)]
    [TestCase("int32", BaseFunnyType.Int32)]
    [TestCase("int64", BaseFunnyType.Int64)]
    public void MultiplyOfTwoIntegersTest(string inputTypes, BaseFunnyType expectedOutputType) {
        var runtime = $"a:{inputTypes}; b:{inputTypes}; y = a * b".Build();
        Assert.AreEqual(expectedOutputType, runtime.Variables.Single(v => v.IsOutput && v.Name == "y").Type.BaseType);
    }

    [TestCase("int", BaseFunnyType.Int32)]
    [TestCase("int32", BaseFunnyType.Int32)]
    [TestCase("int64", BaseFunnyType.Int64)]
    public void RemainsOfTwoIntegersTest(string inputTypes, BaseFunnyType expectedOutputType) {
        var runtime = $"a:{inputTypes}; b:{inputTypes}; y = a %b".Build();
        Assert.AreEqual(expectedOutputType, runtime.Variables.Single(v => v.IsOutput && v.Name == "y").Type.BaseType);
    }

    [TestCase("y:real = 1", BaseFunnyType.Real)]
    [TestCase("y:int = 1", BaseFunnyType.Int32)]
    [TestCase("y:byte = 1", BaseFunnyType.UInt8)]
    [TestCase("x:int; y:real = x", BaseFunnyType.Real)]
    public void OutputType_checkOutputTest(string expression, BaseFunnyType expectedType) {
        var runtime = expression.Build();
        Assert.AreEqual(expectedType, runtime.Variables.Single(v => v.IsOutput && v.Name == "y").Type.BaseType);
    }

    [TestCase("y:real = x+1", "x", BaseFunnyType.Real)]
    [TestCase("y:real = x", "x", BaseFunnyType.Real)]
    [TestCase("y:bool = x", "x", BaseFunnyType.Bool)]
    [TestCase("x:int; y:real = x+a", "a", BaseFunnyType.Real)]
    public void OutputType_checkInputTest(string expression, string variable, BaseFunnyType expectedType) {
        var runtime = expression.Build();

        Assert.AreEqual(expectedType, runtime[variable].Type.BaseType);
    }

    [TestCase("y:int[] = x", new[] { 1, 2, 3 }, new[] { 1, 2, 3 })]
    [TestCase("y:real[] = x", new[] { 1.0, 2.0, 3.0 }, new[] { 1.0, 2.0, 3.0 })]
    [TestCase("z:real[] = x; y = z", new[] { 1.0, 2.0, 3.0 }, new[] { 1.0, 2.0, 3.0 })]
    [TestCase("y:int[] = x.reverse();", new[] { 1, 2, 3 }, new[] { 3, 2, 1 })]
    [TestCase("a:int = 5; y:real = a+x", 2.5, 7.5)]
    public void OutputType_runtimeTest(string expression, object xValue, object expectedY) =>
        expression.Calc("x", xValue).AssertResultHas("y", expectedY);


    [TestCase("y = 1", new string[0])]
    [TestCase("y = x*1.0", new[] { "x" })]
    [TestCase("y = x/2", new[] { "x" })]
    [TestCase("y = in1/2+ in2", new[] { "in1", "in2" })]
    [TestCase("y = in1/2 + (in2*in3)", new[] { "in1", "in2", "in3" })]
    public void InputVarablesListWithAutoTypesIsCorrect(string expr, string[] inputNames) {
        var inputs = expr.Build().Variables.Where(i => !i.IsOutput);
        Assert.IsTrue(inputs.All(i => i.Type == FunnyType.Real));
        CollectionAssert.AreEquivalent(inputNames, inputs.Select(i => i.Name));
    }

    [TestCase("0x1", "out", BaseFunnyType.Int32)]
    [TestCase("1.0", "out", BaseFunnyType.Real)]
    [TestCase("1", "out", BaseFunnyType.Int32)]
    [TestCase("true", "out", BaseFunnyType.Bool)]
    [TestCase("z = x", "z", BaseFunnyType.Any)]
    [TestCase("y = x/2", "y", BaseFunnyType.Real)]
    [TestCase("x:bool \r z:bool \r y = x and z", "y", BaseFunnyType.Bool)]
    public void OutputVariablesListIsCorrect(string expr, string outputName, BaseFunnyType type) {
        var runtime = expr.Build();
        var output = runtime.Variables.Single(v => v.IsOutput);
        Assert.AreEqual(output.Type, FunnyType.PrimitiveOf(type));
        Assert.AreEqual(output.Name, outputName);
    }

    private static Type GetClrType(FunnyType funnyType) =>
        FunnyConverter.RealIsDouble.GetOutputConverterFor(funnyType).ClrType;

    // ═══════════════════════════════════════════════════════════════
    // Signed/unsigned LCA resolution
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SignedUnsignedLCA_ResolvesToInt64() {
        "a:int32 = 1; b:uint32 = 2; c = if(true) a else b"
            .Calc().AssertResultHas("c", 1L);
    }

    [Test]
    public void SignedUnsignedLCA_Int16_UInt16_ResolvesToInt32() {
        "a:uint16 = 1; b:int16 = 2; c = if(true) a else b"
            .Calc().AssertResultHas("c", 1);
    }

    [Test]
    public void SignedUnsignedLCA_Int64_UInt64_ResolvesToReal() {
        "a:uint64 = 1; b:int64 = 2; c = if(true) a else b"
            .Calc().AssertResultHas("c", 1.0d);
    }

    // ═══ Abstract type → concrete mapping (no overflow) ═══
    // TIC's internal abstract types (U12/U24/U48) must resolve to concrete CLR types
    // wide enough to hold the literal. Regression tests for past overflow bugs.

    [TestCase("out = if(false) [0] else [300]",         new[] { 300 })]
    [TestCase("out = if(false) [255] else [300]",       new[] { 300 })]
    [TestCase("out = if(false) [0] else [4095]",        new[] { 4095 })]
    [TestCase("out = if(false) [0] else [70000]",       new[] { 70000 })]
    [TestCase("out = if(false) [255] else [100000]",    new[] { 100000 })]
    public void AbstractInt_ResolvesToInt32_NoOverflow(string expr, int[] expected) =>
        expr.Calc().AssertResultHas("out", expected);

    // After LCA-of-Preferreds propagation: preferreds of literal 0 (I32) and 5B (I64) LCA
    // to I64, so the resolved element type is Int64 (also holds 5B, matches the branch that
    // needed the widening). Previously resolved to UInt64 via ancestor when preferreds were
    // dropped on mismatch.
    [TestCase("out = if(false) [0] else [5000000000]",  new long[] { 5_000_000_000L })]
    public void AbstractInt_ExceedsUInt32_ResolvesToInt64(string expr, long[] expected) =>
        expr.Calc().AssertResultHas("out", expected);

    [Test]
    public void I48_LargeUint32_NoOverflow() {
        var result = "x:uint32 = 3000000000; y:int = 1; out = max(x, y)".Calc();
        Assert.AreEqual(3_000_000_000L, result.Get("out"));
    }

    #region FloatFamily dialect
    // Real literals carry [F32..Real, Pref=Real] — narrow to F32 at typed target.

    // Real literal — no annotation → Real (Pref=Real).
    [Test]
    public void Float32_RealLiteral_UnAnnotated_StaysReal() {
        var rt = "out = 1.5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(1.5, rt["out"].Value);
    }

    // if(c) T1 else T2 with mixed real literals + f32 target → LCA narrows to F32.
    [Test]
    public void Float32_IfElse_TwoRealLiterals_NarrowToF32() {
        var rt = "c:bool; out:float32 = if(c) 1.0 else 2.0".BuildWithFloats();
        rt["c"].Value = true;
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_IfElse_TwoRealLiterals_ElseBranch_NarrowToF32() {
        var rt = "c:bool; out:float32 = if(c) 1.0 else 2.0".BuildWithFloats();
        rt["c"].Value = false;
        rt.Run();
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // LCA of float32 typed + real typed variables = Real.
    [Test]
    public void Float32_IfElse_F32AndReal_LcaIsReal() {
        var rt = "c:bool; x:float32=1.5; y:real=2.5; out = if(c) x else y".BuildWithFloats();
        rt["c"].Value = true;
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(1.5, rt["out"].Value);
    }

    [Test]
    public void Float32_IfElse_F32AndInt_LcaIsF32() {
        // Both branches lift to F32 (int has [i64..Real,Pref=Real] but constrained by f32).
        var rt = "c:bool; x:float32=1.5; y:int=2; out:float32 = if(c) x else y".BuildWithFloats();
        rt["c"].Value = false;
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // Same but implicit LCA (no annotation): TIC picks the narrower F32
    // (since int→f32 widens and f32 covers both).
    [Test]
    public void Float32_IfElse_F32AndInt_ImplicitLca_ResolvesF32() {
        var rt = "c:bool; x:float32=1.5; y:int=2; out = if(c) x else y".BuildWithFloats();
        rt["c"].Value = true;
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    // Explicit cast in one branch, target f32.
    [Test]
    public void Float32_IfElse_ExplicitReal_TargetF32_ParseError() {
        // Cannot narrow: one branch is :real explicitly, out is :float32.
        Assert.Throws<FunnyParseException>(() =>
            "c:bool; x:real=1.0; out:float32 = if(c) x else 2.0".BuildWithFloats());
    }

    // Variable inference from usage: TIC decides x is F32 because of `out:float32 = x`.
    [Test]
    public void Float32_VariableInferred_ViaOutputAnnotation() {
        var rt = "x = 1.5; out:float32 = x".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    // Mixed arithmetic (int + real) narrowed to f32.
    [Test]
    public void Float32_MixedArithmetic_NarrowsToF32() {
        var rt = "x = 1 + 2.0; out:float32 = x".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3.0f, rt["out"].Value);
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
    }

    // Chain of assignments: a:f32 -> b -> c -> out.
    [Test]
    public void Float32_ChainPropagation_ThroughLocals() {
        var rt = "a:float32=1.0\r b = a\r c = b * 2.0\r out = c".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // Long chain: 5 hops.
    [Test]
    public void Float32_LongChain_5Hops_PropagatesF32() {
        var rt = "a:float32=1.0\r b=a+0.5\r c=b*2.0\r d=c-0.5\r e=d\r out=e".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    // float64 alias should equal Real.
    [Test]
    public void Float64_Keyword_IsAliasForReal_InArithmetic() {
        var rt = "x:float64=1.5\r y:real=x\r out = y+1.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(2.5, rt["out"].Value);
    }

    // Type annotation on function parameter propagates.
    [Test]
    public void Float32_FunctionReturnTypeInference() {
        var rt = "f(x:float32) = x + 1.0\r out = f(1.5)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    // Deferred narrowing: backward propagation from `out:float32` narrows y and z to F32.
    [Test]
    public void Float32_DeferredNarrowing_ThroughIntermediate() {
        var rt = "z = 3.14; y = z + 1.0; out:float32 = y".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.14f + 1.0f, rt["out"].Value);
    }

    // Two independent chains, only one narrowed.
    [Test]
    public void Float32_TwoIndependentChains_DifferentTypes() {
        var rt = "a:float32=1.0\r b:real=2.0\r outA=a+1.0\r outB=b+1.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["outA"].Type.ToString());
        Assert.AreEqual("Real", rt["outB"].Type.ToString());
    }

    // Both branches literal-typed with f32 annotation on one — Real literal
    // has flexible constraint [F32..Real], narrows to F32 to match sibling.
    [Test]
    public void Float32_IfElse_TypedBranch_F32AndRealLiteral() {
        var rt = "c:bool; x:float32=1.5; out = if(c) x else 2.0".BuildWithFloats();
        rt["c"].Value = false;
        rt.Run();
        // TIC narrows the real-literal to F32 (its [F32..Real] range meets F32).
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // Narrow: `out:float32` forces literal 2.0 to F32; LCA(F32, F32) = F32.
    [Test]
    public void Float32_IfElse_TypedBranch_F32AndRealLiteral_TargetF32() {
        var rt = "c:bool; x:float32=1.5; out:float32 = if(c) x else 2.0".BuildWithFloats();
        rt["c"].Value = false;
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // Var declared as `real`, mixed with int in if-else → LCA=real.
    [Test]
    public void Float32_IfElse_RealVarAndInt_LcaIsReal() {
        var rt = "c:bool; x:real=1.5; out = if(c) x else 2".BuildWithFloats();
        rt["c"].Value = true;
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
    }
    #endregion
}
