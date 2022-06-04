using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.UnitTests; 

public class FunctionDictionaryTest {
    //todo FunDicTests
}

class PapaFunction : FunctionWithManyArguments {
    public const string PapaReturn = "papa is here";
    public PapaFunction(string name) : base(name, FunnyType.Text) { }

    public override object Calc(object[] args) => PapaReturn;
}

class MamaFunction : FunctionWithManyArguments {
    public const string MamaReturn = "mama called";

    public MamaFunction(string name) : base(name, FunnyType.Text) { }

    public override object Calc(object[] args) => MamaReturn;
}