using System.Linq;
using NFun.Exceptions;
using NFun.TestTools;
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



    [Test]
    public void TwoFieldStructInitialization() =>
        "y = {a = 1.0; b ='vasa'}"
            .Calc()
            .AssertReturns(
                "y",
                new { a = 1.0, b = "vasa" });

    [Test]
    public void StructField_OfInt8_PreservesType() {
        var rt = Funny.Hardcore.Build("v:int8=5\r p={x=v}\r out=p.x");
        rt.Run();
        Assert.AreEqual("Int8", rt["out"].Type.ToString());
        Assert.AreEqual((sbyte)5, rt["out"].Value);
    }

    [Test]
    public void StructField_OfByte_PreservesType() {
        var rt = Funny.Hardcore.Build("v:byte=5\r p={x=v}\r out=p.x");
        rt.Run();
        Assert.AreEqual("UInt8", rt["out"].Type.ToString());
        Assert.AreEqual((byte)5, rt["out"].Value);
    }

    [Test]
    public void StructField_OfFloat32_PreservesType() {
        var rt = Funny.Hardcore
            .WithDialect(floatFamilySupport: FloatFamilySupport.Float32AndFloat64)
            .Build("v:float32=1.5\r p={x=v}\r out=p.x");
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }


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

        ("first = {b = 24; c=25}; " +
         "second = {d = first; e = first.c; f = 3}; " +
         "y = second.d.b + second.e + second.f")
            .AssertResultHas("y", 52);
    }

    [Test]
    public void ConstantAccessNestedCreatedSimple() {
        ("first = {b = 24; c=25}; " +
         "second = {d = first; e = first.c}; " +
         "y = second.d.b + second.e").AssertResultHas("y", 49);
    }


    [Test]
    public void ConstantAccessNestedCreatedSuperSimple() {
        ("first = {b = 24}; " +
         "second = {d = 1.0; e = first.b}; " +
         "y = second.e").AssertResultHas("y", 24);
    }

    [Test]
    public void ConstantAccessManyNestedCreatedHellTest() {
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "c3 = {cf1_1 = b2.bf2_1; cf2_24 = a1.af1_24}; " +
         "e4 = {ef1 = a1.af1_24; ef2 = b2.bf2_1; ef3 = a1;  ef4_24 = c3.cf2_24}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + c3.cf2_24 + e4.ef4_24").AssertResultHas("y", 73);
    }

    [Test]
    public void ConstantAccessManyNestedCreatedHellTest3() {
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "c3 = {cf1_1 = b2.bf2_1; cf2_24 = a1.af1_24}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + c3.cf2_24 + a1.af1_24").AssertResultHas("y", 73);
    }

    [Test]
    public void ConstantAccess_twinComplex() {
        ("x1 = {aField = 24;}; " +
         "x2 = {cField = x1.aField}; " +
         "y = x1.aField  + x1.aField").AssertResultHas("y", 48);
    }

    [Test]
    public void ConstantAccessManyNestedCreatedHellTest4() {
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1 + a1.af1_24").AssertResultHas("y", 50);
    }

    [Test]
    public void ConstantAccessManyNestedCreatedHellTest5() {
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1 + a1.af1_24").AssertResultHas("y", 50);
    }

    [Test]
    public void Constant_TwinAccessToTwinNested() {
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "y = a1.af1_24 + b2.bf1.af2_1").AssertResultHas("y", 25);
    }

    [Test]
    public void ConstantAccessManyNestedCreatedHellTest2() {
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf2_1 = 1}; " +
         "c3 = {cf1_1 = b2.bf2_1; cf2_24 = 24}; " +
         "e4 = {ef1 = a1.af1_24; ef2 = b2.bf2_1; ef3 = a1;  ef4_24 = c3.cf2_24}; " +
         "y = a1.af1_24 + 1 + c3.cf2_24 + e4.ef4_24").AssertResultHas("y", 73);
    }

    [Test]
    public void ConstantAccess3EquationNested() {
        ("a1 = {af1_24 = 24; af2_1=1}; " +
         "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1 + a1.af1_24").AssertResultHas("y", 50);
    }

    [Test]
    public void ConstantAccess3EquationNested3() {
        ("a1 = { af1_24 = 24; af2_1=1 }; " +
         "b2 = { bf1 = a1; bf2_1 = a1.af2_1 }; " +
         "y = a1.af1_24 + b2.bf1.af2_1 + a1.af1_24").AssertResultHas("y", 49);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(42)]
    public void ConstantNCountAccessConcrete(int n) {
        ("str = {field = 1.0}; " +
         $"y = {string.Join("+", Enumerable.Range(0, n).Select(_ => "str.field"))}")
            .AssertResultHas("y", (double)n);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(42)]
    public void ConstantNCountAccess(int n) {
        ("str = {field = 1}; " +
         $"y = {string.Join("+", Enumerable.Range(0, n).Select(_ => "str.field"))}")
            .AssertResultHas("y", n);
    }

    [Test]
    public void ConstantAccess3EquationNested2() {
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
    public void ConstAccessDoubleNestedCreatedComposite2() =>
        ("a = {b= [1]};" +
         "d = {f = a};" +
         "y = d.f.b[0]")
            .AssertResultHas("y", 1);

    [Test]
    public void ConstAccessDoubleNestedCreatedComposite3() =>
        ("d = {f = {b= [1]}};" +
         "y = d.f.b[0]")
            .AssertResultHas("y", 1);

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
    public void GenericLambdaInStruct() =>
        "a = {dec = (rule it-1); inc = (rule it+1);}; y = a.inc(1) + a.inc(2) + a.dec(3)"
            .AssertResultHas("y", 7);

    [Test]
    public void ComplexOutputType() {
        var runtime =
            @"
foo = [[[
    {
        y = [[
                    {
                        x = [
                            {a = {id = 1}, b = {id = 2}},
                            {a = {id = 3}, b = {id = 4}}
                        ],
                        y = {a = {id = 5}, b = {id = 6}}
                    },
                    {
                        x = [
                            {a = {id = 7}, b = {id = 8}},
                            {a = {id = 9}, b = {id = 10}}
                        ],
                        y = {a = {id = 11}, b = {id = 12}}
                    }
                ],
                []
        ]
        x= [
            { x = default, y = {a = {id = 13}, b = {id = 14}} },
            { x = [{a = {id = 15}, b = {id = 16}}], y = default }
        ]
        }]]]; bar = foo;".Build();
        runtime.Run();
        var bar = runtime["bar"].CreateGetterOf<SuperPuperComplexModel[][][]>()();
        FunnyAssert.AreSame(expected:
            new[] {
                new[] {
                    new[] {
                        new SuperPuperComplexModel {
                            y = new[] {
                                new[] {
                                    new SuperComplexModel {
                                        x = new[] {
                                            new ComplexModel {
                                                a = new ModelWithInt { id = 1 }, b = new ModelWithInt { id = 2 }
                                            },
                                                new ComplexModel {
                                                a = new ModelWithInt { id = 3 }, b = new ModelWithInt { id = 4 }
                                            }
                                        },
                                        y = new ComplexModel {
                                            a = new ModelWithInt { id = 5 }, b = new ModelWithInt { id = 6 }
                                        }
                                    },
                                        new SuperComplexModel {
                                        x = new[] {
                                            new ComplexModel {
                                                a = new ModelWithInt { id = 7 }, b = new ModelWithInt { id = 8 }
                                            },
                                                new ComplexModel {
                                                a = new ModelWithInt { id = 9 }, b = new ModelWithInt { id = 10 }
                                            }
                                        },
                                        y = new ComplexModel {
                                            a = new ModelWithInt { id = 11 }, b = new ModelWithInt { id = 12 }
                                        }
                                    }
                                },
                                    new SuperComplexModel[0]
                            },
                            x = new[] {
                                new SuperComplexModel {
                                    x = new ComplexModel[0],
                                    y = new ComplexModel {
                                        a = new ModelWithInt { id = 13 }, b = new ModelWithInt { id = 14 }
                                    },
                                },
                                    new SuperComplexModel {
                                    x = new[] {
                                        new ComplexModel {
                                            a = new ModelWithInt { id = 15 }, b = new ModelWithInt { id = 16 }
                                        }
                                    },
                                    y = new ComplexModel {
                                        a = new ModelWithInt { id = 0 }, b = new ModelWithInt { id = 0 }
                                    }
                                }
                            },
                        }
                    }
                }
            },
            actual: bar);
    }


    [Test]
    public void ComplexOutputTypeConstruction() {
             var runtime =
            @"
yx1a = {id = 1}
yx1b = {id = 2}
yx1 = {a = yx1a, b = yx1b}
yx2a = {id = 3}
yx2b = {id = 4}
yx2 = {a = yx2a, b = yx2b}
yy1a = {id = 5}
yy1b = {id = 6}
yy1 = {a = yy1a, b = yy1b}
y2x1a = {id = 7}
y2x1b = {id = 8}
y2x1 = {a = y2x1a, b = y2x1b}
y2x2a = {id = 9}
y2x2b = {id = 10}
y2x2 = {a = y2x2a, b = y2x2b}
y2ya = {id = 11}
y2yb = {id = 12}
y2y = {a = y2ya, b = y2yb}
y1 = { x = [ yx1,yx2 ], y = yy1 }
y2 = { x = [ y2x1, y2x2 ], y = y2y }
x1ya = {id = 13}
x1yb = {id = 14}
x1y = {a = x1ya, b = x1yb}
x1 ={ x = default, y = x1y }
x2xa = {id = 15}
x2xb = {id = 16}
x2x = {a = x2xa, b = x2xb}
x2 = { x = [x2x], y = default }
fooinY = [[ y1, y2 ], []]
fooinX= [ x1,x2 ]

fooin = {y = fooinY, x = fooinX }

foo = [[[fooin]]]; bar = foo;".Build();
        runtime.Run();
        var bar = runtime["bar"].CreateGetterOf<SuperPuperComplexModel[][][]>()();
        FunnyAssert.AreSame(expected:
            new[] {
                new[] {
                    new[] {
                        new SuperPuperComplexModel {
                            y = new[] {
                                new[] {
                                    new SuperComplexModel {
                                        x = new[] {
                                            new ComplexModel {
                                                a = new ModelWithInt { id = 1 }, b = new ModelWithInt { id = 2 }
                                            },
                                                new ComplexModel {
                                                a = new ModelWithInt { id = 3 }, b = new ModelWithInt { id = 4 }
                                            }
                                        },
                                        y = new ComplexModel {
                                            a = new ModelWithInt { id = 5 }, b = new ModelWithInt { id = 6 }
                                        }
                                    },
                                        new SuperComplexModel {
                                        x = new[] {
                                            new ComplexModel {
                                                a = new ModelWithInt { id = 7 }, b = new ModelWithInt { id = 8 }
                                            },
                                                new ComplexModel {
                                                a = new ModelWithInt { id = 9 }, b = new ModelWithInt { id = 10 }
                                            }
                                        },
                                        y = new ComplexModel {
                                            a = new ModelWithInt { id = 11 }, b = new ModelWithInt { id = 12 }
                                        }
                                    }
                                },
                                    new SuperComplexModel[0]
                            },
                            x = new[] {
                                new SuperComplexModel {
                                    x = new ComplexModel[0],
                                    y = new ComplexModel {
                                        a = new ModelWithInt { id = 13 }, b = new ModelWithInt { id = 14 }
                                    },
                                },
                                    new SuperComplexModel {
                                    x = new[] {
                                        new ComplexModel {
                                            a = new ModelWithInt { id = 15 }, b = new ModelWithInt { id = 16 }
                                        }
                                    },
                                    y = new ComplexModel {
                                        a = new ModelWithInt { id = 0 }, b = new ModelWithInt { id = 0 }
                                    }
                                }
                            },
                        }
                    }
                }
            },
            actual: bar);
    }

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

    [Test]
    public void MR5Bug3_ArrayOfAnonStructWithFnField_FU719() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type s = {f:rule(int)->int}\rarr:s[] = [{f=rule it*2}, {f=rule it*3}]"));
    }

    [Test]
    public void MR5Bug3_ArrayOfAnonStructWithFnField_ThreeElements() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type s = {f:rule(int)->int}\rarr:s[] = [{f=rule it*2}, {f=rule it*3}, {f=rule it*4}]"));
    }

    [Test]
    public void MR5Bug6_NarrowStructAnnotation_RejectsExtraFieldAccess() {
        // Width subtyping (Specs/Types.md L90-91): `{x,y,z}` IS convertible to `{x,y}`.
        // Assignment succeeds, but accessing a field outside the declared annotation
        // must be rejected at compile time.
        Assert.Throws<FunnyParseException>(() =>
            "a = {x=1, y=2, z=3}\rb:{x:int, y:int} = a\rout = b.z".Calc());
    }

    [Test]
    public void MR5Bug6_NarrowStructAnnotation_LegitNarrowingWorks() {
        "a = {x=1, y=2, z=3}\rb:{x:int, y:int} = a\rout = b.x"
            .Calc().AssertResultHas("out", 1);
    }

    [Test]
    public void MR9Bug2_DuplicateFieldInStructLiteral_ClearError() {
        var ex = Assert.Throws<FunnyParseException>(() => "y = {a=1, a=2}".Calc());
        StringAssert.Contains("duplicate", ex.Message.ToLowerInvariant());
        StringAssert.Contains("'a'", ex.Message);
    }

    [Test]
    public void MR9Bug2_DuplicateFieldCaseInsensitive_ClearError() {
        var ex = Assert.Throws<FunnyParseException>(() => "y = {a=1, A=2}".Calc());
        StringAssert.Contains("duplicate", ex.Message.ToLowerInvariant());
    }

    [Test]
    public void MR9Bug2_Control_DistinctFields_Work() {
        "y = {a=1, b=2}".Calc();
    }

    #region FloatFamily dialect

    [Test]
    public void Float32_Struct_FieldConstruction_ExplicitAnnotation() {
        var rt = "v:float32=1.5\r p={x=v}\r out=p.x".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Struct_TypedFunctionParam_AccessField() {
        var rt = "f(p:{x:float32}):float32 = p.x\r v:float32=1.5\r out = f({x=v})".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Struct_NestedStruct_WithF32Field() {
        var rt = "v:float32=1.5\r p={a={b=v}}\r out=p.a.b".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Struct_TwoF32Fields_Sum() {
        var rt = "a:float32=1.5\r b:float32=2.5\r p={a=a,b=b}\r out=p.a+p.b".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(4.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Struct_IfElse_FieldLca_NarrowToF32() {
        var rt = "c:bool; out:{a:float32} = if(c) {a=1.0} else {a=2.0}".BuildWithFloats();
        rt["c"].Value = true;
        rt.Run();
        Assert.AreEqual(1.0f, ((System.Collections.Generic.IDictionary<string, object>)rt["out"].Value)["a"]);
    }

    [Test]
    public void Float32_Struct_IfElse_MixedF32AndInt_TargetF32() {
        var rt = "c:bool; x:float32=1.5; out:{a:float32} = if(c) {a=x} else {a=2}".BuildWithFloats();
        rt["c"].Value = false;
        rt.Run();
        Assert.AreEqual(2.0f, ((System.Collections.Generic.IDictionary<string, object>)rt["out"].Value)["a"]);
    }

    [Test]
    public void Float32_Struct_Equality_TwoF32Structs() {
        var rt = "a:float32=1.5; b:float32=1.5; out = {x=a} == {x=b}".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Float32_Struct_Inequality_TwoDifferentF32Structs() {
        var rt = "a:float32=1.5; b:float32=2.5; out = {x=a} == {x=b}".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Float32_StructArray_MapAccessF32Field() {
        var rt = "arr:{x:float32}[] = [{x=1.5},{x=2.5}]\r out = arr.map(rule it.x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(new[] { 1.5f, 2.5f }, rt["out"].Value);
    }

    [Test]
    public void Float32_Struct_RealLiteral_TargetF32Field_Narrows() {
        var rt = "out:{x:float32} = {x=1.5}".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.5f, ((System.Collections.Generic.IDictionary<string, object>)rt["out"].Value)["x"]);
    }

    [Test]
    public void Float32_Struct_MixedFieldTypes_PreserveEach() {
        var rt = "v:float32=3.14\r p={pi=v, count=5, name='pi'}\r a=p.pi\r b=p.count\r c=p.name".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["a"].Type.ToString());
        Assert.AreEqual(3.14f, rt["a"].Value);
        Assert.AreEqual(5, rt["b"].Value);
        Assert.AreEqual("pi", rt["c"].Value);
    }

    [Test]
    public void Float32_Struct_ThreeLevelNesting_F32Access() {
        var rt = "v:float32=42.0\r p={a={b={c=v}}}\r out=p.a.b.c".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(42.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Struct_PassAndReturn_PreserveF32Field() {
        var rt = "identity(s:{x:float32}):{x:float32} = s\r v:float32=1.5\r p={x=v}\r q=identity(p)\r out=q.x".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }
    #endregion

    [Test]
    public void StructArrayFieldArith_WidensToReal() =>
        "data:{x:int,y:real}[]=[{x=1,y=2.5}]\rout = data[0].y + 1"
            .Calc().AssertResultHas("out", 3.5);
}
