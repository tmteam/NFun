using System;
using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tokenization;

namespace NFun.Types; 

public static class VarTypeConverter {
    private static readonly bool[,] PrimitiveConvertMap;
    private const int PrimitiveTypeCount = 16;
    static VarTypeConverter() {
        PrimitiveConvertMap = new bool [PrimitiveTypeCount, PrimitiveTypeCount];
        //every type can be converted to itself
        for (int i = 1; i < PrimitiveTypeCount; i++)
            PrimitiveConvertMap[i, i] = true;
        //except arrays and funs
        PrimitiveConvertMap[(int)BaseFunnyType.ArrayOf, (int)BaseFunnyType.ArrayOf] = false;
        PrimitiveConvertMap[(int)BaseFunnyType.Fun, (int)BaseFunnyType.Fun] = false;

        //every type can be converted to any
        for (int i = 1; i < PrimitiveTypeCount; i++)
            PrimitiveConvertMap[i, (int)BaseFunnyType.Any] = true;
        for (int i = (int)BaseFunnyType.UInt8; i < (int)BaseFunnyType.Real; i++)
        {
            //every number can be converted to real
            PrimitiveConvertMap[i, (int)BaseFunnyType.Real] = true;
            //every number can be converted from u8
            PrimitiveConvertMap[(int)BaseFunnyType.UInt8, i] = true;
        }

        PrimitiveConvertMap[(int)BaseFunnyType.UInt16, (int)BaseFunnyType.UInt32] = true;
        PrimitiveConvertMap[(int)BaseFunnyType.UInt16, (int)BaseFunnyType.UInt64] = true;
        PrimitiveConvertMap[(int)BaseFunnyType.UInt16, (int)BaseFunnyType.Int32] = true;
        PrimitiveConvertMap[(int)BaseFunnyType.UInt16, (int)BaseFunnyType.Int64] = true;

        PrimitiveConvertMap[(int)BaseFunnyType.UInt32, (int)BaseFunnyType.UInt64] = true;
        PrimitiveConvertMap[(int)BaseFunnyType.UInt32, (int)BaseFunnyType.Int64] = true;

        PrimitiveConvertMap[(int)BaseFunnyType.Int16, (int)BaseFunnyType.Int32] = true;
        PrimitiveConvertMap[(int)BaseFunnyType.Int16, (int)BaseFunnyType.Int64] = true;

        PrimitiveConvertMap[(int)BaseFunnyType.Int32, (int)BaseFunnyType.Int64] = true;

    }

    private static readonly Func<object, object> ToText = o => new TextFunnyArray(o?.ToString() ?? "");
    private static readonly Func<object, object> NoConvertion = o => o;
    
