using System;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests;

[TestFixture]
public class OptionalTypesApiTest {

    // ═══════════════════════════════════════════════════════════════
    // Variable types: Optional type metadata
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void OptionalVariable_TypeIsOptional() =>
        "x:int? = none".AssertRuntimes(r => {
            var x = r["x"];
            Assert.AreEqual(FunnyType.OptionalOf(FunnyType.Int32), x.Type);
            Assert.IsTrue(x.IsOutput);
        });

    [Test]
    public void OptionalVariable_WithValue_TypeIsOptional() =>
        "x:int? = 42".AssertRuntimes(r => {
            var x = r["x"];
            Assert.AreEqual(FunnyType.OptionalOf(FunnyType.Int32), x.Type);
        });

    [TestCase("x:int? = none")]
    [TestCase("x:real? = none")]
    [TestCase("x:bool? = none")]
    [TestCase("x:int64? = none")]
    public void OptionalNoneVariable_ValueIsNull(string expr) =>
        expr.AssertRuntimes(r => {
            r.Run();
            Assert.IsNull(r["x"].Value);
        });

    [Test]
    public void OptionalInt_WithValue_ValueIsInt() =>
        "x:int? = 42".AssertRuntimes(r => {
            r.Run();
            Assert.AreEqual(42, r["x"].Value);
        });

    [Test]
    public void OptionalReal_WithValue_ValueIsDouble() =>
        "x:real? = 3.14".AssertRuntimes(r => {
            r.Run();
            var val = r["x"].Value;
            Assert.IsInstanceOf<double>(val);
            Assert.AreEqual(3.14, (double)val, 0.001);
        });

    [Test]
    public void OptionalBool_WithValue_ValueIsBool() =>
        "x:bool? = true".AssertRuntimes(r => {
            r.Run();
            Assert.AreEqual(true, r["x"].Value);
        });

    [Test]
    public void OptionalText_WithValue_ValueIsString() =>
        "x:text? = 'hello'".AssertRuntimes(r => {
            r.Run();
            Assert.AreEqual("hello", r["x"].Value);
        });

    // ═══════════════════════════════════════════════════════════════
    // Input: Setting optional input variables
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void OptionalInput_SetValue_RunReturnsValue() {
        var runtime = Funny.Hardcore.Build("x:int?\r y = x ?? 0");
        runtime["x"].Value = 42;
        runtime.Run();
        Assert.AreEqual(42, runtime["y"].Value);
    }

    [Test]
    public void OptionalInput_SetNull_RunReturnsDefault() {
        var runtime = Funny.Hardcore.Build("x:int?\r y = x ?? 0");
        runtime["x"].Value = null;
        runtime.Run();
        Assert.AreEqual(0, runtime["y"].Value);
    }

    [Test]
    public void OptionalInput_DefaultIsNone_CoalesceReturnsDefault() {
        var runtime = Funny.Hardcore.Build("x:int?\r y = x ?? 99");
        // Don't set x — default for int? is none
        runtime.Run();
        Assert.AreEqual(99, runtime["y"].Value);
    }

    [Test]
    public void OptionalRealInput_SetValue_Works() {
        var runtime = Funny.Hardcore.Build("x:real?\r y = x ?? 0.0");
        runtime["x"].Value = 3.14;
        runtime.Run();
        Assert.AreEqual(3.14, (double)runtime["y"].Value, 0.001);
    }

    [Test]
    public void OptionalRealInput_SetNull_ReturnsDefault() {
        var runtime = Funny.Hardcore.Build("x:real?\r y = x ?? 0.0");
        runtime["x"].Value = null;
        runtime.Run();
        Assert.AreEqual(0.0, runtime["y"].Value);
    }

    [Test]
    public void OptionalBoolInput_SetValue_Works() {
        var runtime = Funny.Hardcore.Build("x:bool?\r y = x ?? false");
        runtime["x"].Value = true;
        runtime.Run();
        Assert.AreEqual(true, runtime["y"].Value);
    }

