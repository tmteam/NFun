using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Operators;

public class CompareOperatorsTest {
    [TestCase("true == false", false)]
    [TestCase("true==true==1", false)]
    [TestCase("8==8==1", false)]
    [TestCase("true==true==true", true)]
    [TestCase("8==8==8", false)]
    [TestCase("[0,0,1]!=[0,false,1]", true)]
    [TestCase("[0,0,1]==[0,false,1]", false)]
    [TestCase("[false,0,1]!=[0,false,1]", true)]
    [TestCase("[false,0,1,'vasa',[1,2,3]]==[false,0,1,'vasa',[1,2,3]]", true)]
    [TestCase("[false,0,1,'vasa',[1,2,3]]!=[false,0,1,'vasa',[1,2,3]]", false)]
    [TestCase("[false,0,1,'peta',[1,2,3]]==[false,0,1,'vasa',[1,2,3]]", false)]
    [TestCase("[false,0,1,'peta',[1,2,3]]!=[false,0,1,'vasa',[1,2,3]]", true)]
    [TestCase("[false,0,1,'vasa',[1,2,[1,2]]]==[false,0,1,'vasa',[1,2,[1,2]]]", true)]
    [TestCase("[false,0,1,'vasa',[1,2,[10000,2]]]==[false,0,1,'vasa',[1,2,[1,2]]]", false)]
    [TestCase("[false,0,1,'vasa',[1,2,[10000,2]]]!=[false,0,1,'vasa',[1,2,[1,2]]]", true)]
    [TestCase("[false,0,1]==[false,0,1]", true)]
    [TestCase("[false,0,1]!=[false,0,1]", false)]
    [TestCase("[false,0,1]==[0,false,1]", false)]
    [TestCase("[false,0,1]!=[0,false,1]", true)]
    [TestCase("[0,0,1]==[0,0,1]", true)]
    [TestCase("[0,0,1]!=[0,0,1]", false)]
    [TestCase("[0,1,1]==[0,0,1]", false)]
    [TestCase("[0,1,1]!=[0,0,1]", true)]
    [TestCase("[0,0,1.0]==[0,0,1]", true)]
    [TestCase("[0,0,1]!=[0,0,1.0]", false)]
    [TestCase("[0,1.0,1]==[0,0,1]", false)]
    [TestCase("[0,1,1]!=[0,0.0,1]", true)]
    [TestCase("[false,true, false]==[false,true, false]", true)]
    [TestCase("[false,true, false]!=[false,true, false]", false)]
    [TestCase("0 == 0 == 8", false)]
    [TestCase("8 == 1 == 0", false)]
    [TestCase("true == 1", false)]
    [TestCase("1==1.0", true)]
    [TestCase("0==0.0", true)]
    [TestCase("1==1", true)]
    [TestCase("1==0", false)]
    [TestCase("1==true", false)]
    [TestCase("1==false", false)]
    [TestCase("0==true", false)]
    [TestCase("0==false", false)]
    [TestCase("true==true", true)]
    [TestCase("true==false", false)]
    [TestCase("'a'[0]=='b'[0] ", false)]
    [TestCase("'a'[0]!='b'[0] ", true)]
    [TestCase("'a'[0]== 'a'[0] ", true)]
    [TestCase("'a'[0]!='a'[0] ", false)]
    [TestCase("'avatar'== 'bigben' ", false)]
    [TestCase("'avatar'!= 'bigben' ", true)]
    public void ConstantEquality(string expr, bool expected)
        => expr.AssertReturns("out", expected);

