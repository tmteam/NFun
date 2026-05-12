using System;
using System.Runtime.CompilerServices;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.Functions;

public class NotEqualFunction : GenericFunctionWithTwoArguments {
    public NotEqualFunction() : base(
        CoreFunNames.NotEqual, FunnyType.Bool, FunnyType.Generic(0),
        FunnyType.Generic(0)) { }

    protected override object Calc(object a, object b) => !TypeHelper.AreEqual(a, b);

    // Override the runtime concrete to take `Any` on both sides, regardless of how TIC
    // unifies T. Without this, TIC may narrow T to a type that triggers an implicit
    // ToText (or similar identity-changing) coercion of one operand and produce a
    // silent wrong answer — see EqualFunction below for the full reasoning (Bug CC).
    public override IConcreteFunction CreateConcrete(FunnyType[] _, IFunctionSelectorContext context) =>
        EqualFunction.AnyAnyConcrete(CoreFunNames.NotEqual, (a, b) => !TypeHelper.AreEqual(a, b));
}

public class EqualFunction : GenericFunctionWithTwoArguments {
    public EqualFunction() : base(CoreFunNames.Equal, FunnyType.Bool, FunnyType.Generic(0), FunnyType.Generic(0)) { }

    protected override object Calc(object a, object b)
        => TypeHelper.AreEqual(a, b);

    // TIC infers `==` as `(T,T)->Bool` and unifies T across both operands. When the
    // operands are in different families (e.g. Char vs Char[]) TIC narrows T to one
    // side's type and CreateWithConvertionOrThrow inserts a cast — for `to.IsText`
    // that cast is `ToText`, which wraps a Char in a 1-char text and makes the
    // post-cast equality silently true. Equality has no business applying
    // identity-changing coercions; ignore the inferred T and concretize as
    // `(Any,Any)->Bool`. Numeric promotion (`1 == 1.0`) is handled inside
    // TypeHelper.AreEqual via cross-type double comparison. Array equality is also
    // structural in AreEquivalent. (Bug CC.)
    public override IConcreteFunction CreateConcrete(FunnyType[] _, IFunctionSelectorContext context) =>
        AnyAnyConcrete(CoreFunNames.Equal, TypeHelper.AreEqual);

    internal static IConcreteFunction AnyAnyConcrete(string name, Func<object, object, bool> calc) =>
        new AnyAnyEqualityFunction(name, calc);

    private sealed class AnyAnyEqualityFunction : FunctionWithTwoArgs {
        private readonly Func<object, object, bool> _calc;
        public AnyAnyEqualityFunction(string name, Func<object, object, bool> calc)
            : base(name, FunnyType.Bool, FunnyType.Any, FunnyType.Any) => _calc = calc;
        public override object Calc(object a, object b) => _calc(a, b);
    }
}

public class MoreFunction : GenericFunctionWithTwoArguments {
    public MoreFunction() : base(
        CoreFunNames.More, GenericConstrains.Comparable, FunnyType.Bool, FunnyType.Generic(0),
        FunnyType.Generic(0)) { }

    protected override object Calc(object a, object b) {
        // IEEE 754: NaN is unordered — any comparison with NaN returns false
        if (IEEE754Guard.EitherIsNaN(a, b)) return false;
        return ((IComparable)a).CompareTo(b) > 0;
    }
}

public class MoreOrEqualFunction : GenericFunctionWithTwoArguments {
    public MoreOrEqualFunction() : base(
        CoreFunNames.MoreOrEqual, GenericConstrains.Comparable, FunnyType.Bool,
        FunnyType.Generic(0), FunnyType.Generic(0)) { }

    protected override object Calc(object a, object b) {
        // IEEE 754: NaN is unordered — any comparison with NaN returns false
        if (IEEE754Guard.EitherIsNaN(a, b)) return false;
        return ((IComparable)a).CompareTo(b) >= 0;
    }
}

public class LessFunction : GenericFunctionWithTwoArguments {
    public LessFunction() : base(
        CoreFunNames.Less, GenericConstrains.Comparable, FunnyType.Bool, FunnyType.Generic(0),
        FunnyType.Generic(0)) { }