    [Test]
    public void OptionalTextInput_SetValue_Works() {
        var runtime = Funny.Hardcore.Build("x:text?\r y = x ?? 'default'");
        runtime["x"].Value = "hello";
        runtime.Run();
        Assert.AreEqual("hello", runtime["y"].Value);
    }

    [Test]
    public void OptionalTextInput_SetNull_ReturnsDefault() {
        var runtime = Funny.Hardcore.Build("x:text?\r y = x ?? 'default'");
        runtime["x"].Value = null;
        runtime.Run();
        Assert.AreEqual("default", runtime["y"].Value);
    }

    // ═══════════════════════════════════════════════════════════════
    // Output: Optional output variables
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void OptionalOutput_NoneValue_ReturnsNull() {
        var runtime = Funny.Hardcore.Build("y:int? = none");
        runtime.Run();
        Assert.IsNull(runtime["y"].Value);
    }

    [Test]
    public void OptionalOutput_HasValue_ReturnsValue() {
        var runtime = Funny.Hardcore.Build("y:int? = 42");
        runtime.Run();
        Assert.AreEqual(42, runtime["y"].Value);
    }

    [Test]
    public void OptionalOutput_FromInput_PassThrough() {
        var runtime = Funny.Hardcore.Build("x:int?\r y:int? = x");
        runtime["x"].Value = 7;
        runtime.Run();
        Assert.AreEqual(7, runtime["y"].Value);
    }

    [Test]
    public void OptionalOutput_FromInput_NonePassThrough() {
        var runtime = Funny.Hardcore.Build("x:int?\r y:int? = x");
        runtime["x"].Value = null;
        runtime.Run();
        Assert.IsNull(runtime["y"].Value);
    }

    // ═══════════════════════════════════════════════════════════════
    // Multiple runs: re-run with different values
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void OptionalInput_MultipleRuns_ValueChanges() {
        var runtime = Funny.Hardcore.Build("x:int?\r y = x ?? 0");

        runtime["x"].Value = 10;
        runtime.Run();
        Assert.AreEqual(10, runtime["y"].Value);

        runtime["x"].Value = null;
        runtime.Run();
        Assert.AreEqual(0, runtime["y"].Value);

        runtime["x"].Value = 20;
        runtime.Run();
        Assert.AreEqual(20, runtime["y"].Value);
    }

    // ═══════════════════════════════════════════════════════════════
    // Dialect gate: Optional operators require AllowOptionalTypes
    // ═══════════════════════════════════════════════════════════════

    [TestCase("x:int?\r y = x!")]
    [TestCase("x:int?\r y = x ?? 0")]
    [TestCase("y = 42!")]
    [TestCase("y = 42 ?? 0")]
    public void OptionalOperators_WithDialect_FailsOnParse(string expr) =>
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore
                .WithDialect() // creates new settings where AllowOptionalTypes defaults to false
                .Build(expr));

    [TestCase("x:int?\r y = x!")]
    [TestCase("x:int?\r y = x ?? 0")]
    public void OptionalOperators_DefaultDialect_Succeeds(string expr) =>
        Assert.DoesNotThrow(() => Funny.Hardcore.Build(expr));

    // ═══════════════════════════════════════════════════════════════
    // None literal
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NoneLiteral_CanBuild() =>
        Assert.DoesNotThrow(() => Funny.Hardcore.Build("y = none"));

    [Test]
    public void NoneLiteral_AssignedToOptional_ValueIsNull() {
        var runtime = Funny.Hardcore.Build("y:int? = none");
        runtime.Run();
        Assert.IsNull(runtime["y"].Value);
    }

    // ═══════════════════════════════════════════════════════════════
    // Error cases
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SetNull_OnNonOptionalVariable_Throws() {
        var runtime = Funny.Hardcore.Build("x:int\r y = x");
        Assert.Throws<InvalidCastException>(() => runtime["x"].Value = null);
    }
}
