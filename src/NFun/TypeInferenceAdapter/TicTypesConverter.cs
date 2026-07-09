using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.TypeInferenceAdapter;

public abstract class TicTypesConverter {
    public static readonly TicTypesConverter Concrete = new OnlyConcreteTypesConverter();

    public static TicTypesConverter GenericSignatureConverter(IReadOnlyList<ConstraintsState> constrainsMap,
        IReadOnlyList<(StateStruct, ConstraintsState)> structGenericMap = null)
        => new ConstrainsConverter(constrainsMap, structGenericMap);

    public static TicTypesConverter ReplaceGenericTypesConverter(
        IReadOnlyList<ConstraintsState> constrainsMap, IList<FunnyType> genericArgs,
        IReadOnlyList<(StateStruct, ConstraintsState)> structGenericMap = null)
        => new GenericMapConverter(constrainsMap, genericArgs, structGenericMap);

    public abstract FunnyType Convert(ITicNodeState type);
    /// <summary>Per-conversion unique mark. Set once per ConvertToFunnyStruct entry.</summary>
    private int _convertMark;
    /// <summary>Named struct types currently being converted (for self-referential cycle detection).</summary>
    private HashSet<string> _convertingNamedTypes;

    /// <summary>
    /// Convert a lifted F-bound's <c>StateStruct</c> into a <see cref="FunnyType.StructOf"/>
    /// where back-edges (RefTo's that resolve to <paramref name="ownerCs"/>) become
    /// <see cref="FunnyType.Generic"/>(<paramref name="genericIdx"/>) — encoding
    /// the F-bound self-reference at the FunnyType layer.
    ///
    /// The result is the runtime-facing structural shape that
    /// <c>GenericConstrains.WithStructBound</c> carries.
    ///
    /// Algorithm:
    /// <list type="number">
    ///   <item>Walk <paramref name="bound"/>.Fields recursively.</item>
    ///   <item>For each field's value-node state, peel <c>StateRefTo</c> chains; if any link points to <paramref name="ownerCs"/>'s holder, emit <see cref="FunnyType.Generic"/>(<paramref name="genericIdx"/>).</item>
    ///   <item>Otherwise, recurse via existing <c>StateOptional</c>/<c>StateArray</c>/<c>StateStruct</c>/primitive paths.</item>
    /// </list>
    /// Per-walk visit marks prevent infinite descent on shared struct subgraphs.
    /// </summary>
    public static FunnyType BuildStructBoundFunnyType(StateStruct bound, ConstraintsState ownerCs, int genericIdx) {
        var visited = new HashSet<Tic.TicNode>();
        return BuildStructLayer(bound, bound, ownerCs, genericIdx, visited);
    }

    private static FunnyType BuildStructLayer(StateStruct s, StateStruct bound, ConstraintsState ownerCs, int genericIdx, HashSet<Tic.TicNode> visited) {
        var fieldsSpec = new StructTypeSpecification(s.FieldsCount, isFrozen: s.IsFrozen);
        foreach (var (name, valueNode) in s.Fields)
            fieldsSpec.Add(name, BuildFieldType(valueNode, bound, ownerCs, genericIdx, visited));
        return FunnyType.StructOf(fieldsSpec);
    }

