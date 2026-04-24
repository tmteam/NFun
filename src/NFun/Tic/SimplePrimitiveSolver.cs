using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NFun.Functions;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tic.SolvingStates;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Tic;

/// <summary>
/// Lightweight type solver for primitive-only expressions.
/// When the body has no composite types (arrays, structs, optionals, functions),
/// TIC degenerates to interval arithmetic on the primitive lattice.
///
/// Single-pass architecture: gate check + constraint building are fused into one AST walk.
/// If any non-primitive node is encountered, returns null immediately.
///
/// All interval operations (LCA, GCD, Fits) are O(1) table lookups on ordinals.
/// Total complexity: O(N) where N = AST node count.
/// </summary>
internal sealed class SimplePrimitiveSolver {

    private const byte OPEN = 0xFF;
    private const int PRIM_COUNT = 19; // must match StatePrimitive type count (0..18)

    private struct Group {
        public byte Desc;       // ordinal lower bound, OPEN = unconstrained
        public byte Anc;        // ordinal upper bound, OPEN = unconstrained
        public byte Pref;       // preferred resolution ordinal, OPEN = none
        public int Parent;      // union-find parent (int, not byte — safe for any count)
        public byte Rank;       // union-find rank
        public bool Comparable; // comparable type required
    }

    private Group[] _groups;

    private static readonly byte[] s_lcaTable;   // [PRIM_COUNT * PRIM_COUNT], flattened
    private static readonly byte[] s_gcdTable;   // OPEN = no GCD
    private static readonly bool[] s_fitsTable;  // desc fits into anc?
    private static readonly StatePrimitive[] s_ordToState;

    // Set to true when conflicting constraints are detected (e.g. Real ∩ Bool = ∅)
    private bool _failed;
    // Resolve cache: ordinal per root group, OPEN = not yet resolved. Allocated before resolve phase.
    private byte[] _resolvedCache;

    private int _groupCount;

    // Syntax node id → group id (-1 = not allocated)
    private readonly int[] _nodeGroup;

    // Variable name → group id
    private readonly Dictionary<string, int> _varGroup;

    // Directed edges: expr ≤ var
    private (int fromGroup, int toGroup)[] _edges;
    private int _edgeCount;

    // Generic call tracking: call OrderNumber → generic group id (array, not dict)
    private int[] _callGenericGroup; // -1 = no generic

    // Dependencies
    private readonly IConstantList _constants;
    // Cached preferred int type ordinal (computed once)
    private readonly byte _preferredIntOrd;

    private static readonly TicNode[] s_syntaxNodeSingletons;
    // Cached single-element arrays for RememberGenericCallArguments (avoids per-call allocation)
    private static readonly StateRefTo[][] s_singleRefToArrays;
    // Ordinal → FunnyType mapping (mirrors TicTypesConverter.ToConcrete but indexed by ordinal)
    private static readonly FunnyType[] s_ordToFunnyType;

    private static readonly PrimitiveTypeName[] s_allPrimitives = {
        PrimitiveTypeName.Any, PrimitiveTypeName.Bool, PrimitiveTypeName.Char,
        PrimitiveTypeName.Ip, PrimitiveTypeName.Real, PrimitiveTypeName.I96,
        PrimitiveTypeName.I64, PrimitiveTypeName.I48, PrimitiveTypeName.I32,
        PrimitiveTypeName.I24, PrimitiveTypeName.I16, PrimitiveTypeName.U64,
        PrimitiveTypeName.U48, PrimitiveTypeName.U32, PrimitiveTypeName.U24,
        PrimitiveTypeName.U16, PrimitiveTypeName.U12, PrimitiveTypeName.U8,
        PrimitiveTypeName.None
    };

