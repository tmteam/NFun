using System;
using System.Linq;
using System.Text;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions {

public class ToTextFunction : FunctionWithSingleArg {
    public ToTextFunction() : base(CoreFunNames.ToText, FunnyType.Text, FunnyType.Any) { }

    public override object Calc(object a) => new TextFunnyArray(TypeHelper.GetFunText(a));
}

public class ConcatArrayOfTextsFunction : FunctionWithSingleArg {
    public ConcatArrayOfTextsFunction() : base(
        CoreFunNames.ConcatArrayOfTexts, FunnyType.Text,
        FunnyType.ArrayOf(FunnyType.Any)) { }

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
        FunnyType.Any) { }

    public override object Calc(object[] args) {
        var sb = new StringBuilder();
        foreach (var subElement in args)
            sb.Append(TypeHelper.GetFunText(subElement));
        return new TextFunnyArray(sb.ToString());
    }
}

public class FormatTextFunction : FunctionWithManyArguments {
    public FormatTextFunction() : base("format", FunnyType.Text, FunnyType.Text, FunnyType.ArrayOf(FunnyType.Any)) { }

    public override object Calc(object[] args) {
        var template = ((IFunnyArray)args[0]).ToText();
        var formatArguments = (IFunnyArray)args[1];
        var result = string.Format(template, formatArguments);
        return new TextFunnyArray(result);
    }
}

public class TrimFunction : FunctionWithSingleArg {
    public TrimFunction() : base("trim", FunnyType.Text, FunnyType.Text) { }

    public override object Calc(object a) => ((IFunnyArray)a).ToText().Trim().AsFunText();
}

public class TrimStartFunction : FunctionWithSingleArg {
    public TrimStartFunction() : base("trimStart", FunnyType.Text, FunnyType.Text) { }

    public override object Calc(object a) => ((IFunnyArray)a).ToText().TrimStart().AsFunText();
}

public class TrimEndFunction : FunctionWithSingleArg {
    public TrimEndFunction() : base("trimEnd", FunnyType.Text, FunnyType.Text) { }
    public override object Calc(object a) => ((IFunnyArray)a).ToText().TrimEnd().AsFunText();
}

public class ToUpperFunction : FunctionWithSingleArg {
    public ToUpperFunction() : base("toUpper", FunnyType.Text, FunnyType.Text) { }
    public override object Calc(object a) => ((IFunnyArray)a).ToText().ToUpper().AsFunText();
}

public class ToLowerFunction : FunctionWithSingleArg {
    public ToLowerFunction() : base("toLower", FunnyType.Text, FunnyType.Text) { }
    public override object Calc(object a) => ((IFunnyArray)a).ToText().ToLower().AsFunText();
}


public class SplitFunction : FunctionWithTwoArgs {
    public SplitFunction() : base(
        "split",
        FunnyType.ArrayOf(FunnyType.Text),
        FunnyType.Text,
        FunnyType.Text) { }


    public override object Calc(object a, object b) {
        var inputString = TypeHelper.GetFunText(a);
        var delimeter = TypeHelper.GetFunText(b);
        return new ImmutableFunnyArray(
            inputString.Split(new[] { delimeter }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => new TextFunnyArray(s))
                       .ToArray(), FunnyType.Text);
    }
}


public class JoinFunction : FunctionWithTwoArgs {
    public JoinFunction() : base("join", FunnyType.Text, FunnyType.ArrayOf(FunnyType.Any), FunnyType.Text) { }

    public override object Calc(object a, object b) {
        var arr = (IFunnyArray)a;
        var separator = (IFunnyArray)b;
        var join = string.Join(separator.ToText(), arr.Select(TypeHelper.GetFunText));
        return new TextFunnyArray(join);
    }
}

}