    private static FunnyType BuildFieldType(Tic.TicNode node, StateStruct bound, ConstraintsState ownerCs, int genericIdx, HashSet<Tic.TicNode> visited) {
        // Peel RefTo chains; if any link reaches the F-bound owner, this is a self-edge.
        var current = node;
        while (current.State is StateRefTo r) {
            if (ReferenceEquals(r.Node.State, ownerCs)) return FunnyType.Generic(genericIdx);
            // Post-lift inner nodes carry State = bound (the lifted struct itself), not the outer ownerCs.
            if (ReferenceEquals(r.Node.State, bound)) return FunnyType.Generic(genericIdx);
            // A different CS{StructBound} sharing the same bound shape is also a recursion variable
            // for this F-bound — multiple LiftMuTypes calls within one body can produce parallel CS
            // instances that all represent the same μ-recursion (Cardelli-Mitchell '89 §4.2 coinductive equality).
            if (r.Node.State is ConstraintsState csR && csR.HasStructBound
                && ReferenceEquals(csR.StructBound, bound))
                return FunnyType.Generic(genericIdx);
            current = r.Node;
        }
        var nr = current.GetNonReference();
        if (ReferenceEquals(nr.State, ownerCs))
            return FunnyType.Generic(genericIdx);
        if (ReferenceEquals(nr.State, bound))
            return FunnyType.Generic(genericIdx);
        if (nr.State is ConstraintsState csNr && csNr.HasStructBound
            && ReferenceEquals(csNr.StructBound, bound))
            return FunnyType.Generic(genericIdx);
        if (!visited.Add(nr)) return FunnyType.Any; // cycle without self-edge — bail
        try {
            switch (nr.State) {
                case StateOptional opt:
                    return FunnyType.OptionalOf(BuildFieldType(opt.ElementNode, bound, ownerCs, genericIdx, visited));
                case StateArray arr:
                    return FunnyType.ArrayOf(BuildFieldType(arr.ElementNode, bound, ownerCs, genericIdx, visited));
                case StateStruct str when str.TypeName != null:
                    return FunnyType.NamedStructOf(str.TypeName);
                case StateStruct str:
                    return BuildStructLayer(str, bound, ownerCs, genericIdx, visited);
                case StatePrimitive prim:
                    return ToConcrete(prim.Name);
                case ConstraintsState cs:
                    // For F-bound field types: prefer ANCESTOR (widest acceptable
                    // type the field can hold) over Descendant. The bound is an
                    // UPPER bound on the candidate's field — we want the loosest
                    // possible so concrete narrower types still satisfy.
                    // Example: body `n.v + 1` constrains n.v's CS with Ancestor
                    // = Real; the bound emits Real so candidate.v of any numeric
                    // type satisfies it.
                    if (cs.Ancestor is StatePrimitive ancP)
                        return ToConcrete(ancP.Name);
                    if (cs.HasDescendant && cs.Descendant is StatePrimitive descP)
                        return ToConcrete(descP.Name);
                    return FunnyType.Any;
                default:
                    return FunnyType.Any;
            }
        } finally {
            visited.Remove(nr);
        }
    }

    /// <summary>
    /// Walk a TIC state finding any cycle-rescued struct (TypeName set) — return
    /// NamedStructOf preserving Optional/Array wrapping. Returns null if no
    /// named-struct content is found.
    /// Used by VisitOperator and GenericUserFunction.Create when the converter's
    /// structural conversion would hide the named identity inside a struct
    /// expansion: callers prefer the named form so the runtime call-site match
    /// uses identity rather than full structural compare.
    /// </summary>
    public static FunnyType? BuildNamedTypeFromTicState(ITicNodeState state)
        => BuildNamedTypeFromTicState(state, depth: 0);

    private const int MaxNamedTypeBuildDepth = 128;

    // Cycle/depth guard: identity-through-none patterns
    // (`f(x) = if (x==none) none else x`) can synthesise a constraint state
    // whose Descendant points back into the same chain via RefTo, producing
    // unbounded recursion → stack overflow (Round 6 #84). Bounded depth
    // returns null at the limit; null means "no named-type content" which
    // is the correct fallback for these cycles.
    private static FunnyType? BuildNamedTypeFromTicState(ITicNodeState state, int depth) {
        if (depth > MaxNamedTypeBuildDepth) return null;
        switch (state) {
            case StateRefTo r:
                return BuildNamedTypeFromTicState(r.Node.State, depth + 1);
            case StateOptional opt: {
                var inner = BuildNamedTypeFromTicState(opt.ElementNode.State, depth + 1);
                return inner.HasValue ? FunnyType.OptionalOf(inner.Value) : null;
            }
            case StateArray arr: {
                var inner = BuildNamedTypeFromTicState(arr.ElementNode.State, depth + 1);
                return inner.HasValue ? FunnyType.ArrayOf(inner.Value) : null;
            }
            case StateStruct str when str.TypeName != null:
                return FunnyType.NamedStructOf(str.TypeName);
            case ConstraintsState cs when cs.HasDescendant: {
                var inner = BuildNamedTypeFromTicState(cs.Descendant, depth + 1);
                if (inner.HasValue)
                    return cs.IsOptional ? FunnyType.OptionalOf(inner.Value) : inner;
                return null;
            }
            default:
                return null;
        }
    }