    static SimplePrimitiveSolver() {
        // Build ordinal lookup tables by iterating all StatePrimitive pairs
        // and calling their authoritative LCA/GCD/Fits methods.
        s_ordToState = new StatePrimitive[PRIM_COUNT];
        for (int i = 0; i < s_allPrimitives.Length; i++) {
            var p = new StatePrimitive(s_allPrimitives[i]);
            s_ordToState[p.Order] = p;
        }

        s_lcaTable = new byte[PRIM_COUNT * PRIM_COUNT];
        s_gcdTable = new byte[PRIM_COUNT * PRIM_COUNT];
        s_fitsTable = new bool[PRIM_COUNT * PRIM_COUNT];

        for (int a = 0; a < PRIM_COUNT; a++) {
            for (int b = 0; b < PRIM_COUNT; b++) {
                var pa = s_ordToState[a];
                var pb = s_ordToState[b];
                int idx = a * PRIM_COUNT + b;

                // LCA
                var lca = pa.GetLastCommonPrimitiveAncestor(pb);
                s_lcaTable[idx] = lca != null ? (byte)lca.Order : OPEN;

                // GCD
                var gcd = pa.GetFirstCommonDescendantOrNull(pb);
                s_gcdTable[idx] = gcd != null ? (byte)gcd.Order : OPEN;

                // Fits (desc can be converted to anc)
                s_fitsTable[idx] = pa.CanBePessimisticConvertedTo(pb);
            }
        }

        // Flyweight singletons
        s_syntaxNodeSingletons = new TicNode[PRIM_COUNT];
        var refToSingletons = new StateRefTo[PRIM_COUNT];
        s_singleRefToArrays = new StateRefTo[PRIM_COUNT][];

        s_ordToFunnyType = new FunnyType[PRIM_COUNT];
        for (int i = 0; i < PRIM_COUNT; i++) {
            var p = s_ordToState[i];
            if (p == null) continue;
            s_syntaxNodeSingletons[i] = TicNode.CreateSyntaxNode(0, p, true);
            refToSingletons[i] = new StateRefTo(TicNode.CreateTypeVariableNode("T", p, true));
            s_singleRefToArrays[i] = new[] { refToSingletons[i] };
            s_ordToFunnyType[i] = TypeInferenceAdapter.TicTypesConverter.ToConcrete(p.Name);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte OrdOf(StatePrimitive p) => (byte)p.Order;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte LcaOrd(byte a, byte b) => s_lcaTable[a * PRIM_COUNT + b];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GcdOrd(byte a, byte b) => s_gcdTable[a * PRIM_COUNT + b];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FitsOrd(byte desc, byte anc) => s_fitsTable[desc * PRIM_COUNT + anc];

    private SimplePrimitiveSolver(
        int maxNodeId,
        IConstantList constants,
        DialectSettings dialect) {
        _groups = new Group[maxNodeId + 32];
        for (int i = 0; i < _groups.Length; i++) {
            _groups[i].Parent = i;
            _groups[i].Desc = OPEN;
            _groups[i].Anc = OPEN;
            _groups[i].Pref = OPEN;
        }

        _nodeGroup = new int[maxNodeId];
        _callGenericGroup = new int[maxNodeId];
        // Fill both with -1 in one pass
        Array.Fill(_nodeGroup, -1);
        Array.Fill(_callGenericGroup, -1);

        _varGroup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        _edges = new (int, int)[16];
        _edgeCount = 0;
        _constants = constants;
        _groupCount = 0;
        _preferredIntOrd = dialect.IntegerPreferredType switch {
            IntegerPreferredType.I32  => OrdOf(StatePrimitive.I32),
            IntegerPreferredType.I64  => OrdOf(StatePrimitive.I64),
            IntegerPreferredType.Real => OrdOf(StatePrimitive.Real),
            _                         => OPEN
        };
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Find(int x) {
        while (true) {
            ref var node = ref _groups[x];
            var p = node.Parent;
            if (p == x) return x;
            var gp = _groups[p].Parent;
            node.Parent = gp;
            x = gp;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NewGroup() {
        var id = _groupCount++;
        if (id >= _groups.Length) Grow();
        // _groups[id].Parent = id already set by constructor/Grow
        return id;
    }

    private void Unite(int a, int b) {
        a = Find(a); b = Find(b);
        if (a == b) return;
        if (_groups[a].Rank < _groups[b].Rank) { var t = a; a = b; b = t; }
        _groups[b].Parent = a;
        if (_groups[a].Rank == _groups[b].Rank) _groups[a].Rank++;
        MergeInto(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MergeInto(int root, int other) {
        if (_groups[other].Desc != OPEN) {
            if (_groups[root].Desc == OPEN) _groups[root].Desc = _groups[other].Desc;
            else _groups[root].Desc = LcaOrd(_groups[root].Desc, _groups[other].Desc);
        }
        if (_groups[other].Anc != OPEN) {
            if (_groups[root].Anc == OPEN) _groups[root].Anc = _groups[other].Anc;
            else {
                var gcd = GcdOrd(_groups[root].Anc, _groups[other].Anc);
                if (gcd != OPEN) _groups[root].Anc = gcd;
                else _failed = true; // incompatible ancestor constraints (e.g. I96 vs Bool)
            }
        }
        // After merge, check that the interval [desc..anc] is satisfiable
        if (_groups[root].Desc != OPEN && _groups[root].Anc != OPEN
            && !FitsOrd(_groups[root].Desc, _groups[root].Anc))
            _failed = true;
        _groups[root].Comparable |= _groups[other].Comparable;
        if (_groups[root].Pref == OPEN) _groups[root].Pref = _groups[other].Pref;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow() {
        var oldLen = _groups.Length;
        var nc = oldLen * 2;
        Array.Resize(ref _groups, nc);
        for (int i = oldLen; i < nc; i++) {
            _groups[i].Parent = i;
            _groups[i].Desc = OPEN;
            _groups[i].Anc = OPEN;
            _groups[i].Pref = OPEN;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetOrCreateNodeGroup(int nodeId) {
        var g = _nodeGroup[nodeId];
        if (g >= 0) return g;
        g = NewGroup();
        _nodeGroup[nodeId] = g;
        return g;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetOrCreateVarGroup(string name) {
        if (_varGroup.TryGetValue(name, out var g)) return g;
        g = NewGroup();
        _varGroup[name] = g;
        return g;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetConcrete(int gid, byte ord) {
        var r = Find(gid);
        // Detect conflicting concrete constraints (e.g. Real vs Bool)
        if (_groups[r].Desc != OPEN && !FitsOrd(_groups[r].Desc, ord))
            _failed = true;
        if (_groups[r].Anc != OPEN && !FitsOrd(ord, _groups[r].Anc))
            _failed = true;
        _groups[r].Desc = ord; _groups[r].Anc = ord;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetInterval(int gid, byte desc, byte anc, byte pref) {
        var r = Find(gid);
        _groups[r].Desc = desc; _groups[r].Anc = anc;
        if (_groups[r].Pref == OPEN) _groups[r].Pref = pref;
    }

    private void AddEdge(int exprGid, int varGid) {
        if (_edgeCount >= _edges.Length)
            Array.Resize(ref _edges, _edges.Length * 2);
        _edges[_edgeCount++] = (exprGid, varGid);
    }

    /// <summary>Apply generic constraint bounds to the result group's root.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyGenericConstraint(int resultGid, GenericConstrains c) {
        var r = Find(resultGid);
        if (c.Descendant != null) {
            var descOrd = OrdOf(c.Descendant);
            if (_groups[r].Desc == OPEN) _groups[r].Desc = descOrd;
            else _groups[r].Desc = LcaOrd(_groups[r].Desc, descOrd);
        }
        if (c.Ancestor != null) {
            var ancOrd = OrdOf(c.Ancestor);
            if (_groups[r].Anc == OPEN) _groups[r].Anc = ancOrd;
            else { var gcd = GcdOrd(_groups[r].Anc, ancOrd); if (gcd != OPEN) _groups[r].Anc = gcd; else _failed = true; }
        }
        if (c.IsComparable) _groups[r].Comparable = true;
    }

    /// <summary>Apply generic constraint to group and unite with args. No array allocation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetupPureGenericBinOp(int leftId, int rightId, int resultId, GenericConstrains c) {
        // Use result group as the generic group — no extra NewGroup needed
        var rGid = GetOrCreateNodeGroup(resultId);
        ApplyGenericConstraint(rGid, c);
        Unite(GetOrCreateNodeGroup(leftId), rGid);
        Unite(GetOrCreateNodeGroup(rightId), rGid);
        _callGenericGroup[resultId] = rGid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetupPureGenericUnOp(int operandId, int resultId, GenericConstrains c) {
        var rGid = GetOrCreateNodeGroup(resultId);
        ApplyGenericConstraint(rGid, c);
        Unite(GetOrCreateNodeGroup(operandId), rGid);
        _callGenericGroup[resultId] = rGid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPrimitiveBaseType(BaseFunnyType t) =>
        t is BaseFunnyType.Bool or BaseFunnyType.Char or BaseFunnyType.Ip or BaseFunnyType.Any
            or BaseFunnyType.Real or BaseFunnyType.Int16 or BaseFunnyType.Int32 or BaseFunnyType.Int64
            or BaseFunnyType.UInt8 or BaseFunnyType.UInt16 or BaseFunnyType.UInt32 or BaseFunnyType.UInt64;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasOnlyPrimitiveTypes(IFunctionSignature sig) {
        if (!IsPrimitiveBaseType(sig.ReturnType.BaseType)) return false;
        for (int i = 0; i < sig.ArgTypes.Length; i++)
            if (!IsPrimitiveBaseType(sig.ArgTypes[i].BaseType)) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSimpleTypeSyntax(TypeSyntax syntax) =>
        syntax switch {
            TypeSyntax.EmptyType => true,
            TypeSyntax.Named n  => IsPrimitiveTypeName(n.Name),
            _                   => false
        };

    private static readonly HashSet<string> s_primitiveTypeNames =
        new(StringComparer.OrdinalIgnoreCase) {
            "int16", "int", "int32", "int64",
            "byte", "uint8", "uint16", "uint", "uint32", "uint64",
            "real", "bool", "char", "ip", "any"
        };

    private static bool IsPrimitiveTypeName(string name) => s_primitiveTypeNames.Contains(name);

    private static bool AllAprioriTypesArePrimitive(IAprioriTypesMap aprioriTypes) {
        foreach (var a in aprioriTypes) {
            if (!a.Type.IsNumeric() && a.Type.BaseType is not (
                    BaseFunnyType.Bool or BaseFunnyType.Char
                    or BaseFunnyType.Ip or BaseFunnyType.Any
                    or BaseFunnyType.Empty))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Check if a resolved signature is simple (primitive-only).
    /// For PureGenericFunctionBase: must have exactly 1 constraint.
    /// For IConcreteFunction: all arg/return types must be primitive.
    /// Returns false for null or any other signature type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSimpleSignature(IFunctionSignature sig) {
        if (sig is PureGenericFunctionBase p) return p.Constrains.Length == 1;
        return sig is IConcreteFunction && HasOnlyPrimitiveTypes(sig);
    }

    /// <summary>
    /// Walks the AST, performing gate check and constraint building simultaneously.
    /// Returns false on the first non-primitive-compatible node, allowing immediate bail-out.
    /// Side effect: resolves and caches function signatures on syntax nodes.
    /// </summary>
    private bool WalkAndCheck(ISyntaxNode node, IFunctionRegistry functions) {
        switch (node) {
            case GenericIntSyntaxNode g:
                SetupIntConst(g);
                return true;

            case IpAddressConstantSyntaxNode ip:
                SetConcrete(GetOrCreateNodeGroup(ip.OrderNumber), s_ordIp);
                return true;

            case ConstantSyntaxNode c:
                if (!IsPrimitiveBaseType(c.OutputType.BaseType)) return false;
                SetupConst(c);
                return true;

            case NamedIdSyntaxNode n:
                SetupNamedId(n);
                return true;

            case BinOperatorSyntaxNode b: {
                var sig = functions.GetOrNull(b.Id, 2);
                b.ResolvedSignature = sig;
                if (!IsSimpleSignature(sig)) return false;
                if (!WalkAndCheck(b.Left, functions)) return false;
                if (!WalkAndCheck(b.Right, functions)) return false;
                SetupBinOp(b);
                return true;
            }

            case UnaryOperatorSyntaxNode u: {
                var sig = functions.GetOrNull(u.Id, 1);
                u.ResolvedSignature = sig;
                if (!IsSimpleSignature(sig)) return false;
                if (!WalkAndCheck(u.Operand, functions)) return false;
                SetupUnaryOp(u);
                return true;
            }

            case FunCallSyntaxNode f: {
                var sig = functions.GetOrNull(f.Id, f.Args.Length);
                f.ResolvedSignature = sig;
                if (!IsSimpleSignature(sig)) return false;
                for (int i = 0; i < f.Args.Length; i++)
                    if (!WalkAndCheck(f.Args[i], functions)) return false;
                SetupFunCall(f);
                return true;
            }

            case IfThenElseSyntaxNode ite: {
                foreach (var c in ite.Ifs) {
                    if (!WalkAndCheck(c.Condition, functions)) return false;
                    if (!WalkAndCheck(c.Expression, functions)) return false;
                }
                if (!WalkAndCheck(ite.ElseExpr, functions)) return false;
                SetupIfElse(ite);
                return true;
            }

            case ComparisonChainSyntaxNode cc: {
                for (int i = 0; i < cc.Operands.Count; i++)
                    if (!WalkAndCheck(cc.Operands[i], functions)) return false;
                SetupCompareChain(cc);
                return true;
            }

            default:
                return false; // unknown node = bail out
        }
    }

    private void SetupConst(ConstantSyntaxNode c) =>
        SetConcreteChecked(GetOrCreateNodeGroup(c.OrderNumber), ToPrimitiveOrd(c.OutputType));

    private void SetupIntConst(GenericIntSyntaxNode node) {
        var gid = GetOrCreateNodeGroup(node.OrderNumber);
        if (node.IsHexOrBin) SetupHexBin(gid, node.Value);
        else SetupDecimal(gid, node.Value);
    }

    // Cached ordinals for frequently used primitives in interval setup
    private static readonly byte s_ordI16 = OrdOf(StatePrimitive.I16);
    private static readonly byte s_ordI32 = OrdOf(StatePrimitive.I32);
    private static readonly byte s_ordI64 = OrdOf(StatePrimitive.I64);
    private static readonly byte s_ordI96 = OrdOf(StatePrimitive.I96);
    private static readonly byte s_ordU8  = OrdOf(StatePrimitive.U8);
    private static readonly byte s_ordU12 = OrdOf(StatePrimitive.U12);
    private static readonly byte s_ordU16 = OrdOf(StatePrimitive.U16);
    private static readonly byte s_ordU24 = OrdOf(StatePrimitive.U24);
    private static readonly byte s_ordU32 = OrdOf(StatePrimitive.U32);
    private static readonly byte s_ordU48 = OrdOf(StatePrimitive.U48);
    private static readonly byte s_ordU64 = OrdOf(StatePrimitive.U64);
    private static readonly byte s_ordReal = OrdOf(StatePrimitive.Real);
    private static readonly byte s_ordBool = OrdOf(StatePrimitive.Bool);
    private static readonly byte s_ordChar = OrdOf(StatePrimitive.Char);
    private static readonly byte s_ordIp   = OrdOf(StatePrimitive.Ip);
    private static readonly byte s_ordAny  = OrdOf(StatePrimitive.Any);

    private void SetupHexBin(int gid, object value) {
        if (value is long l) {
            if (l <= 0) {
                if (l >= short.MinValue) SetInterval(gid, s_ordI16, s_ordI64, s_ordI32);
                else if (l >= int.MinValue) SetInterval(gid, s_ordI32, s_ordI64, s_ordI32);
                else SetConcrete(gid, s_ordI64);
                return;
            }
            SetupHexBinPositive(gid, (ulong)l);
        } else if (value is ulong u) SetupHexBinPositive(gid, u);
    }

    private void SetupHexBinPositive(int gid, ulong v) {
        if      (v <= byte.MaxValue)          SetInterval(gid, s_ordU8,  s_ordI96, s_ordI32);
        else if (v <= (ulong)short.MaxValue)  SetInterval(gid, s_ordU12, s_ordI96, s_ordI32);
        else if (v <= ushort.MaxValue)         SetInterval(gid, s_ordU16, s_ordI96, s_ordI32);
        else if (v <= int.MaxValue)            SetInterval(gid, s_ordU24, s_ordI96, s_ordI32);
        else if (v <= uint.MaxValue)           SetInterval(gid, s_ordU32, s_ordI96, s_ordI64);
        else if (v <= (ulong)long.MaxValue)   SetInterval(gid, s_ordU48, s_ordI96, s_ordI64);
        else SetConcrete(gid, s_ordU64);
    }

    private void SetupDecimal(int gid, object value) {
        if (value is long l) {
            if (l <= 0) {
                var desc = l >= short.MinValue ? s_ordI16
                         : l >= int.MinValue   ? s_ordI32
                         :                       s_ordI64;
                SetInterval(gid, desc, s_ordReal, _preferredIntOrd);
                return;
            }
            SetupDecimalPositive(gid, (ulong)l);
        } else if (value is ulong u) SetupDecimalPositive(gid, u);
    }

    private void SetupDecimalPositive(int gid, ulong v) {
        var desc = v <= byte.MaxValue          ? s_ordU8
                 : v <= (ulong)short.MaxValue  ? s_ordU12
                 : v <= ushort.MaxValue         ? s_ordU16
                 : v <= int.MaxValue            ? s_ordU24
                 : v <= uint.MaxValue           ? s_ordU32
                 : v <= (ulong)long.MaxValue   ? s_ordU48
                 :                               s_ordU64;
        SetInterval(gid, desc, s_ordReal, _preferredIntOrd);
    }

    private void SetupNamedId(NamedIdSyntaxNode node) {
        var gid = GetOrCreateNodeGroup(node.OrderNumber);
        // Check constants only if this name is NOT already known as a variable
        if (_varGroup.TryGetValue(node.Id, out var existingVarGid)) {
            node.IdType = NamedIdNodeType.Variable;
            Unite(existingVarGid, gid);
            return;
        }
        if (_constants.TryGetConstant(node.Id, out var constant)) {
            node.IdType = NamedIdNodeType.Constant;
            node.IdContent = constant;
            SetConcreteChecked(gid, ToPrimitiveOrd(constant.Type));
            return;
        }
        node.IdType = NamedIdNodeType.Variable;
        Unite(GetOrCreateVarGroup(node.Id), gid);
    }

    /// <summary>
    /// Pow special case: non-const or negative exponent → force all args and result to Real.
    /// Returns true if the exponent requires Real (handled), false if normal generic path should be used.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPowForcedReal(ISyntaxNode exponentNode) =>
        !(exponentNode is GenericIntSyntaxNode intN && !(intN.Value is long lv && lv < 0));

    private void SetupBinOp(BinOperatorSyntaxNode node) {
        // ResolvedSignature already populated by WalkAndCheck
        var sig = node.ResolvedSignature;
        if (sig is PureGenericFunctionBase pure) {
            if (node.Op == BinOp.Pow && IsPowForcedReal(node.Right)) {
                var rGid = GetOrCreateNodeGroup(node.OrderNumber);
                SetInterval(rGid, s_ordReal, s_ordReal, OPEN);
                Unite(GetOrCreateNodeGroup(node.Left.OrderNumber), rGid);
                Unite(GetOrCreateNodeGroup(node.Right.OrderNumber), rGid);
                _callGenericGroup[node.OrderNumber] = rGid;
                return;
            }
            SetupPureGenericBinOp(node.Left.OrderNumber, node.Right.OrderNumber,
                node.OrderNumber, pure.Constrains[0]);
        } else if (sig is IConcreteFunction) {
            SetConcreteChecked(GetOrCreateNodeGroup(node.Left.OrderNumber), ToPrimitiveOrd(sig.ArgTypes[0]));
            SetConcreteChecked(GetOrCreateNodeGroup(node.Right.OrderNumber), ToPrimitiveOrd(sig.ArgTypes[1]));
            SetConcreteChecked(GetOrCreateNodeGroup(node.OrderNumber), ToPrimitiveOrd(sig.ReturnType));
        }
    }

    private void SetupUnaryOp(UnaryOperatorSyntaxNode node) {
        // ResolvedSignature already populated by WalkAndCheck
        var sig = node.ResolvedSignature;
        if (sig is PureGenericFunctionBase pure)
            SetupPureGenericUnOp(node.Operand.OrderNumber, node.OrderNumber, pure.Constrains[0]);
        else if (sig is IConcreteFunction) {
            SetConcreteChecked(GetOrCreateNodeGroup(node.Operand.OrderNumber), ToPrimitiveOrd(sig.ArgTypes[0]));
            SetConcreteChecked(GetOrCreateNodeGroup(node.OrderNumber), ToPrimitiveOrd(sig.ReturnType));
        }
    }

    private void SetupFunCall(FunCallSyntaxNode call) {
        // ResolvedSignature already populated by WalkAndCheck
        var sig = call.ResolvedSignature;
        if (sig is PureGenericFunctionBase pure) {
            // Pow special case for function calls
            if (call.Id == CoreFunNames.Pow
                && !(call.Args.Length > 1 && !IsPowForcedReal(call.Args[1]))) {
                var rGid = GetOrCreateNodeGroup(call.OrderNumber);
                SetInterval(rGid, s_ordReal, s_ordReal, OPEN);
                for (int i = 0; i < call.Args.Length; i++)
                    Unite(GetOrCreateNodeGroup(call.Args[i].OrderNumber), rGid);
                _callGenericGroup[call.OrderNumber] = rGid;
                return;
            }
            // General PureGeneric: unite all args + result
            var resultGid = GetOrCreateNodeGroup(call.OrderNumber);
            ApplyGenericConstraint(resultGid, pure.Constrains[0]);
            for (int i = 0; i < call.Args.Length; i++)
                Unite(GetOrCreateNodeGroup(call.Args[i].OrderNumber), resultGid);
            _callGenericGroup[call.OrderNumber] = resultGid;
        } else if (sig is IConcreteFunction) {
            for (int i = 0; i < call.Args.Length; i++)
                SetConcreteChecked(GetOrCreateNodeGroup(call.Args[i].OrderNumber), ToPrimitiveOrd(sig.ArgTypes[i]));
            SetConcreteChecked(GetOrCreateNodeGroup(call.OrderNumber), ToPrimitiveOrd(sig.ReturnType));
        }
    }

    private void SetupIfElse(IfThenElseSyntaxNode node) {
        // Children already walked by WalkAndCheck
        var rGid = GetOrCreateNodeGroup(node.OrderNumber);
        foreach (var c in node.Ifs)
            SetConcrete(GetOrCreateNodeGroup(c.Condition.OrderNumber), s_ordBool);
        foreach (var c in node.Ifs)
            AddEdge(GetOrCreateNodeGroup(c.Expression.OrderNumber), rGid);
        AddEdge(GetOrCreateNodeGroup(node.ElseExpr.OrderNumber), rGid);
    }

    private void SetupCompareChain(ComparisonChainSyntaxNode node) {
        // Children already walked by WalkAndCheck
        for (int i = 0; i < node.Operators.Count; i++) {
            bool isEquality = node.Operators[i].Type is TokType.Equal or TokType.NotEqual;
            var gGid = GetOrCreateNodeGroup(node.Operands[i].OrderNumber);
            if (!isEquality) _groups[Find(gGid)].Comparable = true;
            Unite(gGid, GetOrCreateNodeGroup(node.Operands[i + 1].OrderNumber));
        }
        SetConcrete(GetOrCreateNodeGroup(node.OrderNumber), s_ordBool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool NarrowDesc(int gid, byte incoming) {
        var cur = _groups[gid].Desc;
        var next = cur == OPEN ? incoming : LcaOrd(cur, incoming);
        if (next == cur) return false;
        _groups[gid].Desc = next;
        if (_groups[gid].Anc != OPEN && !FitsOrd(next, _groups[gid].Anc))
            { _failed = true; return false; }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool NarrowAnc(int gid, byte incoming) {
        var cur = _groups[gid].Anc;
        if (cur == OPEN) {
            _groups[gid].Anc = incoming;
            if (_groups[gid].Desc != OPEN && !FitsOrd(_groups[gid].Desc, incoming))
                { _failed = true; return false; }
            return true;
        }
        var next = GcdOrd(cur, incoming);
        if (next == OPEN) { _failed = true; return false; }  // incompatible ancestors
        if (next == cur) return false;
        _groups[gid].Anc = next;
        if (_groups[gid].Desc != OPEN && !FitsOrd(_groups[gid].Desc, next))
            { _failed = true; return false; }
        return true;
    }

    private void Propagate() {
        bool changed;
        do {
            changed = false;
            for (int e = 0; e < _edgeCount; e++) {
                var er = Find(_edges[e].fromGroup);
                var vr = Find(_edges[e].toGroup);
                if (er == vr) continue;

                // Push desc up: to.desc = LCA(to.desc, from.desc)
                if (_groups[er].Desc != OPEN) {
                    if (NarrowDesc(vr, _groups[er].Desc)) changed = true;
                    if (_failed) return;
                }
                // Pull anc down: from.anc = GCD(from.anc, to.anc)
                if (_groups[vr].Anc != OPEN) {
                    if (NarrowAnc(er, _groups[vr].Anc)) changed = true;
                    if (_failed) return;
                }
                // Pull concrete desc: when to is concrete, from must be ≥ to
                if (_groups[vr].Desc != OPEN && _groups[vr].Anc != OPEN && _groups[vr].Desc == _groups[vr].Anc) {
                    if (NarrowDesc(er, _groups[vr].Desc)) changed = true;
                    if (_failed) return;
                }
                // Propagate anc: to.anc = GCD(to.anc, from.anc)
                if (_groups[er].Anc != OPEN) {
                    if (NarrowAnc(vr, _groups[er].Anc)) changed = true;
                    if (_failed) return;
                }
                // Comparable propagation (bidirectional — constraint must reach variables)
                if (_groups[er].Comparable && !_groups[vr].Comparable) { _groups[vr].Comparable = true; changed = true; }
                if (_groups[vr].Comparable && !_groups[er].Comparable) { _groups[er].Comparable = true; changed = true; }
                // Preferred propagation (bidirectional — pref is a hint, not a constraint)
                if (_groups[er].Pref != OPEN && _groups[vr].Pref == OPEN) { _groups[vr].Pref = _groups[er].Pref; changed = true; }
                if (_groups[vr].Pref != OPEN && _groups[er].Pref == OPEN) { _groups[er].Pref = _groups[vr].Pref; changed = true; }
            }
        } while (changed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StatePrimitive ResolveGroup(int gid) {
        var r = Find(gid);
        if (_resolvedCache != null && _resolvedCache[r] != OPEN)
            return s_ordToState[_resolvedCache[r]];

        var result = ResolveRoot(r);
        if (result != null && _resolvedCache != null)
            _resolvedCache[r] = (byte)result.Order;
        return result;
    }

    private StatePrimitive ResolveRoot(int r) {
        var d = _groups[r].Desc;
        var a = _groups[r].Anc;
        var p = _groups[r].Pref;

        if (d != OPEN && a != OPEN) {
            if (d == a) return s_ordToState[d];
            if (!FitsOrd(d, a)) return null;
            if (p != OPEN && FitsOrd(d, p) && FitsOrd(p, a)) return s_ordToState[p];
            return s_ordToState[a];
        }
        if (a != OPEN) {
            if (p != OPEN && FitsOrd(p, a)) return s_ordToState[p];
            return s_ordToState[a];
        }
        if (p != OPEN && (d == OPEN || FitsOrd(d, p))) return s_ordToState[p];
        if (d != OPEN) return s_ordToState[d];
        if (_groups[r].Comparable) return StatePrimitive.Real;
        return StatePrimitive.Any;
    }

    private TypeInferenceResults BuildResults() {
        if (_failed) return null;
        var resultBuilder = new TypeInferenceResultsBuilder(_nodeGroup.Length);
        var syntaxNodes = new TicNode[_nodeGroup.Length];

        for (int i = 0; i < _nodeGroup.Length; i++) {
            if (_nodeGroup[i] < 0) continue;
            var resolved = ResolveGroup(_nodeGroup[i]);
            if (resolved == null) return null;
            syntaxNodes[i] = s_syntaxNodeSingletons[resolved.Order];
        }

        for (int i = 0; i < _callGenericGroup.Length; i++) {
            if (_callGenericGroup[i] < 0) continue;
            var resolved = ResolveGroup(_callGenericGroup[i]);
            if (resolved == null) return null;
            resultBuilder.RememberGenericCallArguments(i, s_singleRefToArrays[resolved.Order]);
        }

        // SpsTicResults: no namedNodes dict — SPS applies variable types directly.
        // GetVariableNodeOrNull returns null; GetVariableNode throws (never called when typesApplied).
        resultBuilder.SetResults(new SpsTicResults(syntaxNodes));
        return resultBuilder.Build();
    }

    /// <summary>
    /// Walk the syntax tree and set OutputType (and VariableType for NamedIdSyntaxNode)
    /// directly from SPS resolved groups. Returns false if any node can't resolve.
    /// </summary>
    private bool ApplyTypesToSyntaxTree(SyntaxTree tree) {
        foreach (var child in tree.Children) {
            switch (child) {
                case EquationSyntaxNode eq:
                    // EquationSyntaxNode.OutputType = variable type (mirrors ApplyTiResultEnterVisitor.Visit(Equation))
                    if (!_varGroup.TryGetValue(eq.Id, out var eqVarGid))
                        return false;
                    var eqResolved = ResolveGroup(eqVarGid);
                    if (eqResolved == null) return false;
                    eq.OutputType = s_ordToFunnyType[eqResolved.Order];
                    // Recurse into the expression subtree
                    if (!ApplyTypesRecursive(eq.Expression))
                        return false;
                    break;
                case VarDefinitionSyntaxNode:
                    // VarDefinitionSyntaxNode has no expression children to type — skip
                    break;
                default:
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Recursively set OutputType on a syntax node and its children from SPS groups.
    /// For NamedIdSyntaxNode, also sets VariableType.
    /// Returns false if any node can't be resolved.
    /// </summary>
    private bool ApplyTypesRecursive(ISyntaxNode node) {
        // Set OutputType from node's group
        var nodeId = node.OrderNumber;
        if (nodeId >= 0 && nodeId < _nodeGroup.Length && _nodeGroup[nodeId] >= 0) {
            var resolved = ResolveGroup(_nodeGroup[nodeId]);
            if (resolved == null) return false;
            node.OutputType = s_ordToFunnyType[resolved.Order];
        } else {
            node.OutputType = FunnyType.Empty;
        }

        // NamedIdSyntaxNode: also set VariableType (mirrors ApplyTiResultEnterVisitor.Visit(NamedId))
        if (node is NamedIdSyntaxNode named) {
            if (_varGroup.TryGetValue(named.Id, out var varGid)) {
                var varResolved = ResolveGroup(varGid);
                if (varResolved == null) return false;
                named.VariableType = s_ordToFunnyType[varResolved.Order];
            }
        }

        // Recurse into children
        foreach (var child in node.Children) {
            if (!ApplyTypesRecursive(child))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Single-pass fused gate+solver. Returns null if:
    /// - The expression contains non-primitive types (arrays, structs, optionals, lambdas)
    /// - Constraint propagation detects a conflict
    /// - Any apriori types are non-primitive
    /// In all these cases, the caller should fall through to full TIC.
    /// </summary>
    internal static TypeInferenceResults SolveOrNull(
        SyntaxTree tree,
        IFunctionRegistry functions,
        IConstantList constants,
        IAprioriTypesMap aprioriTypes,
        DialectSettings dialect,
        out bool typesApplied,
        ICustomTypeRegistry customTypes = null) {

        typesApplied = false;
        // Gate: apriori types must all be primitive
        if (!AllAprioriTypesArePrimitive(aprioriTypes))
            return null;

        var solver = new SimplePrimitiveSolver(tree.MaxNodeId, constants, dialect);

        foreach (var a in aprioriTypes) {
            if (a.Type.IsNumeric() || a.Type.BaseType is BaseFunnyType.Bool or BaseFunnyType.Char
                    or BaseFunnyType.Ip or BaseFunnyType.Any)
                solver.SetConcreteChecked(solver.GetOrCreateVarGroup(a.Name), ToPrimitiveOrd(a.Type));
        }

        foreach (var child in tree.Children) {
            switch (child) {
                case EquationSyntaxNode eq:
                    // Gate: type annotation must be primitive
                    if (eq.TypeSpecificationOrNull != null
                        && !IsSimpleTypeSyntax(eq.TypeSpecificationOrNull.TypeSyntax))
                        return null;
                    // Fused walk: gate + constraint building
                    if (!solver.WalkAndCheck(eq.Expression, functions))
                        return null;
                    var defGid = solver.GetOrCreateVarGroup(eq.Id);
                    if (eq.OutputTypeSpecified) {
                        var resolved = TypeSyntaxResolver.Resolve(eq.TypeSpecificationOrNull.TypeSyntax, customTypes);
                        solver.SetConcreteChecked(defGid, ToPrimitiveOrd(resolved));
                    }
                    var exprGid = solver.GetOrCreateNodeGroup(eq.Expression.OrderNumber);
                    // SetDef: preferred propagation (like GraphBuilder.SetDef)
                    var er = solver.Find(exprGid);
                    if (solver._groups[er].Desc != OPEN && solver._groups[er].Desc == solver._groups[er].Anc) {
                        var dr = solver.Find(defGid);
                        if (solver._groups[dr].Pref == OPEN) solver._groups[dr].Pref = solver._groups[er].Desc;
                    }
                    solver.AddEdge(exprGid, defGid);
                    break;
                case VarDefinitionSyntaxNode vd:
                    // Gate: type annotation must be primitive
                    if (!IsSimpleTypeSyntax(vd.TypeSyntax))
                        return null;
                    var resolved2 = TypeSyntaxResolver.Resolve(vd.TypeSyntax, customTypes);
                    solver.SetConcreteChecked(solver.GetOrCreateVarGroup(vd.Id), ToPrimitiveOrd(resolved2));
                    break;
                case UserFunctionDefinitionSyntaxNode:
                    return null; // user functions → full TIC
                case TypeDeclarationSyntaxNode:
                    return null; // type declarations → full TIC
            }
        }

        solver.Propagate();
        if (solver._failed) { typesApplied = false; return null; }
        // Allocate resolve cache — memoizes ResolveGroup by root (no Unite after Propagate, roots stable)
        solver._resolvedCache = new byte[solver._groupCount];
        Array.Fill(solver._resolvedCache, OPEN);
        var results = solver.BuildResults();
        if (results == null) { typesApplied = false; return null; }
        if (solver.ApplyTypesToSyntaxTree(tree))
            typesApplied = true;
        else {
            typesApplied = false;
            return null;
        }
        return results;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ToPrimitiveOrd(FunnyType type) =>
        type.BaseType switch {
            BaseFunnyType.Bool   => s_ordBool,
            BaseFunnyType.UInt8  => s_ordU8,
            BaseFunnyType.UInt16 => s_ordU16,
            BaseFunnyType.UInt32 => s_ordU32,
            BaseFunnyType.UInt64 => s_ordU64,
            BaseFunnyType.Int16  => s_ordI16,
            BaseFunnyType.Int32  => s_ordI32,
            BaseFunnyType.Int64  => s_ordI64,
            BaseFunnyType.Real   => s_ordReal,
            BaseFunnyType.Char   => s_ordChar,
            BaseFunnyType.Ip     => s_ordIp,
            BaseFunnyType.Any    => s_ordAny,
            _                    => OPEN
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetConcreteChecked(int gid, byte ord) {
        if (ord == OPEN) { _failed = true; return; }
        SetConcrete(gid, ord);
    }
}