    [TestCase("1!=0", true)]
    [TestCase("0!=1", true)]
    [TestCase("5!=5", false)]
    [TestCase("5>5", false)]
    [TestCase("5>3", true)]
    [TestCase("5>6", false)]
    [TestCase("5>=5", true)]
    [TestCase("5>=3", true)]
    [TestCase("5>=6", false)]
    [TestCase("5<=5", true)]
    [TestCase("5<=3", false)]
    [TestCase("5<=6", true)]
    [TestCase("'a'[0]< 'b'[0] ", true)]
    [TestCase("'a'[0]<='b'[0] ", true)]
    [TestCase("'a'[0]>='b'[0] ", false)]
    [TestCase("'a'[0]> 'b'[0] ", false)]
    [TestCase("'a'[0]< 'a'[0] ", false)]
    [TestCase("'a'[0]<='a'[0] ", true)]
    [TestCase("'a'[0]>='a'[0] ", true)]
    [TestCase("'a'[0]> 'a'[0] ", false)]
    [TestCase("'a'[0] < 'z'[0]", true)]
    [TestCase("'b'[0] > 'z'[0]", false)]
    [TestCase("'b'[0] <= 'z'[0]", true)]
    [TestCase("'A'[0] >= 'B'[0]", false)]
    [TestCase("'B'[0] > 'C'[0]", false)]
    [TestCase("'A'[0] <= 'z'[0]", true)]
    [TestCase("'B'[0] > 'y'[0]", false)]
    [TestCase("'C'[0] >= 'x'[0]", false)]
    [TestCase("'D'[0] < 'w'[0]", true)]
    [TestCase("'E'[0] <= 'v'[0]", true)]
    [TestCase("'F'[0] <= '1'[0]", false)]
    [TestCase("'1'[0] <= 'a'[0]", true)]
    [TestCase("cmp(a,b) = a>b; 'a'[0].cmp('b'[0])", false)]
    [TestCase("'avatar'< 'bigben' ", true)]
    [TestCase("'avatar'<= 'bigben' ", true)]
    [TestCase("'avatar'>= 'bigben' ", false)]
    [TestCase("'avatar'>  'bigben' ", false)]
    [TestCase("'avatar'< 'avatar' ", false)]
    [TestCase("'avatar'<= 'avatar' ", true)]
    [TestCase("'avatar'>= 'avatar' ", true)]
    [TestCase("'avatar'>  'avatar' ", false)]
    [TestCase("'avatar'.reverse() >  reverse('avatar') ", false)]
    [TestCase("('avatar'.reverse()) >  reverse('avatar') ", false)]
    [TestCase("'avatar'.reverse() <  'avatar'", false)]
    [TestCase("0==0.0", true)]
    [TestCase("0!=0.5", true)]
    [TestCase("-1.0>=-0x1", true)]
    [TestCase("1>=-1.0", true)]
    [TestCase("1==0", false)]
    [TestCase("true==true", true)]
    [TestCase("true==false", false)]
    [TestCase("5!=5", false)]
    [TestCase("5>3", true)]
    [TestCase("5>=5", true)]
    [TestCase("5<=5", true)]
    [TestCase("1<2 == 2<3", true)]
    [TestCase("1<2 == 2<3 == true", true)]
    [TestCase("1<2 and 2<3", true)]
    [TestCase("true or 1>2 and 2<3", true)]
    [TestCase("true or 1>2 or 2>3", true)]
    [TestCase("true and 1>2 or 2>3", false)]
    [TestCase("true and 1>2 or 2<3", true)]
    public void ConstantEquation(string expr, bool expected)
        => expr.AssertReturns("out", expected);


