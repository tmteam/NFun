using System;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests.NamedTypes;

/// <summary>
/// Comprehensive combinations of recursive types with Optional, Array, functions, if-else, etc.
/// </summary>
public class RecursiveTypeCombinationsTest {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    static CalculationResult Calc(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
            namedTypesSupport: NamedTypesSupport.ExperimentalEnabled);

    static FunnyRuntime Build(string expr) =>
        Funny.Hardcore.WithDialect(
            optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
            namedTypesSupport: NamedTypesSupport.ExperimentalEnabled).Build(expr);

    // ═══════════════════════════════════════════════════════════════
    // OPTIONAL ARRAY OF SELF (t[]?)
    // ═══════════════════════════════════════════════════════════════

    #region Optional array of self

    [Test]
    public void OptionalArrayOfSelf_None() {
        var r = Calc("type t = {v:int, children:t[]? = none}; out = t{v=1}.v");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void OptionalArrayOfSelf_WithChildren() {
        var r = Calc(
            "type t = {v:int, children:t[]? = none}; " +
            "out = t{v=0, children=[t{v=1}, t{v=2}]}.v");
        Assert.AreEqual(0, r.Get("out"));
    }

    [Test]
    public void OptionalArrayOfSelf_CoalesceEmpty() {
        var r = Calc(
            "type t = {v:int, children:t[]? = none}; " +
            "out = (t{v=1}.children ?? []).count()");
        Assert.AreEqual(0, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // ARRAY OF OPTIONAL SELF (t?[])
    // ═══════════════════════════════════════════════════════════════

    #region Array of optional self

    [Test]
    public void ArrayOfOptionalSelf_WithNone() {
        var r = Calc(
            "type t = {v:int, items:t?[] = []}; " +
            "out = t{v=0, items=[t{v=1}, none, t{v=2}]}.v");
        Assert.AreEqual(0, r.Get("out"));
    }

    [Test]
    public void ArrayOfOptionalSelf_CountWithNones() {
        var r = Calc(
            "type t = {v:int, items:t?[] = []}; " +
            "out = t{v=0, items=[t{v=1}, none, none]}.items.count()");
        Assert.AreEqual(3, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // RECURSIVE + MAP/FILTER
    // ═══════════════════════════════════════════════════════════════

    #region Recursive + higher-order functions

    [Test]
    public void Recursive_MapChildrenValues() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "root = t{v=0, children=[t{v=10}, t{v=20}, t{v=30}]}; " +
            "out = root.children.map(rule it.v)");
    }

    [Test]
    public void Recursive_FilterChildren() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "root = t{v=0, children=[t{v=1}, t{v=5}, t{v=3}]}; " +
            "out = root.children.filter(rule it.v > 2).count()");
        Assert.AreEqual(2, r.Get("out"));
    }

    [Test]
    public void Recursive_FoldChildrenSum() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "root = t{v=0, children=[t{v=10}, t{v=20}]}; " +
            "out = root.children.fold(0, rule it1 + it2.v)");
        Assert.AreEqual(30, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // RECURSIVE + IF-ELSE
    // ═══════════════════════════════════════════════════════════════

    #region Recursive + if-else

    [Test]
    public void Recursive_IfElse_DifferentDepths() {
        // One branch deeper than other
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "out = if(true) node{v=1, next=node{v=2}} else node{v=3}");
    }

