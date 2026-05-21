using NFun;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Regression tests for bugs originally found by automated lang-mode
/// hunting that ALSO reproduce in expression mode. Fixed in NFun core
/// (master) so the same fix benefits both surface forms.
/// </summary>
public class CrossModeBugFixesTest {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ───────────────────────────────────────────────────────────────
    // Round 6 #74 — Re-declaring a variable via type annotation
    //   `y:int` after `y = 5` previously crashed with a raw
    //   InvalidOperationException "Sequence contains no matching
    //   element" from SearchUsagesHelper.FindFirstUsageOrThrow. The
    //   helper assumed the existing variable had been USED earlier,
    //   but `y = 5` only DEFINED it. Now raises clean FU879
    //   "Variable is already declared".
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void TypeAnnotationAfterAssignment_ThrowsAlreadyDeclared() {
        var ex = Assert.Throws<FunnyParseException>(() => Funny.Hardcore.Build("y = 5\ny:int"));
        StringAssert.Contains("already declared", ex!.Message);
    }

    [Test]
    public void TypeAnnotationAfterTypedAssignment_ThrowsAlreadyDeclared() {
        var ex = Assert.Throws<FunnyParseException>(() => Funny.Hardcore.Build("y:int = 5\ny:real"));
        StringAssert.Contains("already declared", ex!.Message);
    }

    [Test]
    public void TypeAnnotationAfterCompositeAssignment_ThrowsAlreadyDeclared() {
        var ex = Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("y = [1,2]\ny:int[]"));
        StringAssert.Contains("already declared", ex!.Message);
    }

    // ───────────────────────────────────────────────────────────────
    // Round 6 #83 — Re-annotating a variable with an incompatible
    //   composite type (e.g. `x:int = 5; x:text = 'hi'`) crashed
    //   with TIC assertion "Node is already solved" instead of a
    //   clean parse error. TrySetVarType's composite branch
    //   unconditionally overwrote the node's State.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void Reannotate_PrimitiveThenComposite_ThrowsAlreadyDeclared() {
        var ex = Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("x:int = 5\nx:text = 'hi'\nout = x"));
        StringAssert.Contains("already declared", ex!.Message);
    }

    [Test]
    public void Reannotate_PrimitiveThenArray_ThrowsAlreadyDeclared() {
        var ex = Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("x:int = 5\nx:int[] = [1,2]\nout = x"));
        StringAssert.Contains("already declared", ex!.Message);
    }

    // ───────────────────────────────────────────────────────────────
    // Round 6 #84 — `f(x) = if (x==none) none else x; f(5)` triggered
    //   infinite recursion in TicTypesConverter.BuildNamedTypeFromTicState
    //   → StackOverflowException. The identity-through-none pattern
    //   creates a constraint-state cycle that the recursive type-walk
    //   followed indefinitely. Depth-bounded guard now returns null at
    //   the limit (no named-type content), and the surrounding code
    //   falls back to Any?.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void IdentityThroughNone_GenericParam_DoesNotCrash() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("f(x) = if (x==none) none else x\nout = f(5)"));
    }

    [Test]
    public void IdentityThroughNone_TypedParam_StillWorks() {
        // The typed variant always worked — keep as regression coverage.
        var rt = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build("f(x:int?) = if (x==none) none else x\nout = f(5)");
        rt.Run();
        Assert.AreEqual(5, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // Round 6 #75 — `f(x) = if (x==0) 'a' else x; out = f(5)`
    //   silently coerced int 5 to text '5' via VarTypeConverter.ToText.
    //   TIC's Destruction.Apply(ICompositeState anc, ConstraintsState desc)
    //   had a silent `return true` fall-through when arr(Ch) failed to fit
    //   into descendant CS's interval [U8..Re]. The bogus typing
    //   `(Int)->Char[]` then activated the unconditional ToText converter.
    //   Fix: throw IncompatibleNodes when ancestor composite genuinely
    //   excludes the descendant's upper bound.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MixedReturnTypes_VariableVsLiteral_RejectsAtParse() {
        // The function inference is genuinely impossible: x is constrained
        // to Int by `x==0`, but the if-then branch returns Char[]. No type
        // unifies both. Must be rejected, not silently coerced.
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("f(x) = if (x==0) 'a' else x; out = f(5)"));
    }

    [Test]
    public void MixedReturnTypes_LiteralVsVariableUntypedCondition_StillRejected() {
        // Even without `==0` to pin x to Int, mixing a text literal and a
        // bare param x in different branches is rejected (x has no other
        // constraint to fit into Char[]).
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("f(x) = if (false) 'a' else x; out = f(42)"));
    }
}