    [TestCase("x:real; y = x>42", (double)1, false)]
    [TestCase("x:real; y = x>42", (double)42, false)]
    [TestCase("x:real; y = x>42", (double)43, true)]
    [TestCase("x:real; y = x>=42", (double)1, false)]
    [TestCase("x:real; y = x>=42", (double)42, true)]
    [TestCase("x:real; y = x>=42", (double)43, true)]
    [TestCase("x:real; y = x<42", (double)1, true)]
    [TestCase("x:real; y = x<42", (double)42, false)]
    [TestCase("x:real; y = x<42", (double)43, false)]
    [TestCase("x:real; y = x<=42", (double)1, true)]
    [TestCase("x:real; y = x<=42", (double)42, true)]
    [TestCase("x:real; y = x<=42", (double)43, false)]
    [TestCase("x:real; y = x==42", (double)1, false)]
    [TestCase("x:real; y = x==42", (double)42, true)]
    [TestCase("x:real; y = x==42", (double)43, false)]
    [TestCase("x:int64; y = x>42", (long)1, false)]
    [TestCase("x:int64; y = x>42", (long)42, false)]
    [TestCase("x:int64; y = x>42", (long)43, true)]
    [TestCase("x:int64; y = x>=42", (long)1, false)]
    [TestCase("x:int64; y = x>=42", (long)42, true)]
    [TestCase("x:int64; y = x>=42", (long)43, true)]
    [TestCase("x:int64; y = x<42", (long)1, true)]
    [TestCase("x:int64; y = x<42", (long)42, false)]
    [TestCase("x:int64; y = x<42", (long)43, false)]
    [TestCase("x:int64; y = x<=42", (long)1, true)]
    [TestCase("x:int64; y = x<=42", (long)42, true)]
    [TestCase("x:int64; y = x<=42", (long)43, false)]
    [TestCase("x:int64; y = x==42", (long)1, false)]
    [TestCase("x:int64; y = x==42", (long)42, true)]
    [TestCase("x:int64; y = x==42", (long)43, false)]
    [TestCase("x:uint64; y = x>42", (ulong)1, false)]
    [TestCase("x:uint64; y = x>42", (ulong)42, false)]
    [TestCase("x:uint64; y = x>42", (ulong)43, true)]
    [TestCase("x:uint64; y = x>=42", (ulong)1, false)]
    [TestCase("x:uint64; y = x>=42", (ulong)42, true)]
    [TestCase("x:uint64; y = x>=42", (ulong)43, true)]
    [TestCase("x:uint64; y = x<42", (ulong)1, true)]
    [TestCase("x:uint64; y = x<42", (ulong)42, false)]
    [TestCase("x:uint64; y = x<42", (ulong)43, false)]
    [TestCase("x:uint64; y = x<=42", (ulong)1, true)]
    [TestCase("x:uint64; y = x<=42", (ulong)42, true)]
    [TestCase("x:uint64; y = x<=42", (ulong)43, false)]
    [TestCase("x:uint64; y = x==42", (ulong)1, false)]
    [TestCase("x:uint64; y = x==42", (ulong)42, true)]
    [TestCase("x:uint64; y = x==42", (ulong)43, false)]
    [TestCase("x:int; y = x>42", 1, false)]
    [TestCase("x:int; y = x>42", 42, false)]
    [TestCase("x:int; y = x>42", 43, true)]
    [TestCase("x:int; y = x>=42", 1, false)]
    [TestCase("x:int; y = x>=42", 42, true)]
    [TestCase("x:int; y = x>=42", 43, true)]
    [TestCase("x:int; y = x<42", 1, true)]
    [TestCase("x:int; y = x<42", 42, false)]
    [TestCase("x:int; y = x<42", 43, false)]
    [TestCase("x:int; y = x<=42", 1, true)]
    [TestCase("x:int; y = x<=42", 42, true)]
    [TestCase("x:int; y = x<=42", 43, false)]
    [TestCase("x:int; y = x==42", 1, false)]
    [TestCase("x:int; y = x==42", 42, true)]
    [TestCase("x:int; y = x==42", 43, false)]
    [TestCase("x:uint; y = x>42", (uint)1, false)]
    [TestCase("x:uint; y = x>42", (uint)42, false)]
    [TestCase("x:uint; y = x>42", (uint)43, true)]
    [TestCase("x:uint; y = x>=42", (uint)1, false)]
    [TestCase("x:uint; y = x>=42", (uint)42, true)]
    [TestCase("x:uint; y = x>=42", (uint)43, true)]
    [TestCase("x:uint; y = x<42", (uint)1, true)]
    [TestCase("x:uint; y = x<42", (uint)42, false)]
    [TestCase("x:uint; y = x<42", (uint)43, false)]
    [TestCase("x:uint; y = x<=42", (uint)1, true)]
    [TestCase("x:uint; y = x<=42", (uint)42, true)]
    [TestCase("x:uint; y = x<=42", (uint)43, false)]
    [TestCase("x:uint; y = x==42", (uint)1, false)]
    [TestCase("x:uint; y = x==42", (uint)42, true)]
    [TestCase("x:uint; y = x==42", (uint)43, false)]
    [TestCase("x:int16; y = x>42", (Int16)1, false)]
    [TestCase("x:int16; y = x>42", (Int16)42, false)]
    [TestCase("x:int16; y = x>42", (Int16)43, true)]
    [TestCase("x:int16; y = x>=42", (Int16)1, false)]
    [TestCase("x:int16; y = x>=42", (Int16)42, true)]
    [TestCase("x:int16; y = x>=42", (Int16)43, true)]
    [TestCase("x:int16; y = x<42", (Int16)1, true)]
    [TestCase("x:int16; y = x<42", (Int16)42, false)]
    [TestCase("x:int16; y = x<42", (Int16)43, false)]
    [TestCase("x:int16; y = x<=42", (Int16)1, true)]
    [TestCase("x:int16; y = x<=42", (Int16)42, true)]
    [TestCase("x:int16; y = x<=42", (Int16)43, false)]
    [TestCase("x:int16; y = x==42", (Int16)1, false)]
    [TestCase("x:int16; y = x==42", (Int16)42, true)]
    [TestCase("x:int16; y = x==42", (Int16)43, false)]
    [TestCase("x:uint16; y = x>42", (UInt16)1, false)]
    [TestCase("x:uint16; y = x>42", (UInt16)42, false)]
    [TestCase("x:uint16; y = x>42", (UInt16)43, true)]
    [TestCase("x:uint16; y = x>=42", (UInt16)1, false)]
    [TestCase("x:uint16; y = x>=42", (UInt16)42, true)]
    [TestCase("x:uint16; y = x>=42", (UInt16)43, true)]
    [TestCase("x:uint16; y = x<42", (UInt16)1, true)]
    [TestCase("x:uint16; y = x<42", (UInt16)42, false)]
    [TestCase("x:uint16; y = x<42", (UInt16)43, false)]
    [TestCase("x:uint16; y = x<=42", (UInt16)1, true)]
    [TestCase("x:uint16; y = x<=42", (UInt16)42, true)]
    [TestCase("x:uint16; y = x<=42", (UInt16)43, false)]
    [TestCase("x:uint16; y = x==42", (UInt16)1, false)]
    [TestCase("x:uint16; y = x==42", (UInt16)42, true)]
    [TestCase("x:uint16; y = x==42", (UInt16)43, false)]
    [TestCase("x:byte; y = x>42", (byte)1, false)]
    [TestCase("x:byte; y = x>42", (byte)42, false)]
    [TestCase("x:byte; y = x>42", (byte)43, true)]
    [TestCase("x:byte; y = x>=42", (byte)1, false)]
    [TestCase("x:byte; y = x>=42", (byte)42, true)]
    [TestCase("x:byte; y = x>=42", (byte)43, true)]
    [TestCase("x:byte; y = x<42", (byte)1, true)]
    [TestCase("x:byte; y = x<42", (byte)42, false)]
    [TestCase("x:byte; y = x<42", (byte)43, false)]
    [TestCase("x:byte; y = x<=42", (byte)1, true)]
    [TestCase("x:byte; y = x<=42", (byte)42, true)]
    [TestCase("x:byte; y = x<=42", (byte)43, false)]
    [TestCase("x:byte; y = x==42", (byte)1, false)]
    [TestCase("x:byte; y = x==42", (byte)42, true)]
    [TestCase("x:byte; y = x==42", (byte)43, false)]
    [TestCase("x:int8; y = x>42",  (sbyte)1,  false)]
    [TestCase("x:int8; y = x>42",  (sbyte)42, false)]
    [TestCase("x:int8; y = x>42",  (sbyte)43, true)]
    [TestCase("x:int8; y = x>=42", (sbyte)1,  false)]
    [TestCase("x:int8; y = x>=42", (sbyte)42, true)]
    [TestCase("x:int8; y = x>=42", (sbyte)43, true)]
    [TestCase("x:int8; y = x<42",  (sbyte)1,  true)]
    [TestCase("x:int8; y = x<42",  (sbyte)42, false)]
    [TestCase("x:int8; y = x<42",  (sbyte)43, false)]
    [TestCase("x:int8; y = x<=42", (sbyte)1,  true)]
    [TestCase("x:int8; y = x<=42", (sbyte)42, true)]
    [TestCase("x:int8; y = x<=42", (sbyte)43, false)]
    [TestCase("x:int8; y = x==42", (sbyte)1,  false)]
    [TestCase("x:int8; y = x==42", (sbyte)42, true)]
    [TestCase("x:int8; y = x==42", (sbyte)43, false)]
    public void SingleVariableEquation(string expr, object arg, object expected) =>
        expr.Calc("x", arg).AssertReturns("y", expected);

