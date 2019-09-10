using System;
using System.Linq;
using NFun;
using NFun.ParseErrors;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    public class TypeInferenceTest
    {
        [Test]
        public void VoidTest()
        {
            Assert.Pass();
        }
        [TestCase("y = 2",BaseVarType.Int32)]
        [TestCase("y = 2*3",BaseVarType.Int32)]
        [TestCase("y = 2**3",BaseVarType.Real)]
        [TestCase("y = 2%3",BaseVarType.Int32)]

        [TestCase("y = 4/3",BaseVarType.Real)]
        [TestCase("y = 4- 3",BaseVarType.Int32)]
        [TestCase("y = 4+ 3",BaseVarType.Int32)]
        [TestCase("z = 1 + 4/3 + 3 +2*3 -1", BaseVarType.Real)]
        [TestCase("y = -2*-4",BaseVarType.Int32)]
        [TestCase("y = -2*(-4+2)",BaseVarType.Int32)]
        [TestCase("y = -(-(-1))",BaseVarType.Int32)]

        [TestCase("y = 0.2",BaseVarType.Real)]
        [TestCase("y = 1.1_11  ",BaseVarType.Real)]
        [TestCase("y = 4*0.2",BaseVarType.Real)]
        [TestCase("y = 4/0.2",BaseVarType.Real)]
        [TestCase("y = 4/0.2",BaseVarType.Real)]
        [TestCase("y = 2**0.3",BaseVarType.Real)]
        [TestCase("y = 0.2**2",BaseVarType.Real)]
        [TestCase("y = 0.2%2",BaseVarType.Real)]
        [TestCase("y = 3%0.2",BaseVarType.Real)]

        [TestCase("y = 0xfF  ",BaseVarType.Int32)]
        [TestCase("y = 0x00_Ff  ",BaseVarType.Int32)]
        [TestCase("y = 0b001  ",BaseVarType.Int32)]
        [TestCase("y = 0b11  ",BaseVarType.Int32)]
        [TestCase("y = 0x_1",BaseVarType.Int32)]
        
        [TestCase("y = 1==1",BaseVarType.Bool)]
        [TestCase("y = 1==0",BaseVarType.Bool)]
        [TestCase("y = true==true",BaseVarType.Bool)]
        [TestCase("y = 1!=0",BaseVarType.Bool)]
        [TestCase("y = 0!=1",BaseVarType.Bool)]
        [TestCase("y = 5!=5",BaseVarType.Bool)]
        [TestCase("y = 5>3", BaseVarType.Bool)]
        [TestCase("y = 5>6", BaseVarType.Bool)]
        [TestCase("y = 5>=3", BaseVarType.Bool)]
        [TestCase("y = 5>=6", BaseVarType.Bool)]
        [TestCase("y = 5<=5", BaseVarType.Bool)]
        [TestCase("y = 5<=3", BaseVarType.Bool)]
        [TestCase("y = true and true", BaseVarType.Bool)]
        [TestCase("y = true or true", BaseVarType.Bool)]
        [TestCase("y = true xor true", BaseVarType.Bool)]
        [TestCase("y = 1<<2", BaseVarType.Int32)]
        [TestCase("y = 8>>2", BaseVarType.Int32)]
        [TestCase("y = 3|2", BaseVarType.Int32)]
        [TestCase("y = 3^2", BaseVarType.Int32)]
        [TestCase("y = 4&2", BaseVarType.Int32)]

        [TestCase(  
        @"fibrec(n, iter, p1,p2) =
                          if (n >iter) 
                                fibrec(n, iter+1, p1+p2, p1)
                          else 
                                p1+p2  
          fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   
          y = fib(1)",BaseVarType.Real)]
        [TestCase(  
            @"fibrec(n:int, iter, p1,p2) =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(1)",BaseVarType.Real)]
        [TestCase(  
            @"fibrec(n, iter, p1,p2):int =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(1)",BaseVarType.Int32)]
        [TestCase(@"y = [1..7]
                        .map(i->i+i)
                        .sum()", BaseVarType.Int32)]
        [TestCase(@"y = [1..8]
                        .map(i->[i].sum())
                        .sum()", BaseVarType.Int32)]
        [TestCase(@"y = [1..9]
                        .map(i->[1,i].sum())
                        .sum()", BaseVarType.Int32)]
        [TestCase(@"y = [1..10]
                        .map(i->[1..i].sum())
                        .sum()", BaseVarType.Int32)]
        [TestCase(@"y = [1..11]
                        .map(i->[1..i].sum())
                        .sum()", BaseVarType.Int32)]
        [TestCase(@"y = [1..12]
                        .map(i->[1..i]
                                .map(x->2600/x)
                                .sum())
                        .sum()", BaseVarType.Real)]
        [TestCase(@"y = [1..13]
                        .map(i->[1..10]
                                .map(x->2600/x)
                                .sum())
                        .sum()", BaseVarType.Real)]
        [TestCase(@"y = [1..14]
                        .map(i->i/2)
                        .sum()", BaseVarType.Real)]
        [TestCase(
            @"div10(x) = 2600/x
            y = [1..20].map(div10).sum()", BaseVarType.Real)]
        [TestCase(
            @"div11(x) = 2600/x
            supsum(n) = [1..n].map(div11).sum()
            y = [1..20].map(supsum).sum()", BaseVarType.Real)]
        [TestCase(
            @"div12(x) = 2600/x
            supsum(n) = [1..n].map(div12).sum()
            y = [1..20].map(supsum).sum().round()", BaseVarType.Int32)]
        public void SingleEquation_Runtime_OutputTypeCalculatesCorrect(string expr, BaseVarType type)
        {
            
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(VarType.PrimitiveOf(type), res.Results.First().Type);
        }

        [TestCase(  
            @"someRec(n, iter, p1,p2) =
                          if (n >iter) 
                                someRec(n, iter+1, p1+p2, p1)
                          else 
                                p1+p2  
          y = someRec(9,2,1,1)",BaseVarType.Real)]
        
        [TestCase(  
            @"someRec2(n, iter) =
                          if (n >iter) 
                                someRec2(n, iter+1)
                          else 
                                1
          y = someRec2(9,2)",BaseVarType.Int32)]
   //     [TestCase(  
   //         @"someRec3(n, iter) = someRec3(n, iter+1).strConcat(n >iter)
   //       y = someRec3(9,2)",BaseVarType.Text)]
        
        public void SingleEquations_Parsing_OutputTypesCalculateCorrect(string expr, BaseVarType type)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            Assert.AreEqual(type, runtime.Outputs.Single().Type.BaseType);
        }
        [TestCase( "f(n, iter) = f(n, iter+1).strConcat(n >iter)")]
        [TestCase( "f1(n, iter) = f1(n+1, iter).strConcat(n >iter)")]
        [TestCase( "f2(n, iter) = n > iter and f2(n,iter)")]
        [TestCase( "f3(n, iter) = n > iter and f3(n,iter+1)")]
        [TestCase( "f4(n, iter) = f4(n,iter) and (n > iter)")]
        [TestCase( "f5(n) = f5(n) and n>0")]
        [TestCase( "f6(n) = n>0 and f6(n)")]
        [TestCase( "f7(n) = n>0 and true")]
        [TestCase( "f8(n) = n==0 and f8(n)")]
        [TestCase( "f9(n) = f9(n and true)")]
        [TestCase( "fa(n) = fa(n+1)")]
        [TestCase( "fb(n) = fb(n.strConcat(''))")]
        [TestCase("[a].map(z->z)")]
        [TestCase("[a].filter(f->f>2)")]
        [TestCase("[a].reverse()")]
        [TestCase("[a]")]
        [TestCase("y = [-x].all(i-> i < 0.0)")]
        [TestCase("y = [x,x].all(i-> i < 0.0)")]
        [TestCase("y = [-x,x].all(i-> i < 0.0)")]
        [TestCase("y = [1,-x].all(i-> i < 0.0)")]
        [TestCase("y = [x,2.0,3.0].all((i)-> i >1.0)")]
        [TestCase("y = [1..11].map(i->[1..n].sum())")]
        [TestCase("y = [1..12].map(i->[1..n].sum()).sum()")]
        [TestCase("y = [1..11].map(i->[1..i].sum())")]
        [TestCase("y = [1..12].map(i->[1..i].sum()).sum()")]

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
        [TestCase( "y = [x,-x].all(i-> i < 0.0)")]
        [TestCase( "y = [-x,-x].all(i-> i < 0.0)")]
        [TestCase( "z = [-x,-x,-x] \r  y = z.all((i)-> i < 0.0)")]
        [TestCase( "y = [x, -x]")]
        [TestCase( "y = [-x,-x,-x].all((i)-> i < 0.0)")]
        [TestCase( "[x, -x]")]

        public void EquationTypes_SolvesSomehow(string expr)
        {
            Assert.DoesNotThrow(()=>FunBuilder.BuildDefault(expr));
        }
        
        [TestCase("y = 1\rz=2",BaseVarType.Int32, BaseVarType.Int32)]
        [TestCase("y = 2.0\rz=2",BaseVarType.Real, BaseVarType.Int32)]
        [TestCase("y = true\rz=false",BaseVarType.Bool, BaseVarType.Bool)]

        [TestCase("y = 1\rz=y",BaseVarType.Int32, BaseVarType.Int32)]
        [TestCase("z=2 \r y = z",BaseVarType.Int32, BaseVarType.Int32)]

        [TestCase("z=2 \r y = z/2",BaseVarType.Real, BaseVarType.Int32)]
        [TestCase("y = 2\rz=y/2",BaseVarType.Int32, BaseVarType.Real)]

        [TestCase("y = 2.0\rz=y",BaseVarType.Real, BaseVarType.Real)]
        [TestCase("z=2.0 \ry = z",BaseVarType.Real, BaseVarType.Real)]

        [TestCase("y = true\rz=y",BaseVarType.Bool, BaseVarType.Bool)]
        [TestCase("z=true \r y = z",BaseVarType.Bool, BaseVarType.Bool)]

        
        [TestCase("y = 2\r z=y>1",BaseVarType.Int32, BaseVarType.Bool)]
        [TestCase("z=2 \r y = z>1",BaseVarType.Bool, BaseVarType.Int32)]

        [TestCase("y = 2.0\rz=y>1",BaseVarType.Real, BaseVarType.Bool)]
        [TestCase("z=2.0 \r y = z>1",BaseVarType.Bool, BaseVarType.Real)]
        //[TestCase("y = 'hi'\rz=y",BaseVarType.Text, BaseVarType.Text)]
        //[TestCase("y = 'hi'\rz=y.strConcat('lala')",BaseVarType.Text, BaseVarType.Text)]
        //[TestCase("y = true\rz='lala'.strConcat(y)",BaseVarType.Bool, BaseVarType.Text)]

        public void TwinEquations_Runtime_OutputTypesCalculateCorrect(string expr, BaseVarType ytype,BaseVarType ztype)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate();
            var y = res.Get("y");
            Assert.AreEqual(VarType.PrimitiveOf(ytype),y.Type,"y");
            var z = res.Get("z");
            Assert.AreEqual(VarType.PrimitiveOf(ztype),z.Type,"z");
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
        [TestCase("x:real \r y = [1..x][0]")]
        [TestCase("x:real \r y = [x..10][0]")]
        [TestCase("x:real \r y = [1..10][x]")]
        [TestCase("x:real \r y = [1..10][:x]")]
        [TestCase("x:real \r y = [1..10][:x:]")]
        [TestCase("x:real \r y = [1..10][::x]")]
        [TestCase("y = x \r x:real ")]
        [TestCase("z:real \r  y = x+z \r x:real ")]
        [TestCase("y= [1,2,3].fold((x1,x2)->x1+1.5)")]
        [TestCase("a:int \r a=4")]
        [TestCase("a:int a=4")]
        [TestCase("a:real =false")]
        [TestCase("a:real =false")]
        [TestCase("x:bool; a:real =x")]
        public void ObviouslyFailsWithParse(string expr) =>
            Assert.Throws<FunParseException>(
                ()=> FunBuilder.BuildDefault(expr));
        
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
        [TestCase("1", "y= x.strConcat(1)", "11")]        
        [TestCase(true, "x:bool\r y= x and true", true)] 
        
        public void SingleInputTypedEquation(object x,  string expr, object y)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate(Var.New("x", x));
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(y, res.Results.First().Value);
        }

        [TestCase("y= [1,2,3].map(x->x*x)", new[]{1,4,9})] 
        [TestCase("y= [1,2,3].map(x->x)", new[]{1,2,3})] 
        [TestCase("y= [1,2,3].map(x->1)", new[]{1,1,1})] 
        [TestCase("y= [1,2,3].map(x->'hi')", new[]{"hi","hi","hi"})] 
        [TestCase("y= [true,true,false].map(x->'hi')", new[]{"hi","hi","hi"})] 
        [TestCase("y= [1,2,3].filter(x->x>2)", new[]{3})] 
        [TestCase("y= [1,2,3].reduce((x1,x2)->x1+x2)", 6)] 
        [TestCase("y= [1,2,3].reduce((x1,x2)->1)", 1)] 
        [TestCase("y= [1,2,3].reduce((x1,x2)->x1)", 1)] 
        [TestCase("y= [1,2,3].reduce((x1,x2)->x1+1)", 3)] 
        public void ConstantTypedEquation(string expr, object y)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(y, res.Results.First().Value);   
        }


        [TestCase("y(x) = y(x)")]
        [TestCase("y(x):int = y(x)")]
        [TestCase("y(x:int) = y(x)")]
        [TestCase("y(x:int):int = y(x)")]
        [TestCase("y(x) = y(x)+1")]
        [TestCase("y(x:int) = y(x)+1")]
        [TestCase("y(x) = y(x-1)+y(x-2)")]
        [TestCase("fib(x) = if(x<3) 1 else fib(x-1)+fib(x-2)")]
        public void RecFunction_TypeSolved(string expr)
        {
            Assert.DoesNotThrow(()=> FunBuilder.BuildDefault(expr));
        }
        [TestCase("byte",   (byte)1,   BaseVarType.UInt8)]
        [TestCase("uint8",  (byte)1,   BaseVarType.UInt8)]
        [TestCase("uint16", (UInt16)1, BaseVarType.UInt16)]
        [TestCase("uint32", (UInt32)1, BaseVarType.UInt32)]
        [TestCase("uint64", (UInt64)1, BaseVarType.UInt64)]
        [TestCase("int16",  (Int16)1,  BaseVarType.Int16)]
        [TestCase("int",    (int)1,    BaseVarType.Int32)]
        [TestCase("int32",  (int)1,    BaseVarType.Int32)]
        [TestCase("int64",  (long)1,   BaseVarType.Int64)]
        [TestCase("real",  1.0,BaseVarType.Real)]
        [TestCase("bool", true,BaseVarType.Bool)]
        [TestCase("int[]", new []{1,2,3},BaseVarType.ArrayOf)]
        [TestCase("int64[]", new long[]{1,2,3},BaseVarType.ArrayOf)]
        public void OutputEqualsInput(string type, object expected, BaseVarType baseVarType)
        {
            var runtime = FunBuilder.BuildDefault($"x:{type}\r  y = x");
            var res = runtime.Calculate(Var.New("x", expected));
            res.AssertReturns(Var.New("y", expected));
            Assert.AreEqual(baseVarType, res.Get("y").Type.BaseType);
        }
        [Test]
        public void OutputEqualsTextInput()
        {
            var runtime = FunBuilder.BuildDefault($"x:text;  y = x");
            var res = runtime.Calculate(Var.New("x", "1"));
            res.AssertReturns(Var.New("y", "1"));
            Assert.AreEqual(VarType.Text, res.Get("y").Type);
        }
        
        [TestCase("byte", "&", BaseVarType.UInt8)]
        [TestCase("uint8", "&", BaseVarType.UInt8)]
        [TestCase("uint16", "&", BaseVarType.UInt16)]
        [TestCase("uint32", "&", BaseVarType.UInt32)]
        [TestCase("uint64", "&", BaseVarType.UInt64)]
        //[TestCase("int8", "&", BaseVarType.Int8)]
        //[TestCase("int16", "&", BaseVarType.Int16)]
        [TestCase("int", "&", BaseVarType.Int32)]
        [TestCase("int32", "&", BaseVarType.Int32)]
        [TestCase("int64", "&", BaseVarType.Int64)]
        [TestCase("byte", "|", BaseVarType.UInt8)]
        [TestCase("uint8", "|", BaseVarType.UInt8)]
        [TestCase("uint16", "|", BaseVarType.UInt16)]
        [TestCase("uint32", "|", BaseVarType.UInt32)]
        [TestCase("uint64", "|", BaseVarType.UInt64)]
        //[TestCase("int8", "|", BaseVarType.Int8)]
        //[TestCase("int16", "|", BaseVarType.Int16)]
        [TestCase("int", "|", BaseVarType.Int32)]
        [TestCase("int32", "|", BaseVarType.Int32)]
        [TestCase("int64", "|", BaseVarType.Int64)]
        
        [TestCase("byte", "^", BaseVarType.UInt8)]
        [TestCase("uint8", "^", BaseVarType.UInt8)]
        [TestCase("uint16", "^", BaseVarType.UInt16)]
        [TestCase("uint32", "^", BaseVarType.UInt32)]
        [TestCase("uint64", "^", BaseVarType.UInt64)]
        //[TestCase("int8", "^", BaseVarType.Int8)]
        //[TestCase("int16", "^", BaseVarType.Int16)]
        [TestCase("int", "^", BaseVarType.Int32)]
        [TestCase("int32", "^", BaseVarType.Int32)]
        [TestCase("int64", "^", BaseVarType.Int64)]
        
        public void IntegersBitwiseOperatorTest(string inputTypes, string function, BaseVarType expectedOutputType)
        {
            var runtime = FunBuilder.BuildDefault(
                $"a:{inputTypes}; b:{inputTypes}; c=a{function}b;");
            Assert.AreEqual(expectedOutputType, runtime.Outputs.Single().Type.BaseType);
        }
        
        [TestCase("byte",  BaseVarType.UInt8)]
        [TestCase("uint8", BaseVarType.UInt8)]
        [TestCase("uint16", BaseVarType.UInt16)]
        [TestCase("uint32", BaseVarType.UInt32)]
        [TestCase("uint64", BaseVarType.UInt64)]
        //[TestCase("int8",  BaseVarType.Int8)]
        //[TestCase("int16", BaseVarType.Int16)]
        [TestCase("int", BaseVarType.Int32)]
        [TestCase("int32", BaseVarType.Int32)]
        [TestCase("int64", BaseVarType.Int64)]
        
        public void IntegersBitwiseInvertTest(string inputTypes, BaseVarType expectedOutputType)
        {
            var runtime = FunBuilder.BuildDefault(
                $"a:{inputTypes}; b:{inputTypes}; c= ~a");
            Assert.AreEqual(expectedOutputType, runtime.Outputs.Single().Type.BaseType);
        }
        
        [TestCase("int",    BaseVarType.Int32)]
        [TestCase("int32",  BaseVarType.Int32)]
        [TestCase("int64",  BaseVarType.Int64)]
        public void SummOfTwoIntegersTest(string inputTypes, BaseVarType expectedOutputType)
        {
            var runtime = FunBuilder.BuildDefault(
                $"a:{inputTypes}; b:{inputTypes}; y = a + b");
            Assert.AreEqual(expectedOutputType, runtime.Outputs.Single(o => o.Name == "y").Type.BaseType);
        }
        
        [TestCase("int",    BaseVarType.Int32)]
        [TestCase("int32",  BaseVarType.Int32)]
        [TestCase("int64",  BaseVarType.Int64)]
        public void DifferenceOfTwoIntegersTest(string inputTypes, BaseVarType expectedOutputType)
        {
            var runtime = FunBuilder.BuildDefault(
                $"a:{inputTypes}; b:{inputTypes}; y = a - b");
            Assert.AreEqual(expectedOutputType, runtime.Outputs.Single(o => o.Name == "y").Type.BaseType);
        }
        
        [TestCase("int",    BaseVarType.Int32)]
        [TestCase("int32",  BaseVarType.Int32)]
        [TestCase("int64",  BaseVarType.Int64)]
        public void MultiplyOfTwoIntegersTest(string inputTypes, BaseVarType expectedOutputType)
        {
            var runtime = FunBuilder.BuildDefault(
                $"a:{inputTypes}; b:{inputTypes}; y = a * b");
            Assert.AreEqual(expectedOutputType, runtime.Outputs.Single(o => o.Name == "y").Type.BaseType);
        }
        
        [TestCase("int",    BaseVarType.Int32)]
        [TestCase("int32",  BaseVarType.Int32)]
        [TestCase("int64",  BaseVarType.Int64)]
        public void RemainsOfTwoIntegersTest(string inputTypes, BaseVarType expectedOutputType)
        {
            var runtime = FunBuilder.BuildDefault(
                $"a:{inputTypes}; b:{inputTypes}; y = a % b");
            Assert.AreEqual(expectedOutputType, runtime.Outputs.Single(o => o.Name == "y").Type.BaseType);
        }
        
        [TestCase("y:real = 1",  BaseVarType.Real)]
        [TestCase("y:int = 1",  BaseVarType.Int32)]
        [TestCase("y:byte = 1",  BaseVarType.UInt8)]
        [TestCase("x:int; y:real = x",  BaseVarType.Real)]
        public void OutputType_checkOutputTest(string expression,  BaseVarType expectedType){
            var runtime = FunBuilder.BuildDefault(expression);
            Assert.AreEqual(expectedType, runtime.Outputs.Single(o => o.Name == "y").Type.BaseType);
        }
        
        [TestCase("y:real = x+1", "x", BaseVarType.Real)]
        [TestCase("y:real = x", "x", BaseVarType.Real)]
        [TestCase("y:bool = x", "x", BaseVarType.Bool)]
        [TestCase("x:int; y:real = x+a", "a", BaseVarType.Real)]
        public void OutputType_checkInputTest(string expression, string variable, BaseVarType expectedType){
            var runtime = FunBuilder.BuildDefault(expression);
            Assert.AreEqual(expectedType, runtime.Inputs.Single(o => o.Name == variable).Type.BaseType);
        }

        [TestCase("y:int[] = x",new[]{1,2,3},new[]{1,2,3})]
        [TestCase("y:real[] = x",new[]{1.0,2.0,3.0},new[]{1.0,2.0,3.0})]
        [TestCase("z:real[] = x; y = z",new[]{1.0,2.0,3.0},new[]{1.0,2.0,3.0})]
        [TestCase("y:int[] = x.reverse();",new[]{1,2,3},new[]{3,2,1})]
        [TestCase("a:int = 5; y:real = a+x",2.5,7.5)]
        public void OutputType_runtimeTest(string expression, object xValue, object expectedY)
        {
            FunBuilder.BuildDefault(expression)
                .Calculate(Var.New("x", xValue))
                .AssertHas(Var.New("y", expectedY));
        }
        
    }
}