using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Functions;

/// <summary>print(msg: any) -> none — prints msg + newline. Arity [1].</summary>
public class PrintFunction : FunctionWithSingleArg {
    public static readonly PrintFunction Instance = new();
    private PrintFunction() : base("print", FunnyType.Any, FunnyType.Any) {
        ArgProperties = FunArgProperty.FromNames("msg");
    }

    public override object Calc(object a) {
        var text = TypeHelper.GetFunText(a);
        FunnyIO.ActiveOutput.WriteLine(text);
        return FunnyNone.Instance;
    }
}

/// <summary>print(msg: any, end: text) -> none — prints msg + custom ending. Arity [2].</summary>
public class PrintWithEndFunction : FunctionWithTwoArgs {
    public static readonly PrintWithEndFunction Instance = new();
    private PrintWithEndFunction() : base("print", FunnyType.Any, FunnyType.Any, FunnyType.Text) {
        ArgProperties = FunArgProperty.FromNames("msg", "end");
    }

    public override object Calc(object a, object b) {
        var text = TypeHelper.GetFunText(a);
        var end = TypeHelper.GetFunText(b);
        FunnyIO.ActiveOutput.Write(text);
        FunnyIO.ActiveOutput.Write(end);
        return FunnyNone.Instance;
    }
}
