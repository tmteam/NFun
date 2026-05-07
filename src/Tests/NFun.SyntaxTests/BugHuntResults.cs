using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated bug hunting focused on complex/exotic recursive types
/// (mutual recursion, struct↔fun cycles, non-contractive cases, edge combinations).
/// Each test is a confirmed bug — expected behavior per specification does not match
/// actual behavior. All [Ignore] until fixed.
/// </summary>
public class BugHuntResults {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    private static NFun.TestTools.CalculationResult RunRec(string script) =>
        script.CalcWithDialect(
            namedTypesSupport: NFun.NamedTypesSupport.Enabled,
            optionalTypesSupport: NFun.OptionalTypesSupport.Enabled);

    private static NFun.Runtime.FunnyRuntime BuildRec(string script) =>
        NFun.Funny.Hardcore.WithDialect(
            namedTypesSupport: NFun.NamedTypesSupport.Enabled,
            optionalTypesSupport: NFun.OptionalTypesSupport.Enabled).Build(script);

    // ========================================================================
    // CRITICAL: Crashes (StackOverflow / NFunImpossibleException)
    // ========================================================================

    // Bug A FIXED: GetMergedStateOrNull now has cycle guard for StateStruct ↔ StateStruct
    [Test]
    public void BugA_IfElseOverRecursiveVars_Works() {
        "type n = {v:int, next:n? = none}\r".CalcWithDialect(
            namedTypesSupport: NFun.NamedTypesSupport.Enabled,
            optionalTypesSupport: NFun.OptionalTypesSupport.Enabled);
        Assert.DoesNotThrow(() => BuildRec(
            "type n = {v:int, next:n? = none}\r" +
            "r1 = n{v=1}\r" +
            "r2 = n{v=3}\r" +
            "sel = if(true) r1 else r2\r" +
            "out = sel.next?.v ?? -1"));
    }

    // Bug A2 FIXED: same cycle guard handles map(rule f(it)) over recursive types
    [Test]
    public void BugA2_MapOverRecursiveTypeViaUserFn_Works() {
        ("type t = {v:int, next:t? = none}\r" +
         "f(x:t):int = x.v\r" +
         "arr:t[] = [t{v=1}, t{v=2}]\r" +
         "out = arr.map(rule f(it)).sum()")
            .CalcWithDialect(
                namedTypesSupport: NFun.NamedTypesSupport.Enabled,
                optionalTypesSupport: NFun.OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 3);
    }

    // Bug B FIXED: NamedTypeElaborator now recurses into TryCatchSyntaxNode bodies
    [Test]
    public void BugB_TryCatchOverNamedCtor_Works() {
        "type t = {v:int}\rout = try t{v=1}.v catch -1".CalcWithDialect(
            namedTypesSupport: NFun.NamedTypesSupport.Enabled,
            optionalTypesSupport: NFun.OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 1);
    }

    // ========================================================================
    // CRITICAL: TIC failures on patterns that should work
    // ========================================================================

    [Test]
    [Ignore("Bug C: LCA of array of N≥2 vars of recursive named type fails type-check on deep access")]
    public void BugC_LcaOfRecursiveVarsInArray_FU719() {
        RunRec(
            "type tr = {v:int, kids:tr[] = []}\r" +
            "r1 = tr{v=1, kids=[tr{v=2}]}\r" +
            "r2 = tr{v=3, kids=[tr{v=4}]}\r" +
            "arr = [r1, r2]\r" +
            "out = arr[0].kids[0].v")
            .AssertResultHas("out", 2);
    }

    [Test]
    [Ignore("Bug C2: User fn returning recursive named type via Optional self-ref fails on assignment :t")]
    public void BugC2_UserFnReturnsRecOptional_AssignmentFails() {
        RunRec(
            "type t = {v:int, next:t? = none}\r" +
            "f(x:t):t = x\r" +
            "n:t = f(t{v=1})\r" +
            "out = n.v")
            .AssertResultHas("out", 1);
    }

    [Test]
    [Ignore("Bug C3: User fn returning recursive named type via Array self-ref fails on assignment :t")]
    public void BugC3_UserFnReturnsRecArray_AssignmentFails() {
        RunRec(
            "type t = {v:int, kids:t[] = []}\r" +
            "getOne():t = t{v=1, kids=[]}\r" +
            "n:t = getOne()\r" +
            "out = n.v")
            .AssertResultHas("out", 1);
    }

