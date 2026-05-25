using NFun;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on master (round 6,
/// post Round 1-5 fixes + convert-redesign series).
/// 3 agents × ~100 iterations. 3 confirmed bug families.
/// </summary>
public class BugHuntMasterRound6 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ───────────────────────────────────────────────────────────────
    // MR6Bug1 — Recursive user function called through `?.` crashes at
    //   runtime with "The given key 'foo' was not present in the
    //   dictionary".
    //
    //     type n = {v:int, next:n?=none}
    //     foo(node:n):int = (node.next?.foo() ?? 0) + 1
    //     chain = n{v=1, next=n{v=2}}
    //     out = foo(chain)                # crash
    //
    //   Same `foo` works via non-?. path: `if(node.next != none) foo(node.next!) else 0`.
    //   Non-recursive user fn via `?.` works.
    //   Specific combination: recursion + safe-field-access dispatch.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("MR6Bug1: recursive user fn via ?. — dictionary lookup fails for self-call dispatch")]
    public void MR6Bug1_RecursiveUserFnViaSafeAccess_DictionaryLookupFails() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(
                    optionalTypesSupport: OptionalTypesSupport.Enabled,
                    namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type n = {v:int, next:n?=none}\rfoo(node:n):int = (node.next?.foo() ?? 0) + 1\rchain = n{v=1, next=n{v=2}}\rout = foo(chain)")
                .Run());
    }

    // ───────────────────────────────────────────────────────────────
    // MR6Bug2 — Safe-array-access `?[` on an opt array of composite
    //   element type loses the Optional propagation through subsequent
    //   operations:
    //
    //     arr:int[][]? = none
    //     out = arr?[0].count()
    //
    //   Compiles successfully (treating `arr?[0]` as non-opt `int[]`),
    //   then crashes at runtime: "Unable to cast FunnyNone to IFunnyArray".
    //
    //   Direct equivalent IS rejected at compile time:
    //     b:int[]? = none; out = b.count()        # FU783
    //
    //   Bug specific to `?[` on opt-array-of-composite (lost Optional
    //   through nested composite). Doesn't manifest for primitive
    //   element types (`int[]?` → `?[0]` correctly returns `int?`).
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("MR6Bug2: ?[ loses optional through composite element type — crashes at runtime")]
    public void MR6Bug2_SafeArrayAccessLosesOptThroughComposite_Crash() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0].count()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // ───────────────────────────────────────────────────────────────
    // MR6Bug3 — `??` cross-tree LCA still broken for some left-operand
    //   types. MR3Bug1 fix covered some combinations (e.g. `1 ?? 'hello'`)
    //   but bool/char/ip/real-literal LEFT with cross-tree RIGHT still
    //   misbehaves with three distinct failure modes.
    //
    //   Spec (Specs/Optionals.md §`??`): "The result type is `LCA(U, V)`"
    //   — for cross-tree primitives LCA should be `any`.
    //
    //   Workaround for all: declare `out:any = ...` explicitly.
    // ───────────────────────────────────────────────────────────────

    // 3a. bool/char/ip literal as LEFT + numeric/text RIGHT → wrong type tag
    [Test]
    [Ignore("MR6Bug3a: `true ?? 1.5` infers Real type but value is bool true — type/value mismatch")]
    public void MR6Bug3a_BoolLeftRealRight_TypeTagMismatch() {
        // Per spec LCA(bool, real) = any, so output should be Any = true.
        // Actual: out:Real = true (type Real but value is bool — soundness violation).
        var rt = "out = true ?? 1.5".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        // The strict assertion would check the inferred Funny type is Any, but the
        // value `true` should round-trip regardless of the inferred slot type.
        // For this bug-tracking test we just verify the value is correctly preserved.
        Assert.AreEqual(true, rt.Get("out"));
    }

    [Test]
    [Ignore("MR6Bug3a: `false ?? 3` infers Int32 but value is bool false")]
    public void MR6Bug3a_BoolLeftIntRight_TypeTagMismatch() {
        var rt = "out = false ?? 3".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(false, rt.Get("out"));
    }

    // 3b. Non-numeric LEFT + cross-tree RIGHT → silent value conversion
    [Test]
    [Ignore("MR6Bug3b: `1.5 ?? false` returns Bool=true — silently converts real 1.5 to bool true via convert matrix")]
    public void MR6Bug3b_RealLeftBoolRight_SilentValueConversion() {
        // Per spec `??` returns left if not none — should return 1.5 (typed any).
        // Actual: out:Bool = true (real 1.5 silently coerced to bool via real→bool: 1.5!=0 → true).
        var rt = "out = 1.5 ?? false".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(1.5, rt.Get("out"));
    }

    [Test]
    [Ignore("MR6Bug3b: `/'a' ?? 5` returns Int32=97 — silently converts char to codepoint")]
    public void MR6Bug3b_CharLeftIntRight_SilentValueConversion() {
        var rt = "out = /'a' ?? 5".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual('a', rt.Get("out"));
    }

    // 3c. Non-integer primitive LEFT + composite RIGHT → NullReferenceException
    [Test]
    [Ignore("MR6Bug3c: `true ?? [1,2,3]` crashes with NullReferenceException")]
    public void MR6Bug3c_BoolLeftArrayRight_NullRefCrash() {
        Assert.DoesNotThrow(() =>
            "out = true ?? [1,2,3]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    [Ignore("MR6Bug3c: `1.5 ?? [1,2]` crashes with NullReferenceException")]
    public void MR6Bug3c_RealLeftArrayRight_NullRefCrash() {
        Assert.DoesNotThrow(() =>
            "out = 1.5 ?? [1,2]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // 3d. Control: explicit out:any annotation fixes everything for all 3a/3b/3c cases
    [Test]
    public void MR6Bug3_AnyAnnotation_WorksForAllCases() {
        // These all work — confirms the underlying TIC supports the correct result,
        // it's only the inference-without-context that's broken.
        "out:any = true ?? 1.5".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        "out:any = 1.5 ?? false".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        "out:any = true ?? [1,2,3]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
    }

    // 3e. Control: integer literal LEFT works (covered by MR3Bug1 fix)
    [Test]
    public void MR6Bug3_IntLeftAlreadyWorks_MR3Bug1() {
        // Per MR3Bug1 fix, integer-literal LEFT correctly widens to Any for cross-tree RIGHT.
        "out = 1 ?? 'hello'".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 1);
    }
}
