using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.Functions;

/// <summary>
/// Overflow policy of the toXxx numeric conversion family (issue #135).
/// The policy lives in the function NAME, not in the dialect.
/// </summary>
public enum NumericNarrowMode {
    /// <summary>
    /// toXxx — out-of-range → runtime error (same numeric semantics as convert()).
    /// Real source: truncate toward zero, then range-check; NaN/±Inf → error.
    /// </summary>
    Checked,

    /// <summary>
    /// toXxxWrap — two's-complement wrap mod 2^n. Real source: truncate toward
    /// zero, then wrap; NaN/±Inf → error; trunc(x) outside the union of the
    /// int64/uint64 domains → error (wrap of an unrepresentable value is undefined).
    /// </summary>
    Wrap,

    /// <summary>
    /// toXxxClamp — saturate to [T.Min..T.Max]. Real source: clamp first, then
    /// truncate; NaN → error; +Inf → T.Max, -Inf → T.Min.
    /// </summary>
    Clamp
}

/// <summary>
/// One parameterized function class covers the whole toXxx/toXxxWrap/toXxxClamp
/// family: aliases (toByte≡toUint8, toInt≡toInt32, toUint≡toUint32,
/// toFloat64≡toReal) are extra INSTANCES with different names but the same
/// target+mode. Signature: <c>toXxx(x: T): Target</c> where T is Numbers-constrained.
/// </summary>
public class ToNumericFunction : GenericFunctionBase {
    private readonly FunnyType _target;
    private readonly NumericNarrowMode _mode;

    public ToNumericFunction(string name, FunnyType target, NumericNarrowMode mode)
        : base(name, GenericConstrains.Numbers, target, FunnyType.Generic(0)) {
        _target = target;
        _mode = mode;
        ArgProperties = FunArgProperty.FromNames("x");
    }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) {
        var source = concreteTypes[0].BaseType;

        Func<object, object> convert;
        if (_target.BaseType == BaseFunnyType.Real)
            // toReal / toFloat64 — total for every numeric source (precision loss allowed).
            convert = context.RealTypeSelect(NumericNarrowing.ToRealAsDouble, NumericNarrowing.ToRealAsDecimal);
        else if (_target.BaseType == BaseFunnyType.Float32)
            // toFloat32 — total: IEEE rounding, overflow → ±Inf (no Wrap/Clamp variants exist).
            convert = NumericNarrowing.ToFloat32;
        else
            convert = source switch {
                BaseFunnyType.Real => context.RealTypeSelect(
                    NumericNarrowing.FromDouble(o => (double)o, _target.BaseType, _mode),
                    NumericNarrowing.FromDecimal(_target.BaseType, _mode)),
                BaseFunnyType.Float32 => NumericNarrowing.FromDouble(o => (float)o, _target.BaseType, _mode),
                _ => NumericNarrowing.FromInteger(source, _target.BaseType, _mode)
            };

        return new ConcreteImpl(convert) { Name = Name, ArgTypes = concreteTypes, ReturnType = _target };
    }

    private sealed class ConcreteImpl : FunctionWithSingleArg {
        private readonly Func<object, object> _convert;
        public ConcreteImpl(Func<object, object> convert) => _convert = convert;
        public override object Calc(object a) => _convert(a);
    }
}

/// <summary>Factory for all instances of the toXxx family (registration lists).</summary>
public static class ToNumericFunctions {
    private static readonly (string Name, FunnyType Target)[] IntegerTargets = {
        ("toByte", FunnyType.UInt8), ("toUint8", FunnyType.UInt8),
        ("toUint16", FunnyType.UInt16),
        ("toUint", FunnyType.UInt32), ("toUint32", FunnyType.UInt32),
        ("toUint64", FunnyType.UInt64),
        ("toInt8", FunnyType.Int8),
        ("toInt16", FunnyType.Int16),
        ("toInt", FunnyType.Int32), ("toInt32", FunnyType.Int32),
        ("toInt64", FunnyType.Int64),
    };

