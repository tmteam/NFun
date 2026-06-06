using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.Runtime.Lists;
using NFun.Types;

namespace NFun.Functions.Collections;

/// <summary>
/// Lang-mode <c>array(...)</c> factory. Builds a <see cref="MutableFunnyArray"/>
/// from N homogeneous arguments — fixed-length mutable container (<c>a[i]=v</c>
/// works; <c>add</c>/<c>remove</c> don't).
///
/// NFun lacks native varargs — one instance is registered per concrete arity
/// (see BaseFunctions).
/// </summary>
public class ArrayFactoryFunction : GenericFunctionBase {
    private readonly int _arity;

    public ArrayFactoryFunction(int arity)
        : base(
            "array",
            FunnyType.MutableArrayOf(FunnyType.Generic(0)),
            Enumerable.Repeat(FunnyType.Generic(0), arity).ToArray()) {
        _arity = arity;
        ArgProperties = FunArgProperty.FromNames(
            Enumerable.Range(0, arity).Select(i => "x" + i).ToArray());
    }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var elementType = concreteTypesMap[0];
        var concreteArgs = Enumerable.Repeat(elementType, _arity).ToArray();
        return new ConcreteArrayFactory(FunnyType.MutableArrayOf(elementType), concreteArgs, ArgProperties);
    }

    private sealed class ConcreteArrayFactory : FunctionWithManyArguments {
        public ConcreteArrayFactory(FunnyType returnType, FunnyType[] argTypes, FunArgProperty[] argProperties)
            : base("array", returnType, argTypes) {
            ArgProperties = argProperties;
        }

        public override object Calc(object[] args) {
            var elementType = ReturnType.MutableArrayTypeSpecification.FunnyType;
            // Copy args — caller's object[] is reused for the array body.
            var items = new object[args.Length];
            System.Array.Copy(args, items, args.Length);
            return new MutableFunnyArray(elementType, items);
        }

        public override IConcreteFunction Clone(ICloneContext context)
            => new ConcreteArrayFactory(ReturnType, ArgTypes, ArgProperties) { ArgProperties = ArgProperties };
    }
}
