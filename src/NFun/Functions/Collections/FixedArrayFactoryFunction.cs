using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.Runtime.Lists;
using NFun.Types;

namespace NFun.Functions.Collections;

/// <summary>
/// Lang-mode <c>fixedArray(...)</c> factory. Builds a <see cref="FixedFunnyArray"/>
/// from N homogeneous arguments — immutable after construction (no
/// <c>SetAt</c>, no <c>add</c>/<c>remove</c>).
///
/// NFun lacks native varargs — one instance is registered per concrete arity
/// (see BaseFunctions).
/// </summary>
public class FixedArrayFactoryFunction : GenericFunctionBase {
    private readonly int _arity;

    public FixedArrayFactoryFunction(int arity)
        : base(
            "fixedArray",
            FunnyType.FixedArrayOf(FunnyType.Generic(0)),
            Enumerable.Repeat(FunnyType.Generic(0), arity).ToArray()) {
        _arity = arity;
        ArgProperties = FunArgProperty.FromNames(
            Enumerable.Range(0, arity).Select(i => "x" + i).ToArray());
    }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var elementType = concreteTypesMap[0];
        var concreteArgs = Enumerable.Repeat(elementType, _arity).ToArray();
        return new ConcreteFixedArrayFactory(FunnyType.FixedArrayOf(elementType), concreteArgs, ArgProperties);
    }

    private sealed class ConcreteFixedArrayFactory : FunctionWithManyArguments {
        public ConcreteFixedArrayFactory(FunnyType returnType, FunnyType[] argTypes, FunArgProperty[] argProperties)
            : base("fixedArray", returnType, argTypes) {
            ArgProperties = argProperties;
        }

        public override object Calc(object[] args) {
            var elementType = ReturnType.FixedArrayTypeSpecification.FunnyType;
            // Snapshot args into a fresh object[] so the immutable container
            // doesn't share the caller's buffer.
            var items = new object[args.Length];
            System.Array.Copy(args, items, args.Length);
            return new FixedFunnyArray(elementType, items);
        }

        public override IConcreteFunction Clone(ICloneContext context)
            => new ConcreteFixedArrayFactory(ReturnType, ArgTypes, ArgProperties) { ArgProperties = ArgProperties };
    }
}
