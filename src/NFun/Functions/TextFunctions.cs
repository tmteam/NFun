using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions;

public class ToTextFunction : FunctionWithSingleArg {
    public ToTextFunction() : base(CoreFunNames.ToText, FunnyType.Text, FunnyType.Any) { ArgProperties = FunArgProperty.FromNames("value"); }

    public override object Calc(object a) => new TextFunnyArray(TypeHelper.GetFunText(a));
}

public class ToTextFormattedFunction : FunctionWithTwoArgs {
    public ToTextFormattedFunction() : base(
        CoreFunNames.ToTextFormatted, FunnyType.Text, FunnyType.Any, FunnyType.Text) { }

    private static string ResolveFormat(string fmt) {
        if (string.IsNullOrEmpty(fmt))
            return null; // empty format = just toText, no formatting
        return fmt switch {
            "hex" or "HEX" => "X",
            "sci" => "e",
            "SCI" => "E",
            "bin" => "__bin__",
            _ => ValidateMask(fmt)
        };
    }

    // Allowed mask characters (no sections, no % — reserved for future)
    private const string AllowedMaskChars = "0#., ";

    /// <summary>
    /// Validate mask: must start with a mask character (not a letter),
    /// contain only allowed characters, and have at least one 0 or #.
    /// Letters are reserved for future named specifiers.
    /// Sections (;) are reserved for future conditional formatting.
    /// </summary>
    private static string ValidateMask(string fmt) {
        if (char.IsLetter(fmt[0]))
            throw new FunnyRuntimeException(
                $"Unknown format specifier '{fmt}'. Named specifiers: hex, bin, sci");

        bool hasFormat = false;
        foreach (var c in fmt) {
            if (c == '0' || c == '#') hasFormat = true;
            else if (AllowedMaskChars.IndexOf(c) < 0)
                throw new FunnyRuntimeException(
                    $"Invalid character '{c}' in format mask '{fmt}'. Allowed: 0 # . ,");
        }
        if (!hasFormat)
            throw new FunnyRuntimeException(
                $"Invalid format specifier '{fmt}'. Mask must contain '0' or '#'");
        return fmt;
    }

    public override object Calc(object a, object b) {
        var rawFormat = ((IFunnyArray)b).ToText();

        // Split by ':' into segments. Each segment identified by first char:
        //   0 # . , → mask       e.g. "0.00", "#,##0"
        //   > < ^   → alignment  e.g. ">10", "^20"
        //   letter   → named     e.g. "hex", "bin"
        string maskPart = null;
        string alignPart = null;

        foreach (var segment in rawFormat.Split(':')) {
            var s = segment.Trim();
            if (s.Length == 0) continue;
            char first = s[0];
            if (first == '>' || first == '<' || first == '^')
                alignPart = s;
            else
                maskPart = s; // mask or named — both go through ResolveFormat
        }

        // Apply format
        string result;
        var format = string.IsNullOrEmpty(maskPart) ? null : ResolveFormat(maskPart);
        if (format == null)
            result = TypeHelper.GetFunText(a);
        else if (format == "__bin__")
            result = ToBinary(a);
        else
            result = FormatNumeric(a, format, maskPart);

        // Apply alignment
        if (alignPart != null)
            result = ApplyAlignment(result, alignPart, rawFormat);

        return new TextFunnyArray(result);
    }

    private static string FormatNumeric(object a, string format, string rawFormat) {
        try {
            return a switch {
                double d => d.ToString(format, CultureInfo.InvariantCulture),
                decimal m => m.ToString(format, CultureInfo.InvariantCulture),
                int i => i.ToString(format, CultureInfo.InvariantCulture),
                long l => l.ToString(format, CultureInfo.InvariantCulture),
                byte v => v.ToString(format, CultureInfo.InvariantCulture),
                short v => v.ToString(format, CultureInfo.InvariantCulture),
                ushort v => v.ToString(format, CultureInfo.InvariantCulture),
                uint v => v.ToString(format, CultureInfo.InvariantCulture),
                ulong v => v.ToString(format, CultureInfo.InvariantCulture),
                float f => f.ToString(format, CultureInfo.InvariantCulture),
                _ => throw new FunnyRuntimeException($"Format '{rawFormat}' is not supported for type {a?.GetType().Name ?? "null"}")
            };
        }
        catch (FormatException) {
            throw new FunnyRuntimeException($"Invalid format specifier '{rawFormat}'");
        }
    }

    private static string ApplyAlignment(string text, string alignSpec, string rawFormat) {
        if (alignSpec.Length < 2)
            throw new FunnyRuntimeException($"Invalid alignment '{alignSpec}' in '{rawFormat}'. Expected >N, <N, or ^N");

        char direction = alignSpec[0];
        if (direction != '>' && direction != '<' && direction != '^')
            throw new FunnyRuntimeException($"Invalid alignment direction '{direction}' in '{rawFormat}'. Use > (right), < (left), or ^ (center)");

        if (!int.TryParse(alignSpec.Substring(1), out int width) || width <= 0)
            throw new FunnyRuntimeException($"Invalid alignment width in format '{rawFormat}'. Expected positive integer after {direction}");

        if (text.Length >= width) return text;

        return direction switch {
            '>' => text.PadLeft(width),
            '<' => text.PadRight(width),
            '^' => PadCenter(text, width),
            _ => text
        };
    }