    [Test]
    [Ignore("Bug D: Force-unwrap of recursive type loses identity through user fn composition. Naive fix (TypeName on root in ResolveNamedStruct) breaks generic struct types (`type t = {a}`) because TypeName=set forces IsMutable=false. Needs decoupling. See GH #128.")]
    public void BugD_ForceUnwrapRecLoseIdentity_FU719() {
        RunRec(
            "type t = {v: int, k: t? = none}\r" +
            "getK(ti:t):t? = ti.k\r" +
            "getV(ti:t):int = ti.v\r" +
            "ti:t = t{v=1, k=t{v=2}}\r" +
            "y = getV(getK(ti)!)")
            .AssertResultHas("y", 2);
    }

    [Test]
    [Ignore("Bug E: Mixing pre-declared rec-type var with inline literal in same array fails with FU761")]
    public void BugE_PreDeclaredAndInlineLiteralInArray_FU761() {
        RunRec(
            "type t = {v: int, k: t? = none}\r" +
            "ti = t{v=1, k=t{v=2}}\r" +
            "arr = [ti, t{v=3}]\r" +
            "y = arr.count()")
            .AssertResultHas("y", 2);
    }

    [Test]
    [Ignore("Bug E2: Same as Bug E with explicit arr:t[] annotation — still fails")]
    public void BugE2_PreDeclaredAndDeeperInlineWithAnnotation_FU761() {
        RunRec(
            "type t = {v: int, k: t? = none}\r" +
            "ti = t{v=1, k=t{v=2}}\r" +
            "arr:t[] = [ti, t{v=3, k=t{v=4, k=t{v=5}}}]\r" +
            "y = arr.count()")
            .AssertResultHas("y", 2);
    }

    // ========================================================================
    // MODERATE: Wrong errors / wrong inference
    // ========================================================================

    [Test]
    [Ignore("Bug F: rule(x:USER_TYPE) annotated syntax rejects named type aliases (FU406). Inconsistent — fn(x:T) and r:rule(T)->R both accept named types")]
    public void BugF_AnnotatedRuleArgRejectsNamedType_FU406() {
        RunRec(
            "type tr = {v:int}\r" +
            "out = [tr{v=1}].map(rule(x:tr) = x.v)[0]")
            .AssertResultHas("out", 1);
    }

    [Test]
    [Ignore("Bug F2: rule(x:ALIAS) rejects primitive alias too")]
    public void BugF2_AnnotatedRuleArgRejectsPrimitiveAlias_FU406() {
        RunRec(
            "type age = int\r" +
            "out = [1, 2, 3].map(rule(x:age) = x + 1)[0]")
            .AssertResultHas("out", 2);
    }

    [Test]
    [Ignore("Bug G: 3+ ?.next chain after user fn returning :t? fails with FU761. Same root as Bug D. See GH #128.")]
    public void BugG_DeepDotChainAfterUserFnReturningOpt_FU761() {
        RunRec(
            "type t = {v:int, next:t? = none}\r" +
            "f(x:t):t? = x.next\r" +
            "n = t{v=1, next=t{v=2, next=t{v=3, next=t{v=4}}}}\r" +
            "out = f(n)?.next?.next?.v ?? -1")
            .AssertResultHas("out", 4);
    }

    // Bug H FIXED: NamedTypeElaborator now rejects type names colliding with primitives
    [Test]
    public void BugH_NamedTypeShadowsPrimitive_RejectedAtDefinition() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => BuildRec(
            "type Text = {v: int}\r" +
            "t = Text{v=42}\r" +
            "y = t.v"));
    }

    // ========================================================================
    // MINOR: Missing validation for degenerate / tautological declarations
    // ========================================================================

    // Bug I FIXED: cycle detector now walks through Optional/Array in alias body
    [Test]
    public void BugI_OptCycleAliases_Rejected() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => BuildRec(
            "type a = b?\rtype b = a?\ry = 1"));
    }

    // Bug J FIXED
    [Test]
    public void BugJ_OptSelfLoop_Rejected() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => BuildRec(
            "type x = x?\ry:x = none"));
    }

    // Bug J2 FIXED
    [Test]
    public void BugJ2_ArrSelfLoop_Rejected() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => BuildRec(
            "type x = x[]\ry:x = []"));
    }

    [Test]
    [Ignore("Bug K: ?? on non-optional array of recursive type yields misleading FU731 'Recursive type definition' error")]
    public void BugK_CoalesceOnNonOptArrayOfRec_MisleadingError() {
        // Either accept (T → T? implicit lift per spec) or reject with clear message
        RunRec(
            "type t = {v:int, kids:t[] = []}\r" +
            "root = t{v=1}\r" +
            "out = root.kids ?? [t{v=99}]")
            .AssertResultHas("out", new int[] { });
    }
}
