using System;
using System.Collections.Generic;
using System.Reflection;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.ParseErrors {

internal static partial class Errors {

    internal static FunnyParseException UnknownInputs(IEnumerable<VariableUsages> variableUsage) => new(
        910, "Some inputs are unknown", Interval.Empty);

    internal static FunnyParseException NoOutputVariablesSetted(Memory<OutputProperty> expectedOutputs) => new(
        913, "No output values were setted", Interval.Empty);

    internal static FunnyParseException OutputIsUnset() => new(
        916, $"Output is not set. Anonymous equation or '{Parser.AnonymousEquationId}' variable expected", Interval.Empty);

    internal static FunnyParseException TypeCannotBeUsedAsOutputNfunType(FunnyType funnyType) => new(
        919, $"type {funnyType} is not supported for dynamic convertion", Interval.Empty);
}

}