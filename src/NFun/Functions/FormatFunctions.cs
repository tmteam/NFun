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
    public ToNumTextFunction() : base(
        CoreFunNames.ToNumText,
        FunnyType.Text,
        FunnyType.Real, FunnyType.Int32, FunnyType.Int32, FunnyType.Bool, FunnyType.Bool) {
        ArgProperties = new[] {
            new FunArgProperty { Name = "value" },
            new FunArgProperty { Name = "decimals", HasDefault = true, DefaultValue = 2 },
            new FunArgProperty { Name = "minDigits", HasDefault = true, DefaultValue = 0 },
            new FunArgProperty { Name = "thousands", HasDefault = true, DefaultValue = false },
            new FunArgProperty { Name = "forceZeros", HasDefault = true, DefaultValue = true },
        };
    }

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

/// <summary>toHexText(value:int) → text</summary>
public class ToHexTextFunction : FunctionWithSingleArg {
    public ToHexTextFunction() : base(CoreFunNames.ToHexText, FunnyType.Text, FunnyType.Int64) {
        ArgProperties = FunArgProperty.FromNames("value");
    }
    public override object Calc(object a) =>
        new TextFunnyArray(Convert.ToInt64(a).ToString("X"));
}

/// <summary>toBinText(value:int) → text</summary>
public class ToBinTextFunction : FunctionWithSingleArg {
    public ToBinTextFunction() : base(CoreFunNames.ToBinText, FunnyType.Text, FunnyType.Int64) {
        ArgProperties = FunArgProperty.FromNames("value");
    }
    public override object Calc(object a) =>
        new TextFunnyArray(Convert.ToString(Convert.ToInt64(a), 2));
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
