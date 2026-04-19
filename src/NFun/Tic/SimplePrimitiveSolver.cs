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
/// All interval operations (LCA, GCD) are O(1) table lookups.
/// Total complexity: O(N) where N = AST node count.
/// </summary>
internal sealed class SimplePrimitiveSolver {

    // Union-find
    private int[] _parent;
    private byte[] _rank;

    // Per-root constraint interval
    private StatePrimitive[] _desc; // lower bound (null = open)
    private StatePrimitive[] _anc;  // upper bound (null = open)
    private StatePrimitive[] _pref; // preferred resolution
    private bool[] _comparable;     // comparable type required

    private int _groupCount;
    private int _capacity;

    // Syntax node id → group id (-1 = not allocated)
    private readonly int[] _nodeGroup;

    // Variable name → group id
    private readonly Dictionary<string, int> _varGroup;

    // Directed edges: expr ≤ var
    private (int exprGroup, int varGroup)[] _edges;
    private int _edgeCount;

    // Generic call tracking: call OrderNumber → generic group id (array, not dict)
    private int[] _callGenericGroup; // -1 = no generic

    // Dependencies
    private readonly IConstantList _constants;
    // Cached preferred int type (computed once)
    private readonly StatePrimitive _preferredInt;

    private SimplePrimitiveSolver(
        int maxNodeId,
        IConstantList constants,
        DialectSettings dialect) {
        _capacity = maxNodeId + 32;
        _parent = new int[_capacity];
        _rank = new byte[_capacity];
        _desc = new StatePrimitive[_capacity];
        _anc = new StatePrimitive[_capacity];
        _pref = new StatePrimitive[_capacity];
        _comparable = new bool[_capacity];
        for (int i = 0; i < _capacity; i++) _parent[i] = i;

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
        _preferredInt = dialect.IntegerPreferredType switch {
            IntegerPreferredType.I32  => StatePrimitive.I32,
            IntegerPreferredType.I64  => StatePrimitive.I64,
            IntegerPreferredType.Real => StatePrimitive.Real,
            _                         => null
        };
    }

