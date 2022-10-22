using NFun.Exceptions;
using NFun.Tokenization;

namespace NFun.ParseErrors; 

internal partial class Errors {

    internal static FunnyParseException QuoteAtEndOfString(char quoteSymbol, int start, int end) => new(
        110, $"Single '{quoteSymbol}' at end of string.", start, end);

    internal static FunnyParseException BackslashAtEndOfText(int start, int end) => new(
        113, "Single '\\' at end of text.", start, end);

    internal static FunnyParseException UnknownEscapeSequence(string sequence, int start, int end) => new(
        116, $"Unknown escape sequence \\{sequence}", start, end);

    internal static FunnyParseException ClosingQuoteIsMissed(char quoteSymbol, int start, int end) => new(
        119, $"Closing {quoteSymbol} is missed at end of string", start, end);

    internal static FunnyParseException TokenIsReserved(Interval interval, string word, string wordToReplace) => new(
        121, $"Symbol '{word}' is outdated but reserved for future use. Use '{wordToReplace}' instead of '{word}'", interval);
    
    internal static FunnyParseException TokenIsReserved(Interval interval, string word) => new(
        122, $"Symbol '{word}' is reserved for future use and cannot be used in script", interval);

    internal static FunnyParseException UnaryArgumentIsMissing(Tok operatorTok) => new(
        125, $"{ToText(operatorTok)} ???{Nl} right expression is missed{Nl} Example: {ToText(operatorTok)} a",
        operatorTok.Interval);

    internal static FunnyParseException MinusDuplicates(Tok previousTok, Tok currentTok) => new(
        128, "'--' is not allowed", previousTok.Start, currentTok.Finish);

    internal static FunnyParseException OperatorIsUnknown(Tok token) => new(
        131, $"operator '{ToText(token)}' is unknown", token.Interval);

    internal static FunnyParseException NotAToken(Tok token) => new(
        133, $"'{token.Value}' is not valid fun element. What did you mean?", token.Interval);

    internal static FunnyParseException NumberOverflow(Interval interval, FunnyType type) => new(
        136, $"{type} overflow", interval);

    internal static FunnyParseException CannotParseDecimalNumber(Interval interval) => new(
        139, "Cannot parse decimal number", interval);

    internal static FunnyParseException InvalidIpAddress(Tok token) => new(
        142, $"'{token.Value}' is not valid ip address", token.Interval);
}