    // ═══════════════════════════════════════════════════════════════
    // Struct equality with structural subtyping
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void StructEquality_SameFields_Equal() =>
        "{x=1} == {x=1}".AssertReturns(true);

    [Test]
    public void StructEquality_DifferentFieldCount_NotEqual() =>
        // Per spec: structs must have same field list for equality
        "{x=1, y=2} == {x=1}".AssertReturns(false);

    [Test]
    public void StructEquality_SharedFieldDiffers_NotEqual() =>
        "{x=1, y=2} == {x=1, y=3}".AssertReturns(false);

    [Test]
    public void StructNotEqual_DifferentFieldCount_True() =>
        "{x=1} != {x=1, y=2}".AssertReturns(true);

    [Test]
    public void StructInArray_DifferentFieldCount_NotFound() =>
        // {x=1} not in [{x=1,y=2}] — different field counts
        "out = {x=1} in [{x=1, y=2}]".AssertReturns("out", false);

    [Test]
    public void StructSubsetEqual_ShouldBeFalse() =>
        "out = {a = 1} == {a = 1, b = 2}".AssertReturns("out", false);

    [Test]
    public void StructSupersetEqual_ShouldBeFalse() =>
        "out = {a = 1, b = 2} == {a = 1}".AssertReturns("out", false);