    protected override object Calc(object a, object b) {
        // IEEE 754: NaN is unordered — any comparison with NaN returns false
        if (IEEE754Guard.EitherIsNaN(a, b)) return false;
        return ((IComparable)a).CompareTo(b) < 0;
    }
}

public class LessOrEqualFunction : GenericFunctionWithTwoArguments {
    public LessOrEqualFunction() : base(
        CoreFunNames.LessOrEqual, GenericConstrains.Comparable, FunnyType.Bool,
        FunnyType.Generic(0), FunnyType.Generic(0)) { }

    protected override object Calc(object a, object b) {
        // IEEE 754: NaN is unordered — any comparison with NaN returns false
        if (IEEE754Guard.EitherIsNaN(a, b)) return false;
        return ((IComparable)a).CompareTo(b) <= 0;
    }
}

/// <summary>
/// IEEE 754: NaN is unordered. IComparable.CompareTo treats NaN as the smallest value,
/// which violates IEEE 754. This helper detects double NaN operands.
/// </summary>
internal static class IEEE754Guard {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool EitherIsNaN(object a, object b)
        => a is double da && double.IsNaN(da) || b is double db && double.IsNaN(db);
}

public class MinFunction : PureGenericFunctionBase {
    public MinFunction() : base("min", GenericConstrains.Comparable, 2) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var generic = concreteTypesMap[0];
        ComparablesGuard.RejectIfNotComparable("min", generic);
        FunctionWithTwoArgs function = new MinConcreteFunction();
        function.Setup(Name, generic);
        return function;
    }

    private class MinConcreteFunction : FunctionWithTwoArgs {
        public override object Calc(object a, object b) {
            // IEEE 754: NaN propagates through min
            if (IEEE754Guard.EitherIsNaN(a, b))
                return a is double da && double.IsNaN(da) ? a : b;
            var left = (IComparable)a;
            var right = (IComparable)b;
            return left.CompareTo(right) > 0 ? b : a;
        }
    }
}

public class MaxFunction : PureGenericFunctionBase {
    public MaxFunction() : base("max", GenericConstrains.Comparable, 2) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var generic = concreteTypesMap[0];
        ComparablesGuard.RejectIfNotComparable("max", generic);
        var function = new MaxConcreteFunction();
        function.Setup(Name, generic);
        return function;
    }

    private class MaxConcreteFunction : FunctionWithTwoArgs {
        public override object Calc(object a, object b) {
            // IEEE 754: NaN propagates through max
            if (IEEE754Guard.EitherIsNaN(a, b))
                return a is double da && double.IsNaN(da) ? a : b;
            var arg1 = (IComparable)a;
            var arg2 = (IComparable)b;
            var result = arg1.CompareTo(arg2) > 0 ? a : b;
            return result;
        }
    }
}

/// <summary>
/// Defensive guard for binary `min(T,T)` / `max(T,T)` (Bugs KK + LL). The TIC
/// `Comparable` generic constraint admits types that the array variant
/// `[T].max()` and the relational operators `< > <= >=` correctly reject —
/// notably Bool and Ip. Without this guard, `max(true, false)` returns a value
/// (Bool happens to implement IComparable in .NET) and `max(ip, ip)` crashes
/// with a raw InvalidCastException (System.Net.IPAddress is not IComparable).
/// Per Specs/Operators.md L115-118 the Comparable set is text / char / numbers.
/// </summary>
internal static class ComparablesGuard {
    public static void RejectIfNotComparable(string functionName, FunnyType t) {
        // Targeted rejection: bool and ip — the two known non-Comparable concrete
        // primitives. Any is left through because it's the "unconstrained" TIC
        // result for generic user-function forwards (`g(a,b) = max(a,b)` resolves
        // T to Any when a/b are unconstrained); the runtime values can still be
        // numeric/text/char. Other non-Comparable types (struct, fun, etc.) never
        // reach this constraint via the binary `(T,T)→T` Comparable signature.
        if (t.BaseType == BaseFunnyType.Bool || t.BaseType == BaseFunnyType.Ip)
            throw new NFun.Exceptions.FunnyParseException(
                777,
                $"Function '{functionName}' requires Comparable operands " +
                $"(text, char, or numeric); got '{t}'.",
                new NFun.Tokenization.Interval(0, 0));
    }
}