    private static string PadCenter(string text, int width) {
        int totalPad = width - text.Length;
        int padLeft = totalPad / 2;
        return text.PadLeft(text.Length + padLeft).PadRight(width);
    }

    private static string ToBinary(object a) => a switch {
        byte v => Convert.ToString(v, 2),
        short v => Convert.ToString(v, 2),
        ushort v => Convert.ToString(v, 2),
        int v => Convert.ToString(v, 2),
        uint v => Convert.ToString((int)v, 2),
        long v => Convert.ToString(v, 2),
        ulong v => Convert.ToString((long)v, 2),
        _ => throw new FunnyRuntimeException($"Format 'bin' is only supported for integer types, got {a?.GetType().Name ?? "null"}")
    };
}

public class ConcatArrayOfTextsFunction : FunctionWithSingleArg {
    public ConcatArrayOfTextsFunction() : base(
        CoreFunNames.ConcatArrayOfTexts, FunnyType.Text,
        FunnyType.ArrayOf(FunnyType.Any)) {
    }

    public override object Calc(object a) {
        var sb = new StringBuilder();
        foreach (var subElement in (IFunnyArray)a)
        {
            sb.Append(TypeHelper.GetFunText(subElement));
        }

        return new TextFunnyArray(sb.ToString());
    }
}

public class Concat2TextsFunction : FunctionWithTwoArgs {
    public Concat2TextsFunction() : base(CoreFunNames.Concat2Texts, FunnyType.Text, FunnyType.Any, FunnyType.Any) { }

    public override object Calc(object a, object b)
        => new TextFunnyArray(TypeHelper.GetFunText(a) + TypeHelper.GetFunText(b));
}

public class Concat3TextsFunction : FunctionWithManyArguments {
    public Concat3TextsFunction() : base(
        CoreFunNames.Concat3Texts, FunnyType.Text, FunnyType.Any, FunnyType.Any,
        FunnyType.Any) {
    }

    public override object Calc(object[] args) {
        var sb = new StringBuilder();
        foreach (var subElement in args)
            sb.Append(TypeHelper.GetFunText(subElement));
        return new TextFunnyArray(sb.ToString());
    }
}

public class TrimFunction : FunctionWithSingleArg {
    public TrimFunction() : base("trim", FunnyType.Text, FunnyType.Text) { ArgProperties = FunArgProperty.FromNames("str"); }

    public override object Calc(object a) => ((IFunnyArray)a).ToText().Trim().AsFunText();
}

public class TrimStartFunction : FunctionWithSingleArg {
    public TrimStartFunction() : base("trimStart", FunnyType.Text, FunnyType.Text) { ArgProperties = FunArgProperty.FromNames("str"); }

    public override object Calc(object a) => ((IFunnyArray)a).ToText().TrimStart().AsFunText();
}

public class TrimEndFunction : FunctionWithSingleArg {
    public TrimEndFunction() : base("trimEnd", FunnyType.Text, FunnyType.Text) { ArgProperties = FunArgProperty.FromNames("str"); }
    public override object Calc(object a) => ((IFunnyArray)a).ToText().TrimEnd().AsFunText();
}

public class ToUpperFunction : FunctionWithSingleArg {
    public ToUpperFunction() : base("toUpper", FunnyType.Text, FunnyType.Text) { ArgProperties = FunArgProperty.FromNames("str"); }
    public override object Calc(object a) => ((IFunnyArray)a).ToText().ToUpper().AsFunText();
}

public class ToLowerFunction : FunctionWithSingleArg {
    public ToLowerFunction() : base("toLower", FunnyType.Text, FunnyType.Text) { ArgProperties = FunArgProperty.FromNames("str"); }
    public override object Calc(object a) => ((IFunnyArray)a).ToText().ToLower().AsFunText();
}

public class SplitFunction : FunctionWithTwoArgs {
    public SplitFunction() : base(
        "split",
        FunnyType.ArrayOf(FunnyType.Text),
        FunnyType.Text,
        FunnyType.Text) {
        ArgProperties = FunArgProperty.FromNames("str", "separator");
    }


    public override object Calc(object a, object b) {
        var inputString = TypeHelper.GetFunText(a);
        var delimiter = TypeHelper.GetFunText(b);

        if (string.IsNullOrEmpty(delimiter))
            return new ImmutableFunnyArray(
                inputString.SelectToArray(inputString.Length, c => new TextFunnyArray(c.ToString())), FunnyType.Text);

        return new ImmutableFunnyArray(
            inputString.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
                .SelectToArray(s => new TextFunnyArray(s)), FunnyType.Text);
    }
}

public class JoinFunction : FunctionWithTwoArgs {
    public JoinFunction() : base("join", FunnyType.Text, FunnyType.ArrayOf(FunnyType.Any), FunnyType.Text) { ArgProperties = FunArgProperty.FromNames("arr", "separator"); }

    public override object Calc(object a, object b) {
        var arr = (IFunnyArray)a;
        var separator = (IFunnyArray)b;
        var join = string.Join(separator.ToText(), arr.Select(TypeHelper.GetFunText));
        return new TextFunnyArray(join);
    }
}