    #region Union-Find

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Find(int x) {
        while (_parent[x] != x) { _parent[x] = _parent[_parent[x]]; x = _parent[x]; }
        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NewGroup() {
        var id = _groupCount++;
        if (id >= _capacity) Grow();
        // _parent[id] = id already set by constructor/Grow
        return id;
    }

    private void Unite(int a, int b) {
        a = Find(a); b = Find(b);
        if (a == b) return;
        if (_rank[a] < _rank[b]) { var t = a; a = b; b = t; }
        _parent[b] = a;
        if (_rank[a] == _rank[b]) _rank[a]++;
        MergeInto(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MergeInto(int root, int other) {
        if (_desc[other] != null) {
            if (_desc[root] == null) _desc[root] = _desc[other];
            else _desc[root] = _desc[root].GetLastCommonPrimitiveAncestor(_desc[other]);
        }
        if (_anc[other] != null) {
            if (_anc[root] == null) _anc[root] = _anc[other];
            else {
                var gcd = _anc[root].GetFirstCommonDescendantOrNull(_anc[other]);
                if (gcd != null) _anc[root] = gcd;
            }
        }
        _comparable[root] |= _comparable[other];
        if (_pref[root] == null) _pref[root] = _pref[other];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow() {
        var nc = _capacity * 2;
        Array.Resize(ref _parent, nc);
        Array.Resize(ref _rank, nc);
        Array.Resize(ref _desc, nc);
        Array.Resize(ref _anc, nc);
        Array.Resize(ref _pref, nc);
        Array.Resize(ref _comparable, nc);
        for (int i = _capacity; i < nc; i++) _parent[i] = i;
        _capacity = nc;
    }

    #endregion

    #region Constraint helpers

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
    private void SetConcrete(int gid, StatePrimitive type) {
        var r = Find(gid);
        _desc[r] = type; _anc[r] = type;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetInterval(int gid, StatePrimitive desc, StatePrimitive anc, StatePrimitive pref) {
        var r = Find(gid);
        _desc[r] = desc; _anc[r] = anc;
        if (_pref[r] == null) _pref[r] = pref;
    }

    private void AddEdge(int exprGid, int varGid) {
        if (_edgeCount >= _edges.Length)
            Array.Resize(ref _edges, _edges.Length * 2);
        _edges[_edgeCount++] = (exprGid, varGid);
    }

    /// <summary>Apply generic constraint to group and unite with args. No array allocation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetupPureGenericBinOp(int leftId, int rightId, int resultId, GenericConstrains c) {
        // Use result group as the generic group — no extra NewGroup needed
        var rGid = GetOrCreateNodeGroup(resultId);
        var r = Find(rGid);
        if (c.Descendant != null) {
            if (_desc[r] == null) _desc[r] = c.Descendant;
            else _desc[r] = _desc[r].GetLastCommonPrimitiveAncestor(c.Descendant);
        }
        if (c.Ancestor != null) {
            if (_anc[r] == null) _anc[r] = c.Ancestor;
            else { var gcd = _anc[r].GetFirstCommonDescendantOrNull(c.Ancestor); if (gcd != null) _anc[r] = gcd; }
        }
        if (c.IsComparable) _comparable[Find(rGid)] = true;
        Unite(GetOrCreateNodeGroup(leftId), rGid);
        Unite(GetOrCreateNodeGroup(rightId), rGid);
        _callGenericGroup[resultId] = rGid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetupPureGenericUnOp(int operandId, int resultId, GenericConstrains c) {
        var rGid = GetOrCreateNodeGroup(resultId);
        var r = Find(rGid);
        if (c.Descendant != null) {
            if (_desc[r] == null) _desc[r] = c.Descendant;
            else _desc[r] = _desc[r].GetLastCommonPrimitiveAncestor(c.Descendant);
        }
        if (c.Ancestor != null) {
            if (_anc[r] == null) _anc[r] = c.Ancestor;
            else { var gcd = _anc[r].GetFirstCommonDescendantOrNull(c.Ancestor); if (gcd != null) _anc[r] = gcd; }
        }
        if (c.IsComparable) _comparable[Find(rGid)] = true;
        Unite(GetOrCreateNodeGroup(operandId), rGid);
        _callGenericGroup[resultId] = rGid;
    }

    #endregion

    #region AST walking

    private void WalkExpression(ISyntaxNode node) {
        switch (node) {
            case GenericIntSyntaxNode g:  SetupIntConst(g); break;
            case IpAddressConstantSyntaxNode ip:
                SetConcrete(GetOrCreateNodeGroup(ip.OrderNumber), StatePrimitive.Ip); break;
            case ConstantSyntaxNode c:    SetupConst(c); break;
            case NamedIdSyntaxNode n:     SetupNamedId(n); break;
            case BinOperatorSyntaxNode b:
                WalkExpression(b.Left); WalkExpression(b.Right); SetupBinOp(b); break;
            case UnaryOperatorSyntaxNode u:
                WalkExpression(u.Operand); SetupUnaryOp(u); break;
            case FunCallSyntaxNode f:
                for (int i = 0; i < f.Args.Length; i++) WalkExpression(f.Args[i]);
                SetupFunCall(f); break;
            case IfThenElseSyntaxNode ite: SetupIfElse(ite); break;
            case ComparisonChainSyntaxNode cc: SetupCompareChain(cc); break;
        }
    }

    private void SetupConst(ConstantSyntaxNode c) =>
        SetConcrete(GetOrCreateNodeGroup(c.OrderNumber), ToPrimitive(c.OutputType));

    private void SetupIntConst(GenericIntSyntaxNode node) {
        var gid = GetOrCreateNodeGroup(node.OrderNumber);
        if (node.IsHexOrBin) SetupHexBin(gid, node.Value);
        else SetupDecimal(gid, node.Value);
    }

    private void SetupHexBin(int gid, object value) {
        if (value is long l) {
            if (l <= 0) {
                if (l >= short.MinValue) SetInterval(gid, StatePrimitive.I16, StatePrimitive.I64, StatePrimitive.I32);
                else if (l >= int.MinValue) SetInterval(gid, StatePrimitive.I32, StatePrimitive.I64, StatePrimitive.I32);
                else SetConcrete(gid, StatePrimitive.I64);
                return;
            }
            SetupHexBinPositive(gid, (ulong)l);
        } else if (value is ulong u) SetupHexBinPositive(gid, u);
    }

    private void SetupHexBinPositive(int gid, ulong v) {
        if      (v <= byte.MaxValue)          SetInterval(gid, StatePrimitive.U8,  StatePrimitive.I96, StatePrimitive.I32);
        else if (v <= (ulong)short.MaxValue)  SetInterval(gid, StatePrimitive.U12, StatePrimitive.I96, StatePrimitive.I32);
        else if (v <= ushort.MaxValue)         SetInterval(gid, StatePrimitive.U16, StatePrimitive.I96, StatePrimitive.I32);
        else if (v <= int.MaxValue)            SetInterval(gid, StatePrimitive.U24, StatePrimitive.I96, StatePrimitive.I32);
        else if (v <= uint.MaxValue)           SetInterval(gid, StatePrimitive.U32, StatePrimitive.I96, StatePrimitive.I64);
        else if (v <= (ulong)long.MaxValue)   SetInterval(gid, StatePrimitive.U48, StatePrimitive.I96, StatePrimitive.I64);
        else SetConcrete(gid, StatePrimitive.U64);
    }

    private void SetupDecimal(int gid, object value) {
        if (value is long l) {
            if (l <= 0) {
                var desc = l >= short.MinValue ? StatePrimitive.I16
                         : l >= int.MinValue   ? StatePrimitive.I32
                         :                       StatePrimitive.I64;
                SetInterval(gid, desc, StatePrimitive.Real, _preferredInt);
                return;
            }
            SetupDecimalPositive(gid, (ulong)l);
        } else if (value is ulong u) SetupDecimalPositive(gid, u);
    }

    private void SetupDecimalPositive(int gid, ulong v) {
        var desc = v <= byte.MaxValue          ? StatePrimitive.U8
                 : v <= (ulong)short.MaxValue  ? StatePrimitive.U12
                 : v <= ushort.MaxValue         ? StatePrimitive.U16
                 : v <= int.MaxValue            ? StatePrimitive.U24
                 : v <= uint.MaxValue           ? StatePrimitive.U32
                 : v <= (ulong)long.MaxValue   ? StatePrimitive.U48
                 :                               StatePrimitive.U64;
        SetInterval(gid, desc, StatePrimitive.Real, _preferredInt);
    }

    private void SetupNamedId(NamedIdSyntaxNode node) {
        var gid = GetOrCreateNodeGroup(node.OrderNumber);
        // Check constants only if this name is NOT already known as a variable
        if (_varGroup.TryGetValue(node.Id, out _)) {
            node.IdType = NamedIdNodeType.Variable;
            AddEdge(gid, GetOrCreateVarGroup(node.Id));
            return;
        }
        if (_constants.TryGetConstant(node.Id, out var constant)) {
            node.IdType = NamedIdNodeType.Constant;
            node.IdContent = constant;
            SetConcrete(gid, ToPrimitive(constant.Type));
            return;
        }
        node.IdType = NamedIdNodeType.Variable;
        AddEdge(gid, GetOrCreateVarGroup(node.Id));
    }

    private void SetupBinOp(BinOperatorSyntaxNode node) {
        // ResolvedSignature already populated by SimpleExpressionDetector
        var sig = node.ResolvedSignature;
        if (sig is PureGenericFunctionBase pure) {
            // Pow special case: non-const or negative exponent → force Real
            if (node.Op == BinOp.Pow
                && !(node.Right is GenericIntSyntaxNode intN && !(intN.Value is long lv && lv < 0))) {
                var rGid = GetOrCreateNodeGroup(node.OrderNumber);
                SetInterval(rGid, StatePrimitive.Real, StatePrimitive.Real, null);
                Unite(GetOrCreateNodeGroup(node.Left.OrderNumber), rGid);
                Unite(GetOrCreateNodeGroup(node.Right.OrderNumber), rGid);
                _callGenericGroup[node.OrderNumber] = rGid;
                return;
            }
            SetupPureGenericBinOp(node.Left.OrderNumber, node.Right.OrderNumber,
                node.OrderNumber, pure.Constrains[0]);
        } else if (sig is IConcreteFunction) {
            SetConcrete(GetOrCreateNodeGroup(node.Left.OrderNumber), ToPrimitive(sig.ArgTypes[0]));
            SetConcrete(GetOrCreateNodeGroup(node.Right.OrderNumber), ToPrimitive(sig.ArgTypes[1]));
            SetConcrete(GetOrCreateNodeGroup(node.OrderNumber), ToPrimitive(sig.ReturnType));
        }
    }

    private void SetupUnaryOp(UnaryOperatorSyntaxNode node) {
        var sig = node.ResolvedSignature;
        if (sig is PureGenericFunctionBase pure)
            SetupPureGenericUnOp(node.Operand.OrderNumber, node.OrderNumber, pure.Constrains[0]);
        else if (sig is IConcreteFunction) {
            SetConcrete(GetOrCreateNodeGroup(node.Operand.OrderNumber), ToPrimitive(sig.ArgTypes[0]));
            SetConcrete(GetOrCreateNodeGroup(node.OrderNumber), ToPrimitive(sig.ReturnType));
        }
    }

    private void SetupFunCall(FunCallSyntaxNode call) {
        var sig = call.ResolvedSignature;
        if (sig is PureGenericFunctionBase pure) {
            // Pow special case for function calls
            if (call.Id == CoreFunNames.Pow
                && !(call.Args.Length > 1 && call.Args[1] is GenericIntSyntaxNode intN
                     && !(intN.Value is long lv && lv < 0))) {
                var rGid = GetOrCreateNodeGroup(call.OrderNumber);
                SetInterval(rGid, StatePrimitive.Real, StatePrimitive.Real, null);
                for (int i = 0; i < call.Args.Length; i++)
                    Unite(GetOrCreateNodeGroup(call.Args[i].OrderNumber), rGid);
                _callGenericGroup[call.OrderNumber] = rGid;
                return;
            }
            // General PureGeneric: unite all args + result
            var c = pure.Constrains[0];
            var resultGid = GetOrCreateNodeGroup(call.OrderNumber);
            var r = Find(resultGid);
            if (c.Descendant != null) {
                if (_desc[r] == null) _desc[r] = c.Descendant;
                else _desc[r] = _desc[r].GetLastCommonPrimitiveAncestor(c.Descendant);
            }
            if (c.Ancestor != null) {
                if (_anc[r] == null) _anc[r] = c.Ancestor;
                else { var gcd = _anc[r].GetFirstCommonDescendantOrNull(c.Ancestor); if (gcd != null) _anc[r] = gcd; }
            }
            if (c.IsComparable) _comparable[Find(resultGid)] = true;
            for (int i = 0; i < call.Args.Length; i++)
                Unite(GetOrCreateNodeGroup(call.Args[i].OrderNumber), resultGid);
            _callGenericGroup[call.OrderNumber] = resultGid;
        } else if (sig is IConcreteFunction) {
            for (int i = 0; i < call.Args.Length; i++)
                SetConcrete(GetOrCreateNodeGroup(call.Args[i].OrderNumber), ToPrimitive(sig.ArgTypes[i]));
            SetConcrete(GetOrCreateNodeGroup(call.OrderNumber), ToPrimitive(sig.ReturnType));
        }
    }

    private void SetupIfElse(IfThenElseSyntaxNode node) {
        foreach (var c in node.Ifs) { WalkExpression(c.Condition); WalkExpression(c.Expression); }
        WalkExpression(node.ElseExpr);
        var rGid = GetOrCreateNodeGroup(node.OrderNumber);
        foreach (var c in node.Ifs)
            SetConcrete(GetOrCreateNodeGroup(c.Condition.OrderNumber), StatePrimitive.Bool);
        foreach (var c in node.Ifs)
            AddEdge(GetOrCreateNodeGroup(c.Expression.OrderNumber), rGid);
        AddEdge(GetOrCreateNodeGroup(node.ElseExpr.OrderNumber), rGid);
    }

    private void SetupCompareChain(ComparisonChainSyntaxNode node) {
        for (int i = 0; i < node.Operands.Count; i++) WalkExpression(node.Operands[i]);
        for (int i = 0; i < node.Operators.Count; i++) {
            bool isEquality = node.Operators[i].Type is TokType.Equal or TokType.NotEqual;
            var c = isEquality ? GenericConstrains.Any : GenericConstrains.Comparable;
            // Use first operand's group as the generic group — no extra allocation
            var gGid = GetOrCreateNodeGroup(node.Operands[i].OrderNumber);
            var r = Find(gGid);
            if (c.IsComparable) _comparable[r] = true;
            Unite(gGid, GetOrCreateNodeGroup(node.Operands[i + 1].OrderNumber));
        }
        SetConcrete(GetOrCreateNodeGroup(node.OrderNumber), StatePrimitive.Bool);
    }

    #endregion

    #region Propagation

    private void Propagate() {
        bool changed = true;
        int safety = 20;
        while (changed && safety-- > 0) {
            changed = false;
            for (int e = 0; e < _edgeCount; e++) {
                var er = Find(_edges[e].exprGroup);
                var vr = Find(_edges[e].varGroup);
                if (er == vr) continue;

                // Push desc up: var.desc = LCA(var.desc, expr.desc)
                if (_desc[er] != null) {
                    var cur = _desc[vr];
                    var lca = cur == null ? _desc[er] : cur.GetLastCommonPrimitiveAncestor(_desc[er]);
                    if (lca != cur) { _desc[vr] = lca; changed = true; }
                }
                // Pull anc down: expr.anc = GCD(expr.anc, var.anc)
                if (_anc[vr] != null) {
                    var cur = _anc[er];
                    var gcd = cur == null ? _anc[vr] : cur.GetFirstCommonDescendantOrNull(_anc[vr]);
                    if (gcd != null && gcd != cur) { _anc[er] = gcd; changed = true; }
                }
                // Propagate expr ancestor to var
                if (_anc[er] != null) {
                    var cur = _anc[vr];
                    var gcd = cur == null ? _anc[er] : cur.GetFirstCommonDescendantOrNull(_anc[er]);
                    if (gcd != null && gcd != cur) { _anc[vr] = gcd; changed = true; }
                }
                // Comparable propagation
                if (_comparable[er] && !_comparable[vr]) { _comparable[vr] = true; changed = true; }
                // Preferred propagation
                if (_pref[er] != null && _pref[vr] == null) { _pref[vr] = _pref[er]; changed = true; }
            }
        }
    }

    #endregion

    #region Resolution and output

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StatePrimitive ResolveGroup(int gid) {
        var r = Find(gid);
        var d = _desc[r];
        var a = _anc[r];

        if (d != null && a != null) {
            if (d.Equals(a)) return d;
            if (d.CanBePessimisticConvertedTo(a)) return a;
            return null; // desc > anc: unsatisfiable constraint, fall back to full TIC
        }
        if (a != null) return a;
        var p = _pref[r];
        if (p != null && (d == null || d.CanBePessimisticConvertedTo(p))) return p;
        if (d != null) return d;
        if (_comparable[r]) return StatePrimitive.Real;
        return StatePrimitive.Any;
    }

    private TypeInferenceResults BuildResults() {
        var resultBuilder = new TypeInferenceResultsBuilder();
        var syntaxNodes = new TicNode[_nodeGroup.Length];
        var namedNodes = new Dictionary<string, TicNode>(
            _varGroup.Count, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _nodeGroup.Length; i++) {
            if (_nodeGroup[i] < 0) continue;
            var resolved = ResolveGroup(_nodeGroup[i]);
            if (resolved == null) return null;
            syntaxNodes[i] = TicNode.CreateSyntaxNode(i, resolved, true);
        }

        foreach (var (name, gid) in _varGroup) {
            var resolved = ResolveGroup(gid);
            if (resolved == null) return null;
            namedNodes[name] = TicNode.CreateNamedNode(name, resolved);
        }

        // Generic call arguments — scan the array (sparse but no dictionary overhead)
        for (int i = 0; i < _callGenericGroup.Length; i++) {
            if (_callGenericGroup[i] < 0) continue;
            var resolved = ResolveGroup(_callGenericGroup[i]);
            if (resolved == null) return null;
            var ticNode = TicNode.CreateTypeVariableNode("T", resolved, true);
            resultBuilder.RememberGenericCallArguments(i, new[] { new StateRefTo(ticNode) });
        }

        resultBuilder.SetResults(new TicResultsWithoutGenerics(namedNodes, syntaxNodes));
        return resultBuilder.Build();
    }

    #endregion

    #region Public API

    internal static TypeInferenceResults Solve(
        SyntaxTree tree,
        IConstantList constants,
        IAprioriTypesMap aprioriTypes,
        DialectSettings dialect,
        ICustomTypeRegistry customTypes = null) {
        var solver = new SimplePrimitiveSolver(tree.MaxNodeId, constants, dialect);

        foreach (var a in aprioriTypes) {
            if (a.Type.IsNumeric() || a.Type.BaseType is BaseFunnyType.Bool or BaseFunnyType.Char
                    or BaseFunnyType.Ip or BaseFunnyType.Any)
                solver.SetConcrete(solver.GetOrCreateVarGroup(a.Name), ToPrimitive(a.Type));
        }

        foreach (var child in tree.Children) {
            switch (child) {
                case EquationSyntaxNode eq:
                    solver.WalkExpression(eq.Expression);
                    var defGid = solver.GetOrCreateVarGroup(eq.Id);
                    if (eq.OutputTypeSpecified) {
                        var resolved = TypeSyntaxResolver.Resolve(eq.TypeSpecificationOrNull.TypeSyntax, customTypes);
                        solver.SetConcrete(defGid, ToPrimitive(resolved));
                    }
                    var exprGid = solver.GetOrCreateNodeGroup(eq.Expression.OrderNumber);
                    // SetDef: preferred propagation (like GraphBuilder.SetDef)
                    var er = solver.Find(exprGid);
                    if (solver._desc[er] != null && solver._desc[er].Equals(solver._anc[er])) {
                        var dr = solver.Find(defGid);
                        if (solver._pref[dr] == null) solver._pref[dr] = solver._desc[er];
                    }
                    solver.AddEdge(exprGid, defGid);
                    break;
                case VarDefinitionSyntaxNode vd:
                    var resolved2 = TypeSyntaxResolver.Resolve(vd.TypeSyntax, customTypes);
                    solver.SetConcrete(solver.GetOrCreateVarGroup(vd.Id), ToPrimitive(resolved2));
                    break;
            }
        }

        solver.Propagate();
        return solver.BuildResults();
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StatePrimitive ToPrimitive(FunnyType type) =>
        type.BaseType switch {
            BaseFunnyType.Bool   => StatePrimitive.Bool,
            BaseFunnyType.UInt8  => StatePrimitive.U8,
            BaseFunnyType.UInt16 => StatePrimitive.U16,
            BaseFunnyType.UInt32 => StatePrimitive.U32,
            BaseFunnyType.UInt64 => StatePrimitive.U64,
            BaseFunnyType.Int16  => StatePrimitive.I16,
            BaseFunnyType.Int32  => StatePrimitive.I32,
            BaseFunnyType.Int64  => StatePrimitive.I64,
            BaseFunnyType.Real   => StatePrimitive.Real,
            BaseFunnyType.Char   => StatePrimitive.Char,
            BaseFunnyType.Ip     => StatePrimitive.Ip,
            BaseFunnyType.Any    => StatePrimitive.Any,
            _                    => StatePrimitive.Any
        };
}