    /// <summary>
    /// Dialect-independent part: all integer targets in 3 modes, plus checked-only
    /// toReal (total — no Wrap/Clamp names are registered).
    /// </summary>
    internal static GenericFunctionBase[] CreateBaseFamily() {
        var result = new List<GenericFunctionBase>(IntegerTargets.Length * 3 + 1);
        foreach (var (name, target) in IntegerTargets)
        {
            result.Add(new ToNumericFunction(name, target, NumericNarrowMode.Checked));
            result.Add(new ToNumericFunction(name + "Wrap", target, NumericNarrowMode.Wrap));
            result.Add(new ToNumericFunction(name + "Clamp", target, NumericNarrowMode.Clamp));
        }

        result.Add(new ToNumericFunction("toReal", FunnyType.Real, NumericNarrowMode.Checked));
        return result.ToArray();
    }

    /// <summary>
    /// Float-family-dialect-only names: toFloat32 (total, IEEE overflow → ±Inf)
    /// and toFloat64 (alias of toReal). Plain mode only — no Wrap/Clamp.
    /// </summary>
    internal static GenericFunctionBase[] CreateFloatFamilyExtras() => new GenericFunctionBase[] {
        new ToNumericFunction("toFloat32", FunnyType.Float32, NumericNarrowMode.Checked),
        new ToNumericFunction("toFloat64", FunnyType.Real, NumericNarrowMode.Checked),
    };
}

/// <summary>
/// The single conversion table of the toXxx family:
/// (source BaseFunnyType × target × mode) → Func&lt;object,object&gt;.
///
/// Algebra: every integer value is canonicalized to its 64-bit two's-complement
/// image <c>(ulong bits, bool isNegative)</c> — this covers the whole
/// [-2^63 .. 2^64) domain, the union of int64 and uint64. Real sources are first
/// truncated toward zero (Checked/Wrap) or clamped (Clamp) and must land inside
/// that domain; then the same mode logic applies uniformly for every target.
/// </summary>
internal static class NumericNarrowing {
    // Exact double images of the 64-bit domain bounds: -2^63 and 2^64.
    private const double Int64MinAsDouble = -9.2233720368547758E18;
    private const double UInt64DomainEndAsDouble = 1.8446744073709552E19;

    private static readonly decimal Int64MinAsDecimal = long.MinValue;
    private static readonly decimal UInt64MaxAsDecimal = ulong.MaxValue;

    /// <summary>toReal in the double dialect — total, precision loss allowed (uint64 → 1.8446744073709552E19).</summary>
    internal static readonly Func<object, object> ToRealAsDouble = o => Convert.ToDouble(o);

    /// <summary>toReal in the decimal dialect — total for all numeric sources.</summary>
    internal static readonly Func<object, object> ToRealAsDecimal = o => Convert.ToDecimal(o);

    /// <summary>toFloat32 — IEEE rounding; double overflow → ±Infinity, never throws.</summary>
    internal static readonly Func<object, object> ToFloat32 = o => Convert.ToSingle(o);

    /// <summary>Description of an integer narrowing target (Min/Max in every needed numeric domain).</summary>
    private sealed class IntTarget {
        public readonly BaseFunnyType Type;
        public readonly bool IsSigned;
        public readonly long Min;
        public readonly ulong Max;
        /// <summary>Unchecked two's-complement cast of the low bits to the target CLR type (boxed).</summary>
        public readonly Func<ulong, object> FromBits;
        public readonly object MinBoxed;
        public readonly object MaxBoxed;
        public readonly double MinAsDouble;
        public readonly double MaxAsDouble;
        public readonly decimal MinAsDecimal;
        public readonly decimal MaxAsDecimal;

        public IntTarget(BaseFunnyType type, bool isSigned, long min, ulong max, Func<ulong, object> fromBits) {
            Type = type;
            IsSigned = isSigned;
            Min = min;
            Max = max;
            FromBits = fromBits;
            MinBoxed = fromBits(unchecked((ulong)min));
            MaxBoxed = fromBits(max);
            MinAsDouble = min;
            // (double)Max rounds UP for Int64/UInt64 (to 2^63 / 2^64): 'd >= MaxAsDouble' catches
            // the rounded images; every double strictly below is <= Max after truncation.
            MaxAsDouble = max;
            MinAsDecimal = min;
            MaxAsDecimal = max;
        }
    }

