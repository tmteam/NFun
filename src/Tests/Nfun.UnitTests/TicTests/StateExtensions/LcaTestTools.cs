namespace NFun.UnitTests.TicTests.StateExtensions;

using System.Collections.Generic;
using System.Linq;
using Tic;
using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static Tic.SolvingStates.StatePrimitive;

public record TypeMap(StatePrimitive Left, StatePrimitive Right, StatePrimitive Lca);

public static class LcaTestTools {
    /// <summary>
    /// Full 23-point primitive lattice: Any, Char, Bool, Ip, None and the numeric family
    /// Real → F32 → I96 → { I64..I8 ; U64..U4 } — including the abstract mid-points
    /// (I96/I48/I24/I12, U48/U24/U12/U4) that have no runtime representation.
    /// </summary>
    public static readonly StatePrimitive[] PrimitiveTypes = {
        Any, Char, Bool, Ip,
        Real, F32, I96, I64, I48, I32, I24, I16, I12, I8,
        U64, U48, U32, U24, U16, U12, U8, U4,
        None,
    };

    /// <summary>
    /// Exhaustive expected-LCA table over the 22 lattice points EXCLUDING None
    /// (22×22 = 484 ordered pairs). Expected values come from an independent
    /// partial-order oracle (see <see cref="Leq"/>) by brute-force least-upper-bound
    /// search — NOT from the production LcaMap closure formulas.
    ///
    /// None is excluded deliberately: at state level None joins through the Optional
    /// axis (LCA(None, T) = opt(T) for T ∉ {Any, None}), so its pairs are not
    /// expressible as a primitive TypeMap. They are covered exhaustively by the None
    /// loops in LcaOptionalTest / GcdOptionalTest / NoneAlgebraTest.
    /// </summary>
    public static readonly IList<TypeMap> PrimitiveTypesLca;

    static LcaTestTools() {
        var tablePoints = PrimitiveTypes.Where(p => !p.Equals(None)).ToArray();

        var maps = new List<TypeMap>(tablePoints.Length * tablePoints.Length);
        foreach (var a in tablePoints)
        foreach (var b in tablePoints)
        {
            var upperBounds = tablePoints.Where(c => Leq(a, c) && Leq(b, c)).ToList();
            Assert.IsNotEmpty(upperBounds,
                $"Lattice defect: no common upper bound for ({a}, {b})");
            var minimal = upperBounds
                .Where(c => upperBounds.All(other => !Leq(other, c) || other.Equals(c)))
                .ToList();
            Assert.AreEqual(1, minimal.Count,
                $"Lattice defect: LUB({a}, {b}) is not unique. " +
                $"Minimal upper bounds: {string.Join(", ", minimal)}");
            maps.Add(new TypeMap(a, b, minimal[0]));
        }

        PrimitiveTypesLca = maps;
    }

    /// <summary>
    /// Independent subtyping oracle `a ≤ b` — derived from the first principles of the
    /// 23-point lattice (Specs/Tic/TicTypeSystem.md), NOT from the LcaMap closure formulas:
    ///   * Any is the top; None ≤ Any only; Char/Bool/Ip relate only to self and Any.
    ///   * Real is the numeric top; F32 sits below Real and above every integer;
    ///     I96 is the integer top.
    ///   * I_n ≤ I_k iff n ≤ k;  U_m ≤ U_j iff m ≤ j.
    ///   * U_m ≤ I_n iff n ≥ m + 1 (a sign bit is required); I_n ≤ U_m — never.
    ///   * U4 has nominal width 7 (the common subset of I8 and U8).
    /// </summary>
    public static bool Leq(StatePrimitive a, StatePrimitive b) {
        if (a.Equals(b)) return true;
        if (b.Equals(Any)) return true;
        if (a.Equals(Any)) return false;
        if (a.Equals(None) || b.Equals(None)) return false;
        if (!a.IsNumeric || !b.IsNumeric) return false; // Char/Bool/Ip are incomparable
        if (b.Equals(Real)) return true;
        if (a.Equals(Real)) return false;
        if (b.Equals(F32)) return true;  // every integer ≤ F32
        if (a.Equals(F32)) return false;
        if (b.Equals(I96)) return true;  // every integer ≤ I96
        if (a.Equals(I96)) return false;

        var (aSigned, aWidth) = SignAndWidth(a);
        var (bSigned, bWidth) = SignAndWidth(b);
        if (aSigned == bSigned) return aWidth <= bWidth;
        if (!aSigned && bSigned) return bWidth >= aWidth + 1;
        return false; // signed never fits into unsigned
    }

    private static (bool signed, int width) SignAndWidth(StatePrimitive p) =>
        p.Name switch {
            PrimitiveTypeName.I64 => (true, 64),
            PrimitiveTypeName.I48 => (true, 48),
            PrimitiveTypeName.I32 => (true, 32),
            PrimitiveTypeName.I24 => (true, 24),
            PrimitiveTypeName.I16 => (true, 16),
            PrimitiveTypeName.I12 => (true, 12),
            PrimitiveTypeName.I8 => (true, 8),
            PrimitiveTypeName.U64 => (false, 64),
            PrimitiveTypeName.U48 => (false, 48),
            PrimitiveTypeName.U32 => (false, 32),
            PrimitiveTypeName.U24 => (false, 24),
            PrimitiveTypeName.U16 => (false, 16),
            PrimitiveTypeName.U12 => (false, 12),
            PrimitiveTypeName.U8 => (false, 8),
            PrimitiveTypeName.U4 => (false, 7),
            _ => throw new System.InvalidOperationException($"{p} is not a fixed-width integer")
        };

    public static void AssertLca(ITicNodeState a, ITicNodeState b, ITicNodeState expected) {

        var result1 = a.Lca(b);
        var result2 = b.Lca(a);

        var aRef = SolvingStates.Ref(TicNode.CreateTypeVariableNode("a", a));
        var bRef = SolvingStates.Ref(TicNode.CreateTypeVariableNode("b", b));

        var aRefRef = SolvingStates.Ref(TicNode.CreateTypeVariableNode("aa", aRef));
        var bRefRef = SolvingStates.Ref(TicNode.CreateTypeVariableNode("bb", bRef));

        var result3 = aRef.Lca(bRef);
        var result4 = bRefRef.Lca(aRefRef);

        Assert.AreEqual(expected, result1, $"1: {a.StateDescription} LCA {b.StateDescription} = {result1.StateDescription}, but was expected {expected.StateDescription}");
        Assert.AreEqual(expected, result2, $"1: {b.StateDescription} LCA {a.StateDescription} = {result2.StateDescription}, but was expected {expected.StateDescription}");
        Assert.AreEqual(expected, result3, $"1: {aRef.StateDescription} LCA {bRef.StateDescription} = {result3.StateDescription}, but was expected {expected.StateDescription}");
        Assert.AreEqual(expected, result4, $"1: {aRefRef.StateDescription} LCA {bRefRef.StateDescription} = {result4.StateDescription}, but was expected {expected.StateDescription}");
    }
}
