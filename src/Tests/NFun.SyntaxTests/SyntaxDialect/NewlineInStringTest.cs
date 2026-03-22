using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.SyntaxDialect;

public class NewlineInStringTest {

    [TestCase("y='hello\\nworld'", "hello\nworld")]
    [TestCase("y='hello\\rworld'", "hello\rworld")]
    [TestCase("y='hello\\r\\nworld'", "hello\r\nworld")]
    [TestCase("y='\\n'", "\n")]
    [TestCase("y='\\r'", "\r")]
    [TestCase("y='no newlines here'", "no newlines here")]
    public void DenyNewline_EscapedNewlinesAreAllowed(string expr, string expected) =>
        expr.BuildWithDialect(allowNewlineInStrings: AllowNewlineInStrings.Deny)
            .Calc().AssertReturns(expected);

    [TestCase("y='hello\nworld'")]
    [TestCase("y='hello\rworld'")]
    [TestCase("y='hello\r\nworld'")]
    [TestCase("y=\"hello\nworld\"")]
    [TestCase("y='pre\n{42}after'")]
    public void DenyNewline_RawNewlineThrows(string expr) =>
        expr.AssertObviousFailsOnParse(allowNewlineInStrings: AllowNewlineInStrings.Deny);

    [TestCase("y='hello\nworld'", "hello\nworld")]
    [TestCase("y='hello\rworld'", "hello\rworld")]
    [TestCase("y=' \\r\r'", " \r\r")]
    [TestCase("y='\t \\n\n'", "\t \n\n")]
    public void AllowNewline_RawNewlinesWork(string expr, string expected) =>
        expr.BuildWithDialect(allowNewlineInStrings: AllowNewlineInStrings.Allow)
            .Calc().AssertReturns(expected);
}