    private FunnyType ConvertToFunnyStruct(StateStruct str) {
        if (_convertMark == 0) _convertMark = Tic.SolvingFunctions.NextMark();

        // Struct-level cycle detection for self-referential named types.
        // When TIC solving fills a recursion boundary with the enclosing named struct,
        // field nodes are shared between levels. Per-node marks can miss this cycle,
        // so we also track by TypeName.
        if (str.TypeName != null) {
            _convertingNamedTypes ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // A named struct nested inside ANOTHER named-struct conversion (depth ≥ 1) returns
            // NamedStructOf, preserving identity. Otherwise depth-1 structs get expanded as plain
            // Struct, losing the named identity required for runtime Fit on F-bounded functions.
            if (_convertingNamedTypes.Count > 0 && !_convertingNamedTypes.Contains(str.TypeName))
                return FunnyType.NamedStructOf(str.TypeName);
            if (!_convertingNamedTypes.Add(str.TypeName))
                return FunnyType.NamedStructOf(str.TypeName);
        }

        var fields = new StructTypeSpecification(str.FieldsCount, isFrozen: str.IsFrozen);
        foreach (var ticField in str.Fields)
        {
            var fieldNode = ticField.Value.GetNonReference();
            if (fieldNode.VisitMark == _convertMark)
            {
                // Cycle detected — use TypeName if available (from node state or parent chain)
                if (fieldNode.State is StateStruct { TypeName: { } tn })
                    fields.Add(ticField.Key, FunnyType.NamedStructOf(tn));
                // If this is a StateStruct (TypeName lost via GetNonReferenced)
                // and we're inside a named struct conversion, use the parent's TypeName.
                else if (fieldNode.State is StateStruct
                         && _convertingNamedTypes is { Count: > 0 })
                    fields.Add(ticField.Key,
                        FunnyType.NamedStructOf(_convertingNamedTypes.First()));
                else
                    fields.Add(ticField.Key, FunnyType.Any);
                continue;
            }
            var prev = fieldNode.VisitMark;
            fieldNode.VisitMark = _convertMark;
            fields.Add(ticField.Key, Convert(fieldNode.State));
            fieldNode.VisitMark = prev;
        }

        if (str.TypeName != null)
            _convertingNamedTypes!.Remove(str.TypeName);

        return FunnyType.StructOf(fields);
    }

    private FunnyType ConvertToFunnyFun(StateFun fun)
        => FunnyType.FunOf(Convert(fun.ReturnType), fun.ArgNodes.SelectToArray(a => Convert(a.State)));

    private FunnyType ConvertToFunnyArray(StateArray array)
        => FunnyType.ArrayOf(Convert(array.Element));

    private const int OptionalConvertMark = -58000;
    private FunnyType ConvertToFunnyOptional(StateOptional opt) {
        // Cycle guard: generic functions with if..else none create cyclic Optionals
        var elem = opt.ElementNode;
        if (elem.VisitMark == OptionalConvertMark)
            return FunnyType.Any; // break cycle
        var prev = elem.VisitMark;
        elem.VisitMark = OptionalConvertMark;
        var result = FunnyType.OptionalOf(Convert(opt.Element));
        elem.VisitMark = prev;
        return result;
    }