    public static Func<object, object> GetConverterOrNull(TypeBehaviour typeBehaviour, FunnyType @from, FunnyType to) {
        //todo coverage
        if (from.Equals(to))
            return NoConvertion;
        // ToText shortcut for primitive → text conversions. Don't engage it for
        // collection-typed sources (Array / List / MutableArray) — those need
        // structural conversion below; naive `.ToString()` on a collection
        // yields "[a,b,c]" rather than the element string.
        if (to.IsText
            && from.BaseType != BaseFunnyType.ArrayOf
            && from.BaseType != BaseFunnyType.List
            && from.BaseType != BaseFunnyType.MutableArray
            && from.BaseType != BaseFunnyType.FixedArray)
            return ToText;
        if (to.BaseType == BaseFunnyType.Any)
            return NoConvertion;
        // Any → T: recursive type cycle placeholder — runtime value has the actual type
        if (from.BaseType == BaseFunnyType.Any)
            return NoConvertion;

        // Custom → same Custom: identity
        if (from.BaseType == BaseFunnyType.Custom && to.BaseType == BaseFunnyType.Custom
            && Equals(from.CustomTypeDefinition, to.CustomTypeDefinition))
            return NoConvertion;

        // NamedStruct ↔ Struct: named type boundary in recursive types.
        // Runtime values are FunnyStruct regardless of named vs anonymous typing.
        if (to.BaseType == BaseFunnyType.NamedStruct && from.BaseType == BaseFunnyType.Struct)
            return NoConvertion;
        if (from.BaseType == BaseFunnyType.NamedStruct && to.BaseType == BaseFunnyType.Struct)
            return NoConvertion;
        if (from.BaseType == BaseFunnyType.NamedStruct && to.BaseType == BaseFunnyType.NamedStruct)
            return NoConvertion;

        // None → Optional(T): FunnyNone stays FunnyNone
        if (from.BaseType == BaseFunnyType.None && to.BaseType == BaseFunnyType.Optional)
            return NoConvertion;

        // T → Optional(T): implicit wrapping (boxed value is already valid)
        // At runtime, value might be FunnyNone (from coalesce chains); pass it through.
        if (to.BaseType == BaseFunnyType.Optional && from.BaseType != BaseFunnyType.Optional)
        {
            var inner = GetConverterOrNull(typeBehaviour, from, to.OptionalTypeSpecification.ElementType);
            if (inner == null || inner == NoConvertion) return NoConvertion;
            return o => o is FunnyNone ? o : inner(o);
        }

        // Optional(NamedStruct) → Struct: runtime values are FunnyStruct either way,
        // NoConvertion is safe because FunnyNone passes through unchanged.
        if (from.BaseType == BaseFunnyType.Optional
            && from.OptionalTypeSpecification.ElementType.BaseType == BaseFunnyType.NamedStruct
            && to.BaseType == BaseFunnyType.Struct)
            return o => o is FunnyNone ? o : o;
        if (from.BaseType == BaseFunnyType.Optional
            && from.OptionalTypeSpecification.ElementType.BaseType == BaseFunnyType.Struct
            && to.BaseType == BaseFunnyType.NamedStruct)
            return o => o is FunnyNone ? o : o;

        if (from.BaseType == BaseFunnyType.Char)
            return typeBehaviour.GetFromCharToNumberConverterOrNull(to.BaseType);
        // real → integer per spec §1.1: truncate (toward zero), not banker's round.
        // GetRealToIntConverterOrNull returns null for non-integer targets; we fall
        // through to the general numeric converter (which handles real → real etc.).
        if (from.BaseType == BaseFunnyType.Real)
        {
            var realToInt = TypeBehaviour.GetRealToIntConverterOrNull(to.BaseType);
            if (realToInt != null) return realToInt;
        }
        if (from.IsNumeric())
            return  typeBehaviour.GetNumericConverterOrNull(to.BaseType);

        // Stage C — any concrete collection → Enumerable<T> is identity at runtime
        // because all collection runtime types implement IFunnyEnumerable. The
        // generic-resolution layer collapses element types; the boxed reference passes
        // through unchanged.
        if (to.BaseType == BaseFunnyType.Enumerable
            && (from.BaseType == BaseFunnyType.List
                || from.BaseType == BaseFunnyType.MutableArray
                || from.BaseType == BaseFunnyType.FixedArray
                || from.BaseType == BaseFunnyType.ArrayOf
                || from.BaseType == BaseFunnyType.Set
                || from.BaseType == BaseFunnyType.Enumerable
                || from.BaseType == BaseFunnyType.Clearable
                || from.BaseType == BaseFunnyType.Map))
            return NoConvertion;

        // Mutable<T> is satisfied by list / array / set / map runtime values —
        // and by another Mutable (no-op). Every concrete kind for which
        // ConstructorLattice.IsMutable is true. FixedArray and ee-mode T[] are
        // rejected here because they're immutable; that rejection is what makes
        // clear() etc. emit a parse error instead of a runtime exception.
        if (to.BaseType == BaseFunnyType.Clearable
            && (from.BaseType == BaseFunnyType.List
                || from.BaseType == BaseFunnyType.MutableArray
                || from.BaseType == BaseFunnyType.Set
                || from.BaseType == BaseFunnyType.Map
                || from.BaseType == BaseFunnyType.Clearable))
            return NoConvertion;

        // Lang-mode list<T> ≤ T[] per Stage 0 collections hierarchy
        // (`List ⊆ Array ⊆ FixedArray ⊆ Enumerable`). Allows existing LINQ
        // generic functions keyed on `T[]` to consume list literals + factory
        // results without per-function overloads. Element conversion piggy-backs
        // on the array path's recursive resolution.
        if (from.BaseType == BaseFunnyType.List && to.BaseType == BaseFunnyType.ArrayOf)
        {
            var fromElem = from.ListTypeSpecification.FunnyType;
            var toElem = to.ArrayTypeSpecification.FunnyType;
            var elementConverter = GetConverterOrNull(typeBehaviour, fromElem, toElem);
            if (elementConverter == null)
                return null;
            return o => {
                var origin = (NFun.Runtime.Lists.IFunnyList)o;
                var array = new object[origin.Count];
                int i = 0;
                foreach (var e in origin)
                    array[i++] = elementConverter == NoConvertion ? e : elementConverter(e);
                return new Runtime.Arrays.ImmutableFunnyArray(array, toElem);
            };
        }

        // Lang-mode array<T> ≤ T[] (Stage 0). Symmetric to the list case above.
        if (from.BaseType == BaseFunnyType.MutableArray && to.BaseType == BaseFunnyType.ArrayOf)
        {
            var fromElem = from.MutableArrayTypeSpecification.FunnyType;
            var toElem = to.ArrayTypeSpecification.FunnyType;
            var elementConverter = GetConverterOrNull(typeBehaviour, fromElem, toElem);
            if (elementConverter == null)
                return null;
            return o => {
                var origin = (NFun.Runtime.Lists.IFunnyMutableArray)o;
                var array = new object[origin.Count];
                for (int i = 0; i < origin.Count; i++) {
                    var e = origin.GetElementOrNull(i);
                    array[i] = elementConverter == NoConvertion ? e : elementConverter(e);
                }
                return new Runtime.Arrays.ImmutableFunnyArray(array, toElem);
            };
        }

        // list<T> ≤ array<T> per Stage 0 (lattice direction `List ⊂ Array`).
        // List handle is already an IFunnyMutableArray, so the runtime cast is
        // a copy into a fixed-length array (size pinned). Element conversion
        // recurses on the inner types.
        if (from.BaseType == BaseFunnyType.List && to.BaseType == BaseFunnyType.MutableArray)
        {
            var fromElem = from.ListTypeSpecification.FunnyType;
            var toElem = to.MutableArrayTypeSpecification.FunnyType;
            var elementConverter = GetConverterOrNull(typeBehaviour, fromElem, toElem);
            if (elementConverter == null)
                return null;
            return o => {
                var origin = (NFun.Runtime.Lists.IFunnyList)o;
                var items = new object[origin.Count];
                int i = 0;
                foreach (var e in origin)
                    items[i++] = elementConverter == NoConvertion ? e : elementConverter(e);
                return new NFun.Runtime.Lists.MutableFunnyArray(toElem, items);
            };
        }

        // Reverse direction: legacy T[] → lang array<T> (for assignment slots
        // that absorb ee-mode LINQ results).
        if (from.BaseType == BaseFunnyType.ArrayOf && to.BaseType == BaseFunnyType.MutableArray)
        {
            var fromElem = from.ArrayTypeSpecification.FunnyType;
            var toElem = to.MutableArrayTypeSpecification.FunnyType;
            var elementConverter = GetConverterOrNull(typeBehaviour, fromElem, toElem);
            if (elementConverter == null) return null;
            return o => {
                var origin = (Runtime.Arrays.IFunnyArray)o;
                var items = new object[origin.Count];
                for (int i = 0; i < origin.Count; i++) {
                    var e = origin.GetElementOrNull(i);
                    items[i] = elementConverter == NoConvertion ? e : elementConverter(e);
                }
                return new NFun.Runtime.Lists.MutableFunnyArray(toElem, items);
            };
        }

        // Per the Stage-0 lattice: implicit casts are upcast-only.
        // array → list and fixedArray → list/array require explicit `.toList()`
        // / `.toArray()` / etc. (Stage C user-facing API).

        // FixedArray conversions per Stage 0 lattice (`Array ⊂ FixedArray`):
        //   list/array → fixedArray (subtype direction; copy snapshot)
        //   fixedArray → T[]        (LINQ subtyping)
        if (from.BaseType == BaseFunnyType.List && to.BaseType == BaseFunnyType.FixedArray)
        {
            var fromElem = from.ListTypeSpecification.FunnyType;
            var toElem = to.FixedArrayTypeSpecification.FunnyType;
            var elementConverter = GetConverterOrNull(typeBehaviour, fromElem, toElem);
            if (elementConverter == null) return null;
            return o => {
                var origin = (NFun.Runtime.Lists.IFunnyList)o;
                var items = new object[origin.Count];
                int i = 0;
                foreach (var e in origin)
                    items[i++] = elementConverter == NoConvertion ? e : elementConverter(e);
                return new NFun.Runtime.Lists.FixedFunnyArray(toElem, items);
            };
        }
        if (from.BaseType == BaseFunnyType.MutableArray && to.BaseType == BaseFunnyType.FixedArray)
        {
            var fromElem = from.MutableArrayTypeSpecification.FunnyType;
            var toElem = to.FixedArrayTypeSpecification.FunnyType;
            var elementConverter = GetConverterOrNull(typeBehaviour, fromElem, toElem);
            if (elementConverter == null) return null;
            return o => {
                var origin = (NFun.Runtime.Lists.IFunnyMutableArray)o;
                var items = new object[origin.Count];
                for (int i = 0; i < origin.Count; i++) {
                    var e = origin.GetElementOrNull(i);
                    items[i] = elementConverter == NoConvertion ? e : elementConverter(e);
                }
                return new NFun.Runtime.Lists.FixedFunnyArray(toElem, items);
            };
        }
        if (from.BaseType == BaseFunnyType.FixedArray && to.BaseType == BaseFunnyType.ArrayOf)
        {
            var fromElem = from.FixedArrayTypeSpecification.FunnyType;
            var toElem = to.ArrayTypeSpecification.FunnyType;
            var elementConverter = GetConverterOrNull(typeBehaviour, fromElem, toElem);
            if (elementConverter == null) return null;
            return o => {
                // Stage C — under unified ee↔lang model, a `fixedArray<T>` slot may at
                // runtime hold either FixedFunnyArray (lang-mode literal / map result)
                // OR ee-mode ImmutableFunnyArray (Concretest-routed legacy literal that
                // didn't get wrapped). Accept both.
                int count = o switch {
                    NFun.Runtime.Lists.IFunnyFixedArray f => f.Count,
                    IFunnyArray a => a.Count,
                    _ => throw new System.InvalidCastException($"fixedArray→array converter: unsupported source {o?.GetType()}"),
                };
                var array = new object[count];
                int idx = 0;
                foreach (var e in (System.Collections.Generic.IEnumerable<object>)o) {
                    array[idx++] = elementConverter == NoConvertion ? e : elementConverter(e);
                }
                return new Runtime.Arrays.ImmutableFunnyArray(array, toElem);
            };
        }
        // Stage C — ee-mode T[] flows into lang-mode fixedArray<T> slot. Since T[] IS
        // fixedArray in ee semantics (single collection type), the conversion is a
        // re-wrap with element conversion: ee ImmutableFunnyArray → lang FixedFunnyArray.
        if (from.BaseType == BaseFunnyType.ArrayOf && to.BaseType == BaseFunnyType.FixedArray)
        {
            var fromElem = from.ArrayTypeSpecification.FunnyType;
            var toElem = to.FixedArrayTypeSpecification.FunnyType;
            var elementConverter = GetConverterOrNull(typeBehaviour, fromElem, toElem);
            if (elementConverter == null) return null;
            return o => {
                var origin = (Runtime.Arrays.IFunnyArray)o;
                var array = new object[origin.Count];
                for (int i = 0; i < origin.Count; i++) {
                    var e = origin.GetElementOrNull(i);
                    array[i] = elementConverter == NoConvertion ? e : elementConverter(e);
                }
                return new NFun.Runtime.Lists.FixedFunnyArray(toElem, array);
            };
        }

        // Per the lattice, fixedArray → list / array is downcast direction.
        // Requires an explicit `.toList()` / `.toArray()` — no silent runtime
        // cast here.

        // Per the Stage-0 lattice: list ⊆ array. Implicit cast is ONLY in the
        // upcast direction. The reverse (array → list) requires an explicit
        // `.toList()` — kept symmetric with toArray / toSet / toFixedArray
        // (Stage C user-facing API). No "silent" array→list converter here.

        if (from.BaseType != to.BaseType)
            return null;
        
        if (from.BaseType == BaseFunnyType.Optional)
        {
            var elementConverter = GetConverterOrNull(typeBehaviour, @from.OptionalTypeSpecification.ElementType, to.OptionalTypeSpecification.ElementType);
            if (elementConverter == null)
                return null;
            if (elementConverter == NoConvertion)
                return NoConvertion;
            return o => o is FunnyNone ? o : elementConverter(o);
        }

        if (from.BaseType == BaseFunnyType.ArrayOf)
        {
            if (to == FunnyType.ArrayOf(FunnyType.Any))
                return o => o;

            var elementConverter = GetConverterOrNull(typeBehaviour, @from.ArrayTypeSpecification.FunnyType, to.ArrayTypeSpecification.FunnyType);
            if (elementConverter == null)
                return null;

            return o => {
                var origin = (IFunnyArray)o;
                var array = new object[origin.Count];
                int index = 0;
                foreach (var e in origin)
                {
                    array[index] = elementConverter(e);
                    index++;
                }

                return new ImmutableFunnyArray(array, to.ArrayTypeSpecification.FunnyType);
            };
        }
        // Stage C — fixedArray<X> → fixedArray<Y> via element conversion. Runtime
        // source may be either FixedFunnyArray (lang-mode literal / map result) or
        // ee-mode IFunnyArray (Concretest-routed legacy literal); accept both.
        if (from.BaseType == BaseFunnyType.FixedArray)
        {
            var fromElem = from.FixedArrayTypeSpecification.FunnyType;
            var toElem = to.FixedArrayTypeSpecification.FunnyType;
            var elementConverter = GetConverterOrNull(typeBehaviour, fromElem, toElem);
            if (elementConverter == null) return null;
            if (elementConverter == NoConvertion) return NoConvertion;
            return o => {
                int count = o switch {
                    IFunnyArray a => a.Count,
                    NFun.Runtime.Lists.IFunnyEnumerable e => e.Count,
                    _ => throw new System.InvalidCastException($"fixedArray converter: unsupported source {o?.GetType()}"),
                };
                var array = new object[count];
                int i = 0;
                foreach (var item in (System.Collections.Generic.IEnumerable<object>)o)
                    array[i++] = elementConverter(item);
                return new NFun.Runtime.Lists.FixedFunnyArray(toElem, array);
            };
        }
        // set<X> → set<Y> via element conversion. Source is MutableFunnySet (lang-mode).
        if (from.BaseType == BaseFunnyType.Set)
        {
            var fromElem = from.SetTypeSpecification.FunnyType;
            var toElem = to.SetTypeSpecification.FunnyType;
            var elementConverter = GetConverterOrNull(typeBehaviour, fromElem, toElem);
            if (elementConverter == null) return null;
            if (elementConverter == NoConvertion) return NoConvertion;
            return o => {
                var origin = (NFun.Runtime.Lists.IFunnyMutableSet)o;
                var items = new List<object>(origin.Count);
                foreach (var item in origin)
                    items.Add(elementConverter(item));
                return new NFun.Runtime.Lists.MutableFunnySet(toElem, items);
            };
        }
        if (from.BaseType == BaseFunnyType.Fun)
        {
            var fromInputs = from.FunTypeSpecification.Inputs;
            var toInputs = to.FunTypeSpecification.Inputs;
            if (fromInputs.Length != toInputs.Length)
                return null;
            var inputConverters = new Func<object, object>[fromInputs.Length];
            for (int i = 0; i < fromInputs.Length; i++)
            {
                var fromInput = fromInputs[i];
                var toInput = toInputs[i];
                var inputConverter = GetConverterOrNull(typeBehaviour, toInput, fromInput);
                if (inputConverter == null)
                    return null;
                inputConverters[i] = inputConverter;
            }

            var outputConverter =
                GetConverterOrNull(typeBehaviour, @from.FunTypeSpecification.Output, to.FunTypeSpecification.Output);
            if (outputConverter == null)
                return null;

            object Converter(object input) => new ConcreteFunctionWithConvertion(
                origin: (IConcreteFunction)input,
                resultType: to.FunTypeSpecification,
                inputConverters: inputConverters,
                outputConverter: outputConverter);

            return Converter;
        }

        if (from.BaseType == BaseFunnyType.Struct)
        {
            var fieldConverters = new Dictionary<string, Func<object, object>>(StringComparer.InvariantCultureIgnoreCase);
            bool needsConversion = false;
            foreach (var (key, toFieldType) in to.StructTypeSpecification)
            {
                if (!from.StructTypeSpecification.TryGetValue(key, out var fromFieldType))
                    return null;
                var fieldConverter = GetConverterOrNull(typeBehaviour, fromFieldType, toFieldType);
                if (fieldConverter == null)
                    return null;
                fieldConverters[key] = fieldConverter;
                if (!fromFieldType.Equals(toFieldType))
                    needsConversion = true;
            }
            if (!needsConversion)
                return NoConvertion;
            return o => {
                var origin = (FunnyStruct)o;
                var fields = new FunnyStruct.FieldsDictionary(origin.Count);
                foreach (var (key, value) in origin)
                {
                    if (fieldConverters.TryGetValue(key, out var converter))
                        fields[key] = converter(value);
                    else
                        fields[key] = value;
                }
                return new FunnyStruct(fields);
            };
        }
        return null;
    }
    
