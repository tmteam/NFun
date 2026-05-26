using System.Collections.Generic;
using NFun.Functions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation;

/// <summary>
/// Pure syntactic analysis of boolean conditions to determine which variables
/// are guaranteed non-none in each branch of an if-then-else.
/// </summary>
internal static class NarrowingAnalyzer {
    private static readonly HashSet<string> Empty = new();

    public readonly record struct Result(HashSet<string> WhenTrue, HashSet<string> WhenFalse) {
        public static readonly Result None = new(Empty, Empty);
        public bool IsEmpty => WhenTrue.Count == 0 && WhenFalse.Count == 0;
    }

    /// <summary>
    /// Analyze a condition expression. Returns which variables are guaranteed non-none
    /// when the condition is true (WhenTrue) and when it is false (WhenFalse).
    /// </summary>
    public static Result Analyze(ISyntaxNode condition) => condition switch {
        // x == none  →  WhenTrue={}, WhenFalse={x}
        // x != none  →  WhenTrue={x}, WhenFalse={}
        BinOperatorSyntaxNode { Op: BinOp.Equal } eq    => AnalyzeEqualNone(eq.Left, eq.Right, isEqual: true),
        BinOperatorSyntaxNode { Op: BinOp.NotEqual } ne => AnalyzeEqualNone(ne.Left, ne.Right, isEqual: false),

        // a and b  →  WhenTrue=union(a.T, b.T), WhenFalse=intersect(a.F, b.F)
        BinOperatorSyntaxNode { Op: BinOp.And } and     => CombineAnd(Analyze(and.Left), Analyze(and.Right)),

        // a or b  →  WhenTrue=intersect(a.T, b.T), WhenFalse=union(a.F, b.F)
        BinOperatorSyntaxNode { Op: BinOp.Or } or       => CombineOr(Analyze(or.Left), Analyze(or.Right)),

        // not a  →  swap
        UnaryOperatorSyntaxNode { Op: UnOp.Not } not     => Swap(Analyze(not.Operand)),

        // Single-pair comparison chains: x == none, x != none
        ComparisonChainSyntaxNode chain when chain.Operators.Count == 1 => AnalyzeChain(chain),

        _ => Result.None
    };

    private static Result AnalyzeEqualNone(ISyntaxNode left, ISyntaxNode right, bool isEqual) {
        // x == none / x != none
        var varName = GetVariableIfOtherIsNone(left, right);
        if (varName != null) {
            var set = new HashSet<string> { varName };
            return isEqual ? new Result(Empty, set) : new Result(set, Empty);
        }
        // Compared to a bool literal — soundness rule:
        //
        //   `flag == true` is true ONLY when flag = true     → then-branch narrows
        //   `flag == true` is false when flag ∈ {false,none} → else does NOT narrow
        //   `flag != true` is true when flag ∈ {false,none}  → then does NOT narrow
        //   `flag != true` is false ONLY when flag = true    → else-branch narrows
        // (same for `== false` / `!= false`).
        //
        // Previously: returned Result(set, set), claiming both branches prove
        // not-`none` because "none != true and none != false". That's wrong —
        // `flag == true` being false doesn't tell you flag is not none, it
        // tells you flag is not `true` (the literal). flag could still be
        // none. The bug let `y = if(flag == true) flag else flag` infer
        // `y:bool`, but at runtime the else delivered `none` → cast crash.
        // (MR8Bug2.)
        varName = GetVariableIfOtherIsBoolLiteral(left, right);
        if (varName != null) {
            var set = new HashSet<string> { varName };
            return isEqual ? new Result(set, Empty) : new Result(Empty, set);
        }
        return Result.None;
    }

    private static Result AnalyzeChain(ComparisonChainSyntaxNode chain) {
        var op = chain.Operators[0].Type;
        if (op is TokType.Equal or TokType.NotEqual)
            return AnalyzeEqualNone(chain.Operands[0], chain.Operands[1], isEqual: op == TokType.Equal);
        return Result.None;
    }