    private static readonly Dictionary<BaseFunnyType, IntTarget> IntTargets = new() {
        { BaseFunnyType.UInt8, new(BaseFunnyType.UInt8, false, 0, byte.MaxValue, bits => unchecked((byte)bits)) },
        { BaseFunnyType.UInt16, new(BaseFunnyType.UInt16, false, 0, ushort.MaxValue, bits => unchecked((ushort)bits)) },
        { BaseFunnyType.UInt32, new(BaseFunnyType.UInt32, false, 0, uint.MaxValue, bits => unchecked((uint)bits)) },
        { BaseFunnyType.UInt64, new(BaseFunnyType.UInt64, false, 0, ulong.MaxValue, bits => bits) },
        { BaseFunnyType.Int8, new(BaseFunnyType.Int8, true, sbyte.MinValue, (ulong)sbyte.MaxValue, bits => unchecked((sbyte)bits)) },
        { BaseFunnyType.Int16, new(BaseFunnyType.Int16, true, short.MinValue, (ulong)short.MaxValue, bits => unchecked((short)bits)) },
        { BaseFunnyType.Int32, new(BaseFunnyType.Int32, true, int.MinValue, int.MaxValue, bits => unchecked((int)bits)) },
        { BaseFunnyType.Int64, new(BaseFunnyType.Int64, true, long.MinValue, long.MaxValue, bits => unchecked((long)bits)) },
    };

    /// <summary>Integer source → integer target, all three modes.</summary>
    internal static Func<object, object> FromInteger(BaseFunnyType source, BaseFunnyType target, NumericNarrowMode mode) {
        Func<object, (ulong Bits, bool IsNegative)> read = source switch {
            BaseFunnyType.UInt8 => o => ((byte)o, false),
            BaseFunnyType.UInt16 => o => ((ushort)o, false),
            BaseFunnyType.UInt32 => o => ((uint)o, false),
            BaseFunnyType.UInt64 => o => ((ulong)o, false),
            BaseFunnyType.Int8 => o => { var v = (sbyte)o; return (unchecked((ulong)(long)v), v < 0); },
            BaseFunnyType.Int16 => o => { var v = (short)o; return (unchecked((ulong)(long)v), v < 0); },
            BaseFunnyType.Int32 => o => { var v = (int)o; return (unchecked((ulong)(long)v), v < 0); },
            BaseFunnyType.Int64 => o => { var v = (long)o; return (unchecked((ulong)v), v < 0); },
            _ => throw new NFunImpossibleException($"toXxx: unsupported source type {source}")
        };

        var t = IntTargets[target];
        return mode switch {
            NumericNarrowMode.Checked => o => {
                var (bits, isNegative) = read(o);
                return CheckedNarrow(o, bits, isNegative, t);
            },
            NumericNarrowMode.Wrap => o => t.FromBits(read(o).Bits),
            NumericNarrowMode.Clamp => o => {
                var (bits, isNegative) = read(o);
                return ClampNarrow(bits, isNegative, t);
            },
            _ => throw new NFunImpossibleException($"toXxx: unknown mode {mode}")
        };
    }