    public static Func<object, object> GetConverterOrThrow(TypeBehaviour typeBehaviour, FunnyType from, FunnyType to,  Interval interval) {
        var res = GetConverterOrNull(typeBehaviour, @from, to);
        if (res == null)
            throw Errors.ImpossibleCast(from, to, interval);
        return res;
    }

    public static bool CanBeConverted(FunnyType from, FunnyType to) {
        while (true)
        {
            if (to.IsText) return true;

            // None → Optional(T) is always valid
            if (from.BaseType == BaseFunnyType.None && to.BaseType == BaseFunnyType.Optional)
                return true;

            // NamedStruct ↔ Struct: always compatible at runtime
            if ((to.BaseType == BaseFunnyType.NamedStruct && from.BaseType == BaseFunnyType.Struct) ||
                (from.BaseType == BaseFunnyType.NamedStruct && to.BaseType == BaseFunnyType.Struct) ||
                (from.BaseType == BaseFunnyType.NamedStruct && to.BaseType == BaseFunnyType.NamedStruct))
                return true;

            // T → Optional(T) - implicit wrapping
            if (to.BaseType == BaseFunnyType.Optional && from.BaseType != BaseFunnyType.Optional)
                return CanBeConverted(from, to.OptionalTypeSpecification.ElementType);

            // list<T> → T[] per collections hierarchy (Stage 0). And the reverse
            // for lang-mode mutable variable reassignment (`out = concat(out,…)`).
            if (from.BaseType == BaseFunnyType.List && to.BaseType == BaseFunnyType.ArrayOf)
                return CanBeConverted(from.ListTypeSpecification.FunnyType,
                                      to.ArrayTypeSpecification.FunnyType);
            // Upcast-only per Stage 0 lattice: List ⊆ MutableArray ⊆ FixedArray
            // ⊆ ArrayOf (ee), with FixedArray ⇄ ArrayOf as the ee↔lang bridge
            // (same semantic shape). Downcast requires explicit `.toXxx()`.
            if (from.BaseType == BaseFunnyType.List && to.BaseType == BaseFunnyType.MutableArray)
                return CanBeConverted(from.ListTypeSpecification.FunnyType,
                                      to.MutableArrayTypeSpecification.FunnyType);
            if (from.BaseType == BaseFunnyType.MutableArray && to.BaseType == BaseFunnyType.ArrayOf)
                return CanBeConverted(from.MutableArrayTypeSpecification.FunnyType,
                                      to.ArrayTypeSpecification.FunnyType);
            if (from.BaseType == BaseFunnyType.List && to.BaseType == BaseFunnyType.FixedArray)
                return CanBeConverted(from.ListTypeSpecification.FunnyType,
                                      to.FixedArrayTypeSpecification.FunnyType);
            if (from.BaseType == BaseFunnyType.MutableArray && to.BaseType == BaseFunnyType.FixedArray)
                return CanBeConverted(from.MutableArrayTypeSpecification.FunnyType,
                                      to.FixedArrayTypeSpecification.FunnyType);
            if (from.BaseType == BaseFunnyType.FixedArray && to.BaseType == BaseFunnyType.ArrayOf)
                return CanBeConverted(from.FixedArrayTypeSpecification.FunnyType,
                                      to.ArrayTypeSpecification.FunnyType);
            if (from.BaseType == BaseFunnyType.ArrayOf && to.BaseType == BaseFunnyType.FixedArray)
                return CanBeConverted(from.ArrayTypeSpecification.FunnyType,
                                      to.FixedArrayTypeSpecification.FunnyType);

            // Stage C — any concrete collection is convertible to Enumerable<T>
            // (constraint-only top of the lattice). Element-type check by container kind.
            if (to.BaseType == BaseFunnyType.Enumerable) {
                var toElem = to.EnumerableTypeSpecification.FunnyType;
                switch (from.BaseType) {
                    case BaseFunnyType.Enumerable:
                        return CanBeConverted(from.EnumerableTypeSpecification.FunnyType, toElem);
                    case BaseFunnyType.List:
                        return CanBeConverted(from.ListTypeSpecification.FunnyType, toElem);
                    case BaseFunnyType.MutableArray:
                        return CanBeConverted(from.MutableArrayTypeSpecification.FunnyType, toElem);
                    case BaseFunnyType.FixedArray:
                        return CanBeConverted(from.FixedArrayTypeSpecification.FunnyType, toElem);
                    case BaseFunnyType.ArrayOf:
                        return CanBeConverted(from.ArrayTypeSpecification.FunnyType, toElem);
                    case BaseFunnyType.Set:
                        return CanBeConverted(from.SetTypeSpecification.FunnyType, toElem);
                    case BaseFunnyType.Clearable:
                        return CanBeConverted(from.ClearableTypeSpecification.FunnyType, toElem);
                }
            }

            // Stage C — Mutable<T> typeclass. Satisfied only by mutable kinds
            // (list / array / set). FixedArray and ee-mode T[] reject here so
            // `clear(fixedArray(...))` fails at parse time.
            if (to.BaseType == BaseFunnyType.Clearable) {
                var toElem = to.ClearableTypeSpecification.FunnyType;
                switch (from.BaseType) {
                    case BaseFunnyType.Clearable:
                        return CanBeConverted(from.ClearableTypeSpecification.FunnyType, toElem);
                    case BaseFunnyType.List:
                        return CanBeConverted(from.ListTypeSpecification.FunnyType, toElem);
                    case BaseFunnyType.MutableArray:
                        return CanBeConverted(from.MutableArrayTypeSpecification.FunnyType, toElem);
                    case BaseFunnyType.Set:
                        return CanBeConverted(from.SetTypeSpecification.FunnyType, toElem);
                    // FixedArray, ArrayOf (ee), Enumerable — NOT mutable, reject.
                    default:
                        return false;
                }
            }

            if (to.BaseType == from.BaseType)
            {
                switch (to.BaseType)
                {
                    case BaseFunnyType.ArrayOf:
                        @from = @from.ArrayTypeSpecification.FunnyType;
                        to = to.ArrayTypeSpecification.FunnyType;
                        continue;
                    case BaseFunnyType.List:
                        // list<T> is INVARIANT — element must match exactly, not be convertible.
                        return @from.ListTypeSpecification.FunnyType
                            .Equals(to.ListTypeSpecification.FunnyType);
                    case BaseFunnyType.Optional:
                        @from = @from.OptionalTypeSpecification.ElementType;
                        to = to.OptionalTypeSpecification.ElementType;
                        continue;
                    //Check for Fun and struct types is quite expensive, so there is no big reason to write optimized code
                    case BaseFunnyType.Fun:
                        return GetConverterOrNull(Dialects.Origin.Converter.TypeBehaviour, @from, to) != null;
                    case BaseFunnyType.Struct:
                        return GetConverterOrNull(Dialects.Origin.Converter.TypeBehaviour, @from, to) != null;
                    case BaseFunnyType.Set:
                        // set<T> is INVARIANT — element must match exactly.
                        return @from.SetTypeSpecification.FunnyType
                            .Equals(to.SetTypeSpecification.FunnyType);
                    case BaseFunnyType.Clearable:
                        // Mutable<T> is INVARIANT — element must match exactly.
                        return @from.ClearableTypeSpecification.FunnyType
                            .Equals(to.ClearableTypeSpecification.FunnyType);
                }
            }

            // Custom types: convert only to same custom, Any, or Text (handled above)
            if (from.BaseType == BaseFunnyType.Custom || to.BaseType == BaseFunnyType.Custom)
                return from.BaseType == BaseFunnyType.Custom && to.BaseType == BaseFunnyType.Custom
                    && Equals(from.CustomTypeDefinition, to.CustomTypeDefinition);

            // NamedStruct: same name = convertible; otherwise only to Any/Text (handled above)
            if (from.BaseType == BaseFunnyType.NamedStruct || to.BaseType == BaseFunnyType.NamedStruct)
                return from.BaseType == BaseFunnyType.NamedStruct && to.BaseType == BaseFunnyType.NamedStruct
                    && string.Equals(from.NamedStructTypeName, to.NamedStructTypeName, StringComparison.OrdinalIgnoreCase);

            if ((int)from.BaseType >= PrimitiveTypeCount || (int)to.BaseType >= PrimitiveTypeCount)
                return false;

            return PrimitiveConvertMap[(int)from.BaseType, (int)to.BaseType];
        }
    }