    private static string GetVariableIfOtherIsNone(ISyntaxNode a, ISyntaxNode b) {
        if (IsNone(b)) return GetVariableOrSafeAccessRoot(a);
        if (IsNone(a)) return GetVariableOrSafeAccessRoot(b);
        return null;
    }

    private static string GetVariableIfOtherIsBoolLiteral(ISyntaxNode a, ISyntaxNode b) {
        if (IsBoolLiteral(b)) return GetVariableOrSafeAccessRoot(a);
        if (IsBoolLiteral(a)) return GetVariableOrSafeAccessRoot(b);
        return null;
    }

    /// <summary>
    /// Returns the variable name if the node is a simple variable reference,
    /// the ROOT variable of a safe-access chain (a?.foo, a?.bar()),
    /// or a field path like "s.age" for direct field access narrowing.
    /// </summary>
    private static string GetVariableOrSafeAccessRoot(ISyntaxNode node) {
        if (node is NamedIdSyntaxNode v) return v.Id;
        // a?.field → StructFieldAccessSyntaxNode with IsSafeAccess → narrows root
        if (node is StructFieldAccessSyntaxNode { IsSafeAccess: true } sfa)
            return GetVariableOrSafeAccessRoot(sfa.Source);
        // s.field → direct field access → narrows the field itself (s.field path)
        if (node is StructFieldAccessSyntaxNode { IsSafeAccess: false } dfa
            && dfa.Source is NamedIdSyntaxNode src)
            return src.Id + "." + dfa.FieldName;
        // a?.method() → FunCallSyntaxNode with IsSafeAccess + IsPipeForward
        if (node is FunCallSyntaxNode { IsSafeAccess: true, IsPipeForward: true } fc && fc.Args.Length > 0)
            return GetVariableOrSafeAccessRoot(fc.Args[0]);
        return null;
    }

    /// <summary>Checks if a narrowing identifier is a field path (contains '.')</summary>
    internal static bool IsFieldPath(string id) => id.Contains('.');

    /// <summary>Splits "s.age" into ("s", "age")</summary>
    internal static (string varName, string fieldName) SplitFieldPath(string path) {
        var dot = path.IndexOf('.');
        return (path[..dot], path[(dot + 1)..]);
    }

    private static bool IsNone(ISyntaxNode node) =>
        node is ConstantSyntaxNode { Value: FunnyNone };

    private static bool IsBoolLiteral(ISyntaxNode node) =>
        node is ConstantSyntaxNode c && c.Value is bool;

    private static Result Swap(Result r) =>
        r.IsEmpty ? r : new Result(r.WhenFalse, r.WhenTrue);

    private static Result CombineAnd(Result a, Result b) {
        if (a.IsEmpty) return b;
        if (b.IsEmpty) return a;
        return new Result(Union(a.WhenTrue, b.WhenTrue), Intersect(a.WhenFalse, b.WhenFalse));
    }

    private static Result CombineOr(Result a, Result b) {
        // No early return: IsEmpty means "no narrowing info" (e.g., literal `true`).
        // For OR, WhenTrue = Intersect (both sides must prove non-none independently),
        // so intersecting with empty correctly yields empty.
        // Early return would incorrectly preserve the other side's WhenTrue.
        // Example: `x != none or true` — WhenTrue must be {} (true branch is always taken).
        return new Result(Intersect(a.WhenTrue, b.WhenTrue), Union(a.WhenFalse, b.WhenFalse));
    }

    private static HashSet<string> Union(HashSet<string> a, HashSet<string> b) {
        if (a.Count == 0) return b;
        if (b.Count == 0) return a;
        var result = new HashSet<string>(a);
        result.UnionWith(b);
        return result;
    }

    private static HashSet<string> Intersect(HashSet<string> a, HashSet<string> b) {
        if (a.Count == 0 || b.Count == 0) return Empty;
        var result = new HashSet<string>(a);
        result.IntersectWith(b);
        return result.Count == 0 ? Empty : result;
    }
}