    class OnlyConcreteTypesConverter : TicTypesConverter {
        public OnlyConcreteTypesConverter() { }
        public override FunnyType Convert(ITicNodeState type) {
            while (true)
            {
                switch (type)
                {
                    case StateRefTo refTo:
                        type = refTo.Element;
                        continue;
                    case StatePrimitiveCustom custom:
                        return custom.OriginalFunnyType;
                    // Note: previously had a rule that converted Any → NamedStructOf when inside a
                    // named struct conversion (the "recursion boundary" heuristic). Removed because
                    // it conflates genuine Any fields (e.g. `type t = {a:any}`) with the recursion
                    // boundary case. With LangTiHelper.ResolveNamedStruct now stamping TypeName on
                    // the root, recursion boundary identity is preserved through ConvertToFunnyStruct's
                    // own _convertingNamedTypes machinery — no need to special-case Any here.
                    case StatePrimitive primitive:
                        return ToConcrete(primitive.Name);
                    case ConstraintsState constrains when constrains.Preferred != null:
                        if (constrains.HasDescendant && constrains.Descendant is StateOptional)
                            return FunnyType.OptionalOf(ToConcrete(constrains.Preferred.Name));
                        return ToConcrete(constrains.Preferred.Name);
                    case ConstraintsState constrains when !constrains.HasAncestor:
                    {
                        if (constrains.IsComparable) return FunnyType.Real;
                        // Abstract desc with no ancestor: resolve to narrowest concrete ancestor.
                        // e.g., [I48..null] from LCA(int32, uint32) → Int64
                        if (constrains.HasDescendant && constrains.Descendant is StatePrimitive { IsAbstract: true } abs)
                            return constrains.IsOptional
                                ? FunnyType.OptionalOf(ToConcrete(abs.ConcreteAncestor.Name))
                                : ToConcrete(abs.ConcreteAncestor.Name);
                        // Inside a named struct: Empty constraint = recursion boundary
                        if (_convertingNamedTypes is { Count: > 0 } && constrains.NoConstrains)
                            return FunnyType.NamedStructOf(_convertingNamedTypes.First());
                        return FunnyType.Any;
                    }
                    case ConstraintsState constrains:
                    {
                        if (constrains.Ancestor.Name.HasFlag(PrimitiveTypeName._isAbstract))
                        {
                            switch (constrains.Ancestor.Name)
                            {
                                case PrimitiveTypeName.I96:
                                {
                                    if (constrains.HasDescendant &&
                                        constrains.Descendant.CanBePessimisticConvertedTo(StatePrimitive.I32))
                                        return FunnyType.Int32;
                                    return FunnyType.Int64;
                                }
                                case PrimitiveTypeName.I48:
                                {
                                    if (constrains.HasDescendant &&
                                        constrains.Descendant.CanBePessimisticConvertedTo(StatePrimitive.I32))
                                        return FunnyType.Int32;
                                    return FunnyType.UInt32;
                                }
                                case PrimitiveTypeName.U48:
                                    return FunnyType.UInt32;
                                case PrimitiveTypeName.U24:
                                    return FunnyType.UInt16;
                                case PrimitiveTypeName.U12:
                                    return FunnyType.UInt8;
                                case PrimitiveTypeName.I24:
                                {
                                    if (constrains.HasDescendant &&
                                        constrains.Descendant.CanBePessimisticConvertedTo(StatePrimitive.I16))
                                        return FunnyType.Int16;
                                    return FunnyType.Int32;
                                }
                                case PrimitiveTypeName.I12:
                                {
                                    // I12 abstract: descendant fits I8 (e.g. U4) → I8; else widen to I16.
                                    if (constrains.HasDescendant &&
                                        constrains.Descendant.CanBePessimisticConvertedTo(StatePrimitive.I8))
                                        return FunnyType.Int8;
                                    return FunnyType.Int16;
                                }
                                case PrimitiveTypeName.U4:
                                    // U4 = lattice bottom (0..127 range). Materialise as UInt8 — the
                                    // narrowest concrete unsigned that covers U4's value range.
                                    return FunnyType.UInt8;
                                default:
                                    throw new NotSupportedException();
                            }
                        }

                        return ToConcrete(constrains.Ancestor.Name);
                    }

                    case StateArray array:
                        return ConvertToFunnyArray(array);
                    case StateOptional opt:
                        return ConvertToFunnyOptional(opt);
                    case StateFun fun:
                        return ConvertToFunnyFun(fun);
                    case StateStruct str:
                        return ConvertToFunnyStruct(str);
                    default:
                        throw new NFunImpossibleException($"Type {type?.ToString()??"<null>"} is not supported for convertion");
                }
            }
        }
    }

