using System;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions;

/// <summary>assert(condition: bool) — oops if false. Arity [1].</summary>
public class AssertFunction : FunctionWithSingleArg {
    public static readonly AssertFunction Instance = new();
    private AssertFunction() : base("assert", FunnyType.Any, FunnyType.Bool) {
        ArgProperties = FunArgProperty.FromNames("condition");
    }

    public override object Calc(object a) {
        if (!(bool)a)
            throw new FunnyRuntimeException("Assertion failed");
        return FunnyNone.Instance;
    }
}

/// <summary>assert(condition: bool, message: text) — oops with message if false. Arity [2].</summary>
public class AssertWithMessageFunction : FunctionWithTwoArgs {
    public static readonly AssertWithMessageFunction Instance = new();
    private AssertWithMessageFunction() : base("assert", FunnyType.Any, FunnyType.Bool, FunnyType.Text) {
        ArgProperties = FunArgProperty.FromNames("condition", "message");
    }

    public override object Calc(object a, object b) {
        if (!(bool)a) {
            var msg = TypeHelper.GetFunText(b);
            throw new FunnyRuntimeException($"Assertion failed: {msg}");
        }
        return FunnyNone.Instance;
    }
}

/// <summary>
/// assertEqual[of T](actual: T, expected: T) — oops if not equal.
/// Generic: TIC resolves T, deep equality via TypeHelper.AreEqual.
/// </summary>
public class AssertEqualFunction : GenericFunctionWithTwoArguments {
    public AssertEqualFunction() : base(
        "assertEqual",
        new[] { GenericConstrains.Any },
        FunnyType.Any,
        FunnyType.Generic(0),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("actual", "expected");
    }

    protected override object Calc(object a, object b) {
        // Use value comparison: convert to same numeric type if both are numbers
        bool equal;
        if (a is IConvertible ca && b is IConvertible cb) {
            try {
                var da = Convert.ToDouble(ca);
                var db = Convert.ToDouble(cb);
                equal = Math.Abs(da - db) < 0.0001;
            } catch {
                equal = TypeHelper.AreEqual(a, b);
            }
        } else {
            equal = TypeHelper.AreEqual(a, b);
        }
        if (!equal) {
            var actualText = TypeHelper.GetFunText(a);
            var expectedText = TypeHelper.GetFunText(b);
            throw new FunnyRuntimeException(
                $"Assertion failed: expected {expectedText}, got {actualText}");
        }
        return FunnyNone.Instance;
    }
}

/// <summary>
/// assertNotEqual[of T](actual: T, notExpected: T) — oops if equal.
/// </summary>
public class AssertNotEqualFunction : GenericFunctionWithTwoArguments {
    public AssertNotEqualFunction() : base(
        "assertNotEqual",
        new[] { GenericConstrains.Any },
        FunnyType.Any,
        FunnyType.Generic(0),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("actual", "notExpected");
    }

    protected override object Calc(object a, object b) {
        if (TypeHelper.AreEqual(a, b)) {
            var text = TypeHelper.GetFunText(a);
            throw new FunnyRuntimeException(
                $"Assertion failed: values should not be equal, but both are {text}");
        }
        return FunnyNone.Instance;
    }
}

/// <summary>
/// assertType[of T](value: T, expectedType: text) — checks TIC-resolved type name.
/// Generic: T resolved at compile time, type name compared at runtime.
/// Note: inside generic user functions, types resolve to wider ranges (e.g., Real
/// instead of Int32) because function body solve uses ignorePreferred.
/// For accurate type checks, use assertType at top level.
/// </summary>
public class AssertTypeFunction : GenericFunctionBase {
    public AssertTypeFunction() : base(
        "assertType",
        FunnyType.Any,                      // return type
        FunnyType.Generic(0),               // value: T
        FunnyType.Text) {                   // expectedType: text
        ArgProperties = FunArgProperty.FromNames("value", "expectedType");
    }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var resolvedType = concreteTypesMap[0];
        var typeName = FunnyTypeToShortName(resolvedType);
        return new ConcreteAssertType(typeName);
    }

    private sealed class ConcreteAssertType : FunctionWithTwoArgs {
        private readonly string _typeName;

        public ConcreteAssertType(string typeName) {
            _typeName = typeName;
            Name = "assertType";
            ArgTypes = new[] { FunnyType.Any, FunnyType.Text };
            ReturnType = FunnyType.Any;
        }

        public override object Calc(object a, object b) {
            var expected = TypeHelper.GetFunText(b);
            if (!string.Equals(_typeName, expected, StringComparison.OrdinalIgnoreCase))
                throw new FunnyRuntimeException(
                    $"Type assertion failed: expected '{expected}', got '{_typeName}'");
            return FunnyNone.Instance;
        }
    }

    internal static string FunnyTypeToShortName(FunnyType type) => type.BaseType switch {
        BaseFunnyType.Bool => "bool",
        BaseFunnyType.Char => "char",
        BaseFunnyType.UInt8 => "byte",
        BaseFunnyType.UInt16 => "uint16",
        BaseFunnyType.UInt32 => "uint32",
        BaseFunnyType.UInt64 => "uint64",
        BaseFunnyType.Int16 => "int16",
        BaseFunnyType.Int32 => "int",
        BaseFunnyType.Int64 => "int64",
        BaseFunnyType.Real => "real",
        BaseFunnyType.Ip => "ip",
        BaseFunnyType.Any => "any",
        BaseFunnyType.None => "none",
        BaseFunnyType.ArrayOf when type.ArrayTypeSpecification.FunnyType.BaseType == BaseFunnyType.Char
            => "text",
        BaseFunnyType.ArrayOf
            => FunnyTypeToShortName(type.ArrayTypeSpecification.FunnyType) + "[]",
        BaseFunnyType.List
            => "list<" + FunnyTypeToShortName(type.ListTypeSpecification.FunnyType) + ">",
        BaseFunnyType.Optional
            => FunnyTypeToShortName(type.OptionalTypeSpecification.ElementType) + "?",
        BaseFunnyType.Struct => "{" + string.Join(", ",
            System.Linq.Enumerable.Select(type.StructTypeSpecification,
                f => f.Key + ":" + FunnyTypeToShortName(f.Value))) + "}",
        BaseFunnyType.Fun => "fun",
        _ => type.ToString(),
    };
}
