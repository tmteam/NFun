using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.ErrorHandling;

[TestFixture]
public class TryCatchWithErrorTest {
    // ── try expr catch(e) expr — access error object ─────────────

    [Test]
    public void TryCatchE_AccessMessage() =>
        "y = try oops('hello') catch(e) e.message".AssertReturns("y", "hello");

    [Test]
    public void TryCatchE_AccessMessage_DefaultEmpty() =>
        "y = try oops() catch(e) e.message".AssertReturns("y", "oops");

    [Test]
    public void TryCatchE_NoError_ReturnsValue() =>
        "y = try 42 catch(e) 0".AssertReturns("y", 42);

    // ── e.data access ────────────────────────────────────────────

    [Test]
    public void TryCatchE_AccessData_Int() =>
        "y = try oops('fail', 42) catch(e) e.data".AssertReturns("y", 42);

    [Test]
    public void TryCatchE_AccessData_None_WhenNotProvided() {
        var r = "y = try oops('fail') catch(e) e.data".BuildWithDialect(
            optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        // data = none when not provided
        Assert.AreEqual(null, r["y"].Value);
    }

    // ── error variable scoping ───────────────────────────────────

    [Test]
    public void TryCatchE_VariableName_Custom() =>
        "y = try oops('test') catch(err) err.message".AssertReturns("y", "test");

    // ── catch(e) with complex fallback ───────────────────────────

    [Test]
    public void TryCatchE_FallbackUsesMessage_Concat() =>
        "y = try oops('bad') catch(e) concat('error: ', e.message)".AssertReturns("y", "error: bad");

    // ── nested try with catch(e) ─────────────────────────────────

    [Test]
    public void TryCatchE_Nested_InnerCatches() =>
        "y = try (try oops('inner') catch(e) e.message) catch(e) 'outer'".AssertReturns("y", "inner");

    [Test]
    public void TryCatchE_Nested_OuterCatches() =>
        "y = try (try oops('inner') catch(e) oops('rethrown')) catch(e) e.message"
            .AssertReturns("y", "rethrown");
}