    private class ConcreteFunctionWithConvertion : IConcreteFunction {
        private readonly IConcreteFunction _origin;
        private readonly FunTypeSpecification _resultType;
        private readonly Func<object, object>[] _inputConverters;
        private readonly Func<object, object> _outputConverter;

        public ConcreteFunctionWithConvertion(
            IConcreteFunction origin,
            FunTypeSpecification resultType,
            Func<object, object>[] inputConverters,
            Func<object, object> outputConverter) {
            _origin = origin;
            _resultType = resultType;
            _inputConverters = inputConverters;
            _outputConverter = outputConverter;
        }

        public string Name => _origin.Name;
        public FunnyType[] ArgTypes => _resultType.Inputs;
        public FunnyType ReturnType => _resultType.Output;

        public object Calc(object[] parameters) {
            var convertedParameters = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) {
                convertedParameters[i] = _inputConverters[i](parameters[i]);
            }

            var result = _origin.Calc(convertedParameters);
            var convertedResult = _outputConverter(result);
            return convertedResult;
        }

        public IConcreteFunction Clone(ICloneContext context) 
            => new ConcreteFunctionWithConvertion(_origin.Clone(context), _resultType, _inputConverters, _outputConverter);

        public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, TypeBehaviour typeBehaviour, Interval interval)
            => throw new NotSupportedException("Function convertation is not supported for expression building");
    }
}