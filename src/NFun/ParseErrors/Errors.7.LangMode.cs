using NFun.Exceptions;
using NFun.Tokenization;

namespace NFun.ParseErrors;

internal static partial class Errors {

    // Lang-mode (indent-based) parsing errors: 800+

    internal static FunnyParseException IndentExpected(Tok current) => new(
        800, $"Indented block expected but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException DedentExpected(Tok current) => new(
        801, $"Unexpected indentation: '{ToText(current)}'", current.Interval);

    internal static FunnyParseException ColonExpectedAfterStatement(Tok current, string keyword) => new(
        802, $"':' expected after '{keyword}' statement but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException ForIteratorNameExpected(Tok current) => new(
        810, $"Iterator variable name expected after 'for' but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException ForInKeywordExpected(Tok current) => new(
        811, $"'in' keyword expected after iterator variable but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException ForCollectionExpected(Tok current) => new(
        812, $"Collection expression expected after 'in' but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException WhileConditionExpected(Tok current) => new(
        820, $"Condition expected after 'while' but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException WhenArmConditionExpected(Tok current) => new(
        830, $"When arm condition/value expected but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException WhenArmBodyExpected(Tok current) => new(
        831, $"When arm body expected but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException PrintExpressionExpected(Tok current) => new(
        840, $"Expression expected after 'print' but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException CatchParenthesisCbrExpected(Tok current) => new(
        850, $"')' expected after error variable name in catch but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException CatchErrorVariableExpected(Tok current) => new(
        851, $"Error variable name expected after 'catch(' but was '{ToText(current)}'", current.Interval);

    internal static FunnyParseException TryCatchOrAnywayExpected(int start, int finish) => new(
        852, "try block must have at least a 'catch' or 'anyway' clause", start, finish);
}
