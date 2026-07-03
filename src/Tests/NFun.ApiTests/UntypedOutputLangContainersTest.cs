using NFun;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests;

/// <summary>
/// The untyped Api output path (DynamicTypeOutputFunnyConverter.AnyConverter) must
/// convert lang-mode runtime containers to CLR values. Regression: its switch knew
/// TextFunnyArray/IFunnyArray/FunnyStruct only, so an ee-mode `map` result
/// (FixedFunnyArray after the Enumerable-LINQ migration) leaked RAW into the output:
/// `ids.map(rule it.toText())` returned a funny array of char-arrays instead of
/// string[] (found by NFun.ConcurrentTests once they were finally run).
/// </summary>
public class UntypedOutputLangContainersTest {

    [Test]
    public void UntypedCalc_MapToText_ReturnsClrStrings() {
        var input = new UserInputModel("vasa", 13, size: 21, balance: decimal.Zero, iq: 1, 1, 2, 101, 102);
        var result = Funny.CalcNonGeneric("ids.map(rule it.toText())", input);
        Assert.IsInstanceOf<string[]>(result, $"got {result.GetType()}: {result.ToStringSmart()}");
        CollectionAssert.AreEqual(new[] { "1", "2", "101", "102" }, (string[])result);
    }

    [Test]
    public void UntypedCalc_MapToSquare_ReturnsClrInts() {
        var input = new UserInputModel("vasa", 13, size: 21, balance: decimal.Zero, iq: 1, 1, 2, 3, 4);
        var result = Funny.CalcNonGeneric("ids.map(rule it*it)", input);
        Assert.IsInstanceOf<int[]>(result, $"got {result.GetType()}");
        CollectionAssert.AreEqual(new[] { 1, 4, 9, 16 }, (int[])result);
    }
}
