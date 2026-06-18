using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.ParseErrors;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions;

public class ConvertFunction : GenericFunctionBase {
    public ConvertFunction() : base(
        "convert",
        new[] { GenericConstrains.Any, GenericConstrains.Any },
        FunnyType.Generic(0), FunnyType.Generic(1)) {
        ArgProperties = FunArgProperty.FromNames("value");
    }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) {
        var from = concreteTypes[1];
        var to = concreteTypes[0];

        // Compile-time hard-reject diagnostics with directional hints. Matrix
        // ✗ cells stay rejected and `:T?` does NOT rescue them. The inner
        // helper (TryBuildConverterFn) is strictly Try-shaped; emitting the
        // hint at this layer keeps the runtime any-dispatcher path free of
        // parse-time exceptions. Unwrap opt for the check — `:T?` annotation
        // must not silently lift a hard reject.
        var fromCore = from.BaseType == BaseFunnyType.Optional
            ? from.OptionalTypeSpecification.ElementType : from;
        var toCore = to.BaseType == BaseFunnyType.Optional
            ? to.OptionalTypeSpecification.ElementType : to;
        ThrowIfHardReject(fromCore, toCore);

        // PRAGMATIC matrix §2.3: opt(A) source propagates through the wrapper.
        //   opt(A) → opt(B): apply class(A → B); none preserved
        //   opt(A) → B:      always 🪂 — throws on none, otherwise apply (A → B)
        //   opt(A) → text:   ✓ (toText handles none)
        //   opt(A) → any:    I
        //   opt(A) → byte[]: ✗ (no canonical byte repr for none) — falls through to FU887
        // Identity opt(A)→opt(A) is also handled here (no-op).
        if (from.BaseType == BaseFunnyType.Optional)
        {
            var fromUnwrapped = from.OptionalTypeSpecification.ElementType;

            // opt(A) → any | opt(A) → opt(A): identity. (text branch handled below.)
            if (to == FunnyType.Any || from == to)
                return new ConcreteConverter(o => o, from, to);

            // opt(A) → text: toText knows how to render none.
            if (to == FunnyType.Text)
                return new ConcreteConverter(o => new TextFunnyArray(TypeHelper.GetFunText(o)), from, to);

            // opt(A) → byte[] / other serializer targets: ✗ — no canonical byte
            // representation exists for `none`. The inner (A → byte[]) converter
            // would otherwise build successfully, masking the absence of a
            // morphism with a runtime-on-none throw. Reject at compile time.
            if (to.ArrayTypeSpecification?.FunnyType == FunnyType.UInt8 ||
                to.ArrayTypeSpecification?.FunnyType == FunnyType.Bool)
                throw Errors.ConvertNotSupported(from.ToString(), to.ToString(),
                    "Serialization of optional values is not defined: `none` has no canonical byte representation.");

            if (to.BaseType == BaseFunnyType.Optional)
            {
                // opt(A) → opt(B): apply (A → B) on the value side; preserve none.
                var toUnwrapped = to.OptionalTypeSpecification.ElementType;
                if (fromUnwrapped == toUnwrapped || toUnwrapped == FunnyType.Any)
                    return new ConcreteConverter(o => o, from, to);
                var innerFn = TryBuildConverterFn(fromUnwrapped, toUnwrapped, context);
                if (innerFn == null)
                    throw Errors.ConvertNotSupported(from.ToString(), to.ToString());
                // Inner failures (parse/overflow) still bubble for :opt(B) — the user
                // asked for opt(B) but the value branch can still throw. SoftFailureConverter
                // semantics apply only when the OUTER target is opt; here both sides are
                // opt so we wrap to rescue inner soft failures into `none` too — matches
                // the principle "the optional wrapper absorbs soft errors".
                // `none` at runtime is FunnyNone.Instance, not null.
                return new SoftFailureConverter(o => o is FunnyNone ? o : innerFn(o), from, to);
            }

            // opt(A) → B (non-opt): 🪂 — throws on none.
            {
                var innerFn = TryBuildConverterFn(fromUnwrapped, to, context);
                if (innerFn == null)
                    throw Errors.ConvertNotSupported(from.ToString(), to.ToString());
                return new ConcreteConverter(o => {
                    if (o is FunnyNone)
                        throw new FunnyRuntimeException(
                            $"Cannot convert `none` to non-optional `{to}`. Use `:{to}?` or unwrap with `!`.");
                    return innerFn(o);
                }, from, to);
            }
        }

        // PRAGMATIC matrix §1.5/§2.4: `any → T` (T ≠ any, ≠ text) is 🪂.
        // At compile time we don't know the runtime type of the `any` value, so we
        // build a dispatcher: at runtime, inspect the actual CLR type of the value,
        // map it to a FunnyType, and apply the regular (actualType → T) converter.
        // This mirrors all the normal (S → T) rules — including soft failures
        // (parse, overflow) and static-✗ cases (which become runtime errors here
        // since we couldn't check at compile time).
        //
        // The to == text branch is handled below by VarTypeConverter (toText is
        // total for any source). to == any is identity (already handled above for
        // from == to). All other targets need this dispatcher.
        if (from == FunnyType.Any && to != FunnyType.Text && to != FunnyType.Any)
        {
            // opt-target: wrap in SoftFailureConverter for `none` rescue.
            if (to.BaseType == BaseFunnyType.Optional)
            {
                var unwrapped = to.OptionalTypeSpecification.ElementType;
                return new SoftFailureConverter(BuildAnyDispatcher(unwrapped, context), from, to);
            }
            return new ConcreteConverter(BuildAnyDispatcher(to, context), from, to);
        }

        // PRAGMATIC matrix §3: when target is opt(U), the `?` is the user opt-in for
        // "try" semantics — soft-fallible (🪂) conversions return `none` on failure
        // instead of throwing. The underlying (from → U) converter does the work; we
        // wrap it in a try/catch that maps documented soft exceptions to `none`.
        // Static-impossible (✗) pairs stay rejected — the `?` doesn't rescue them.
        if (to.BaseType == BaseFunnyType.Optional)
        {
            var unwrapped = to.OptionalTypeSpecification.ElementType;
            // Identity / lift T → opt(T): no inner conversion needed.
            if (from == unwrapped || unwrapped == FunnyType.Any)
                return new ConcreteConverter(o => o, from, to);
            // Build the inner (from → U) converter. If `unwrapped` is opt itself
            // (opt(opt(T)) request), fall through to the normal path which will
            // either find an identity match or throw FU887.
            var innerFn = TryBuildConverterFn(from, unwrapped, context);
            if (innerFn == null)
                throw Errors.ConvertNotSupported(from.ToString(), to.ToString());
            return new SoftFailureConverter(innerFn, from, to);
        }

        // Non-opt target: standard path. Build a Func<object,object> via the
        // shared helper and wrap in ConcreteConverter (which catches and rethrows
        // as FunnyRuntimeException — Phase A unchanged behavior).
        var fn = TryBuildConverterFn(from, to, context);
        if (fn == null)
            throw Errors.ConvertNotSupported(from.ToString(), to.ToString());
        return new ConcreteConverter(fn, from, to);
    }

    /// <summary>
    /// Hard-reject pairs per the PRAGMATIC matrix. Compile-time hints emitted
    /// from <see cref="ThrowIfHardReject"/>; runtime any-dispatcher path just
    /// sees no morphism.
    /// </summary>
    private static bool IsHardReject(FunnyType from, FunnyType to) =>
        // §1.4: ip → i32 ✗ — would yield negative for high IPs.
        (from == FunnyType.Ip && to == FunnyType.Int32) ||
        // §1.3: real → char ✗ — a real is not a codepoint.
        (from == FunnyType.Real && to == FunnyType.Char);

    /// <summary>
    /// Compile-time hard-reject diagnostic with a directional hint. Called
    /// from <see cref="CreateConcrete"/> before <see cref="TryBuildConverterFn"/>
    /// so the user gets `:uint`/`:long`/intermediate-step suggestions instead
    /// of a generic FU887.
    /// </summary>
    private static void ThrowIfHardReject(FunnyType from, FunnyType to) {
        if (from == FunnyType.Ip && to == FunnyType.Int32)
            throw Errors.ConvertNotSupported(
                from.ToString(), to.ToString(),
                "Use :uint (natural) or :long (widening) instead — :int would lose the non-negative property for high IPs.");
        if (from == FunnyType.Real && to == FunnyType.Char)
            throw Errors.ConvertNotSupported(
                from.ToString(), to.ToString(),
                "A real value is not a codepoint. Convert via an integer first, e.g. `convert(convert(x):int):char`.");
    }

    /// <summary>
    /// Resolves the raw conversion delegate for a (from → to) pair, or null if no
    /// morphism exists. Both `to` and `from` are non-opt at this point — opt handling
    /// is done by the caller. Strictly Try-shaped: never throws.
    /// </summary>
    private static Func<object, object> TryBuildConverterFn(
        FunnyType from, FunnyType to, IFunctionSelectorContext context) {

        // Hard rejects (ip→i32, real→char) handled by ThrowIfHardReject at the
        // compile-time call site; here they fall through to `return null` like
        // any other "no morphism" pair.
        if (IsHardReject(from, to))
            return null;

        if (to == FunnyType.Any || from == to)
            return o => o;

        // WORKAROUND: scoped reject for one shape — `if specificType then
        // specialBehavior`-style. The root cause is in `VarTypeConverter`:
        // its `T → Optional<U>` implicit lift returns `NoConvertion` instead
        // of `null` when no `T → U` morphism exists (a deliberate trust hack
        // for struct width-subtyping that `?.` rescues at runtime). When that
        // lie propagates into the element-conversion path of List→Array
        // etc., we get a typed-lie at runtime: `convert(['abc']):int?[]` ⇒
        // TextFunnyArray under `array<Int32?>`. Composite-element parsing
        // isn't supported by `convert()` yet, so the user-visible answer is
        // FU887; this block makes sure that's what fires.
        //
        // Proper fix: make the `T → Optional<U>` lift honest (return null on
        // missing `T → U`) and add a separate explicit width-subtyping path
        // for the struct case the lift was trying to keep working. Tracked
        // in CLAUDE.md "Current workarounds" #9. Bug #34.
        if (TryGetCollectionElement(from, out var fromElem)
            && TryGetCollectionElement(to, out var toElem)
            && toElem.BaseType == BaseFunnyType.Optional
            && fromElem.BaseType != BaseFunnyType.Optional)
        {
            var unwrapped = toElem.OptionalTypeSpecification.ElementType;
            if (fromElem != unwrapped
                && unwrapped != FunnyType.Any
                && VarTypeConverter.GetConverterOrNull(
                    context.Converter.TypeBehaviour, fromElem, unwrapped) == null)
                return null;
        }

        if (to == FunnyType.Text)
        {
            if (from.ArrayTypeSpecification?.FunnyType == FunnyType.UInt8)
                return o => {
                    var array = (IFunnyArray)o;
                    return new TextFunnyArray(Encoding.Unicode.GetString(array.ToArrayOf<byte>()));
                };
            return o => new TextFunnyArray(TypeHelper.GetFunText(o));
        }

        if (from == FunnyType.Text && to.ArrayTypeSpecification?.FunnyType == FunnyType.UInt8)
            return o => new ImmutableFunnyArray(Encoding.Unicode.GetBytes(((IFunnyArray)o).ToText()));

        // C-style bool ↔ numeric (PRAGMATIC matrix §1.2).
        var boolNumeric = CreateBoolNumericConverterOrNull(from, to);
        if (boolNumeric != null)
            return boolNumeric;

        var converter = VarTypeConverter.GetConverterOrNull(context.Converter.TypeBehaviour, from, to);
        if (converter != null)
            return converter;

        if (to == FunnyType.Ip)
        {
            var ipConverterOrNull = CreateToIpConverterOrNull(from);
            if (ipConverterOrNull != null)
                return ipConverterOrNull;
        }

        if (from == FunnyType.Char)
        {
            var charConverterOrNull = CreateFromCharConverterOrNull(to);
            if (charConverterOrNull != null)
                return charConverterOrNull;
        }

        if (from == FunnyType.Ip)
        {
            var ipConverterOrNull = CreateFromIpConverterOrNull(to);
            if (ipConverterOrNull != null)
                return ipConverterOrNull;
        }

        if (to.ArrayTypeSpecification?.FunnyType == FunnyType.UInt8)
        {
            var serializer = CreateSerializerOrNull(from);
            if (serializer != null)
                return serializer;
        }
        else if (to.ArrayTypeSpecification?.FunnyType == FunnyType.Bool)
        {
            var serializer = CreateBinarizerOrNull(from);
            if (serializer != null)
                return serializer;
        }

        if (from.ArrayTypeSpecification?.FunnyType == FunnyType.UInt8)
        {
            var deserializer = CreateDeserializerOrNull(to);
            if (deserializer != null)
                return deserializer;
        }
        else if (from.IsText)
        {
            var parser = CreateParserOrNull(to);
            if (parser != null)
                return parser;
        }

        return null;
    }

    /// <summary>
    /// Extracts the element type from any single-arg collection (ee-mode T[],
    /// lang-mode list / array / fixedArray). Returns false for non-collection
    /// or two-arg shapes. Used by the collection→collection unsoundness guard.
    /// </summary>
    private static bool TryGetCollectionElement(FunnyType collection, out FunnyType element) {
        switch (collection.BaseType) {
            case BaseFunnyType.ArrayOf:
                element = collection.ArrayTypeSpecification.FunnyType;
                return true;
            case BaseFunnyType.List:
                element = collection.ListTypeSpecification.FunnyType;
                return true;
            case BaseFunnyType.MutableArray:
                element = collection.MutableArrayTypeSpecification.FunnyType;
                return true;
            case BaseFunnyType.FixedArray:
                element = collection.FixedArrayTypeSpecification.FunnyType;
                return true;
            default:
                element = default;
                return false;
        }
    }

    /// <summary>
    /// Builds a runtime dispatcher for `convert(x:any):T`. At each call, looks at
    /// the actual CLR type of the value, maps it to a FunnyType, and applies the
    /// regular (actualSourceType → T) converter resolved via TryBuildConverterFn.
    /// On no-morphism, throws ArgumentException — wrapped to FunnyRuntimeException
    /// by ConcreteConverter for `:T` targets, caught by SoftFailureConverter for
    /// `:T?` targets and yielding `none`.
    /// </summary>
    private static Func<object, object> BuildAnyDispatcher(FunnyType to, IFunctionSelectorContext context) =>
        value => {
            if (value is FunnyNone)
                throw new ArgumentException(
                    $"Cannot convert `none` held in any to non-optional `{to}`");
            var actualFrom = InferFunnyTypeFromValueOrEmpty(value);
            if (actualFrom.Equals(FunnyType.Empty))
                throw new ArgumentException(
                    $"any holds {value?.GetType().Name ?? "null"}, no convert to {to} exists");
            // Same identity / lift fast-paths as the main CreateConcrete logic.
            if (actualFrom == to)
                return value;
            var fn = TryBuildConverterFn(actualFrom, to, context);
            if (fn == null)
                throw new ArgumentException(
                    $"any holds {actualFrom}, no convert to {to} exists");
            return fn(value);
        };

    /// <summary>
    /// Infers the FunnyType of a runtime value based on its CLR type. Used by the
    /// any-dispatcher to delegate to the normal converter pipeline. Returns
    /// FunnyType.Empty when the CLR type isn't representable as a primitive
    /// morphism source (struct, custom object, …) so the caller can reject cleanly.
    /// </summary>
    private static FunnyType InferFunnyTypeFromValueOrEmpty(object value) => value switch {
        byte   => FunnyType.UInt8,
        ushort => FunnyType.UInt16,
        uint   => FunnyType.UInt32,
        ulong  => FunnyType.UInt64,
        short  => FunnyType.Int16,
        int    => FunnyType.Int32,
        long   => FunnyType.Int64,
        double or decimal => FunnyType.Real,
        bool   => FunnyType.Bool,
        char   => FunnyType.Char,
        System.Net.IPAddress => FunnyType.Ip,
        Runtime.Arrays.TextFunnyArray  => FunnyType.Text,
        Runtime.Arrays.IFunnyArray arr => FunnyType.ArrayOf(arr.ElementType),
        // Structs, custom CLR objects, and anything else not in the primitive
        // matrix have no algebraic morphism to a primitive target. Signal Empty
        // so the caller throws a clear "no convert" error.
        _      => FunnyType.Empty,
    };

    /// <summary>
    /// C-style bool ↔ numeric morphism per PRAGMATIC matrix §1.2.
    /// Total in both directions; returns null if (from, to) isn't a bool↔numeric pair.
    /// </summary>
    private static Func<object, object> CreateBoolNumericConverterOrNull(FunnyType from, FunnyType to) {
        // bool → numeric: false → 0, true → 1.
        if (from == FunnyType.Bool) {
            return to.BaseType switch {
                BaseFunnyType.UInt8  => o => (byte)((bool)o ? 1 : 0),
                BaseFunnyType.UInt16 => o => (ushort)((bool)o ? 1 : 0),
                BaseFunnyType.UInt32 => o => (uint)((bool)o ? 1u : 0u),
                BaseFunnyType.UInt64 => o => (ulong)((bool)o ? 1UL : 0UL),
                BaseFunnyType.Int16  => o => (short)((bool)o ? 1 : 0),
                BaseFunnyType.Int32  => o => (bool)o ? 1 : 0,
                BaseFunnyType.Int64  => o => (long)((bool)o ? 1L : 0L),
                BaseFunnyType.Real   => o => (bool)o ? 1.0 : 0.0,
                _ => null
            };
        }
        // numeric → bool: 0 → false, non-zero → true.
        if (to == FunnyType.Bool) {
            return from.BaseType switch {
                BaseFunnyType.UInt8  => o => (byte)o != 0,
                BaseFunnyType.UInt16 => o => (ushort)o != 0,
                BaseFunnyType.UInt32 => o => (uint)o != 0,
                BaseFunnyType.UInt64 => o => (ulong)o != 0,
                BaseFunnyType.Int16  => o => (short)o != 0,
                BaseFunnyType.Int32  => o => (int)o != 0,
                BaseFunnyType.Int64  => o => (long)o != 0,
                // real → bool: 0.0/-0.0/NaN → false, finite non-zero / ±Inf → true.
                // `d != 0.0` already yields false for both ±0.0 (IEEE-754 equality).
                // NaN compares unequal to everything, so `d != 0.0` is true for NaN — must
                // exclude NaN explicitly. ±Inf is finite-or-not-NaN, so passes through.
                BaseFunnyType.Real => o => {
                    if (o is decimal m) return m != 0m;
                    var d = (double)o;
                    return !double.IsNaN(d) && d != 0.0;
                },
                _ => null
            };
        }
        return null;
    }

    private static byte[] BytesBE(uint v) =>
        new byte[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };

    private static Func<object, object> CreateToIpConverterOrNull(FunnyType from) =>
        from.BaseType switch {
            // Per PRAGMATIC matrix §1.4: i32/i64/u64 → ip are all 🪂 (must fit
            // [0, 2^32-1]). Throws OverflowException on bad value, which the runtime
            // wrapper layers turn into a FunnyRuntimeException for `:ip` and `none`
            // for `:ip?`. u32 → ip is total ✓ (every u32 value is a valid IPv4).
            //
            // Network byte order: the numeric value's most-significant octet is the
            // first IP octet. `IPAddress(long)` is host-endian and would invert this
            // — see the matching ip → uXX path in `CreateFromIpConverterOrNull`.
            BaseFunnyType.Int32 => o => {
                var v = (int)o;
                if (v < 0)
                    throw new OverflowException($"Cannot convert negative int {v} to Ip: IPv4 requires non-negative value in [0, 2^32-1]");
                return new IPAddress(BytesBE((uint)v));
            },
            BaseFunnyType.UInt32 => o => new IPAddress(BytesBE((UInt32)o)),
            BaseFunnyType.Int64 => o => {
                var v = (long)o;
                if (v < 0L || v > uint.MaxValue)
                    throw new OverflowException($"Cannot convert int64 {v} to Ip: IPv4 requires value in [0, 2^32-1]");
                return new IPAddress(BytesBE((uint)v));
            },
            BaseFunnyType.UInt64 => o => {
                var v = (ulong)o;
                if (v > uint.MaxValue)
                    throw new OverflowException($"Cannot convert uint64 {v} to Ip: IPv4 requires value in [0, 2^32-1]");
                return new IPAddress(BytesBE((uint)v));
            },
            BaseFunnyType.ArrayOf => from.ArrayTypeSpecification.FunnyType.BaseType switch {
                BaseFunnyType.Char => o => IPAddress.Parse(((IFunnyArray)o).ToText()),
                BaseFunnyType.UInt8 => o => new IPAddress(((IFunnyArray)o).As<byte>().ToArray()),
                BaseFunnyType.UInt16 => o => {
                    var a = ((IFunnyArray)o).GetElementOrNull(0);
                    var b = ((IFunnyArray)o).GetElementOrNull(1);
                    var c = ((IFunnyArray)o).GetElementOrNull(2);
                    var d = ((IFunnyArray)o).GetElementOrNull(3);
                    return new IPAddress(new byte[] {
                        (byte)(UInt16)a, (byte)(UInt16)b, (byte)(UInt16)c, (byte)(UInt16)d
                    });
                },
                BaseFunnyType.UInt32 => o => {
                    var a = ((IFunnyArray)o).GetElementOrNull(0);
                    var b = ((IFunnyArray)o).GetElementOrNull(1);
                    var c = ((IFunnyArray)o).GetElementOrNull(2);
                    var d = ((IFunnyArray)o).GetElementOrNull(3);
                    return new IPAddress(new byte[] {
                        (byte)(UInt32)a, (byte)(UInt32)b, (byte)(UInt32)c, (byte)(UInt32)d
                    });
                },
                BaseFunnyType.UInt64 => o => {
                    var a = ((IFunnyArray)o).GetElementOrNull(0);
                    var b = ((IFunnyArray)o).GetElementOrNull(1);
                    var c = ((IFunnyArray)o).GetElementOrNull(2);
                    var d = ((IFunnyArray)o).GetElementOrNull(3);
                    return new IPAddress(new byte[] {
                        (byte)(UInt64)a, (byte)(UInt64)b, (byte)(UInt64)c, (byte)(UInt64)d
                    });
                },
                BaseFunnyType.Int16 => o => {
                    var a = ((IFunnyArray)o).GetElementOrNull(0);
                    var b = ((IFunnyArray)o).GetElementOrNull(1);
                    var c = ((IFunnyArray)o).GetElementOrNull(2);
                    var d = ((IFunnyArray)o).GetElementOrNull(3);
                    return new IPAddress(new byte[] { (byte)(Int16)a, (byte)(Int16)b, (byte)(Int16)c, (byte)(Int16)d });
                },
                BaseFunnyType.Int32 => o => {
                    var a = ((IFunnyArray)o).GetElementOrNull(0);
                    var b = ((IFunnyArray)o).GetElementOrNull(1);
                    var c = ((IFunnyArray)o).GetElementOrNull(2);
                    var d = ((IFunnyArray)o).GetElementOrNull(3);
                    return new IPAddress(new byte[] { (byte)(Int32)a, (byte)(Int32)b, (byte)(Int32)c, (byte)(Int32)d });
                },
                BaseFunnyType.Int64 => o => {
                    var a = ((IFunnyArray)o).GetElementOrNull(0);
                    var b = ((IFunnyArray)o).GetElementOrNull(1);
                    var c = ((IFunnyArray)o).GetElementOrNull(2);
                    var d = ((IFunnyArray)o).GetElementOrNull(3);
                    return new IPAddress(new byte[] { (byte)(Int64)a, (byte)(Int64)b, (byte)(Int64)c, (byte)(Int64)d });
                },
                _ => null
            },
            _ => null
        };

    private static Func<object, object> CreateFromIpConverterOrNull(FunnyType to) {
        static byte[] ToBytes(object arg) => ((IPAddress)arg).GetAddressBytes();
        // ip → uXX uses network (big-endian) byte order to preserve the natural
        // numeric identity: 127.0.0.1 → 0x7F000001 = 2130706433. BitConverter
        // is host-endian (typically little-endian) and would yield 0x0100007F
        // — the bytes-of-an-IP table in Functions.md §Serialization documents
        // big-endian explicitly, and Bug hunt #9 verified the previous
        // implementation contradicted it.
        static uint ToUInt32BE(object arg) {
            var b = ToBytes(arg);
            return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        }

        return to.BaseType switch {
            // BaseFunnyType.Int32 deliberately omitted — ip→i32 is ✗ per PRAGMATIC
            // matrix §1.4 (would produce negative for high IPs, losing the natural
            // non-negative identity of an IPv4). The compile-time reject is emitted
            // by the explicit pre-check in TryBuildConverterFn so users get a
            // pointed hint about :uint / :long alternatives instead of a generic
            // FU887 from this method returning null.
            BaseFunnyType.UInt32 => o => ToUInt32BE(o),
            BaseFunnyType.Int64 => o => (long)ToUInt32BE(o),
            BaseFunnyType.UInt64 => o => (ulong)ToUInt32BE(o),
            BaseFunnyType.ArrayOf => to.ArrayTypeSpecification.FunnyType.BaseType switch {
                BaseFunnyType.UInt8 => o => new ImmutableFunnyArray(ToBytes(o)),
                BaseFunnyType.UInt16 => o => new ImmutableFunnyArray(ToBytes(o).SelectToArray(x => (UInt16)x)),
                BaseFunnyType.UInt32 => o => new ImmutableFunnyArray(ToBytes(o).SelectToArray(x => (UInt32)x)),
                BaseFunnyType.UInt64 => o => new ImmutableFunnyArray(ToBytes(o).SelectToArray(x => (UInt64)x)),
                BaseFunnyType.Int16 => o => new ImmutableFunnyArray(ToBytes(o).SelectToArray(x => (Int16)x)),
                BaseFunnyType.Int32 => o => new ImmutableFunnyArray(ToBytes(o).SelectToArray(x => (Int32)x)),
                BaseFunnyType.Int64 => o => new ImmutableFunnyArray(ToBytes(o).SelectToArray(x => (Int64)x)),
                BaseFunnyType.Bool => o => ToBoolFunnyArray(ToBytes(o)),
                _ => null
            },
            _ => null
        };
    }

    private static Func<object, object> CreateFromCharConverterOrNull(FunnyType to) {
        int GetUnicodeBytes(object o1, out byte[] bytes) {
            var chars = new[] { (char)o1 };
            bytes = new byte[8];
            return Encoding.Unicode.GetBytes(chars, 0, 1, bytes, 0);
        }

        return to.BaseType switch {
            BaseFunnyType.UInt8 => o => Convert.ToByte((char)o),
            BaseFunnyType.UInt16 => o => GetUnicodeBytes(o, out var bytes) > 2
                ? throw new OverflowException($"Cannot convert char value '{o}' to unt16")
                : BitConverter.ToUInt16(bytes, 0),
            BaseFunnyType.Int16 => o => GetUnicodeBytes(o, out var bytes) > 2
                ? throw new OverflowException($"Cannot convert char value '{o}' to int16")
                : BitConverter.ToInt16(bytes, 0),
            BaseFunnyType.UInt32 => o => GetUnicodeBytes(o, out var bytes) > 4
                ? throw new OverflowException($"Cannot convert char value '{o}' to unt32")
                : BitConverter.ToUInt32(bytes, 0),
            BaseFunnyType.Int32 => o => GetUnicodeBytes(o, out var bytes) > 4
                ? throw new OverflowException($"Cannot convert char value '{o}' to int32")
                : BitConverter.ToInt32(bytes, 0),
            BaseFunnyType.UInt64 => o => {
                GetUnicodeBytes(o, out var bytes);
                return BitConverter.ToUInt64(bytes, 0);
            },
            BaseFunnyType.Int64 => o => {
                GetUnicodeBytes(o, out var bytes);
                return BitConverter.ToInt64(bytes, 0);
            },
            _ => null
        };
    }

    private static Func<object, object> CreateBinarizerOrNull(FunnyType fromType) =>
        fromType.BaseType switch {
            BaseFunnyType.Char => o => ToBoolFunnyArray(BitConverter.GetBytes((char)o)),
            BaseFunnyType.Bool => o => new ImmutableFunnyArray(new[] { (bool)o }),
            BaseFunnyType.UInt8 => o => ToBoolFunnyArray(new[] { (byte)o }),
            BaseFunnyType.UInt16 => o => ToBoolFunnyArray(BitConverter.GetBytes((ushort)o)),
            BaseFunnyType.UInt32 => o => ToBoolFunnyArray(BitConverter.GetBytes((uint)o)),
            BaseFunnyType.UInt64 => o => ToBoolFunnyArray(BitConverter.GetBytes((long)o)),
            BaseFunnyType.Int16 => o => ToBoolFunnyArray(BitConverter.GetBytes((short)o)),
            BaseFunnyType.Int32 => o => ToBoolFunnyArray(BitConverter.GetBytes((int)o)),
            BaseFunnyType.Int64 => o => ToBoolFunnyArray(BitConverter.GetBytes((long)o)),
            BaseFunnyType.Real => o => ToBoolFunnyArray(BitConverter.GetBytes((double)o)),
            _ when fromType.IsText => o => ToBoolFunnyArray(Encoding.Unicode.GetBytes(((IFunnyArray)o).ToText())),
            _ => null
        };

    private static ImmutableFunnyArray ToBoolFunnyArray(byte[] array) {
        var bitArray = new BitArray(array);
        var arr = new bool[bitArray.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = bitArray[i];
        }

        return new ImmutableFunnyArray(arr);
    }


    private static Func<object, object> CreateSerializerOrNull(FunnyType from) =>
        from.BaseType switch {
            BaseFunnyType.Bool => o => new ImmutableFunnyArray(new[] { (byte)((bool)o ? 1 : 0) }),
            BaseFunnyType.UInt8 => o => new ImmutableFunnyArray(new[] { (byte)o }),
            BaseFunnyType.Char => o => {
                var chars = new[] { (char)o };
                var bytes = Encoding.Unicode.GetBytes(chars);
                if (bytes.Length == 2 && bytes[1] == 0)
                    bytes = new[] { bytes[0] };
                return new ImmutableFunnyArray(bytes);
            },
            BaseFunnyType.UInt16 => o => new ImmutableFunnyArray(BitConverter.GetBytes((ushort)o)),
            BaseFunnyType.UInt32 => o => new ImmutableFunnyArray(BitConverter.GetBytes((uint)o)),
            BaseFunnyType.UInt64 => o => new ImmutableFunnyArray(BitConverter.GetBytes((long)o)),
            BaseFunnyType.Int16 => o => new ImmutableFunnyArray(BitConverter.GetBytes((short)o)),
            BaseFunnyType.Int32 => o => new ImmutableFunnyArray(BitConverter.GetBytes((int)o)),
            BaseFunnyType.Int64 => o => new ImmutableFunnyArray(BitConverter.GetBytes((long)o)),
            BaseFunnyType.Real => o => new ImmutableFunnyArray(BitConverter.GetBytes((double)o)),
            _ => from.IsText ? o => new ImmutableFunnyArray(Encoding.Unicode.GetBytes(((IFunnyArray)o).ToText())) : null
        };

    private static Func<object, object> CreateParserOrNull(FunnyType to) =>
        to.BaseType switch {
            BaseFunnyType.Bool => o => {
                var str = ((IFunnyArray)o).ToText();
                if (string.Equals(str, "true", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(str, "1", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(str, "false", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(str, "0", StringComparison.Ordinal)) return false;
                throw new FormatException($"Cannot convert '{str}' to bool");
            },
            // PRAGMATIC matrix §1.6: text → char is 🪂. Accept only single-character
            // text; otherwise throw ArgumentException — SoftFailureConverter rescues
            // it to `none` for `:char?` and ConcreteConverter rewraps as
            // FunnyRuntimeException for `:char`.
            BaseFunnyType.Char => o => {
                var str = ((IFunnyArray)o).ToText();
                if (str.Length == 1) return str[0];
                throw new ArgumentException(
                    $"Cannot convert text '{str}' to char: length must be exactly 1, got {str.Length}");
            },
            BaseFunnyType.UInt8 =>  o => byte.Parse(((IFunnyArray)o).ToText()),
            BaseFunnyType.UInt16 => o => ushort.Parse(((IFunnyArray)o).ToText()),
            BaseFunnyType.UInt32 => o => UInt32.Parse(((IFunnyArray)o).ToText()),
            BaseFunnyType.UInt64 => o => UInt64.Parse(((IFunnyArray)o).ToText()),
            BaseFunnyType.Int16 =>  o => Int16.Parse(((IFunnyArray)o).ToText()),
            BaseFunnyType.Int32 =>  o => Int32.Parse(((IFunnyArray)o).ToText()),
            BaseFunnyType.Int64 =>  o => Int64.Parse(((IFunnyArray)o).ToText()),
            BaseFunnyType.Real =>   o => double.Parse(((IFunnyArray)o).ToText(), CultureInfo.InvariantCulture),
            _ => null
        };

    private static Func<object, object> CreateDeserializerOrNull(FunnyType to)
        => to.BaseType switch {
            BaseFunnyType.Char => o => {
                var bytes = ((IFunnyArray)o).ToArrayOf<byte>();
                if (bytes.Length == 1)
                    return Encoding.ASCII.GetChars(bytes)[0];
                if (bytes.Length == 2)
                    return Encoding.Unicode.GetChars(bytes)[0];
                throw new ArgumentException(
                    $"byte[] → char requires exactly 1 (ASCII) or 2 (UTF-16) bytes; got {bytes.Length}");
            },
            // Strict-length byte[] deserialization per PRAGMATIC matrix §1.6.
            // Each numeric target requires exactly N bytes for its width.
            // Throws ArgumentException on length mismatch — caught by
            // SoftFailureConverter for `:T?` rescue (returns `none`).
            // Previous behavior silently zero-padded short arrays via
            // AsByteArray, masking the strictness the spec requires.
            BaseFunnyType.Bool => o => StrictBytes(o, 1)[0] == 1,
            BaseFunnyType.UInt8 => o => StrictBytes(o, 1)[0],
            BaseFunnyType.UInt16 => o => BitConverter.ToUInt16(StrictBytes(o, 2), 0),
            BaseFunnyType.UInt32 => o => BitConverter.ToUInt32(StrictBytes(o, 4), 0),
            BaseFunnyType.UInt64 => o => BitConverter.ToUInt64(StrictBytes(o, 8), 0),
            BaseFunnyType.Int16 => o => BitConverter.ToInt16(StrictBytes(o, 2), 0),
            BaseFunnyType.Int32 => o => BitConverter.ToInt32(StrictBytes(o, 4), 0),
            BaseFunnyType.Int64 => o => BitConverter.ToInt64(StrictBytes(o, 8), 0),
            BaseFunnyType.Real => o => BitConverter.ToDouble(StrictBytes(o, 8), 0),
            _ => to.IsText ? o => new ImmutableFunnyArray(Encoding.Unicode.GetBytes(((IFunnyArray)o).ToText())) : null
        };

    /// <summary>
    /// Validates that the input byte array has exactly the expected width for
    /// the target primitive type. Throws ArgumentException on mismatch — soft
    /// exception, caught by SoftFailureConverter for `:T?` to yield `none`.
    /// </summary>
    private static byte[] StrictBytes(object array, int expectedWidth) {
        var val = (IFunnyArray)array;
        if (val.Count != expectedWidth)
            throw new ArgumentException(
                $"byte[] deserialization requires exactly {expectedWidth} bytes; got {val.Count}");
        return val.SelectToArray(expectedWidth, Convert.ToByte);
    }

    private class ConcreteConverter : FunctionWithSingleArg {
        private readonly Func<object, object> _converter;

        public ConcreteConverter(Func<object, object> converter, FunnyType from, FunnyType to) : base(
            "convert", to,
            from) =>
            _converter = converter;

        public override object Calc(object a) {
            try
            {
                return _converter(a);
            }
            catch (Exception e)
            {
                throw new FunnyRuntimeException($"Cannot convert {a} to type {ReturnType}", e);
            }
        }
    }

    /// <summary>
    /// PRAGMATIC matrix §3: when the user writes `:T?`, soft runtime failures
    /// (parse, overflow, shape mismatch) translate to `none` instead of throwing.
    /// Hard failures (OOM, stack overflow, internal NFun bugs surfacing as
    /// FunnyRuntimeException) still propagate — those are not recoverable.
    ///
    /// "Soft" exceptions caught here (closed set — all converter throw-sites
    /// route soft failures through one of these typed exceptions at the source):
    ///   • FormatException — text parsing failures (int.Parse, IPAddress.Parse, …)
    ///   • OverflowException — numeric narrowing, char codepoint overflow
    ///   • ArgumentException / ArgumentOutOfRangeException — shape mismatch
    ///     (wrong-length byte[], char-length, any-dispatcher no-morphism, …)
    /// </summary>
    private class SoftFailureConverter : FunctionWithSingleArg {
        private readonly Func<object, object> _converter;

        public SoftFailureConverter(Func<object, object> converter, FunnyType from, FunnyType optTo) : base(
            "convert", optTo,
            from) =>
            _converter = converter;

        public override object Calc(object a) {
            try
            {
                return _converter(a);
            }
            // Return FunnyNone.Instance (the runtime `none` value), NOT CLR null —
            // composite opt targets (T[]?, opt struct fields) read through deref
            // paths that crash with NRE on null. (MR8Bug1.)
            catch (FormatException) { return FunnyNone.Instance; }
            catch (OverflowException) { return FunnyNone.Instance; }
            catch (ArgumentException) { return FunnyNone.Instance; }
        }
    }

}
