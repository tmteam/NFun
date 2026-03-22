using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class NonLatinIdentifiersTest {

    // ═══════════════════════════════════════════════════════════════
    // Cyrillic
    // ═══════════════════════════════════════════════════════════════

    [TestCase("сумма = 42", "сумма", 42)]
    [TestCase("имя = 'hello'", "имя", "hello")]
    [TestCase("Переменная = true", "Переменная", true)]
    public void Cyrillic_Variable(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    [TestCase("а = 1\r б = 2\r сумма = а + б", "сумма", 3)]
    public void Cyrillic_MultipleVariables(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    [TestCase("удвоить(x) = x * 2\r y = удвоить(5)", "y", 10)]
    public void Cyrillic_UserFunction(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ═══════════════════════════════════════════════════════════════
    // German / accented Latin
    // ═══════════════════════════════════════════════════════════════

    [TestCase("größe = 42", "größe", 42)]
    [TestCase("café = 'latte'", "café", "latte")]
    [TestCase("naïve = true", "naïve", true)]
    public void Accented_Variable(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ═══════════════════════════════════════════════════════════════
    // Greek
    // ═══════════════════════════════════════════════════════════════

    [TestCase("α = 3.14", "α", 3.14)]
    [TestCase("Ω = 100", "Ω", 100)]
    [TestCase("δ = α + 1", "δ", null)] // input α
    public void Greek_Variable(string expr, string id, object expected) {
        if (expected != null)
            expr.AssertResultHas(id, expected);
        else
            "α = 10\r δ = α + 1".Calc("α", 10).AssertResultHas("δ", 11);
    }

    // ═══════════════════════════════════════════════════════════════
    // CJK (Chinese, Japanese, Korean)
    // ═══════════════════════════════════════════════════════════════

    [TestCase("数量 = 100", "数量", 100)]
    [TestCase("名前 = 'Taro'", "名前", "Taro")]
    [TestCase("결과 = 42", "결과", 42)]
    public void CJK_Variable(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ═══════════════════════════════════════════════════════════════
    // Arabic / Hebrew
    // ═══════════════════════════════════════════════════════════════

    [TestCase("قيمة = 42", "قيمة", 42)]
    [TestCase("שם = 'test'", "שם", "test")]
    public void RTL_Variable(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ═══════════════════════════════════════════════════════════════
    // BMP Symbols (UnicodeCategory.OtherSymbol) — ★, ♠, →
    // ═══════════════════════════════════════════════════════════════

    [TestCase("★ = 5", "★", 5)]
    [TestCase("♠ = true", "♠", true)]
    public void BmpSymbol_Variable(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ═══════════════════════════════════════════════════════════════
    // Emoji (surrogate pairs, above U+FFFF)
    // ═══════════════════════════════════════════════════════════════

    [TestCase("🎉 = 42", "🎉", 42)]
    [TestCase("🚀 = 'launch'", "🚀", "launch")]
    public void Emoji_Variable(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    [Test]
    public void Emoji_InExpression() =>
        "🎉 = 10\r 🚀 = 🎉 + 5".Calc("🎉", 10).AssertResultHas("🚀", 15);

    [Test]
    public void Emoji_UserFunction() =>
        "🔥(x) = x * 2\r y = 🔥(5)".AssertResultHas("y", 10);

    // ═══════════════════════════════════════════════════════════════
    // Mixed scripts in one identifier
    // ═══════════════════════════════════════════════════════════════

    [TestCase("data_данные = 42", "data_данные", 42)]
    [TestCase("my変数 = 7", "my変数", 7)]
    [TestCase("α_beta = 3", "α_beta", 3)]
    public void MixedScript_Variable(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ═══════════════════════════════════════════════════════════════
    // Underscore + non-latin
    // ═══════════════════════════════════════════════════════════════

    [TestCase("_имя = 42", "_имя", 42)]
    [TestCase("_数 = 7", "_数", 7)]
    public void Underscore_NonLatin(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ═══════════════════════════════════════════════════════════════
    // Non-latin with digits
    // ═══════════════════════════════════════════════════════════════

    [TestCase("переменная1 = 42", "переменная1", 42)]
    [TestCase("α2 = 7", "α2", 7)]
    public void NonLatin_WithDigits(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ═══════════════════════════════════════════════════════════════
    // Emoji + other chars in continuation
    // ═══════════════════════════════════════════════════════════════

    [TestCase("player_⭐ = 100", "player_⭐", 100)]
    [TestCase("результат_🚀 = 42", "результат_🚀", 42)]
    public void Emoji_InContinuation(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ═══════════════════════════════════════════════════════════════
    // Keywords remain ASCII — non-latin lookalikes are valid identifiers
    // ═══════════════════════════════════════════════════════════════

    [TestCase("іf = 42", "іf", 42)]     // Cyrillic 'і' (U+0456), not Latin 'i'
    [TestCase("оr = 7", "оr", 7)]       // Cyrillic 'о' (U+043E), not Latin 'o'
    [TestCase("nоt = true", "nоt", true)] // Cyrillic 'о' in 'not'
    public void CyrillicLookalike_NotKeyword(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ═══════════════════════════════════════════════════════════════
    // Non-latin in user function with multiple args
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NonLatin_FunctionMultipleArgs() =>
        "сложить(а, б) = а + б\r y = сложить(3, 4)".AssertResultHas("y", 7);

    // ═══════════════════════════════════════════════════════════════
    // Non-latin in arrays and complex expressions
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NonLatin_InArrayExpression() =>
        "данные = [1,2,3]\r сумма = данные.sum()".AssertResultHas("сумма", 6);

    [Test]
    public void NonLatin_InIfExpression() =>
        "флаг = true\r y = if(флаг) 1 else 0".AssertResultHas("y", 1);

    // ═══════════════════════════════════════════════════════════════
    // Hidden multiplication with non-latin
    // Non-latin after number = implicit multiplication (same as latin)
    // ═══════════════════════════════════════════════════════════════

    [TestCase("2α", 2.0, 6.28)] // 2*α where α=3.14
    public void HiddenMultiplication_NonLatin(string expr, double x, double expected) =>
        ("α:real\r y = " + expr).Calc("α", 3.14).AssertReturns("y", expected);

    // ═══════════════════════════════════════════════════════════════
    // Case handling — NFun forbids identifiers differing only in case.
    // Cyrillic/Greek upper and lower are distinct tokens but
    // NFun treats them as case-insensitive and rejects case-only diffs.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void CaseInsensitive_CyrillicSameCase() =>
        "а = 1\r y = а + 1".AssertResultHas("y", 2);

    [Test]
    public void CaseInsensitive_GreekSameCase() =>
        "σ = 10\r y = σ + 1".AssertResultHas("y", 11);

    [TestCase("А = 1\r y = а")]  // Cyrillic А vs а
    [TestCase("Σ = 1\r y = σ")]  // Greek Σ vs σ
    public void CaseMismatch_NonLatin_Throws(string expr) =>
        expr.AssertObviousFailsOnParse();

    // ═══════════════════════════════════════════════════════════════
    // Struct fields with non-latin (if supported)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NonLatin_StructField() =>
        "obj = {имя = 'Alice', возраст = 30}\r y = obj.возраст".AssertResultHas("y", 30);

    // ═══════════════════════════════════════════════════════════════
    // Invalid identifiers — should NOT be valid identifier starts
    // ═══════════════════════════════════════════════════════════════

    [TestCase("$ = 42")]   // Sc (currency)
    [TestCase("€ = 42")]   // Sc (currency)
    [TestCase("£ = 42")]   // Sc (currency)
    [TestCase("+ = 42")]   // Sm (math)
    [TestCase("= = 42")]   // Sm (math)
    [TestCase("< = 42")]   // Sm (math)
    [TestCase("> = 42")]   // Sm (math)
    [TestCase("| = 42")]   // Sm (math)
    [TestCase("~ = 42")]   // Sm (math)
    [TestCase("% = 42")]   // Po (punctuation) / operator
    [TestCase(", = 42")]   // Po (punctuation)
    [TestCase(". = 42")]   // Po (punctuation)
    [TestCase("; = 42")]   // newline equivalent
    public void InvalidIdentifier_OperatorOrPunctuation(string expr) =>
        expr.AssertObviousFailsOnParse();
}
