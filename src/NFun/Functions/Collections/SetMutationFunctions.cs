using NFun;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Lists;
using NFun.Types;

namespace NFun.Functions.Collections;

// Stage 4 / B.x: mutation API for lang-mode `set<T>`. Set has different
// semantics from list — Add/Remove are idempotent (a second `tryAdd` of an
// existing element is a no-op), so we surface that to the user via
// `tryAdd` / `tryRemove` returning bool ("did the call actually change the
// set?"). Matches the .NET `HashSet<T>.Add` / `.Remove` semantics.

/// <summary>
/// <c>tryAdd(set&lt;T&gt;, T): bool</c> — adds the element. Returns true when
/// the set was actually changed (the element wasn't already present).
/// </summary>
public class SetTryAddFunction : GenericFunctionWithTwoArguments {
    public SetTryAddFunction() : base(
        "tryAdd",
        FunnyType.Bool,
        FunnyType.SetOf(FunnyType.Generic(0)),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("set", "item");
    }

    public override NFun.Interpretation.Functions.IConcreteFunction CreateConcrete(
        FunnyType[] concreteTypes,
        IFunctionSelectorContext context) {
        // Enforce Immutable on the inferred element type. The set(...) factory
        // already enforces this, but `[].toSet().tryAdd(x)` (element type
        // inferred as Any) used to silently accept mutable composites. Bug
        // hunt round 3 #18.
        ImmutableTypePredicate.RequireImmutable(concreteTypes[0], "tryAdd", "element");
        return base.CreateConcrete(concreteTypes, context);
    }

    protected override object Calc(object a, object b) {
        if (a is not IFunnyMutableSet set)
            throw new FunnyRuntimeException("tryAdd() requires a mutable set");
        return set.Add(b);
    }
}

/// <summary>
/// <c>tryRemove(set&lt;T&gt;, T): bool</c> — removes the element. Returns true
/// when the element was present and got removed.
/// </summary>
public class SetTryRemoveFunction : GenericFunctionWithTwoArguments {
    public SetTryRemoveFunction() : base(
        "tryRemove",
        FunnyType.Bool,
        FunnyType.SetOf(FunnyType.Generic(0)),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("set", "item");
    }

    public override NFun.Interpretation.Functions.IConcreteFunction CreateConcrete(
        FunnyType[] concreteTypes,
        IFunctionSelectorContext context) {
        ImmutableTypePredicate.RequireImmutable(concreteTypes[0], "tryRemove", "element");
        return base.CreateConcrete(concreteTypes, context);
    }

    protected override object Calc(object a, object b) {
        if (a is not IFunnyMutableSet set)
            throw new FunnyRuntimeException("tryRemove() requires a mutable set");
        return set.Remove(b);
    }
}

// clear() is deferred until the Mutable<T> typeclass lands (see task #248):
// IFunctionRegistry is keyed by (name, arity) so two distinct arity-1 `clear`
// functions can't both register. The proper fix is a Mutable<T> constraint
// that List / MutableArray / Set all satisfy; until then `clear(list<T>)` is
// the only registered shape.
