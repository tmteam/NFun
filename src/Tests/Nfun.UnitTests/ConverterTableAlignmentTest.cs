using NFun;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests;

/// <summary>
/// VarTypeConverter.CanBeConverted (the predicate) must agree with
/// GetConverterOrNull (the factory) — review N2/D1 found ≥5 divergent pairs.
/// One semantics: "a runtime conversion exists".
/// </summary>
public class ConverterTableAlignmentTest {

    private static readonly FunnyType Text = FunnyType.Text;

    [Test]
    public void CollectionToText_NotImplicitlyConvertible() {
        // The factory deliberately excludes collection sources from the ToText
        // shortcut (naive ToString yields "[a,b,c]"); the predicate said `true`
        // for ANYTHING → text — e.g. `a:list<text>; a[0] = [1,2]` slipped through.
        Assert.IsFalse(VarTypeConverter.CanBeConverted(
            FunnyType.ListOf(FunnyType.Int32), Text));
        Assert.IsFalse(VarTypeConverter.CanBeConverted(
            FunnyType.ArrayOf(FunnyType.Int32), Text));
        Assert.IsFalse(VarTypeConverter.CanBeConverted(
            FunnyType.MutableArrayOf(FunnyType.Int32), Text));
        Assert.IsFalse(VarTypeConverter.CanBeConverted(
            FunnyType.FixedArrayOf(FunnyType.Int32), Text));
    }

    [Test]
    public void PrimitiveToText_StillConvertible() {
        // ToText for primitives is public pinned behavior (FunnyTypeTest).
        Assert.IsTrue(VarTypeConverter.CanBeConverted(FunnyType.Int32, Text));
        Assert.IsTrue(VarTypeConverter.CanBeConverted(FunnyType.Bool, Text));
    }

    [Test]
    public void CharArrayToText_Convertible() {
        // text IS arr(char) — identity, not the ToText shortcut.
        Assert.IsTrue(VarTypeConverter.CanBeConverted(
            FunnyType.ArrayOf(FunnyType.Char), Text));
    }

    [Test]
    public void MapToEnumerable_Convertible() {
        // Factory: any concrete collection → Enumerable is NoConvertion; the
        // predicate's switch was missing the Map case.
        var map = FunnyType.MapOf(FunnyType.Text, FunnyType.Int32);
        Assert.IsTrue(VarTypeConverter.CanBeConverted(
            map, FunnyType.EnumerableOf(FunnyType.Any)));
    }

    [Test]
    public void MapToClearable_Convertible() {
        var map = FunnyType.MapOf(FunnyType.Text, FunnyType.Int32);
        Assert.IsTrue(VarTypeConverter.CanBeConverted(
            map, FunnyType.ClearableOf(FunnyType.Any)));
    }

    [Test]
    public void MapToSameMap_Convertible() {
        // Factory short-circuits from.Equals(to) → NoConvertion; the predicate
        // had no equality short-circuit, so map<K,V> → same map<K,V> was false.
        var map = FunnyType.MapOf(FunnyType.Text, FunnyType.Int32);
        Assert.IsTrue(VarTypeConverter.CanBeConverted(map, map));
    }

    [Test]
    public void LegacyArrayToMutableArray_Convertible() {
        // Reverse ee→lang bridge exists in the factory (assignment slots that
        // absorb ee-mode LINQ results); predicate was missing it.
        Assert.IsTrue(VarTypeConverter.CanBeConverted(
            FunnyType.ArrayOf(FunnyType.UInt8),
            FunnyType.MutableArrayOf(FunnyType.Int32)));
    }

    [Test]
    public void FixedArraySameKind_ElementCovariant() {
        Assert.IsTrue(VarTypeConverter.CanBeConverted(
            FunnyType.FixedArrayOf(FunnyType.UInt8),
            FunnyType.FixedArrayOf(FunnyType.Int32)));
        Assert.IsFalse(VarTypeConverter.CanBeConverted(
            FunnyType.FixedArrayOf(FunnyType.Int32),
            FunnyType.FixedArrayOf(FunnyType.Bool)));
    }
}