    private class ConstrainsConverter : TicTypesConverter {
        private readonly IReadOnlyList<ConstraintsState> _constrainsMap;
        private readonly IReadOnlyList<(StateStruct Struct, ConstraintsState Wrapper)> _structGenericMap;

        public ConstrainsConverter(IReadOnlyList<ConstraintsState> constrainsMap,
            IReadOnlyList<(StateStruct, ConstraintsState)> structGenericMap = null) {
            _constrainsMap = constrainsMap;
            _structGenericMap = structGenericMap;
        }

        public override FunnyType Convert(ITicNodeState type)
            => type switch {
                   StateRefTo refTo           => Convert(refTo.Element),
                   StatePrimitiveCustom custom         => custom.OriginalFunnyType,
                   StatePrimitive primitive   => ToConcrete(primitive.Name),
                   ConstraintsState constrains => FunnyType.Generic(GetGenericIndexOrThrow(constrains)),
                   StateArray array           => ConvertToFunnyArray(array),
                   StateOptional opt          => ConvertToFunnyOptional(opt),
                   StateFun fun               => ConvertToFunnyFun(fun),
                   StateStruct str            => TryGetStructGenericIndex(str, out var idx) ? FunnyType.Generic(idx) : ConvertToFunnyStruct(str),
                   _                          => throw new NotSupportedException($"State {type} is not supported for convertion to Fun type")
               };

        private int GetGenericIndexOrThrow(ConstraintsState constraints) {
            // Generic identity = object identity (TicResolution.md §2, invariant R1):
            // independent generics may have equal content, so structural Equals conflates.
            var index = _constrainsMap.IndexOfByReference(constraints);
            if (index == -1)
                throw new InvalidOperationException("Unknown constraints");
            return index;
        }

        private bool TryGetStructGenericIndex(StateStruct str, out int index) {
            if (_structGenericMap != null)
            {
                for (int i = 0; i < _structGenericMap.Count; i++)
                {
                    if (StructMatchesByFields(_structGenericMap[i].Struct, str))
                    {
                        // The wrapper's position in the extended constrains map gives the generic
                        // index. The wrapper is the exact object appended to extendedGenerics —
                        // identity lookup (§2 contract extends to wrappers).
                        index = _constrainsMap.IndexOfByReference(_structGenericMap[i].Wrapper);
                        return index >= 0;
                    }
                }
            }
            index = -1;
            return false;
        }

        /// <summary>
        /// Match struct generics by field names.
        /// The actual struct must have at least all fields of the expected struct.
        /// This handles the case where TIC solving creates new StateStruct objects
        /// via MergeStructs, and where the actual struct might have additional fields.
        /// </summary>
        private static bool StructMatchesByFields(StateStruct expected, StateStruct actual) {
            if (ReferenceEquals(expected, actual)) return true;
            // The actual struct must contain all fields from the expected (struct generic) pattern.
            // It may have additional fields (from other constraints or merges during solving).
            foreach (var (key, _) in expected.Fields)
            {
                if (actual.GetFieldOrNull(key) == null) return false;
            }
            return true;
        }
    }

    private class GenericMapConverter : TicTypesConverter {
        private readonly IReadOnlyList<ConstraintsState> _constrainsMap;
        private readonly IList<FunnyType> _argTypes;
        private readonly IReadOnlyList<(StateStruct Struct, ConstraintsState Wrapper)> _structGenericMap;

        public GenericMapConverter(IReadOnlyList<ConstraintsState> constrainsMap, IList<FunnyType> argTypes,
            IReadOnlyList<(StateStruct, ConstraintsState)> structGenericMap = null) {
            _constrainsMap = constrainsMap;
            _argTypes = argTypes;
            _structGenericMap = structGenericMap;
        }

