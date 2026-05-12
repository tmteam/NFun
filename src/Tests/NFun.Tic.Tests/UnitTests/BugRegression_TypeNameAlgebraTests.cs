using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

/// <summary>
/// Unit tests for pure pair functions <see cref="StateStruct.LcaTypeName"/>,
/// <see cref="StateStruct.MergedTypeName"/>, <see cref="StateStruct.MergedIsOptionalSourced"/>.
///
/// These tests model the algebraic contracts in isolation — independent of the
/// surrounding solver. They are derived from the same algebraic intent that drove
/// the GH #128 fixes (C/D/E/G/K) and the GH #126 follow-up: the system's behaviour
/// around named-struct identity rests on the correctness and asymmetry of these
/// two name-merging rules.
///
/// Theoretical contract (per StateStruct.cs lines 140-157):
///   MergedTypeName  : "most specific consistent identity"
///                     - one side null → take the other (the named one wins, no conflict)
///                     - both equal (case-insensitive) → that name
///                     - both differ → null (caller must reject or downgrade)
///   LcaTypeName     : "most specific identity COMMON to both"
///                     - both equal → that name
///                     - any other case → null (LCA must not invent a name)
///
/// Asymmetry: Merge is PERMISSIVE (takes the named one when only one side has it);
/// LCA is STRICT (drops to anonymous whenever either side is anonymous). This
/// asymmetry is intentional — Merge represents unification of constraints (we know
/// MORE than either side alone), while LCA represents the supremum (we know LESS).
/// </summary>
class BugRegression_TypeNameAlgebraTests {

    // ─── LcaTypeName: strict identity preservation ───

    [Test]
    public void Lca_BothNull_ReturnsNull() =>
        Assert.IsNull(StateStruct.LcaTypeName(null, null));

    [Test]
    public void Lca_LeftNull_RightNamed_ReturnsNull() =>
        Assert.IsNull(StateStruct.LcaTypeName(null, "t"));

    [Test]
    public void Lca_LeftNamed_RightNull_ReturnsNull() =>
        Assert.IsNull(StateStruct.LcaTypeName("t", null));

    [Test]
    public void Lca_BothSame_ReturnsName() =>
        Assert.AreEqual("t", StateStruct.LcaTypeName("t", "t"));

    [Test]
    public void Lca_BothSameCaseInsensitive_ReturnsFirst() =>
        Assert.AreEqual("Foo", StateStruct.LcaTypeName("Foo", "foo"));

    [Test]
    public void Lca_BothSameAllUppercase() =>
        Assert.AreEqual("FOO", StateStruct.LcaTypeName("FOO", "foo"));

    [Test]
    public void Lca_DifferentNames_ReturnsNull() =>
        Assert.IsNull(StateStruct.LcaTypeName("t", "s"));

    [Test]
    public void Lca_EmptyStrings_AreEqualToThemselves() =>
        Assert.AreEqual("", StateStruct.LcaTypeName("", ""));

    [Test]
    public void Lca_OneEmptyOneNamed_ReturnsNull() =>
        Assert.IsNull(StateStruct.LcaTypeName("", "t"));

    [Test]
    public void Lca_IsSymmetric_BothNamed() =>
        Assert.AreEqual(
            StateStruct.LcaTypeName("t", "s"),
            StateStruct.LcaTypeName("s", "t"));

    [Test]
    public void Lca_IsSymmetric_OneNull() =>
        Assert.AreEqual(
            StateStruct.LcaTypeName("t", null),
            StateStruct.LcaTypeName(null, "t"));

    [Test]
    public void Lca_IsIdempotent() {
        var once = StateStruct.LcaTypeName("t", "t");
        var twice = StateStruct.LcaTypeName(once, once);
        Assert.AreEqual(once, twice);
    }

    // ─── MergedTypeName: permissive identity preservation ───

    [Test]
    public void Merged_BothNull_ReturnsNull() =>
        Assert.IsNull(StateStruct.MergedTypeName(null, null));

    [Test]
    public void Merged_LeftNull_RightNamed_TakesRight() =>
        Assert.AreEqual("t", StateStruct.MergedTypeName(null, "t"));

    [Test]
    public void Merged_LeftNamed_RightNull_TakesLeft() =>
        Assert.AreEqual("t", StateStruct.MergedTypeName("t", null));

    [Test]
    public void Merged_BothSame_ReturnsName() =>
        Assert.AreEqual("t", StateStruct.MergedTypeName("t", "t"));

    [Test]
    public void Merged_BothSameCaseInsensitive_ReturnsFirst() =>
        Assert.AreEqual("Foo", StateStruct.MergedTypeName("Foo", "foo"));

    [Test]
    public void Merged_DifferentNames_ReturnsNull() =>
        Assert.IsNull(StateStruct.MergedTypeName("t", "s"));

    [Test]
    public void Merged_EmptyStrings_AreEqualToThemselves() =>
        Assert.AreEqual("", StateStruct.MergedTypeName("", ""));