    /// <summary>
    /// Real (double-backed) source → integer target. Checked/Wrap: truncate toward
    /// zero first, then range-check / wrap; NaN/±Inf and truncated values outside
    /// [-2^63 .. 2^64) → error. Clamp: clamp before truncation; NaN → error,
    /// +Inf → Max, -Inf → Min. Float32 sources reuse this path via an exact
    /// float→double reader (same truncation semantics).
    /// </summary>
    internal static Func<object, object> FromDouble(Func<object, double> read, BaseFunnyType target, NumericNarrowMode mode) {
        var t = IntTargets[target];
        return mode switch {
            NumericNarrowMode.Checked or NumericNarrowMode.Wrap => o => {
                var d = read(o);
                if (double.IsNaN(d) || double.IsInfinity(d))
                    throw OutOfRange(o, t.Type);
                var truncated = Math.Truncate(d);
                if (truncated < Int64MinAsDouble || truncated >= UInt64DomainEndAsDouble)
                    throw OutOfRange(o, t.Type);
                var (bits, isNegative) = truncated < 0
                    ? (unchecked((ulong)(long)truncated), true)
                    : (unchecked((ulong)truncated), false);
                return mode == NumericNarrowMode.Checked
                    ? CheckedNarrow(o, bits, isNegative, t)
                    : t.FromBits(bits);
            },
            NumericNarrowMode.Clamp => o => {
                var d = read(o);
                if (double.IsNaN(d))
                    throw OutOfRange(o, t.Type);
                if (d <= t.MinAsDouble) return t.MinBoxed;
                if (d >= t.MaxAsDouble) return t.MaxBoxed;
                // Min < d < Max ⇒ trunc(d) lands inside [Min..Max] (truncation moves toward 0 ∈ [Min..Max])
                var truncated = Math.Truncate(d);
                var bits = truncated < 0 ? unchecked((ulong)(long)truncated) : unchecked((ulong)truncated);
                return t.FromBits(bits);
            },
            _ => throw new NFunImpossibleException($"toXxx: unknown mode {mode}")
        };
    }

    /// <summary>
    /// Real (decimal dialect) source → integer target: same truncation-order
    /// semantics as the double path; the NaN/Inf arm does not exist in the
    /// decimal domain.
    /// </summary>
    internal static Func<object, object> FromDecimal(BaseFunnyType target, NumericNarrowMode mode) {
        var t = IntTargets[target];
        return mode switch {
            NumericNarrowMode.Checked or NumericNarrowMode.Wrap => o => {
                var m = (decimal)o;
                var truncated = decimal.Truncate(m);
                // '>' not '>=': ulong.MaxValue is exact in decimal (the double path uses '>= 2^64' because ulong.MaxValue is not representable there)
                if (truncated < Int64MinAsDecimal || truncated > UInt64MaxAsDecimal)
                    throw OutOfRange(o, t.Type);
                var (bits, isNegative) = truncated < 0
                    ? (unchecked((ulong)(long)truncated), true)
                    : ((ulong)truncated, false);
                return mode == NumericNarrowMode.Checked
                    ? CheckedNarrow(o, bits, isNegative, t)
                    : t.FromBits(bits);
            },
            NumericNarrowMode.Clamp => o => {
                var m = (decimal)o;
                if (m <= t.MinAsDecimal) return t.MinBoxed;
                if (m >= t.MaxAsDecimal) return t.MaxBoxed;
                var truncated = decimal.Truncate(m);
                var bits = truncated < 0 ? unchecked((ulong)(long)truncated) : (ulong)truncated;
                return t.FromBits(bits);
            },
            _ => throw new NFunImpossibleException($"toXxx: unknown mode {mode}")
        };
    }

    // v ∈ [-2^63 .. 2^64) encoded as (bits, isNegative): v fits target T iff
    //   isNegative: T is signed ∧ (long)bits ≥ T.Min   (negative v ≤ T.Max holds trivially)
    //   otherwise:  bits ≤ T.Max
    private static object CheckedNarrow(object original, ulong bits, bool isNegative, IntTarget t) {
        var fits = isNegative
            ? t.IsSigned && unchecked((long)bits) >= t.Min
            : bits <= t.Max;
        if (!fits)
            throw OutOfRange(original, t.Type);
        return t.FromBits(bits);
    }

    private static object ClampNarrow(ulong bits, bool isNegative, IntTarget t) {
        if (isNegative)
            return unchecked((long)bits) < t.Min ? t.MinBoxed : t.FromBits(bits);
        return bits > t.Max ? t.MaxBoxed : t.FromBits(bits);
    }

    private static FunnyRuntimeException OutOfRange(object value, BaseFunnyType target) =>
        new($"Cannot convert {value} to type {target}");
}
