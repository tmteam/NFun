using NFun.Exceptions;
using NFun.Tokenization;

namespace NFun.ParseErrors; 

internal static partial class Errors {
    internal static FunnyParseException NewLineMissedBeforeRepeatedIf(Interval interval) => new(
        210, $"Not first if has to start from new line{Nl} Example: if (a) b {Nl} if(c) d  else e ", interval);

    internal static FunnyParseException IfElseExpressionIsDenied(Interval interval) => new(
        216, "If-else expressions are denied for the dialect", interval);

    internal static FunnyParseException StructFieldSpecificationIsNotSupportedYet(Interval interval) => new(
        222, $"Struct field type specification is not supported yet", interval);
}