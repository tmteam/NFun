using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class NonLatinIdentifiersTest {

    // ── Variable declarations across alphabets ─────────────────────────

    // Cyrillic
    [TestCase("сумма = 42", "сумма", 42)]
    [TestCase("имя = 'hello'", "имя", "hello")]
    [TestCase("Переменная = true", "Переменная", true)]
    // German / accented Latin
    [TestCase("größe = 42", "größe", 42)]
    [TestCase("café = 'latte'", "café", "latte")]
    [TestCase("naïve = true", "naïve", true)]
    // Greek
    [TestCase("α = 3.14", "α", 3.14)]
    [TestCase("Ω = 100", "Ω", 100)]
    // CJK
    [TestCase("数量 = 100", "数量", 100)]
    [TestCase("名前 = 'Taro'", "名前", "Taro")]
    [TestCase("결과 = 42", "결과", 42)]
    // RTL (Arabic / Hebrew)
    [TestCase("قيمة = 42", "قيمة", 42)]
    [TestCase("שם = 'test'", "שם", "test")]
    // BMP symbols (★, ♠)
    [TestCase("★ = 5", "★", 5)]
    [TestCase("♠ = true", "♠", true)]
    // Emoji (surrogate pairs above U+FFFF)
    [TestCase("🎉 = 42", "🎉", 42)]
    [TestCase("🚀 = 'launch'", "🚀", "launch")]
    // Mixed scripts in one identifier
    [TestCase("data_данные = 42", "data_данные", 42)]
    [TestCase("my変数 = 7", "my変数", 7)]
    [TestCase("α_beta = 3", "α_beta", 3)]
    // Underscore + non-latin
    [TestCase("_имя = 42", "_имя", 42)]
    [TestCase("_数 = 7", "_数", 7)]
    // Non-latin with digits
    [TestCase("переменная1 = 42", "переменная1", 42)]
    [TestCase("α2 = 7", "α2", 7)]
    // Emoji in continuation
    [TestCase("player_⭐ = 100", "player_⭐", 100)]
    [TestCase("результат_🚀 = 42", "результат_🚀", 42)]
    // Cyrillic lookalikes — keywords stay ASCII
    [TestCase("іf = 42", "іf", 42)]      // Cyrillic 'і' (U+0456), not Latin 'i'
    [TestCase("оr = 7", "оr", 7)]        // Cyrillic 'о' (U+043E), not Latin 'o'
    [TestCase("nоt = true", "nоt", true)] // Cyrillic 'о' in 'not'
    public void NonLatin_Variable(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ── Multi-line and structural usage ───────────────────────────────

    [TestCase("а = 1\r б = 2\r сумма = а + б", "сумма", 3)]
    [TestCase("удвоить(x) = x * 2\r y = удвоить(5)", "y", 10)]
    [TestCase("сложить(а, б) = а + б\r y = сложить(3, 4)", "y", 7)]
    [TestCase("🔥(x) = x * 2\r y = 🔥(5)", "y", 10)]
    [TestCase("данные = [1,2,3]\r сумма = данные.sum()", "сумма", 6)]
    [TestCase("флаг = true\r y = if(флаг) 1 else 0", "y", 1)]
    [TestCase("obj = {имя = 'Alice', возраст = 30}\r y = obj.возраст", "y", 30)]
    [TestCase("а = 1\r y = а + 1", "y", 2)]
    [TestCase("σ = 10\r y = σ + 1", "y", 11)]
    public void NonLatin_MultilineScript(string expr, string id, object expected) =>
        expr.AssertResultHas(id, expected);

    // ── Greek/Cyrillic via input variable (single-script case) ────────

    [Test]
    public void Greek_VariableInExpression()
        => "α = 10\r δ = α + 1".Calc("α", 10).AssertResultHas("δ", 11);

    [Test]
    public void Emoji_InExpression()
        => "🎉 = 10\r 🚀 = 🎉 + 5".Calc("🎉", 10).AssertResultHas("🚀", 15);

    // ── Hidden multiplication with non-latin (2α = 2 * α) ─────────────

    [TestCase("2α", 2.0, 6.28)] // 2*α where α=3.14
    public void HiddenMultiplication_NonLatin(string expr, double x, double expected) =>
        ("α:real\r y = " + expr).Calc("α", 3.14).AssertReturns("y", expected);

    // ── Case-only mismatch is rejected (NFun is case-insensitive) ─────

    [TestCase("А = 1\r y = а")]  // Cyrillic А vs а
    [TestCase("Σ = 1\r y = σ")]  // Greek Σ vs σ
    public void CaseMismatch_NonLatin_Throws(string expr) =>
        expr.AssertObviousFailsOnParse();

    // ── Operators / punctuation / currency are NOT valid identifier starts ──

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
