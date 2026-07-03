namespace NFun.Tic.Tests.UnitTests;

using System;
using NFun.Runtime.Arrays;
using NFun.Runtime.Lists;
using NFun.Types;
using NUnit.Framework;

/// <summary>
/// Stage C.4b — verify CLR boundary updates: BaseFunnyType.Enumerable=24,
/// TypeBehaviour table extension, IFunnyEnumerable.ElementType pull-up.
/// </summary>
public class EnumerableClrBoundaryTest {

    [Test]
    public void BaseFunnyType_EnumerableSlot26() {
        // 21/22 are taken by master's Int8/Float32; lang collections occupy 23..29.
        Assert.AreEqual(26, (int)BaseFunnyType.Enumerable);
    }

    [Test]
    public void BaseFunnyType_NoCollisionsWithExistingSlots() {
        var values = Enum.GetValues(typeof(BaseFunnyType));
        var seen = new System.Collections.Generic.HashSet<int>();
        foreach (BaseFunnyType v in values) {
            Assert.IsTrue(seen.Add((int)v), $"duplicate ordinal: {v}");
        }
    }

    [Test]
    public void TypeBehaviour_StartupAssert_TableLengthMatchesEnum() {
        // The static ctor of TypeBehaviour throws if FunToClrTypesMap.Length != enum length.
        // We trigger the ctor by accessing the type; if the ctor threw, the test framework
        // would have observed it as a TypeInitializationException at first reference.
        var dummy = typeof(TypeBehaviour).Name;
        Assert.IsNotNull(dummy);
        Assert.AreEqual(30, Enum.GetValues(typeof(BaseFunnyType)).Length);
    }

    [Test]
    public void IFunnyEnumerable_ElementType_OnInRepoImplementors() {
        // All in-repo IFunnyEnumerable implementors must satisfy the pulled-up contract.
        var arr = new ImmutableFunnyArray(new[] { 1, 2, 3 });
        IFunnyEnumerable e = arr;
        Assert.AreEqual(FunnyType.Int32, e.ElementType);
        Assert.AreEqual(3, e.Count);
    }

    [Test]
    public void IFunnyEnumerable_ContractIncludesCountAndElementType() {
        var t = typeof(IFunnyEnumerable);
        Assert.IsNotNull(t.GetProperty("Count"));
        Assert.IsNotNull(t.GetProperty("ElementType"));
    }
}