        public override FunnyType Convert(ITicNodeState type) {
            while (true)
            {
                switch (type)
                {
                    case StateRefTo refTo:
                        type = refTo.Element;
                        continue;
                    case StatePrimitiveCustom custom:
                        return custom.OriginalFunnyType;
                    case StatePrimitive primitive:
                        return ToConcrete(primitive.Name);
                    case ConstraintsState constrains:
                        // Generic identity = object identity (TicResolution.md §2, invariant R1).
                        var index = _constrainsMap.IndexOfByReference(constrains);
                        if (index == -1) throw new InvalidOperationException("Unknown constrains");
                        return _argTypes[index];
                    case StateArray array:
                        return ConvertToFunnyArray(array);
                    case StateOptional opt:
                        return ConvertToFunnyOptional(opt);
                    case StateFun fun:
                        return ConvertToFunnyFun(fun);
                    case StateStruct str:
                        if (TryGetStructGenericType(str, out var concreteType))
                            return concreteType;
                        return ConvertToFunnyStruct(str);
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        private bool TryGetStructGenericType(StateStruct str, out FunnyType concreteType) {
            if (_structGenericMap != null)
            {
                for (int i = 0; i < _structGenericMap.Count; i++)
                {
                    if (StructMatchesByFields(_structGenericMap[i].Struct, str))
                    {
                        // Wrapper is the exact object appended to extendedGenerics — identity lookup.
                        var wrapperIndex = _constrainsMap.IndexOfByReference(_structGenericMap[i].Wrapper);
                        if (wrapperIndex >= 0)
                        {
                            concreteType = _argTypes[wrapperIndex];
                            return true;
                        }
                    }
                }
            }
            concreteType = default;
            return false;
        }

        /// <summary>
        /// Match struct generics by field names.
        /// Same logic as ConstrainsConverter.StructMatchesByFields.
        /// </summary>
        private static bool StructMatchesByFields(StateStruct expected, StateStruct actual) {
            if (ReferenceEquals(expected, actual)) return true;
            foreach (var (key, _) in expected.Fields)
            {
                if (actual.GetFieldOrNull(key) == null) return false;
            }
            return true;
        }
    }

    public static FunnyType ToConcrete(PrimitiveTypeName name) =>
        name switch {
            PrimitiveTypeName.Any  => FunnyType.Any,
            PrimitiveTypeName.Ip   => FunnyType.Ip,
            PrimitiveTypeName.Char => FunnyType.Char,
            PrimitiveTypeName.Bool => FunnyType.Bool,
            PrimitiveTypeName.Real => FunnyType.Real,
            PrimitiveTypeName.F32  => FunnyType.Float32,
            PrimitiveTypeName.I64  => FunnyType.Int64,
            PrimitiveTypeName.I32  => FunnyType.Int32,
            PrimitiveTypeName.I24  => FunnyType.Int32,
            PrimitiveTypeName.I16  => FunnyType.Int16,
            PrimitiveTypeName.I12  => FunnyType.Int16,
            PrimitiveTypeName.I8   => FunnyType.Int8,
            PrimitiveTypeName.U64  => FunnyType.UInt64,
            PrimitiveTypeName.U32  => FunnyType.UInt32,
            PrimitiveTypeName.U16  => FunnyType.UInt16,
            PrimitiveTypeName.U8   => FunnyType.UInt8,
            PrimitiveTypeName.I96  => FunnyType.Int64,
            PrimitiveTypeName.I48  => FunnyType.Int64,
            // Abstract types can appear as bare StatePrimitive when MergeOrNull collapses
            // a constraint interval to a single point (ancestor == descendant).
            // Map each to its nearest concrete ancestor that fits all values.
            PrimitiveTypeName.U48  => FunnyType.UInt64,
            PrimitiveTypeName.U24  => FunnyType.UInt32,
            PrimitiveTypeName.U12  => FunnyType.UInt16,
            PrimitiveTypeName.U4   => FunnyType.UInt8,
            PrimitiveTypeName.None => FunnyType.None,
            _ => throw new ArgumentOutOfRangeException()
        };
}
