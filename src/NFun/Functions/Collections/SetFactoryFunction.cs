using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.Runtime.Lists;
using NFun.Types;

namespace NFun.Functions.Collections;

/// <summary>
/// Lang-mode <c>set(...)</c> factory. Builds a <see cref="MutableFunnySet"/>
/// from N homogeneous arguments. Duplicate inputs collapse — the resulting set
/// has cardinality ≤ <c>arity</c>.
///
/// NFun lacks native varargs — one instance is registered per concrete arity
/// (see BaseFunctions).
/// </summary>
public class SetFactoryFunction : GenericFunctionBase {
    private readonly int _arity;

    public SetFactoryFunction(int arity)
        : base(
            "set",
            FunnyType.SetOf(FunnyType.Generic(0)),
            Enumerable.Repeat(FunnyType.Generic(0), arity).ToArray()) {
        _arity = arity;
        ArgProperties = FunArgProperty.FromNames(
            Enumerable.Range(0, arity).Select(i => "x" + i).ToArray());
    }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var elementType = concreteTypesMap[0];
        var concreteArgs = Enumerable.Repeat(elementType, _arity).ToArray();
        return new ConcreteSetFactory(FunnyType.SetOf(elementType), concreteArgs, ArgProperties);
    }

    private sealed class ConcreteSetFactory : FunctionWithManyArguments {
        public ConcreteSetFactory(FunnyType returnType, FunnyType[] argTypes, FunArgProperty[] argProperties)
            : base("set", returnType, argTypes) {
            ArgProperties = argProperties;
        }

        public override object Calc(object[] args) {
            var elementType = ReturnType.SetTypeSpecification.FunnyType;
            return new MutableFunnySet(elementType, args);
        }

        public override IConcreteFunction Clone(ICloneContext context)
            => new ConcreteSetFactory(ReturnType, ArgTypes, ArgProperties) { ArgProperties = ArgProperties };
    }
}
