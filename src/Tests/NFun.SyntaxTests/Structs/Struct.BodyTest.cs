using System.Linq;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests.Structs;

public class StructBodyTest {
    [TestCase("y = {a = 1.0}")]
    [TestCase("y = {a = 1.0;}")]
    [TestCase("y = {;a =; 1.0;}")]
    [TestCase("y = {a = 1.0,}")]
    public void SingleFieldStructInitialization(string expr) =>
        expr
            .Calc()
            .AssertReturns("y", new { a = 1.0 });

    [Ignore("Lca calculation")]
    [TestCase("y = [{a =1.0},{id = 31},{id = 42}][0]")]
    public void StructLca(string expr) =>
        expr
            .Calc()
            .AssertReturns("y", new { a = 1.0 });

    [Test]
    public void TwoFieldStructInitialization() =>
        "y = {a = 1.0; b ='vasa'}"
            .Calc()
            .AssertReturns(
                "y",
                new { a = 1.0, b = "vasa" });


    [TestCase("y = {a = 1.0; b ='vasa'; c = 12*5.0}")]
    [TestCase("y = {a = 1.0;; b ='vasa', c = 12*5.0;}")]
    [TestCase("y = {a = 1.0, b ='vasa', c = 12*5.0,}")]
    [TestCase("y = {a = 1.0,; b ='vasa';, c = 12*5.0;,}")]
    [TestCase("y = {a = 1.0;,; b ='vasa';, c = 12*5.0;,}")]
    [TestCase("y = {a = 1.0;;;;,; b ='vasa';, c = 12*5.0;,}")]
    [TestCase("y = {a = 1.0;;,;;; b ='vasa';,c = 12*5.0;;,;;;}")]
    [TestCase("y = {a = 1.0;;;;;;b ='vasa';;c = 12*5.0;;;;;;}")]
    public void ThreeFieldStructInitializationWithCalculation(string expr) =>
        expr
            .Calc()
            .AssertReturns(
                "y", new { a = 1.0, b = "vasa", c = 60.0 });

    [Test]
    public void ConstAccessNested() =>
        "y = { b = 'foo'}.b"
            .Calc()
            .AssertReturns("y", "foo");

    [Test]
    public void ConstAccessNestedComposite() =>
        "y = { b = [1,2,3]}.b[1]"
            .Calc()
            .AssertReturns("y", 2);

    [Test]
    public void ConstAccessDoubleNestedComposite() =>
        "y = { a1 = {b2 = [1,2,3]}}.a1.b2[1]"
            .Calc()
            .AssertReturns("y", 2);


    [Test]
    public void StructInitializationWithCalculationAndNestedStruct() =>
        ("y = {" +
         "  a = true;" +
         "  b = { " +
         "           c=[1.0,2.0,3.0];" +
         "           d=false" +
         "        };" +
         "  c = 12*5.0" +
         "}").Calc()
        .AssertReturns(
            "y", new { a = true, b = new { c = new[] { 1.0, 2.0, 3.0 }, d = false }, c = 60.0 });


    [Test]
    public void SingleFieldAccess() =>
        "y:int = a.age"
            .Build()
            .Calc(
                ("a", new { age = 42 }))
            .AssertReturns("y", 42);

    [Test]
    public void AccessToNestedFieldsWithExplicitTi() =>
        "y:int = {age = 42; name = 'vasa'}.age"
            .Calc()
            .AssertReturns("y", 42);

    [Test]
    public void AccessToNestedRealField() =>
        "y = {age = 42.0; name = 'vasa'}.age"
            .Calc()
            .AssertReturns("y", 42.0);

    [Test]
    public void AccessToNestedIntField() =>
        "y = {age = 42; name = 'vasa'}.age"
            .Calc()
            .AssertReturns("y", 42);

    [Test]
    public void AccessToNestedTextField() =>
        "y = {age = 42; name = 'vasa'}.name"
            .Calc()
            .AssertReturns("y", "vasa");

    [Test]
    public void AccessToNestedFieldsWithExplicitTi2() =>
        "y:any = {age = 42; name = 'vasa'}.name"
            .Calc()
            .AssertReturns("y", (object)"vasa");


    [Test]
    public void TwoFieldsAccess() =>
        "y1:int = a.age; y2:real = a.size"
            .Build()
            .Calc(
                "a",
                new { age = 42, size = 1.1 })
            .AssertReturns(("y1", 42), ("y2", 1.1));

    [Test]
    public void ThreeFieldsAccess() =>
        "agei:int = a.age; sizer = a.size+12.0; name = a.name"
            .Build()
            .Calc(
                "a", new { age = 42, size = 1.1, name = "vasa" })
            .AssertReturns(("agei", 42), ("sizer", 13.1), ("name", "vasa"));

