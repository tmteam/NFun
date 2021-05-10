using System.Linq;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests.Structs
{
    public class StructBodyTest
    {
        [Test]
        public void SingleFieldStructInitialization() =>
            "y = @{a = 1.0}"
                .Calc()
                .OLD_AssertReturns(VarVal.NewStruct("y", new{a =1.0}));

        [Test]
        public void TwoFieldStructInitialization() =>
            "y = @{a = 1.0; b ='vasa'}"
                .Calc()
                .OLD_AssertReturns(VarVal.NewStruct("y",
                    new {a = 1.0, b = "vasa"}));


        [Test]
        public void ThreeFieldStructInitializationWithCalculation() =>
            "y = @{a = 1.0; b ='vasa'; c = 12*5.0}"
                .Calc()
                .OLD_AssertReturns(VarVal.NewStruct("y", new
                    {
                        a = 1.0,
                        b = "vasa",
                        c = 60.0
                    }));

        [Test]
        public void ConstAccessNested() =>
            "y = @{ b = 'foo'}.b"
                .Calc()
                .OLD_AssertReturns(VarVal.New("y", "foo"));
        
        [Test]
        public void ConstAccessNestedComposite() =>
            "y = @{ b = [1,2,3]}.b[1]"
                .Calc()
                .OLD_AssertReturns(VarVal.New("y", 2.0));
        
        [Test]
        public void ConstAccessDoubleNestedComposite() =>
            "y = @{ a1 = @{b2 = [1,2,3]}}.a1.b2[1]"
                .Calc()
                .OLD_AssertReturns(VarVal.New("y", 2.0));
        

        [Test]
        public void StructInitializationWithCalculationAndNestedStruct() =>
           ( "y = @{" +
                       "  a = true;" +
                       "  b = @{ " +
                       "           c=[1.0,2.0,3.0];" +
                       "           d=false" +
                       "        }" +
                       "  c = 12*5.0" +
                       "}").Calc()
                .OLD_AssertReturns(VarVal.NewStruct("y", new
                {
                    a = true,
                    b = new
                    {
                        c = new []{1.0,2.0,3.0},
                        d = false
                        
                    },
                    c = 60.0
                }));
                
        
        
        [Test]
        public void SingleFieldAccess() =>
            "y:int = a.age"
                .Build()
                .Calculate(VarVal.New("a",FunnyStruct.Create("age",42),VarType.StructOf("age",VarType.Int32)))
                .OLD_AssertReturns(VarVal.New("y",42));
        
        [Test]
        public void AccessToNestedFieldsWithExplicitTi() =>
            "y:int = @{age = 42; name = 'vasa'}.age"
                .Calc()
                .OLD_AssertReturns(VarVal.New("y",42));
        [Test]
        public void AccessToNestedFields() =>
            "y = @{age = 42; name = 'vasa'}.age"
                .Calc()
                .OLD_AssertReturns(VarVal.New("y",42.0));
        [Test]
        public void AccessToNestedFields2() =>
            "y = @{age = 42; name = 'vasa'}.name"
                .Calc()
                .OLD_AssertReturns(VarVal.New("y","vasa"));
        
        [Test]
        public void AccessToNestedFieldsWithExplicitTi2() =>
            "y:anything = @{age = 42; name = 'vasa'}.name"
                .Calc()
                .OLD_AssertReturns(VarVal.New("y",(object)"vasa"));
        
        
        [Test]
        public void TwoFieldsAccess()
        {
            var result = "y1:int = a.age; y2:real = a.size"
                .Build()
                .Calculate(VarVal.NewStruct("a", 
                    new
                    {
                        age = 42,
                        size = 1.1
                    }));
            result.OLD_AssertHas(VarVal.New("y1", 42));
            result.OLD_AssertHas(VarVal.New("y2", 1.1));
        }
        
        [Test]
        public void ThreeFieldsAccess()
        {
            var result = "agei:int = a.age; sizer = a.size+12.0; name = a.name"
                .Build()
                .Calculate(VarVal.NewStruct("a", new
                {
                    age = 42,
                    size = 1.1,
                    name = "vasa"
                }));
            result.OLD_AssertHas(VarVal.New("agei", 42));
            result.OLD_AssertHas(VarVal.New("sizer", 13.1));
            result.OLD_AssertHas(new VarVal("name", "vasa", VarType.Anything));
        }

        [Test]
        public void ConstantAccessCreated() =>
            "a = @{b = 1; c=2}; y = a.b + a.c".AssertHas("y", 3.0);

        [Test]
        public void NegateFieldAccess() =>
            "a = @{b = 1}; y = -a.b".AssertHas("y", -1.0);

        [Test]
        public void NegateFieldAccessWithBrackets() =>
            "a = @{b = 1}; y = -(a.b)".AssertHas("y",-1.0);

        [Test]
        public void DoubleNegateFieldAccessWithBrackets() =>
            "a = @{b = 1}; y = -(-a.b)".AssertHas("y", 1.0);
        
        [Test]
        public void ArithmFieldAccess() =>
            "a = @{b = 1}; y = -1* (a.b)".AssertHas("y",-1.0);

        [Test]
        public void ConcreteArithmFieldAccess() =>
            "a = @{b = 1}; y:int = -1* (a.b)".AssertHas("y", -1);
        
        [Test]
        public void ConcreteFieldAccess() =>
            "a = @{b = 1}; y:int = a.b".AssertHas("y",1);

        [Test]
        public void VarAccessCreated() =>
            "a = @{b = x; c=2}; y = a.b + a.c".Calc("x",42.0).AssertHas("y",44.0);

        [Test]
        public void VarAccessCreatedInverted() =>
            "a = @{b = 55; c=x}; y = a.b + a.c".Calc("x",42.0).AssertHas("y",97.0);
        
        [Test]
        public void VarTwinAccessCreated() =>
            "a = @{b = x; c=x}; y = a.b + a.c".Calc("x",42.0).AssertHas("y",84.0);

        [Test]
        public void ConstantAccessNestedCreated()
        {
            TraceLog.IsEnabled = true;

            ("first = @{b = 24; c=25}; " +
             "second = @{d = first; e = first.c; f = 3}; " +
             "y = second.d.b + second.e + second.f")
                .AssertHas("y", 52.0);
        }

        [Test]
        public void ConstantAccessNestedCreatedSimple()
        {
            TraceLog.IsEnabled = true;
            ("first = @{b = 24; c=25}; " +
             "second = @{d = first; e = first.c}; " +
             "y = second.d.b + second.e").AssertHas("y", 49.0);
        }


        [Test]
        public void ConstantAccessNestedCreatedSuperSimple()
        {
            TraceLog.IsEnabled = true;
            ("first = @{b = 24}; " +
             "second = @{d = 1.0; e = first.b}; " +
             "y = second.e").AssertHas("y", 24.0);
        }

        [Test]
        public void ConstantAccessManyNestedCreatedHellTest()
        {
            TraceLog.IsEnabled = true;
            ("a1 = @{af1_24 = 24; af2_1=1}; " +
             "b2 = @{bf1 = a1; bf2_1 = a1.af2_1}; " +
             "c3 = @{cf1_1 = b2.bf2_1; cf2_24 = a1.af1_24}; " +
             "e4 = @{ef1 = a1.af1_24; ef2 = b2.bf2_1; ef3 = a1;  ef4_24 = c3.cf2_24}; " +
             "y = a1.af1_24 + b2.bf1.af2_1 + c3.cf2_24 + e4.ef4_24").AssertHas("y", 73.0);
        }

        [Test]
        public void ConstantAccessManyNestedCreatedHellTest3()
        {
            TraceLog.IsEnabled = true;
            ("a1 = @{af1_24 = 24; af2_1=1}; " +
             "b2 = @{bf1 = a1; bf2_1 = a1.af2_1}; " +
             "c3 = @{cf1_1 = b2.bf2_1; cf2_24 = a1.af1_24}; " +
             "y = a1.af1_24 + b2.bf1.af2_1 + c3.cf2_24 + a1.af1_24").AssertHas("y", 73.0);
        }

        [Test]
        public void ConstantAccess_twinComplex()
        {
            TraceLog.IsEnabled = true;
            ("x1 = @{aField = 24;}; " +
             "x2 = @{cField = x1.aField}; " +
             "y = x1.aField  + x1.aField").AssertHas("y", 48.0);
        }

        [Test]
        public void ConstantAccessManyNestedCreatedHellTest4()
        {
            TraceLog.IsEnabled = true;
            ("a1 = @{af1_24 = 24; af2_1=1}; " +
             "b2 = @{bf1 = a1; bf2_1 = a1.af2_1}; " +
             "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1 + a1.af1_24").AssertHas("y", 50.0);
        }

        [Test]
        public void ConstantAccessManyNestedCreatedHellTest5()
        {
            TraceLog.IsEnabled = true;
            ("a1 = @{af1_24 = 24; af2_1=1}; " +
             "b2 = @{bf1 = a1; bf2_1 = a1.af2_1}; " +
             "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1 + a1.af1_24").AssertHas("y", 50.0);
        }

        [Test]
        public void Constant_TwinAccessToTwinNested()
        {
            TraceLog.IsEnabled = true;
            ("a1 = @{af1_24 = 24; af2_1=1}; " +
             "b2 = @{bf1 = a1; bf2_1 = a1.af2_1}; " +
             "y = a1.af1_24 + b2.bf1.af2_1").AssertHas("y", 25.0);
        }

        [Test]
        public void ConstantAccessManyNestedCreatedHellTest2()
        {
            TraceLog.IsEnabled = true;
            ("a1 = @{af1_24 = 24; af2_1=1}; " +
             "b2 = @{bf2_1 = 1}; " +
             "c3 = @{cf1_1 = b2.bf2_1; cf2_24 = 24}; " +
             "e4 = @{ef1 = a1.af1_24; ef2 = b2.bf2_1; ef3 = a1;  ef4_24 = c3.cf2_24}; " +
             "y = a1.af1_24 + 1 + c3.cf2_24 + e4.ef4_24").AssertHas("y", 73.0);
        }

        [Test]
        public void ConstantAccess3EquationNested()
        {
            TraceLog.IsEnabled = true;
            ("a1 = @{af1_24 = 24; af2_1=1}; " +
             "b2 = @{bf1 = a1; bf2_1 = a1.af2_1}; " +
             "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1 + a1.af1_24").AssertHas("y", 50.0);
        }

        [Test]
        public void ConstantAccess3EquationNested3()
        {
            TraceLog.IsEnabled = true;
            ("a1 = @{af1_24 = 24; af2_1=1}; " +
             "b2 = @{bf1 = a1; bf2_1 = a1.af2_1}; " +
             "y = a1.af1_24 + b2.bf1.af2_1 + a1.af1_24").AssertHas("y", 49.0);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(42)]
        public void ConstantNCountAccessConcrete(int n)
        {
            TraceLog.IsEnabled = true;
            ("str = @{field = 1.0}; " +
             $"y = {string.Join("+", Enumerable.Range(0, n).Select(_ => "str.field"))}")
                .AssertHas("y", (double) n);

        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(42)]
        public void ConstantNCountAccess(int n)
        {
            TraceLog.IsEnabled = true;
            ("str = @{field = 1}; " +
             $"y = {string.Join("+", Enumerable.Range(0, n).Select(_ => "str.field"))}")
                .AssertHas("y", (double) n);
        }

        [Test]
        public void ConstantAccess3EquationNested2()
        {
            TraceLog.IsEnabled = true;
            ("a1 = @{af1_24 = 24; af2_1=1}; " +
             "b2 = @{bf1 = a1; bf2_1 = a1.af2_1}; " +
             "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1")
                .AssertHas("y", 26.0);
        }

        [Test]
        public void VarAccessNestedCreated() =>
            ("a = @{b = x; c=x}; " +
                       "b = @{d = a; e = a.c; f = 3}; " +
                       "y = b.d.b + b.e + b.f")
                .Calc("x",42.0)
            .AssertHas("y", 87.0);
        
        [Test]
        public void ConstAccessNestedCreatedComposite() =>
            ( "a = @{b= [1,2,3]};" +
                        "y = a.b[1]")
                .Calc()
            .AssertHas("y", 2.0);
        
        [Test]
        public void ConstAccessDoubleNestedCreatedComposite() =>
            ( "a = @{b= [1,2,3]; c = 'vasa'};" +
                        "d = @{e = 'lala'; f = a};" +
                        "y = d.f.b[1]")
                .AssertHas("y", 2.0);
        
        [Test]
        public void ConstAccessDoubleNestedCreated() => "d = @{f = @{b= 2.0}}; y = d.f.b".AssertHas("y", 2.0);
        [Test]
        public void ConstAccessFourNestedCreated() =>
            ("d = @{f1 = @{f2= @{f3= @{f4= 2.0}}}};" +
                       "y = d.f1.f2.f3.f4").AssertHas("y", 2.0);

        [Test]
        public void ConstAccessFourNested() =>
            "y = @{f1 = @{f2= @{f3= @{f4= 2.0}}}}.f1.f2.f3.f4".AssertHas("y", 2.0);

        [Test]
        public void VarAccessNestedCreatedComposite() => 
            "a = @{b= [x,2,3]}; y = a.b[0]".Calc("x",42.0).AssertHas("y", 42.0);
        
        [Test]
        public void VarAccessNestedCreatedComposite2() =>
            "a = @{b= [x,2,3]};y = -a.b[0]"
                .Calc("x",42.0)
                .AssertHas("y", -42.0);
        [Test]
        public void VarAccessNestedCreatedComposite3() =>
             "a = @{b= [x,2,3]};y = a.b[0]-1"
                .Calc("x",42.0)
                .AssertHas("y", 41.0);
        
        [Test]
        public void VarAccessDoubleNestedCreatedComposite() =>
            ( "a = @{b= [x,2,3]; c = 'vasa'};" +
                        "d = @{e = 'lala'; f = a};" +
                        "y = -d.f.b[0]").Calc("x",42.0).AssertHas("y", -42.0);
        
        [Test]
        public void ConstAccessArrayOfStructs() =>
            ("a = [@{age = 42; name = 'vasa'}, @{age = 21; name = 'peta'}];" +
                        "y = a[1].name;")
            .AssertHas("y","peta");
      
        
        [Test]
        [Ignore("Syntax collision with pipe forward")]
        public void GenericLambdaInStruct() =>
            "a = @{dec = {it-1}; inc = {it+1};};y = a.inc(1) + a.inc(2) + a.dec(3)"
                .AssertHas("y",7.0);

        [TestCase("y = @{a = 1}; z = y.b")]
        [TestCase("x =  @{a = 1}; y = x.b")]
        [TestCase("y = @{a = 1}.b")]
        [TestCase("y = @{a = y}")]
        [TestCase("y = @a = y}")]
        [TestCase("y = @{a = y")]
        [TestCase("y = @{a == y}")]
        [TestCase("y = @{a != y}")]
        [TestCase("y = @{ = y}")]
        [TestCase("y = @{ {= y}")]
        [TestCase("y = @{{}")]
        [TestCase("y = @{a=b=c}")]
        [TestCase("y = @{b = c; a = y}")]
        [TestCase("y = @{a = y.a}")]
        [TestCase("y = @{a = @{ b = y}}")]
        [TestCase("y = @{a = @{ b = y.a}}")]
        [TestCase("y = @{a = @{ b = y.a.b}}")]
        [TestCase("y = @{a = y}")]
        [TestCase("y = @{a = y-1}")]
        [TestCase("y = @{a:int = 0}")]
        [TestCase("y = @{a:int = 'test'}")]
        [TestCase("y = @{a:bool = false}")]

        [TestCase("y1 = @{a = y2}; y2 = @{a = y1}")]
        public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();
    }
}