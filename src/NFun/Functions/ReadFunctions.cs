using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions;

/// <summary>readLine() -> text — reads a line from input. Arity [0].</summary>
public class ReadLineFunction : FunctionWithManyArguments {
    public static readonly ReadLineFunction Instance = new();
    private ReadLineFunction() : base("readLine", FunnyType.Text) {
        ArgTypes = System.Array.Empty<FunnyType>();
    }

    public override object Calc(object[] args) {
        var line = FunnyIO.ActiveInput.ReadLine() ?? "";
        return new TextFunnyArray(line);
    }

    public override IConcreteFunction Clone(Interpretation.Nodes.ICloneContext context) => this;
}

/// <summary>readChar() -> char — reads a single character from input. Arity [0].</summary>
public class ReadCharFunction : FunctionWithManyArguments {
    public static readonly ReadCharFunction Instance = new();
    private ReadCharFunction() : base("readChar", FunnyType.Char) {
        ArgTypes = System.Array.Empty<FunnyType>();
    }

    public override object Calc(object[] args) {
        var ch = FunnyIO.ActiveInput.Read();
        return ch < 0 ? '\0' : (char)ch;
    }

    public override IConcreteFunction Clone(Interpretation.Nodes.ICloneContext context) => this;
}
