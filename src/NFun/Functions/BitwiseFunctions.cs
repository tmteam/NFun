using System;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.Functions;

// Per Specs/Operators.md L185 the shift count is always masked to the bit width
// of the operand type: `x << n` == `x << (n % bits)`. For int32/int64/uint32/uint64
// the CLR shift operators apply this mask natively (`& 0x1F` / `& 0x3F`).
// For narrower types (byte/int16/uint16) C# widens the operand to int *before*
// shifting, so the CLR mask is computed against 32 bits — which lets the value
// overshoot the narrow type's range and the post-shift cast truncates to 0.
// The explicit `& (bits-1)` below restores the spec semantics for those types.

public class BitShiftLeftFunction : GenericFunctionBase {
    public BitShiftLeftFunction() : base(
        CoreFunNames.BitShiftLeft,
        GenericConstrains.Integers,
        FunnyType.Generic(0),
        FunnyType.Generic(0),
        FunnyType.UInt8) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context)
        => concreteTypes[0].BaseType switch {
               BaseFunnyType.UInt64 => new UInt64Function(),
               BaseFunnyType.UInt32 => new UInt32Function(),
               BaseFunnyType.UInt16 => new UInt16Function(),
               BaseFunnyType.UInt8  => new UInt8Function(),
               BaseFunnyType.Int32  => new Int32Function(),
               BaseFunnyType.Int64  => new Int64Function(),
               BaseFunnyType.Int16  => new Int16Function(),
               _                    => throw new Exceptions.NFunImpossibleException("Unsupported type for this function")
           };

    private class Int16Function : FunctionWithTwoArgs {
        public Int16Function() : base(CoreFunNames.BitShiftLeft, FunnyType.Int16, FunnyType.Int16, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (Int16)((Int16)a << ((byte)b & 0x0F));
    }

    private class Int32Function : FunctionWithTwoArgs {
        public Int32Function() : base(CoreFunNames.BitShiftLeft, FunnyType.Int32, FunnyType.Int32, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (int)a << (byte)b;
    }

    private class Int64Function : FunctionWithTwoArgs {
        public Int64Function() : base(CoreFunNames.BitShiftLeft, FunnyType.Int64, FunnyType.Int64, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (long)a << (byte)b;
    }

    private class UInt8Function : FunctionWithTwoArgs {
        public UInt8Function() : base(
            CoreFunNames.BitShiftLeft, FunnyType.UInt8, FunnyType.UInt8,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (byte)((byte)a << ((byte)b & 0x07));
    }

    private class UInt16Function : FunctionWithTwoArgs {
        public UInt16Function() : base(
            CoreFunNames.BitShiftLeft, FunnyType.UInt16, FunnyType.UInt16,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (UInt16)((UInt16)a << ((byte)b & 0x0F));
    }

    private class UInt32Function : FunctionWithTwoArgs {
        public UInt32Function() : base(
            CoreFunNames.BitShiftLeft, FunnyType.UInt32, FunnyType.UInt32,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (uint)((uint)a << (byte)b);
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public UInt64Function() : base(
            CoreFunNames.BitShiftLeft, FunnyType.UInt64, FunnyType.UInt64,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (ulong)((ulong)a << (byte)b);
    }
}

public class BitShiftRightFunction : GenericFunctionBase {
    public BitShiftRightFunction() : base(
        CoreFunNames.BitShiftRight,
        GenericConstrains.Integers,
        FunnyType.Generic(0),
        FunnyType.Generic(0),
        FunnyType.UInt8) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context)
        => concreteTypes[0].BaseType switch {
               BaseFunnyType.UInt64 => new UInt64Function(),
               BaseFunnyType.UInt32 => new UInt32Function(),
               BaseFunnyType.UInt16 => new UInt16Function(),
               BaseFunnyType.UInt8  => new UInt8Function(),
               BaseFunnyType.Int32  => new Int32Function(),
               BaseFunnyType.Int64  => new Int64Function(),
               BaseFunnyType.Int16  => new Int16Function(),
               _                    => throw new Exceptions.NFunImpossibleException("Unsupported type for this function")
           };

    private class Int16Function : FunctionWithTwoArgs {
        public Int16Function() : base(CoreFunNames.BitShiftRight, FunnyType.Int16, FunnyType.Int16, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (Int16)((Int16)a >> ((byte)b & 0x0F));
    }

    private class Int32Function : FunctionWithTwoArgs {
        public Int32Function() : base(CoreFunNames.BitShiftRight, FunnyType.Int32, FunnyType.Int32, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (int)a >> (byte)b;
    }

    private class Int64Function : FunctionWithTwoArgs {
        public Int64Function() : base(CoreFunNames.BitShiftRight, FunnyType.Int64, FunnyType.Int64, FunnyType.UInt8) { }
        public override object Calc(object a, object b) => (long)a >> (byte)b;
    }

    private class UInt8Function : FunctionWithTwoArgs {
        public UInt8Function() : base(
            CoreFunNames.BitShiftRight, FunnyType.UInt8, FunnyType.UInt8,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (byte)((byte)a >> ((byte)b & 0x07));
    }

    private class UInt16Function : FunctionWithTwoArgs {
        public UInt16Function() : base(
            CoreFunNames.BitShiftRight, FunnyType.UInt16, FunnyType.UInt16,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (UInt16)((UInt16)a >> ((byte)b & 0x0F));
    }

    private class UInt32Function : FunctionWithTwoArgs {
        public UInt32Function() : base(
            CoreFunNames.BitShiftRight, FunnyType.UInt32, FunnyType.UInt32,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (uint)((uint)a >> (byte)b);
    }

    private class UInt64Function : FunctionWithTwoArgs {
        public UInt64Function() : base(
            CoreFunNames.BitShiftRight, FunnyType.UInt64, FunnyType.UInt64,
            FunnyType.UInt8) { }

        public override object Calc(object a, object b) => (ulong)((ulong)a >> (byte)b);
    }
}

// PureGeneric fuses operand and result via one type variable, so `~a + 0` with a:byte widens
// the operand to int32 BEFORE applying `~`, producing -6 instead of byte 250 widened. The
// wrapper picks the concrete at CreateWithConvertionOrThrow time based on the OPERAND's
// actual type and inserts the widening cast AFTER `~`. (Bug JJ.)
public class BitInverseFunction : PureGenericFunctionBase {
    public BitInverseFunction() : base(CoreFunNames.BitInverse, GenericConstrains.Integers, 1) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) {
        var result = PickConcrete(concreteTypes[0].BaseType);
        result.Name = CoreFunNames.BitInverse;
        result.ArgTypes = concreteTypes;
        result.ReturnType = concreteTypes[0];
        return new BitInverseNarrowAwareWrapper(result, concreteTypes[0]);
    }

    internal static FunctionWithSingleArg PickConcrete(BaseFunnyType t) => t switch {
        BaseFunnyType.UInt8  => UInt8Function.Instance,
        BaseFunnyType.UInt16 => UInt16Function.Instance,
        BaseFunnyType.UInt32 => UInt32Function.Instance,
        BaseFunnyType.UInt64 => UInt64Function.Instance,
        BaseFunnyType.Int16  => Int16Function.Instance,
        BaseFunnyType.Int32  => Int32Function.Instance,
        BaseFunnyType.Int64  => Int64Function.Instance,
        _ => throw new Exceptions.NFunImpossibleException("Unsupported type for this function")
    };

    private sealed class BitInverseNarrowAwareWrapper : FunctionWithSingleArg {
        private readonly FunctionWithSingleArg _resolved;

        public BitInverseNarrowAwareWrapper(FunctionWithSingleArg resolved, FunnyType resolvedType) {
            _resolved = resolved;
            Name = resolved.Name;
            ArgTypes = new[] { resolvedType };
            ReturnType = resolvedType;
        }

        public override object Calc(object a) => _resolved.Calc(a);

        public override Interpretation.Nodes.IExpressionNode CreateWithConvertionOrThrow(
            System.Collections.Generic.IList<Interpretation.Nodes.IExpressionNode> children,
            TypeBehaviour typeBehaviour, Tokenization.Interval interval) {
            var operandNode = children[0];
            var operandBase = operandNode.Type.BaseType;
            var resolvedBase = ArgTypes[0].BaseType;
            // Order-mismatch ONLY for zero-extended widening: unsigned-narrow → wider.
            // Signed-narrow → wider sign-extends and the bit pattern is preserved either way.
            bool zeroExtendingWiden = operandBase != resolvedBase &&
                operandBase is BaseFunnyType.UInt8 or BaseFunnyType.UInt16 or BaseFunnyType.UInt32;
            if (zeroExtendingWiden) {
                var narrow = PickConcrete(operandBase);
                var narrowCall = new Interpretation.Nodes.FunOfSingleArgExpressionNode(
                    narrow, operandNode, interval);
                var converter = VarTypeConverter.GetConverterOrThrow(
                    typeBehaviour, operandNode.Type, ArgTypes[0], interval);
                return new Interpretation.Nodes.CastExpressionNode(
                    narrowCall, ArgTypes[0], converter, interval);
            }
            return base.CreateWithConvertionOrThrow(children, typeBehaviour, interval);
        }
    }

    private class UInt8Function : FunctionWithSingleArg {
        public static readonly UInt8Function Instance = new();
        public override object Calc(object a) => (byte)~(byte)a;
    }

    private class UInt16Function : FunctionWithSingleArg {
        public static readonly UInt16Function Instance = new();
        public override object Calc(object a) => (UInt16)(~(UInt16)a);
    }

    private class UInt32Function : FunctionWithSingleArg {
        public static readonly UInt32Function Instance = new();
        public override object Calc(object a) => (UInt32)(~(UInt32)a);
    }

    private class UInt64Function : FunctionWithSingleArg {
        public static readonly UInt64Function Instance = new();
        public override object Calc(object a) => (UInt64)(~(UInt64)a);
    }

    private class Int16Function : FunctionWithSingleArg {
        public static readonly Int16Function Instance = new();
        public override object Calc(object a) => (Int16)(~(Int16)a);
    }

    private class Int32Function : FunctionWithSingleArg {
        public static readonly Int32Function Instance = new();
        public override object Calc(object a) => (Int32)(~(Int32)a);
    }

    private class Int64Function : FunctionWithSingleArg {
        public static readonly Int64Function Instance = new();
        public override object Calc(object a) => (Int64)(~(Int64)a);
    }
}