    [Test]
    public void ConstantAccessCreated() =>
        "a = {b = 1; c=2}; y = a.b + a.c".AssertResultHas("y", 3);

    [Test]
    public void NegateIntFieldAccess() =>
        "a = {b = 1}; y = -a.b".AssertResultHas("y", -1);

    [Test]
    public void NegateRealFieldAccess() =>
        "a = {b = 1.0}; y = -a.b".AssertResultHas("y", -1.0);

    [Test]
    public void NegateFieldAccessWithParenthesis() =>
        "a = {b = 1}; y = -(a.b)".AssertResultHas("y", -1);

    [Test]
    public void DoubleNegateIntFieldAccessWithParenthesis() =>
        "a = {b = 1}; y = -(-a.b)".AssertResultHas("y", 1);

    [Test]
    public void DoubleNegateRealFieldAccessWithParenthesis() =>
        "a = {b = 1.0}; y = -(-a.b)".AssertResultHas("y", 1.0);

    [Test]
    public void ArithmFieldAccess() =>
        "a = {b = 1}; y = -1* (a.b)".AssertResultHas("y", -1);

    [Test]
    public void ConcreteArithmFieldAccess() =>
        "a = {b = 1}; y:int = -1* (a.b)".AssertResultHas("y", -1);

    [Test]
    public void ConcreteFieldAccess() =>
        "a = {b = 1}; y:int = a.b".AssertResultHas("y", 1);

    [Test]
    public void VarAccessCreated() =>
        "a = {b = x; c=2}; y = a.b + a.c".Calc("x", 42).AssertResultHas("y", 44);

    [Test]
    public void VarAccessCreatedInverted() =>
        "a = {b = 55; c=x}; y = a.b + a.c".Calc("x", 42).AssertResultHas("y", 97);

    [Test]
    public void VarTwinAccessCreated() =>
        "a = {b = x; c=x}; y = a.b + a.c".Calc("x", 42.0).AssertResultHas("y", 84.0);

    [Test]
    public void ConstantAccessNestedCreated() {
        TraceLog.IsEnabled = true;

        ("first = {b = 24; c=25}; " +
         "second = {d = first; e = first.c; f = 3}; " +
         "y = second.d.b + second.e + second.f")
            .AssertResultHas("y", 52);
    }

    [Test]
    public void ConstantAccessNestedCreatedSimple() {
        TraceLog.IsEnabled = true;
        ("first = {b = 24; c=25}; " +
         "second = {d = first; e = first.c}; " +
         "y = second.d.b + second.e").AssertResultHas("y", 49);
    }


    [Test]
    public void ConstantAccessNestedCreatedSuperSimple() {
        TraceLog.IsEnabled = true;
        ("first = {b = 24}; " +
         "second = {d = 1.0; e = first.b}; " +
         "y = second.e").AssertResultHas("y", 24);
    }

