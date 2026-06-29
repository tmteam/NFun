using System;
using System.Globalization;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions;

// ═══════════════════════════════════════════════════════════════
// Numeric formatting — typed, TIC-checkable
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// toNumText(value:real, decimals:int=2, minDigits:int=0, thousands:bool=false, forceZeros:bool=true) → text
/// </summary>
public class ToNumTextFunction : FunctionWithManyArguments {
    public ToNumTextFunction() : base(new FunctionSignatureDescription(
        name: CoreFunNames.ToNumText,
        outputType: FunnyType.Text,
        inputTypes: new[] { FunnyType.Real, FunnyType.Int32, FunnyType.Int32, FunnyType.Bool, FunnyType.Bool },
        argProperties: new[] {
            new FunArgProperty { Name = "value" },
            new FunArgProperty { Name = "decimals", HasDefault = true, DefaultValue = 2 },
            new FunArgProperty { Name = "minDigits", HasDefault = true, DefaultValue = 0 },
            new FunArgProperty { Name = "thousands", HasDefault = true, DefaultValue = false },
            new FunArgProperty { Name = "forceZeros", HasDefault = true, DefaultValue = true },
        })) { }

    public override object Calc(object[] args) {
        var value = Convert.ToDouble(args[0]);
        var decimals = (int)args[1];
        var minDigits = (int)args[2];
        var thousands = (bool)args[3];
        var forceZeros = (bool)args[4];

        char decChar = forceZeros ? '0' : '#';

        string intPart;
        if (thousands)
            intPart = minDigits > 1 ? new string('0', minDigits) : "#,##0";
        else if (minDigits > 1)
            intPart = new string('0', minDigits);
        else
            intPart = minDigits == 1 || forceZeros ? "0" : "#";

        string mask = decimals > 0
            ? intPart + "." + new string(decChar, decimals)
            : intPart;

        return new TextFunnyArray(value.ToString(mask, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// toHexText(value: integer) → text. Width-aware: formats at the operand's
/// declared bit width (byte→2 hex chars, int16/uint16→4, int32/uint32→8,
/// int64/uint64→16). Signed values use two's-complement representation at the
/// declared width. Without this, a widening to int64 before formatting would
/// give `int -1` → 'FFFFFFFFFFFFFFFF' instead of 'FFFFFFFF'. (Bug MM.)
/// </summary>
public class ToHexTextFunction : GenericFunctionBase {
    public ToHexTextFunction() : base(
        CoreFunNames.ToHexText, GenericConstrains.Integers,
        FunnyType.Text, FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("value");
    }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) =>
        concreteTypes[0].BaseType switch {
            // Cast signed → unsigned-of-same-width to capture two's-complement at the
            // declared width, then ToString("X") with no fixed length (unpadded — keeps
            // the existing convention `'{255:hex}'='FF'` while preventing the int64
            // sign-extension that turned `int -1` into 'FFFFFFFFFFFFFFFF').
            BaseFunnyType.UInt8 => new HexImpl(FunnyType.UInt8, o => ((byte)o).ToString("X")),
            BaseFunnyType.UInt16 => new HexImpl(FunnyType.UInt16, o => ((ushort)o).ToString("X")),
            BaseFunnyType.UInt32 => new HexImpl(FunnyType.UInt32, o => ((uint)o).ToString("X")),
            BaseFunnyType.UInt64 => new HexImpl(FunnyType.UInt64, o => ((ulong)o).ToString("X")),
            BaseFunnyType.Int16 => new HexImpl(FunnyType.Int16, o => unchecked((ushort)(short)o).ToString("X")),
            BaseFunnyType.Int32 => new HexImpl(FunnyType.Int32, o => unchecked((uint)(int)o).ToString("X")),
            BaseFunnyType.Int64 => new HexImpl(FunnyType.Int64, o => unchecked((ulong)(long)o).ToString("X")),
            _ => throw new Exceptions.NFunImpossibleException($"toHexText: unsupported type {concreteTypes[0]}")
        };

    private sealed class HexImpl : FunctionWithSingleArg {
        private readonly Func<object, string> _format;
        public HexImpl(FunnyType argType, Func<object, string> format) :
            base(CoreFunNames.ToHexText, FunnyType.Text, argType) => _format = format;
        public override object Calc(object a) => new TextFunnyArray(_format(a));
    }
}

/// <summary>
/// toBinText(value: integer) → text. Width-aware (see ToHexTextFunction).
/// `int -1` → '11111111111111111111111111111111' (32 bits) not 64. (Bug MM.)
/// </summary>
public class ToBinTextFunction : GenericFunctionBase {
    public ToBinTextFunction() : base(
        CoreFunNames.ToBinText, GenericConstrains.Integers,
        FunnyType.Text, FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("value");
    }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) =>
        concreteTypes[0].BaseType switch {
            // Same width-preservation strategy as toHexText: capture two's-complement
            // at the operand's declared width via signed→unsigned cast, then format
            // without forcing padding to width (matches existing
            // `'{42:bin}'='101010'` convention).
            BaseFunnyType.UInt8 => new BinImpl(FunnyType.UInt8, o => Convert.ToString((byte)o, 2)),
            BaseFunnyType.UInt16 => new BinImpl(FunnyType.UInt16, o => Convert.ToString((ushort)o, 2)),
            BaseFunnyType.UInt32 => new BinImpl(FunnyType.UInt32, o => Convert.ToString(unchecked((int)(uint)o), 2)),
            BaseFunnyType.UInt64 => new BinImpl(FunnyType.UInt64, o => Convert.ToString(unchecked((long)(ulong)o), 2)),
            BaseFunnyType.Int16 => new BinImpl(FunnyType.Int16, o => Convert.ToString(unchecked((ushort)(short)o), 2)),
            BaseFunnyType.Int32 => new BinImpl(FunnyType.Int32, o => Convert.ToString((int)o, 2)),
            BaseFunnyType.Int64 => new BinImpl(FunnyType.Int64, o => Convert.ToString((long)o, 2)),
            _ => throw new Exceptions.NFunImpossibleException($"toBinText: unsupported type {concreteTypes[0]}")
        };

    private sealed class BinImpl : FunctionWithSingleArg {
        private readonly Func<object, string> _format;
        public BinImpl(FunnyType argType, Func<object, string> format) :
            base(CoreFunNames.ToBinText, FunnyType.Text, argType) => _format = format;
        public override object Calc(object a) => new TextFunnyArray(_format(a));
    }
}

/// <summary>toSciText(value:real, uppercase:bool=true) → text</summary>
public class ToSciTextFunction : FunctionWithTwoArgs {
    public ToSciTextFunction() : base(CoreFunNames.ToSciText, FunnyType.Text, FunnyType.Real, FunnyType.Bool) {
        ArgProperties = new[] {
            new FunArgProperty { Name = "value" },
            new FunArgProperty { Name = "uppercase", HasDefault = true, DefaultValue = true },
        };
    }
    public override object Calc(object a, object b) {
        var value = Convert.ToDouble(a);
        var uppercase = (bool)b;
        return new TextFunnyArray(value.ToString(uppercase ? "E" : "e", CultureInfo.InvariantCulture));
    }
}

// ═══════════════════════════════════════════════════════════════
// Alignment — works on text, TIC-checkable
// ═══════════════════════════════════════════════════════════════

public class PadLeftTextFunction : FunctionWithTwoArgs {
    public PadLeftTextFunction() : base(CoreFunNames.PadLeftText, FunnyType.Text, FunnyType.Text, FunnyType.Int32) {
        ArgProperties = FunArgProperty.FromNames("text", "width");
    }
    public override object Calc(object a, object b) =>
        new TextFunnyArray(((IFunnyArray)a).ToText().PadLeft((int)b));
}

public class PadRightTextFunction : FunctionWithTwoArgs {
    public PadRightTextFunction() : base(CoreFunNames.PadRightText, FunnyType.Text, FunnyType.Text, FunnyType.Int32) {
        ArgProperties = FunArgProperty.FromNames("text", "width");
    }
    public override object Calc(object a, object b) =>
        new TextFunnyArray(((IFunnyArray)a).ToText().PadRight((int)b));
}

public class PadCenterTextFunction : FunctionWithTwoArgs {
    public PadCenterTextFunction() : base(CoreFunNames.PadCenterText, FunnyType.Text, FunnyType.Text, FunnyType.Int32) {
        ArgProperties = FunArgProperty.FromNames("text", "width");
    }
    public override object Calc(object a, object b) {
        var text = ((IFunnyArray)a).ToText();
        int width = (int)b;
        if (text.Length >= width)
            return new TextFunnyArray(text);
        int totalPad = width - text.Length;
        int padLeft = totalPad / 2;
        return new TextFunnyArray(text.PadLeft(text.Length + padLeft).PadRight(width));
    }
}