    [Test]
    public void Merged_OneEmptyOneNamed_TakesNamed() =>
        // Permissive: empty is treated as anonymous-distinct (case-sensitive Equals != null path).
        // OrdinalIgnoreCase.Equals("", "t") = false, so returns null.
        Assert.IsNull(StateStruct.MergedTypeName("", "t"));

    [Test]
    public void Merged_IsSymmetric_BothNamed() =>
        Assert.AreEqual(
            StateStruct.MergedTypeName("t", "s"),
            StateStruct.MergedTypeName("s", "t"));

    [Test]
    public void Merged_IsSymmetric_OneNull() =>
        Assert.AreEqual(
            StateStruct.MergedTypeName("t", null),
            StateStruct.MergedTypeName(null, "t"));

    [Test]
    public void Merged_IsIdempotent() {
        var once = StateStruct.MergedTypeName("t", null);
        var twice = StateStruct.MergedTypeName(once, once);
        Assert.AreEqual(once, twice);
    }

    // ─── Asymmetry: Merged is wider than Lca ───

    [Test]
    public void MergedIsStrictlyWiderThanLca_OneNullOneNamed() {
        var lca = StateStruct.LcaTypeName(null, "t");
        var merged = StateStruct.MergedTypeName(null, "t");
        Assert.IsNull(lca, "LCA must drop to anonymous when either side is null");
        Assert.AreEqual("t", merged, "Merge must take the named side");
        Assert.AreNotEqual(lca, merged, "Merge and Lca must DIFFER on (null, named)");
    }

    [Test]
    public void MergedAndLca_AgreeOnBothNamed_Same() {
        var lca = StateStruct.LcaTypeName("t", "t");
        var merged = StateStruct.MergedTypeName("t", "t");
        Assert.AreEqual(lca, merged);
        Assert.AreEqual("t", lca);
    }

    [Test]
    public void MergedAndLca_AgreeOnBothNull() {
        var lca = StateStruct.LcaTypeName(null, null);
        var merged = StateStruct.MergedTypeName(null, null);
        Assert.IsNull(lca);
        Assert.IsNull(merged);
    }

    [Test]
    public void MergedAndLca_AgreeOnDifferentNames_BothNull() {
        var lca = StateStruct.LcaTypeName("t", "s");
        var merged = StateStruct.MergedTypeName("t", "s");
        Assert.IsNull(lca);
        Assert.IsNull(merged);
    }

    // ─── MergedIsOptionalSourced: OR semantics ───
    // Invariant: IsOptionalSourced marker propagates with OR so cycle-rescue gating
    // (used in TryRepairOptSourcedCycle) sees the full reachable subgraph. Either
    // side opt-sourced → result opt-sourced.

    [Test]
    public void MergedOptSourced_BothFalse_False() =>
        Assert.IsFalse(StateStruct.MergedIsOptionalSourced(false, false));

    [Test]
    public void MergedOptSourced_LeftTrue_True() =>
        Assert.IsTrue(StateStruct.MergedIsOptionalSourced(true, false));

    [Test]
    public void MergedOptSourced_RightTrue_True() =>
        Assert.IsTrue(StateStruct.MergedIsOptionalSourced(false, true));

    [Test]
    public void MergedOptSourced_BothTrue_True() =>
        Assert.IsTrue(StateStruct.MergedIsOptionalSourced(true, true));

    [Test]
    public void MergedOptSourced_IsSymmetric() {
        Assert.AreEqual(
            StateStruct.MergedIsOptionalSourced(true, false),
            StateStruct.MergedIsOptionalSourced(false, true));
        Assert.AreEqual(
            StateStruct.MergedIsOptionalSourced(false, false),
            StateStruct.MergedIsOptionalSourced(false, false));
    }

    [Test]
    public void MergedOptSourced_IsIdempotent() {
        var once = StateStruct.MergedIsOptionalSourced(true, false);
        var twice = StateStruct.MergedIsOptionalSourced(once, once);
        Assert.AreEqual(once, twice);
    }

    // ─── Cross-property invariants (foundation for Bug D/G/K) ───

    [Test]
    public void Bug_C_Gh128_NamedIdentityPreservedOnMergeOfNamedAndAnonymous() {
        // When MergeStructs / GetMergedStateOrNull encounters (named, anonymous) — e.g.
        // declared `type t` instance merged with an open struct from row polymorphism —
        // the named identity MUST survive. This was the root pattern in Bug C/D/G.
        Assert.AreEqual("t", StateStruct.MergedTypeName("t", null));
        Assert.AreEqual("t", StateStruct.MergedTypeName(null, "t"));
    }

    [Test]
    public void Bug_K_Gh128_LcaDropsNamedWhenOneSideAnonymous() {
        // K manifested because LCA correctly drops to anonymous when one side lacks
        // the name (this is the STRICT contract). The K fix was elsewhere — the cycle
        // detector needed to accept contractive Array back-edges. This test pins
        // the LCA contract that drove K's symptom.
        Assert.IsNull(StateStruct.LcaTypeName("t", null));
        Assert.IsNull(StateStruct.LcaTypeName(null, "t"));
    }
}
