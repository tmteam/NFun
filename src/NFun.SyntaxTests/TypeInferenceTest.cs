using System;
using System.Linq;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests
{
    public class TypeInferenceTest
    {
        [TestCase("y = 0x2",BaseFunnyType.Int32)]
        [TestCase("y = 0x2*3",BaseFunnyType.Int32)]
        [TestCase("y = 2**3",BaseFunnyType.Real)]
        [TestCase("y = 0x2.rema(3)", BaseFunnyType.Int32)]

        [TestCase("y = 4/3",BaseFunnyType.Real)]
        [TestCase("y = 0x4- 3",BaseFunnyType.Int32)]
        [TestCase("y = 0x4+ 3",BaseFunnyType.Int32)]
        [TestCase("z = 1 + 4/3 + 3 +2*3 -1", BaseFunnyType.Real)]
        [TestCase("y = -0x2*-4",BaseFunnyType.Int32)]
        [TestCase("y = -2*(-4+0x2)",BaseFunnyType.Int32)]
        [TestCase("y = -(-(-1.0))",BaseFunnyType.Real)]

        [TestCase("y = 0.2",BaseFunnyType.Real)]
        [TestCase("y = 1.1_11  ",BaseFunnyType.Real)]
        [TestCase("y = 4*0.2",BaseFunnyType.Real)]
        [TestCase("y = 4/0.2",BaseFunnyType.Real)]
        [TestCase("y = 4/0.2",BaseFunnyType.Real)]
        [TestCase("y = 2**0.3",BaseFunnyType.Real)]
        [TestCase("y = 0.2**2",BaseFunnyType.Real)]
        [TestCase("y = 0.2.rema(2)", BaseFunnyType.Real)]
        [TestCase("y = 3.rema(0.2)", BaseFunnyType.Real)]

        [TestCase("y = 0xfF  ",BaseFunnyType.Int32)]
        [TestCase("y = 0x00_Ff  ",BaseFunnyType.Int32)]
        [TestCase("y = 0b001  ",BaseFunnyType.Int32)]
        [TestCase("y = 0b11  ",BaseFunnyType.Int32)]
        [TestCase("y = 0x_1",BaseFunnyType.Int32)]
        
        [TestCase("y = 1==1",BaseFunnyType.Bool)]
        [TestCase("y = 1==0",BaseFunnyType.Bool)]
        [TestCase("y = true==true",BaseFunnyType.Bool)]
        [TestCase("y = 1!=0",BaseFunnyType.Bool)]
        [TestCase("y = 0!=1",BaseFunnyType.Bool)]
        [TestCase("y = 5!=5",BaseFunnyType.Bool)]
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
                   
          y = fib(1)", BaseFunnyType.Real)]
        [TestCase(
            @"fibrec(n:int, iter, p1,p2) =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(1)", BaseFunnyType.Real)]
        [TestCase(
            @"fibrec(n, iter, p1,p2):int =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(1)", BaseFunnyType.Int32)]
        [TestCase(@"y = [1..7]
                        .map(fun it+1)
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..8]
                        .map(fun [it,1].sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..9]
                        .map(fun [1,it].sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..10]
                        .map(fun [1..it].sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..11]
                        .map(fun [1..it].sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..12]
                        .map(fun [1..it]
                                .map(fun 2600/it)
                                .sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..13]
                        .map(fun [1..10]
                                .map(fun 2600/it)
                                .sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..14]
                        .map(fun it/2)
                        .sum()", BaseFunnyType.Real)]
        [TestCase(
            @"div10(x) = 2600/x
            y = [1..20].map(div10).sum()", BaseFunnyType.Real)]
        [TestCase(
            @"div11(x) = 2600/x
            supsum(n) = [1..n].map(div11).sum()
            y = [1..20].map(supsum).sum()", BaseFunnyType.Real)]
        //todo
        //[TestCase(
        //    @"div12(x) = 2600/x
        //    supsum(n) = [1..n].map(div12).sum()
        //    y = [1..20].map(supsum).sum().round()", BaseVarType.Int32)]
        public void SingleEquation_Runtime_OutputTypeCalculatesCorrect(string expr, BaseFunnyType type)
        {
            var clrtype = FunnyTypeConverters.GetOutputConverter(FunnyType.PrimitiveOf(type)).ClrType;
            
            expr.Calc().AssertResultIs(clrtype);
        }

        [TestCase(
            @"someRec(n, iter, p1,p2) =
                          if (n >iter) 
                                someRec(n, iter+1, p1+p2, p1)
                          else 
                                p1+p2  
          y = someRec(9,2,1,1)", BaseFunnyType.Real)]

        [TestCase(
            @"someRec2(n, iter) =
                          if (n >iter) 
                                someRec2(n, iter+1)
                          else 
                                1
          y:int = someRec2(0x9,0x2)", BaseFunnyType.Int32)]
        //todo
        //[TestCase(
        //    @"someRec3(n, iter) = someRec3(n, iter+1).strConcat(n >iter)
        //  y = someRec3(9,2)[0]", BaseVarType.Char)]
        [TestCase("(if(true) [1,2] else [])[0]", BaseFunnyType.Real)]
        public void SingleEquations_Parsing_OutputTypesCalculateCorrect(string expr, BaseFunnyType type) => 
            Assert.AreEqual(type, expr.Build().Variables.Single(v=>v.IsOutput).Type.BaseType);

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
        [TestCase("[a].map(fun it)")]
        [TestCase("[a].filter(fun it>2)")]
        [TestCase("[a].reverse()")]
        [TestCase("[a]")]
        [TestCase("y = [-x].all(fun it<0.0)")]
        [TestCase("y = [x,x].all(fun it<0.0)")]
        [TestCase("y = [-x,x].all(fun it<0.0 )")]
        [TestCase("y = [1,-x].all( fun it<0.0 )")]
        [TestCase("y = [x,2.0,3.0].all(fun it >1.0)")]
        [TestCase("y = [1..11].map(fun [1..n].sum())")]
        [TestCase("y = [1..12].map(fun [1..n].sum()).sum()")]
        [TestCase("y = [1..11].map(fun [1..i].sum())")]
        [TestCase("y = [1..12].map(fun [1..i].sum()).sum()")]
        [TestCase("dsum7(x) = x+x")]
        [TestCase(
            @"dsum8(x) = x+x
            y = [1..20].map(dsum8)")]
        [TestCase(
            @"div9(x) = 2600/x
            y = [1..20].map(div9)")]
        [TestCase(@"div10(x) = 2600/x
                    y(n) = [1..n].map(div10).sum()")]
        [TestCase(
            @"dsum11(x:int):int = x+x
            y = [1..20].map(dsum11)")]
        [TestCase(
            @"dsum12(x:real):real = x+x
            y = [1..20].map(dsum12)")]
        [TestCase(@"div13(x:int):real = 2600/x
                    y(n) = [1..n].map(div13).sum()")]
        [TestCase(@"div14(x:real):real = 2600/x
                    y(n) = [1..n].map(div14).sum()")]
        [TestCase(
            @"input:int[]
            dsame15(x:real):real = x
            y = input.map(dsame15)")]
        [TestCase("y = x * -x")]
        [TestCase("y = -x * x")]
        [TestCase("y = [-x,x]")]
        [TestCase("y = [-x]")]

        [TestCase( "y1 = -x \r y2 = -x")]
        [TestCase( "y1 = x  \r y2 = -x")]
        [TestCase( "y = [x,-x].all(fun it<0.0)")]
        [TestCase( "y = [-x,-x].all(fun it<0.0)")]
        [TestCase("z = [-x,-x,-x] \r  y = z.all(fun it < 0.0)")]
        [TestCase( "y = [x, -x]")]
        [TestCase( "y = [-x,-x,-x].all(fun it < 0.0)")]
        [TestCase( "[x, -x]")]
        public void EquationTypes_SolvesSomehow(string expr) => Assert.DoesNotThrow(()=>expr.Build());

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

        public void TwinEquations_Runtime_OutputTypesCalculateCorrect(string expr, BaseFunnyType ytype, BaseFunnyType ztype)
        {
            var res = expr.Calc();
            var y = res.Get("y");
            Assert.AreEqual(y.GetType(), FunnyTypeConverters.GetOutputConverter(FunnyType.PrimitiveOf(ytype)).ClrType);
            var z = res.Get("z");
            Assert.AreEqual(z.GetType(), FunnyTypeConverters.GetOutputConverter(FunnyType.PrimitiveOf(ztype)).ClrType);
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
        //todo: What is expected ?!
        //[TestCase("y= [1,2,3].fold(fun '(fun it1}!'}")]
        [TestCase("a:int \r a=4")]
        [TestCase("a:int a=4")]
        [TestCase("a:real =false")]
        [TestCase("a:real =false")]
        [TestCase("x:bool; a:real =x")]
        public void ObviouslyFailsWithParse(string expr) => expr.AssertObviousFailsOnParse();
        

        [TestCase(new []{1,2},    "x:int[]\r y= x", new []{1,2})]        
        [TestCase(new []{1,2},    "x:int[]\r y= x.concat(x)", new []{1,2,1,2})]
        [TestCase(new []{"1","2"},    "x:text[]\r y= x", new []{"1","2"})]        
        [TestCase(new []{"1","2"},    "x:text[]\r y= x.concat(x)", new []{"1","2","1","2"})]
        [TestCase(new []{1.0,2.0},    "x:real[]\r y= x", new []{1.0,2.0})]        
        
        [TestCase(new []{1.0,2.0},    "x:real[]\r y= x.concat(x)", new []{1.0,2.0,1.0,2.0})]        
        [TestCase(1.0, "x:real\r y= x+1", 2.0)]       
        
        [TestCase(1,    "x:int\r y= x+1", 2)]        
        [TestCase(1.0, "y= x+1", 2.0)]       
        [TestCase(2.0, "y= x*1", 2.0)]       
        [TestCase(1.0, "y= x-1", 0.0)]       
        [TestCase(1.0, "y= 1+x", 2.0)]       
        [TestCase(2.0, "y= 1*x", 2.0)]       
        [TestCase(1.0, "y= 1-x", 0.0)]     
        //todo
        //[TestCase("1", "y= x.strConcat(1)", "11")]        
        [TestCase(true, "x:bool\r y= x and true", true)] 
        
        public void SingleInputTypedEquation(object x,  string expr, object y) => 
            expr.Calc("x", x).AssertReturns(y);

        [TestCase("y:int[]= [1,2,3].map(fun it*it)", new[]{1,4,9})] 
        [TestCase("y:int[]= [1,2,3].map(fun it)", new[]{1,2,3})] 
        [TestCase("y:int[]= [1,2,3].map(fun 1)", new[]{1,1,1})] 
        [TestCase("y= [1,2,3].map(fun 'hi')", new[]{"hi","hi","hi"})] 
        [TestCase("y= [true,true,false].map(fun 'hi')", new[]{"hi","hi","hi"})] 
        [TestCase("y:int[]= [1,2,3].filter (fun it>2)", new[]{3})] 
        [TestCase("y:int= [1,2,3].fold(fun it1+it2)", 6)] 
        [TestCase("y:int= [1,2,3].fold(fun 1)", 1)] 
        [TestCase("y:int= [1,2,3].fold(fun it1)", 1)] 
        [TestCase("y:int= [1,2,3].fold(fun it1+1)", 3)] 

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
            Assert.DoesNotThrow(()=> expr.Build());

        [TestCase("byte",   (byte)1,   BaseFunnyType.UInt8)]
        [TestCase("uint8",  (byte)1,   BaseFunnyType.UInt8)]
        [TestCase("uint16", (UInt16)1, BaseFunnyType.UInt16)]
        [TestCase("uint32", (UInt32)1, BaseFunnyType.UInt32)]
        [TestCase("uint64", (UInt64)1, BaseFunnyType.UInt64)]
        [TestCase("int16",  (Int16)1,  BaseFunnyType.Int16)]
        [TestCase("int",    (int)1,    BaseFunnyType.Int32)]
        [TestCase("int32",  (int)1,    BaseFunnyType.Int32)]
        [TestCase("int64",  (long)1,   BaseFunnyType.Int64)]
        [TestCase("real",          1.0,BaseFunnyType.Real)]
        [TestCase("bool",         true,BaseFunnyType.Bool)]
        public void OutputEqualsInput(string type, object expected, BaseFunnyType baseFunnyType)
        {
            var res = $"x:{type}\r  y = x".Calc("x",expected);
            res.AssertReturns(expected);
            res.AssertResultIs(("y",GetClrType(FunnyType.PrimitiveOf(baseFunnyType))));
        }

        [TestCase("int[]", new[] {1, 2, 3}, BaseFunnyType.Int32)]
        [TestCase("int64[]", new long[] {1, 2, 3}, BaseFunnyType.Int64)]
        public void OutputEqualsInputArray(string type, object expected, BaseFunnyType arrayType)
        {
            var res = $"x:{type}\r  y = x".Calc("x", expected);
            res.AssertReturns(expected);
            res.AssertResultIs(("y", GetClrType(FunnyType.ArrayOf(FunnyType.PrimitiveOf(arrayType)))));
        }
        
        [Test]
        public void OutputEqualsTextInput()
        {
            var res = "x:text;  y = x".Calc("x", "1");
            res.AssertReturns("y", "1");
            res.AssertResultIs(("y",typeof(string)));
        }
        
        [TestCase("byte",   "&", BaseFunnyType.UInt8)]
        [TestCase("uint8",  "&", BaseFunnyType.UInt8)]
        [TestCase("uint16", "&", BaseFunnyType.UInt16)]
        [TestCase("uint32", "&", BaseFunnyType.UInt32)]
        [TestCase("uint64", "&", BaseFunnyType.UInt64)]
        //[TestCase("int8", "&", BaseVarType.Int8)]
        [TestCase("int16",  "&", BaseFunnyType.Int16)]
        [TestCase("int",    "&", BaseFunnyType.Int32)]
        [TestCase("int32",  "&", BaseFunnyType.Int32)]
        [TestCase("int64",  "&", BaseFunnyType.Int64)]
        [TestCase("byte",   "|", BaseFunnyType.UInt8)]
        [TestCase("uint8",  "|", BaseFunnyType.UInt8)]
        [TestCase("uint16", "|", BaseFunnyType.UInt16)]
        [TestCase("uint32", "|", BaseFunnyType.UInt32)]
        [TestCase("uint64", "|", BaseFunnyType.UInt64)]
        //[TestCase("int8", "|", BaseVarType.Int8)]
        [TestCase("int16",  "|", BaseFunnyType.Int16)]
        [TestCase("int",    "|", BaseFunnyType.Int32)]
        [TestCase("int32",  "|", BaseFunnyType.Int32)]
        [TestCase("int64",  "|", BaseFunnyType.Int64)]
        
        [TestCase("byte",   "^", BaseFunnyType.UInt8)]
        [TestCase("uint8", "^", BaseFunnyType.UInt8)]
        [TestCase("uint16", "^", BaseFunnyType.UInt16)]
        [TestCase("uint32", "^", BaseFunnyType.UInt32)]
        [TestCase("uint64", "^", BaseFunnyType.UInt64)]
        //[TestCase("int8", "^", BaseVarType.Int8)]
        [TestCase("int16", "^", BaseFunnyType.Int16)]
        [TestCase("int", "^", BaseFunnyType.Int32)]
        [TestCase("int32", "^", BaseFunnyType.Int32)]
        [TestCase("int64", "^", BaseFunnyType.Int64)]
        
        public void IntegersBitwiseOperatorTest(string inputTypes, string function, BaseFunnyType expectedOutputType)
        {
            var runtime = $"a:{inputTypes}; b:{inputTypes}; c=a{function}b;".Build();
            Assert.AreEqual(expectedOutputType, runtime.Variables.Single(v=>v.IsOutput).Type.BaseType);
        }
        
        [TestCase("byte",  BaseFunnyType.UInt8)]
        [TestCase("uint8", BaseFunnyType.UInt8)]
        [TestCase("uint16", BaseFunnyType.UInt16)]
        [TestCase("uint32", BaseFunnyType.UInt32)]
        [TestCase("uint64", BaseFunnyType.UInt64)]
        //[TestCase("int8",  BaseVarType.Int8)]
        [TestCase("int16", BaseFunnyType.Int16)]
        [TestCase("int", BaseFunnyType.Int32)]
        [TestCase("int32", BaseFunnyType.Int32)]
        [TestCase("int64", BaseFunnyType.Int64)]
        
        public void IntegersBitwiseInvertTest(string inputTypes, BaseFunnyType expectedOutputType)
        {
            var runtime = $"a:{inputTypes}; b:{inputTypes}; c= ~a".Build();
            Assert.AreEqual(expectedOutputType, runtime.Variables.Single(v=>v.IsOutput).Type.BaseType);
        }
        
        [TestCase("int",    BaseFunnyType.Int32)]
        [TestCase("int32",  BaseFunnyType.Int32)]
        [TestCase("int64",  BaseFunnyType.Int64)]
        public void SummOfTwoIntegersTest(string inputTypes, BaseFunnyType expectedOutputType)
        {
            var runtime = $"a:{inputTypes}; b:{inputTypes}; y = a + b".Build();
            Assert.AreEqual(expectedOutputType, runtime.Variables.Single(v=>v.IsOutput && v.Name == "y").Type.BaseType);
        }
        
        [TestCase("int",    BaseFunnyType.Int32)]
        [TestCase("int32",  BaseFunnyType.Int32)]
        [TestCase("int64",  BaseFunnyType.Int64)]
        public void DifferenceOfTwoIntegersTest(string inputTypes, BaseFunnyType expectedOutputType)
        {
            var runtime = $"a:{inputTypes}; b:{inputTypes}; y = a - b".Build();
            Assert.AreEqual(expectedOutputType, runtime.Variables.Single(o => o.Name == "y").Type.BaseType);
        }
        
        [TestCase("int",    BaseFunnyType.Int32)]
        [TestCase("int32",  BaseFunnyType.Int32)]
        [TestCase("int64",  BaseFunnyType.Int64)]
        public void MultiplyOfTwoIntegersTest(string inputTypes, BaseFunnyType expectedOutputType)
        {
            var runtime = $"a:{inputTypes}; b:{inputTypes}; y = a * b".Build();
            Assert.AreEqual(expectedOutputType, runtime.Variables.Single(v=>v.IsOutput && v.Name == "y").Type.BaseType);
        }
        
        [TestCase("int",    BaseFunnyType.Int32)]
        [TestCase("int32",  BaseFunnyType.Int32)]
        [TestCase("int64",  BaseFunnyType.Int64)]
        public void RemainsOfTwoIntegersTest(string inputTypes, BaseFunnyType expectedOutputType)
        {
            var runtime = $"a:{inputTypes}; b:{inputTypes}; y = a .rema(b)".Build();
            Assert.AreEqual(expectedOutputType, runtime.Variables.Single(v=>v.IsOutput && v.Name == "y").Type.BaseType);
        }
        
        [TestCase("y:real = 1",  BaseFunnyType.Real)]
        [TestCase("y:int = 1",  BaseFunnyType.Int32)]
        [TestCase("y:byte = 1",  BaseFunnyType.UInt8)]
        [TestCase("x:int; y:real = x",  BaseFunnyType.Real)]

        public void OutputType_checkOutputTest(string expression,  BaseFunnyType expectedType){
            var runtime = expression.Build();
            Assert.AreEqual(expectedType, runtime.Variables.Single(v=>v.IsOutput && v.Name == "y").Type.BaseType);
        }
        
        [TestCase("y:real = x+1", "x", BaseFunnyType.Real)]
        [TestCase("y:real = x", "x", BaseFunnyType.Real)]
        [TestCase("y:bool = x", "x", BaseFunnyType.Bool)]
        [TestCase("x:int; y:real = x+a", "a", BaseFunnyType.Real)]
        public void OutputType_checkInputTest(string expression, string variable, BaseFunnyType expectedType){
            var runtime = expression.Build();
            
            Assert.AreEqual(expectedType, runtime[variable].Type.BaseType);
        }

        [TestCase("y:int[] = x",new[]{1,2,3},new[]{1,2,3})]
        [TestCase("y:real[] = x",new[]{1.0,2.0,3.0},new[]{1.0,2.0,3.0})]
        [TestCase("z:real[] = x; y = z",new[]{1.0,2.0,3.0},new[]{1.0,2.0,3.0})]
        [TestCase("y:int[] = x.reverse();",new[]{1,2,3},new[]{3,2,1})]
        [TestCase("a:int = 5; y:real = a+x",2.5,7.5)]
        public void OutputType_runtimeTest(string expression, object xValue, object expectedY) => 
            expression.Calc("x",xValue).AssertResultHas("y", expectedY);

        
        [TestCase("y = 1", new string[0])]
        [TestCase("y = x*1.0", new[] { "x" })]
        [TestCase("y = x/2", new[] { "x" })]
        [TestCase("y = in1/2+ in2", new[] { "in1", "in2" })]
        [TestCase("y = in1/2 + (in2*in3)", new[] { "in1", "in2", "in3" })]
        public void InputVarablesListWithAutoTypesIsCorrect(string expr, string[] inputNames)
        {
            var inputs = expr.Build().Variables.Where(i=>!i.IsOutput);
            Assert.IsTrue(inputs.All(i=>i.Type== FunnyType.Real));
            CollectionAssert.AreEquivalent(inputNames, inputs.Select(i=>i.Name));
        }

        [TestCase("0x1", "out", BaseFunnyType.Int32)]
        [TestCase("1.0", "out", BaseFunnyType.Real)]
        [TestCase("1", "out", BaseFunnyType.Real)]
        [TestCase("true", "out", BaseFunnyType.Bool)]
        [TestCase("z = x", "z", BaseFunnyType.Any)]
        [TestCase("y = x/2", "y", BaseFunnyType.Real)]
        [TestCase("x:bool \r z:bool \r y = x and z", "y", BaseFunnyType.Bool)]
        public void OutputVariablesListIsCorrect(string expr, string outputName, BaseFunnyType type)
        {
            var runtime = expr.Build();
            var output = runtime.Variables.Single(v => v.IsOutput);
            Assert.AreEqual(output.Type, FunnyType.PrimitiveOf(type));
            Assert.AreEqual(output.Name, outputName);
        }

        private static Type GetClrType(FunnyType funnyType) => 
            FunnyTypeConverters.GetOutputConverter(funnyType).ClrType;
    }
}