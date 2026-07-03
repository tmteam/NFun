using System.Collections.Generic;
using System.Linq;
using NFun.Runtime.Lists;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests;

/// <summary>
/// Stage 2.2 runtime-container tests for <see cref="MutableFunnyList"/>.
/// Read-surface only — mutation (add/remove/clear) arrives in Stage 3.
/// </summary>
[TestFixture]
public class MutableFunnyListTest {
    [Test]
    public void EmptyList_CountIsZero() {
        var list = new MutableFunnyList(FunnyType.Int32);
        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(FunnyType.Int32, list.ElementType);
    }

    [Test]
    public void FromItems_PreservesOrder() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual(1, list.GetElementOrNull(0));
        Assert.AreEqual(2, list.GetElementOrNull(1));
        Assert.AreEqual(3, list.GetElementOrNull(2));
    }

    [Test]
    public void GetElementOrNull_OutOfRange_ReturnsNull() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2 });
        Assert.IsNull(list.GetElementOrNull(-1));
        Assert.IsNull(list.GetElementOrNull(2));
        Assert.IsNull(list.GetElementOrNull(100));
    }

    [Test]
    public void As_TypedView_ReturnsTypedElements() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.As<int>().ToArray());
    }

    [Test]
    public void Iteration_YieldsElementsInOrder() {
        var list = new MutableFunnyList(FunnyType.Real, new object[] { 1.1, 2.2 });
        var seen = new List<object>();
        foreach (var v in list) seen.Add(v);
        CollectionAssert.AreEqual(new object[] { 1.1, 2.2 }, seen);
    }

    [Test]
    public void Equals_SameElements_ReturnsTrue() {
        var a = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        var b = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        Assert.IsTrue(a.Equals(b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void Equals_DifferentElements_ReturnsFalse() {
        var a = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        var b = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 4 });
        Assert.IsFalse(a.Equals(b));
    }

    [Test]
    public void Equals_DifferentLength_ReturnsFalse() {
        var a = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2 });
        var b = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        Assert.IsFalse(a.Equals(b));
    }

    [Test]
    public void Equals_EmptyLists_ReturnsTrue() {
        var a = new MutableFunnyList(FunnyType.Int32);
        var b = new MutableFunnyList(FunnyType.Int32);
        Assert.IsTrue(a.Equals(b));
    }

    // ─── Stage 3 (B.1): mutators ──────────────────────────────────────

    [Test]
    public void Add_AppendsAndInvalidatesHashCache() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2 });
        var firstHash = list.GetHashCode();
        list.Add(3);
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual(3, list.GetElementOrNull(2));
        Assert.AreNotEqual(firstHash, list.GetHashCode(),
            "Hash must recompute after mutation");
    }

    [Test]
    public void AddAll_AppendsRange() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1 });
        list.AddAll(new object[] { 2, 3, 4 });
        Assert.AreEqual(4, list.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, list.As<int>().ToArray());
    }

    [Test]
    public void Remove_FirstOccurrence_ReturnsTrueAndRemoves() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 2, 3 });
        Assert.IsTrue(list.Remove(2));
        Assert.AreEqual(3, list.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.As<int>().ToArray());
    }

    [Test]
    public void Remove_NotFound_ReturnsFalse() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        Assert.IsFalse(list.Remove(99));
        Assert.AreEqual(3, list.Count);
    }

    [Test]
    public void RemoveAt_ValidIndex_ReturnsElement() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 10, 20, 30 });
        Assert.AreEqual(20, list.RemoveAt(1));
        CollectionAssert.AreEqual(new[] { 10, 30 }, list.As<int>().ToArray());
    }

    [Test]
    public void RemoveAt_OutOfRange_ReturnsNullDoesNotMutate() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2 });
        Assert.IsNull(list.RemoveAt(5));
        Assert.IsNull(list.RemoveAt(-1));
        Assert.AreEqual(2, list.Count);
    }

    [Test]
    public void RemoveLast_NonEmpty_PopsTail() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        Assert.AreEqual(3, list.RemoveLast());
        Assert.AreEqual(2, list.Count);
    }

    [Test]
    public void RemoveLast_Empty_ReturnsNull() {
        var list = new MutableFunnyList(FunnyType.Int32);
        Assert.IsNull(list.RemoveLast());
    }

    [Test]
    public void Clear_EmptiesList() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        list.Clear();
        Assert.AreEqual(0, list.Count);
    }

    [Test]
    public void Clear_EmptyList_NoOp() {
        var list = new MutableFunnyList(FunnyType.Int32);
        Assert.DoesNotThrow(() => list.Clear());
        Assert.AreEqual(0, list.Count);
    }

    [Test]
    public void HashCacheInvalidated_AfterClear() {
        var list = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        var nonEmptyHash = list.GetHashCode();
        list.Clear();
        Assert.AreNotEqual(nonEmptyHash, list.GetHashCode(),
            "Empty list legitimately hashes to 0; cache must yield the updated value");
    }
}
