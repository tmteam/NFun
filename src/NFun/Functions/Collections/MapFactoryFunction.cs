using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.Runtime.Lists;
using NFun.Types;

namespace NFun.Functions.Collections;

/// <summary>
/// Lang-mode map factory.
/// <para>Signature:
/// <c>__mkMap(p0: {key:K, value:V}, p1: {key:K, value:V}, …, pN-1)
///   -&gt; map&lt;K, V&gt;</c></para>
/// Each positional argument is a 2-field struct with fields <c>key</c> and
/// <c>value</c>. Duplicate keys: later args overwrite earlier (matches the
/// natural insertion semantics of <c>Dictionary</c>).
///
/// <b>Temporary name (`__mkMap`).</b> The canonical name should be
/// <c>map(...)</c>, but the existing LINQ <c>map(arr, fn)</c> built-in
/// occupies the <c>(name, arity)</c> slot in <see cref="IFunctionRegistry"/>.
/// The proper fix is dialect-aware separation of extension functions vs
/// regular functions (two distinct registries in lang mode); that lands in
/// master separately. Once it does, this factory renames to <c>map</c>.
///
/// One instance is registered per concrete arity (NFun lacks varargs); see
/// <see cref="BaseFunctions"/>.
/// </summary>
public class MapFactoryFunction : GenericFunctionBase {
    private static readonly FunnyType PairStruct = FunnyType.StructOf(
        ("key", FunnyType.Generic(0)),
        ("value", FunnyType.Generic(1)));

    private readonly int _arity;

    public MapFactoryFunction(int arity)
        : base(
            "__mkMap",
            FunnyType.MapOf(FunnyType.Generic(0), FunnyType.Generic(1)),
            Enumerable.Repeat(PairStruct, arity).ToArray()) {
        _arity = arity;
        ArgProperties = FunArgProperty.FromNames(
            Enumerable.Range(0, arity).Select(i => "p" + i).ToArray());
    }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var keyType = concreteTypesMap[0];
        var valueType = concreteTypesMap[1];
        var concretePair = FunnyType.StructOf(("key", keyType), ("value", valueType));
        var concreteArgs = Enumerable.Repeat(concretePair, _arity).ToArray();
        return new ConcreteMapFactory(
            FunnyType.MapOf(keyType, valueType), concreteArgs, ArgProperties);
    }

    private sealed class ConcreteMapFactory : FunctionWithManyArguments {
        public ConcreteMapFactory(FunnyType returnType, FunnyType[] argTypes, FunArgProperty[] argProperties)
            : base("__mkMap", returnType, argTypes) {
            ArgProperties = argProperties;
        }

        public override object Calc(object[] args) {
            var keyType = ReturnType.MapTypeSpecification.KeyType;
            var valueType = ReturnType.MapTypeSpecification.ValueType;
            var map = new MutableFunnyMap(keyType, valueType);
            foreach (var pair in args) {
                if (pair is not FunnyStruct s)
                    throw new Exceptions.FunnyRuntimeException(
                        "map(...) argument must be a {key, value} struct");
                map.Set(s.GetValue("key"), s.GetValue("value"));
            }
            return map;
        }

        public override IConcreteFunction Clone(ICloneContext context)
            => new ConcreteMapFactory(ReturnType, ArgTypes, ArgProperties) { ArgProperties = ArgProperties };
    }
}
