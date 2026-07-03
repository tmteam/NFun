using System;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions.Lang;

/// <summary>
/// Lang-mode `map(arr, fn)` — widened
/// <c>(Enumerable&lt;T0&gt;, (T0)-&gt;T1) -&gt; FixedArray&lt;T1&gt;</c>.
/// Accepts any iterable including Map&lt;K,V&gt; via synthesized pair-struct.
/// Trade-off: reduced back-prop precision for nested numeric upcast — see
/// <c>specs_tic/TechnicalDebt.md</c> §Closed (#16).
/// </summary>
public class MapEnumerableFunction : GenericFunctionBase {
    public MapEnumerableFunction() : base(
        "map",
        FunnyType.FixedArrayOf(FunnyType.Generic(1)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "f"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var res = new ConcreteMap(context.Converter.TypeBehaviour.GetClrTypeFor(concreteTypesMap[0].BaseType)) {
            Name = Name,
            ArgTypes = new[] {
                FunnyType.EnumerableOf(concreteTypesMap[0]),
                FunnyType.FunOf(concreteTypesMap[1], concreteTypesMap[0])
            },
            ReturnType = FunnyType.FixedArrayOf(concreteTypesMap[1])
        };
        return res;
    }

    private class ConcreteMap : FunctionWithTwoArgs {
        private readonly Type _lambdaInputClrType;
        public ConcreteMap(Type lambdaInputClrType) { _lambdaInputClrType = lambdaInputClrType; }
        public override object Calc(object a, object b) {
            var src = a switch {
                IFunnyArray ifa => ifa.Select(e => e),
                NFun.Runtime.Lists.IFunnyEnumerable ife => ife.Select(e => e),
                _ => throw new FunnyRuntimeException("map: unsupported collection shape"),
            };
            var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
            var coerce = MapCoerce.Get(_lambdaInputClrType);
            if (b is FunctionWithSingleArg mapFunc)
                return new NFun.Runtime.Lists.FixedFunnyArray(elemType, src.Select(e => mapFunc.Calc(coerce(e))).ToArray());
            var map = (IConcreteFunction)b;
            return new NFun.Runtime.Lists.FixedFunnyArray(elemType, src.Select(e => map.Calc(new[] { coerce(e) })).ToArray());
        }
    }
}

internal static class MapCoerce {
    private static readonly Func<object, object> Identity = e => e;

    // Per .map() call we close over the lambda's CLR input type once and reuse
    // the per-element coercion closure for every element. The collection may
    // carry a narrower numeric (byte) than the lambda input declared (Int32 /
    // Real / ...) because TIC widens the element type along the outer chain
    // but the underlying storage stays at the original primitive. Coerce here
    // so the lambda receives the value at its declared CLR type.
    public static Func<object, object> Get(Type target) {
        if (target == null) return Identity;
        if (target == typeof(double))
            return e => e is double or decimal ? e : System.Convert.ToDouble(e, System.Globalization.CultureInfo.InvariantCulture);
        if (target == typeof(decimal))
            return e => e is decimal ? e : System.Convert.ToDecimal(e, System.Globalization.CultureInfo.InvariantCulture);
        if (target == typeof(int))
            return e => e is int ? e : System.Convert.ToInt32(e, System.Globalization.CultureInfo.InvariantCulture);
        if (target == typeof(long))
            return e => e is long ? e : System.Convert.ToInt64(e, System.Globalization.CultureInfo.InvariantCulture);
        if (target == typeof(short))
            return e => e is short ? e : System.Convert.ToInt16(e, System.Globalization.CultureInfo.InvariantCulture);
        if (target == typeof(byte))
            return e => e is byte ? e : System.Convert.ToByte(e, System.Globalization.CultureInfo.InvariantCulture);
        if (target == typeof(ushort))
            return e => e is ushort ? e : System.Convert.ToUInt16(e, System.Globalization.CultureInfo.InvariantCulture);
        if (target == typeof(uint))
            return e => e is uint ? e : System.Convert.ToUInt32(e, System.Globalization.CultureInfo.InvariantCulture);
        if (target == typeof(ulong))
            return e => e is ulong ? e : System.Convert.ToUInt64(e, System.Globalization.CultureInfo.InvariantCulture);
        return Identity;
    }
}