    [Test]
    public void StructSubsetNotEqual_ShouldBeTrue() =>
        "out = {a = 1} != {a = 1, b = 2}".AssertReturns("out", true);

    [Test]
    public void StructDisjointFields_NotEqual() =>
        "out = {a = 1, b = 2} == {a = 1, c = 3}".AssertReturns("out", false);

    [Test]
    public void StructSameFields_Equal() =>
        "out = {a = 1, b = 2} == {a = 1, b = 2}".AssertReturns("out", true);

    [Test]
    public void StructSameFieldsDiffValues_NotEqual() =>
        "out = {a = 1, b = 2} == {a = 1, b = 3}".AssertReturns("out", false);

    [Test]
    public void StructThreeVsTwoFields() =>
        "out = {a = 1, b = 2, c = 3} == {a = 1, b = 2}".AssertReturns("out", false);

    [Test]
    public void StructEqualityDifferentFields_NoCrash() {
        Assert.DoesNotThrow(() =>
            "a = {x=1, y=2}; b = {x=1, z=3}; out = a == b".Calc());
    }

    [Test]
    public void StructEqualityDifferentFieldCount() {
        "a = {x=1, y=2}; b = {x=1}; out = a == b".Calc().AssertResultHas("out", false);
    }

    [Test]
    public void StructIntersect_Works() {
        var r = "a=[{x=1},{x=2}]; b=[{x=2},{x=3}]; out=a.intersect(b)".Calc();
        var arr = (object[])r.Get("out");
        Assert.AreEqual(1, arr.Length, "intersect should find {x=2} in both arrays");
    }

    // `==` no longer applies implicit ToText coercion of one operand to fit the
    // other. Char vs Char[] (and any cross-family pair) returns false directly.
    // Previously inside `rule it == 'a'` TIC narrowed equality's T to Char[]
    // and ToText'd `it` (Char) into a 1-char text — silently producing true.
    [TestCase("out = [/'a'].any(rule it == 'a')", false)]
    [TestCase("out = [/'a'].any(rule it == [/'a'])", false)]
    [TestCase("out = 'hello'.filter(rule it == 'l')", "")]
    public void RuleIt_CharVsText_NoImplicitToText(string expr, object expected) =>
        expr.AssertResultHas("out", expected);

    // Binary min/max reject bool and ip at parse time — matching the array
    // variant `[T].max()` and the relational operators `< > <= >=`. Per
    // Specs/Operators.md L115-118 Comparables are text/char/numeric. Without
    // this guard, max(true,false) silently returned a value (Bool happens to
    // implement IComparable in .NET) and max(ip,ip) crashed with a raw
    // InvalidCastException since IPAddress is not IComparable.
    [TestCase("out = max(true, false)")]
    [TestCase("out = min(true, false)")]
    [TestCase("out = max(127.0.0.1, 192.168.0.1)")]
    [TestCase("out = min(127.0.0.1, 192.168.0.1)")]
    public void MinMax_NonComparable_RejectedAtParse(string expr) =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => expr.Calc());
}