    [Test]
    public void ConstantAccessManyNestedCreatedHellTest() {
        TraceLog.IsEnabled = true;
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "c3 = {cf1_1 = b2.bf2_1; cf2_24 = a1.af1_24}; " +
         "e4 = {ef1 = a1.af1_24; ef2 = b2.bf2_1; ef3 = a1;  ef4_24 = c3.cf2_24}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + c3.cf2_24 + e4.ef4_24").AssertResultHas("y", 73);
    }

    [Test]
    public void ConstantAccessManyNestedCreatedHellTest3() {
        TraceLog.IsEnabled = true;
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "c3 = {cf1_1 = b2.bf2_1; cf2_24 = a1.af1_24}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + c3.cf2_24 + a1.af1_24").AssertResultHas("y", 73);
    }

    [Test]
    public void ConstantAccess_twinComplex() {
        TraceLog.IsEnabled = true;
        ("x1 = {aField = 24;}; " +
         "x2 = {cField = x1.aField}; " +
         "y = x1.aField  + x1.aField").AssertResultHas("y", 48);
    }

    [Test]
    public void ConstantAccessManyNestedCreatedHellTest4() {
        TraceLog.IsEnabled = true;
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1 + a1.af1_24").AssertResultHas("y", 50);
    }

    [Test]
    public void ConstantAccessManyNestedCreatedHellTest5() {
        TraceLog.IsEnabled = true;
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1 + a1.af1_24").AssertResultHas("y", 50);
    }

    [Test]
    public void Constant_TwinAccessToTwinNested() {
        TraceLog.IsEnabled = true;
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "y = a1.af1_24 + b2.bf1.af2_1").AssertResultHas("y", 25);
    }

    [Test]
    public void ConstantAccessManyNestedCreatedHellTest2() {
        TraceLog.IsEnabled = true;
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf2_1 = 1}; " +
         "c3 = {cf1_1 = b2.bf2_1; cf2_24 = 24}; " +
         "e4 = {ef1 = a1.af1_24; ef2 = b2.bf2_1; ef3 = a1;  ef4_24 = c3.cf2_24}; " +
         "y = a1.af1_24 + 1 + c3.cf2_24 + e4.ef4_24").AssertResultHas("y", 73);
    }

    [Test]
    public void ConstantAccess3EquationNested() {
        TraceLog.IsEnabled = true;
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1 + a1.af1_24").AssertResultHas("y", 50);
    }

    [Test]
    public void ConstantAccess3EquationNested3() {
        TraceLog.IsEnabled = true;
        ("a1 = { af1_24 = 24; af2_1=1 }; " +
         "b2 = { bf1 = a1; bf2_1 = a1.af2_1 }; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + a1.af1_24").AssertResultHas("y", 49);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(42)]
    public void ConstantNCountAccessConcrete(int n) {
        TraceLog.IsEnabled = true;
        ("str = {field = 1.0}; " +
         $"y = {string.Join("+", Enumerable.Range(0, n).Select(_ => "str.field"))}")
            .AssertResultHas("y", (double)n);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(42)]
    public void ConstantNCountAccess(int n) {
        TraceLog.IsEnabled = true;
        ("str = {field = 1}; " +
         $"y = {string.Join("+", Enumerable.Range(0, n).Select(_ => "str.field"))}")
            .AssertResultHas("y", n);
    }

    [Test]
    public void ConstantAccess3EquationNested2() {
        TraceLog.IsEnabled = true;
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1")
            .AssertResultHas("y", 26);
    }

    [Test]
    public void VarAccessNestedCreated() =>
        ("a = {b = x; c=x}; " +
         "b = {d = a; e = a.c; f = 3}; " +
         "y = b.d.b + b.e + b.f")
        .Calc("x", 42)
        .AssertResultHas("y", 87);

    [Test]
    public void ConstAccessNestedCreatedComposite() =>
        ("a = {b= [1,2,3]};" +
         "y = a.b[1]")
        .Calc()
        .AssertResultHas("y", 2);

    [Test]
    public void ConstAccessDoubleNestedCreatedComposite() =>
        ("a = {b= [1,2,3]; c = 'vasa'};" +
         "d = {e = 'lala'; f = a};" +
         "y = d.f.b[1]")
        .AssertResultHas("y", 2);

    [Test]
    public void ConstAccessDoubleNestedCreated() => "d = {f = {b= 2.0}}; y = d.f.b".AssertResultHas("y", 2.0);

    [Test]
    public void ConstAccessFourNestedCreated() =>
        ("d = {f1 = {f2= {f3= {f4= 2.0}}}};" +
         "y = d.f1.f2.f3.f4").AssertResultHas("y", 2.0);

    [Test]
    public void ConstAccessFourNested() =>
        "y = { f1 = {f2= {f3= {f4= 2.0}}}}.f1.f2.f3.f4".AssertResultHas("y", 2.0);


    [Test]
    public void ConstAccessFourNestedWithSameFieldNames() =>
        "y = { f = {f= {f= {f= 2.0}}}}.f.f.f.f".AssertResultHas("y", 2.0);

    [Test]
    public void VarAccessNestedCreatedComposite() =>
        "a = { b= [x,2,3]}; y = a.b[0]".Calc("x", 42).AssertResultHas("y", 42);

    [Test]
    public void VarAccessNestedCreatedComposite2() =>
        "a = {b= [x,2,3]};y = -a.b[0]"
            .Calc("x", 42)
            .AssertResultHas("y", -42);

    [Test]
    public void VarAccessNestedCreatedComposite3() =>
        "a = {b= [x,2,3]};y = a.b[0]-1"
            .Calc("x", 42)
            .AssertResultHas("y", 41);

    [Test]
    public void VarAccessDoubleNestedCreatedComposite() =>
        ("a = {b= [x,2,3]; c = 'vasa'};" +
         "d = {e = 'lala'; f = a};" +
         "y = -d.f.b[0]").Calc("x", 42)
        .AssertResultHas("y", -42);

    [Test]
    public void ConstAccessArrayOfStructs() =>
        ("a = [{age = 42; name = 'vasa'}, {age = 21; name = 'peta'}];" +
         "y = a[1].name;")
        .AssertResultHas("y", "peta");


    [Test]
    [Ignore("Syntax collision with pipe forward")]
    public void GenericLambdaInStruct() =>
        "a = {dec = (rule it-1); inc = (rule it+1);}; y = a.inc(1) + a.inc(2) + a.dec(3)"
            .AssertResultHas("y", 7);

    [TestCase("y = {a = 1}; z = y.b")]
    [TestCase("x =  {a = 1}; y = x.b")]
    [TestCase("y = {a = 1}.b")]
    [TestCase("y = {a = y}")]
    [TestCase("y = @a = y}")]
    [TestCase("y = {a = y")]
    [TestCase("y = {a == y}")]
    [TestCase("y = {a != y}")]
    [TestCase("y = { = y}")]
    [TestCase("y = { {= y}")]
    [TestCase("y = {{}")]
    [TestCase("y = {a=b=c}")]
    [TestCase("y = {b = c; a = y}")]
    [TestCase("y = {a = y.a}")]
    [TestCase("y = {a = { b = y}}")]
    [TestCase("y = {a = { b = y.a}}")]
    [TestCase("y = {a = { b = y.a.b}}")]
    [TestCase("y = {a = y}")]
    [TestCase("y = {a = 1,, b=2}")]
    [TestCase("y = {a = 1 b=2}")]
    [TestCase("y = {a = 1; b=2,,}")]
    [TestCase("y = {a = y-1}")]
    [TestCase("y = {a:int = 0}")]
    [TestCase("y = {a:int = 'test'}")]
    [TestCase("y = {a:bool = false}")]
    [TestCase("y = the a = false}")]
    [TestCase("y = the }")]
    [TestCase("y = the ")]
    [TestCase("y = the-{a:bool = false}")]
    [TestCase("y = -{a:bool = false}")]
    [TestCase("y1 = {a = y2}; y2 = {a = y1}")]
    [TestCase("y = {a = 1.0,,}")]
    [TestCase("y = {(a = 1.0)}")]
    [TestCase("y = {a = 1.0()}")]
    [TestCase("y = {a = ()}")]
    [TestCase("y = {a = 1}()")]
    [TestCase("y = (){a = 1}")]
    [TestCase("foo1 = {}; bar = foo1.id")]
    [TestCase("foo2 = {a = 1}; bar = foo2.nonExist")]
    [TestCase("foo3 = {a = 1}; bar = foo3.a.nonExist")]
    [TestCase("f1() = {a = 42}; bar = f1().nonExist")]
    [TestCase("f2() = {a = 42}; bar = f2().a.nonExist")]
    [TestCase("f3() = {a = {id = 42}}; bar = f3().a.nonExist")]
    [TestCase("y = {}; z = y.nonExist")]
    [TestCase("y = {id = 'test'}; z = y.id.nonExist")]
    public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();


   // [Ignore("Bug #68")]
    [TestCase( @"
                foo = {a = {id = 42}}
                baz = foo.b.id
            ")]
    [TestCase( @"
                foo = {a = {id = 42}}
                bar = foo
                baz = bar.b.id
            ")]
    [TestCase( @"foo.nonExist")]
    [TestCase( @"foo.nonExist.nonExist")]
    [TestCase( @"foo.b.nonExist")]
    [TestCase( @"foo.b.nonExist.nonExist")]
    [TestCase( @"bar1 = foo; x = bar1.nonExist")]
    [TestCase( @"bar2 = foo; x = bar2.nonExist.nonExist")]
    [TestCase( @"bar3 = foo; x = bar3.b.nonExist")]
    [TestCase( @"bar4 = foo; x = bar4.b.nonExist.nonExist")]
    [TestCase( @"baz5 = foo; bar = baz5; x = bar.nonExist")]
    [TestCase( @"baz6 = foo; bar = baz6; x = bar.nonExist.nonExist")]
    [TestCase( @"baz7 = foo; bar = baz7; x = bar.b.nonExist")]
    [TestCase( @"baz8 = foo; bar = baz8; x = bar.b.nonExist.nonExist")]
    [TestCase( @"baz9 = foo; bar = baz9.b; x = bar.nonExist")]
    [TestCase( @"baz10 = foo; bar = baz10.b; x = bar.nonExist.nonExist")]
    public void ObviousFailsWithAprioriComplexModel(string expression) =>
        FunnyAssert.ObviousFailsOnParse(() => {
            Funny.Hardcore.WithApriori<ComplexModel>("foo").Build(expression);
        });

    [TestCase(@"foo.nonExist")]
    [TestCase(@"foo.nonExist.nonExist")]
    public void ObviousFailsWithAprioriNonStructModel(string expression) =>
        FunnyAssert.ObviousFailsOnParse(() => {
            Funny.Hardcore.WithApriori<int>("foo").Build(expression);
        });
}