    [Test]
    public void Recursive_IfElse_NodeOrNone() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "out = if(true) node{v=1} else none");
    }

    [Test]
    public void Recursive_IfElse_ArrayBranches() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "out = if(true) [t{v=1}] else [t{v=2}, t{v=3}]");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // RECURSIVE + COALESCE CHAINS
    // ═══════════════════════════════════════════════════════════════

    #region Recursive + coalesce

    [Test]
    public void Recursive_CoalesceChain() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=1}; " +
            "out = n.next?.v ?? n.v");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void Recursive_DoubleCoalesce() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=1, next=node{v=2}}; " +
            "out = n.next?.next?.v ?? n.next?.v ?? n.v");
        Assert.AreEqual(2, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // RECURSIVE + FORCE UNWRAP
    // ═══════════════════════════════════════════════════════════════

    #region Recursive + force unwrap

    [Test]
    public void Recursive_ForceUnwrap_HasValue() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=1, next=node{v=2}}; " +
            "out = n.next!.v");
        Assert.AreEqual(2, r.Get("out"));
    }

    [Test]
    public void Recursive_ForceUnwrap_None_Throws() {
        Assert.Throws<FunnyRuntimeException>(() =>
            Calc("type node = {v:int, next:node? = none}; " +
                 "n = node{v=1}; " +
                 "out = n.next!.v"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // RECURSIVE + TYPE ANNOTATIONS on intermediate vars
    // ═══════════════════════════════════════════════════════════════

    #region Recursive + annotations

    [Test]
    public void Recursive_AnnotatedIntermediate() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n:node = node{v=1, next=node{v=2}}; " +
            "out = n.v");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void Recursive_AnnotatedArray() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "arr:node[] = [node{v=1}, node{v=2}]; " +
            "out = arr.count()");
        Assert.AreEqual(2, r.Get("out"));
    }

    [Test]
    public void Recursive_AnnotatedOptional() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n:node? = if(true) node{v=42} else none; " +
            "out = n?.v ?? 0");
        Assert.AreEqual(42, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // RECURSIVE + DEFAULT KEYWORD
    // ═══════════════════════════════════════════════════════════════

    #region Recursive + default

    [Test]
    public void Recursive_AllDefaults() {
        var r = Calc(
            "type node = {v:int = 0, next:node? = none}; " +
            "out = node{}.v");
        Assert.AreEqual(0, r.Get("out"));
    }

    [Test]
    public void Recursive_DefaultInConstructor() {
        var r = Calc(
            "type node = {v:int = 99, next:node? = none}; " +
            "out = node{v = default}.v");
        // default for int field = 0 (type default, not field default)
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // TWO INDEPENDENT RECURSIVE TYPES in one script
    // ═══════════════════════════════════════════════════════════════

    #region Two independent recursive types

    [Test]
    public void TwoRecursiveTypes_Independent() {
        var r = Calc(
            "type list = {v:int, next:list? = none}; " +
            "type tree = {v:int, left:tree? = none, right:tree? = none}; " +
            "l = list{v=1, next=list{v=2}}; " +
            "t = tree{v=10, left=tree{v=5}}; " +
            "out = l.v + t.v");
        Assert.AreEqual(11, r.Get("out"));
    }

    [Test]
    public void TwoRecursiveTypes_ListAndArray() {
        var r = Calc(
            "type list = {v:int, next:list? = none}; " +
            "type tree = {v:int, children:tree[] = []}; " +
            "l = list{v=1}; " +
            "t = tree{v=2, children=[tree{v=3}]}; " +
            "out = l.v + t.v");
        Assert.AreEqual(3, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // RECURSIVE STRUCT EQUALITY
    // ═══════════════════════════════════════════════════════════════

    #region Equality

    [Test]
    public void Recursive_Equality_Leaves() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "out = node{v=1} == node{v=1}");
        Assert.AreEqual(true, r.Get("out"));
    }

    [Test]
    public void Recursive_Inequality() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "out = node{v=1} == node{v=2}");
        Assert.AreEqual(false, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // INDIRECT RECURSION — break at different levels
    // ═══════════════════════════════════════════════════════════════

    #region Indirect break patterns

    [Test]
    public void Indirect_AnonOpt_BnonOpt_CtoA_Opt() {
        // a→b (non-opt), b→c (non-opt), c→a (optional) — break at last link
        var r = Calc(
            "type a = {x:int, b:b}; " +
            "type b = {y:int, c:c}; " +
            "type c = {z:int, a:a? = none}; " +
            "out = a{x=1, b=b{y=2, c=c{z=3}}}.x");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void Indirect_AllOptional() {
        var r = Calc(
            "type a = {x:int, b:b? = none}; " +
            "type b = {y:int, c:c? = none}; " +
            "type c = {z:int, a:a? = none}; " +
            "v = a{x=1}; out = v.x");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void Indirect_MixedOptAndArray() {
        // a has optional b, b has array of a
        var r = Calc(
            "type a = {x:int, b:b? = none}; " +
            "type b = {y:int, items:a[] = []}; " +
            "out = a{x=1, b=b{y=2, items=[a{x=3}]}}.x");
        Assert.AreEqual(1, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // RECURSIVE IN FUNCTION PARAMS AND RETURNS
    // ═══════════════════════════════════════════════════════════════

    #region Functions with recursive types

    [Test]
    public void Function_TakesRecursiveReturnsInt() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "depth(n:node):int = if(n.next == none) 1 else 1; " +
            "out = depth(node{v=1, next=node{v=2}})");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void Function_TakesRecursiveArray() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "sumValues(arr:t[]):int = arr.fold(0, rule it1 + it2.v); " +
            "out = sumValues([t{v=10}, t{v=20}])");
        Assert.AreEqual(30, r.Get("out"));
    }

    [Test]
    public void Function_GenericOverRecursive() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "first(arr) = arr[0]; " +
            "out = first([node{v=42}]).v");
        Assert.AreEqual(42, r.Get("out"));
    }

    [Test]
    public void Function_RecursiveUserFunction_Depth1() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "lastVal(n:node):int = if(n.next == none) n.v else lastVal(n.next!); " +
            "out = lastVal(node{v=42})");
        Assert.AreEqual(42, r.Get("out"));
    }

    [Test]
    public void Function_RecursiveUserFunction_Depth4() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "lastVal(n:node):int = if(n.next == none) n.v else lastVal(n.next!); " +
            "a = node{v=4}; b = node{v=3, next=a}; c = node{v=2, next=b}; d = node{v=1, next=c}; " +
            "out = lastVal(d)");
        Assert.AreEqual(4, r.Get("out"));
    }

    [Test]
    public void Function_RecursiveUserFunction_Depth2() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "lastVal(n:node):int = if(n.next == none) n.v else lastVal(n.next!); " +
            "a = node{v=2}; b = node{v=1, next=a}; " +
            "out = lastVal(b)");
        Assert.AreEqual(2, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // NON-OPTIONAL RECURSION — errors
    // ═══════════════════════════════════════════════════════════════

    #region Error cases

    [Test]
    public void DirectArrayOfSelf_NoDefault_IsValid() =>
        // t[] is valid recursion — empty array breaks it. Field is required.
        Build("type t = {v:int, children:t[]}");

    [Test]
    public void Error_IndirectNonOpt_Mixed() =>
        Assert.Throws<FunnyParseException>(() =>
            Build("type a = {b:b}; type b = {c:c}; type c = {a:a}"));

    #endregion
}
