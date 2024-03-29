using System;
using NFun.Exceptions;
using NFun.SyntaxParsing;
using NFun.Tokenization;

namespace NFun.ParseErrors; 

internal static partial class Errors {

    internal static FunnyParseException UnknownInputs() => new(
        910, "Some inputs are unknown", Interval.Empty);

    internal static FunnyParseException NoOutputVariablesSetted(Memory<OutputProperty> expectedOutputs) => new(
        913, "No output values were set", Interval.Empty);

    internal static FunnyParseException OutputIsUnset() => new(
        916, $"Output is not set. Anonymous equation or '{Parser.AnonymousEquationId}' variable expected", Interval.Empty);

    internal static FunnyParseException TypeCannotBeUsedAsOutputNfunType(FunnyType funnyType) => new(
        919, $"type {funnyType} is not supported for dynamic conversion", Interval.Empty);
}