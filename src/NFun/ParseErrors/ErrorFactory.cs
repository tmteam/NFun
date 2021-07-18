using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NFun.Exceptions;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic.Errors;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.ParseErrors
{
    internal static class ErrorFactory
    {
        #region 1xx read tokens

        internal static Exception QuoteAtEndOfString(char quoteSymbol, int start, int end) =>
            new FunParseException(110, $"Single '{quoteSymbol}' at end of string.",
                start, end);

        internal static Exception BackslashAtEndOfString(int start, int end)
            => new FunParseException(113, $"Single '\\' at end of string.", start, end);

        internal static Exception UnknownEscapeSequence(string sequence, int start, int end) =>
            new FunParseException(116, $"Unknown escape sequence \\{sequence}",
                start, end);

        internal static Exception ClosingQuoteIsMissed(char quoteSymbol, int start, int end)
            => new FunParseException(119, $"Closing {quoteSymbol} is missed at end of string",
                start, end);

        #endregion

        #region 2xx-3xx parsing

        private static readonly string Nl = Environment.NewLine;

        internal static Exception UnaryArgumentIsMissing(Tok operatorTok)
            => throw new FunParseException(201,
                $"{ErrorsHelper.ToText(operatorTok)} ???{Nl} right expression is missed{Nl} Example: {ErrorsHelper.ToText(operatorTok)} a",
                operatorTok.Interval);

        internal static Exception MinusDuplicates(Tok previousTok, Tok currentTok)
            => throw new FunParseException(204, $"'--' is not allowed",
                previousTok.Start, currentTok.Finish);

        internal static Exception LeftBinaryArgumentIsMissing(Tok token)
            => throw new FunParseException(207, $"expression is missed before '{ErrorsHelper.ToText(token)}'",
                token.Interval);

        internal static Exception RightBinaryArgumentIsMissing(ISyntaxNode leftNode, Tok @operator)
            => throw new FunParseException(210,
                $"{ErrorsHelper.ToShortText(leftNode)} {ErrorsHelper.ToText(@operator)} ???. Right expression is missed{Nl} Example: {ErrorsHelper.ToShortText(leftNode)} {ErrorsHelper.ToText(@operator)} e",
                leftNode.Interval.Start, @operator.Finish);

        internal static Exception OperatorIsUnknown(Tok token)
            => throw new FunParseException(213, $"operator '{ErrorsHelper.ToText(token)}' is unknown", token.Interval);

        internal static Exception NotAToken(Tok token)
            => throw new FunParseException(216, $"'{token.Value}' is not valid fun element. What did you mean?",
                token.Interval);

        internal static Exception FunctionOrStructMemberNameIsMissedAfterDot(Tok token)
            => throw new FunParseException(219, $"Function name expected after '.'{Nl} Example: [1,2].myFunction()",
                token.Interval);

        internal static Exception ArrayIndexOrSliceExpected(Tok openBraket)
            => new FunParseException(222, $"Array index or slice expected after '['{Nl} Example: a[1] or a[1:3:2]",
                openBraket.Interval);

        internal static Exception ArrayIndexExpected(Tok openBraket, Tok closeBracket)
            => new FunParseException(225, $"Array index expected inside of '[]'{Nl} Example: a[1]", openBraket.Start,
                closeBracket.Finish);

        internal static Exception ArrayInitializeSecondIndexMissed(Tok openBracket, Tok lastToken, Tok missedVal)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            if (string.IsNullOrWhiteSpace(missedVal?.Value))

                return new FunParseException(228,
                    $"'[x,..???]. Array hi bound expected but was nothing'{Nl} Example: a[1..2] or a[1..5..2]", start,
                    finish);
            else
                return new FunParseException(231,
                    $"'[x,..???]. Array hi bound expected but was {ErrorsHelper.ToText(missedVal)}'{Nl} Example: a[1..2] or a[1..5..2]",
                    start, finish);
        }

        internal static Exception ArrayInitializeStepMissed(Tok openBracket, Tok lastToken, Tok missedVal)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            if (string.IsNullOrWhiteSpace(missedVal?.Value))
                return new FunParseException(234,
                    $"'[x..y..???]. Array step expected but was nothing'{Nl} Example: a[1..5..2]", start,
                    finish);
            else
                return new FunParseException(237,
                    $"'[x..y..???]. Array step expected but was {ErrorsHelper.ToText(missedVal)}'{Nl} Example: a[1..5..2]",
                    start, finish);
        }

        internal static Exception ArrayIntervalInitializeCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(240,
                $"{(hasStep ? "[x..y..step ???]" : "[x..y ???]")}. ']' was missed'{Nl} Example: a[1..5..2]", start,
                finish);
        }

        internal static Exception ArrayIndexCbrMissed(Tok openBracket, Tok lastToken)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(243,
                $"a[x ??? <- was missed ']'{Nl} Example: a[1] or a[1:2] or a[1:5:2]", start,
                finish);
        }

        internal static Exception ArraySliceCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(246,
                $"a{(hasStep ? "[x:y:step]" : "[x:y]")} <- ']' was missed{Nl} Example: a[1:5:2]",
                start,
                finish);
        }

        internal static Exception ConditionIsMissing(int conditionStart, int end)
            => new FunParseException(249,
                $"if (???) {Nl} Condition expression is missing{Nl} Example: if (a>b)  ... ", conditionStart, end);

        internal static Exception ThenExpressionIsMissing(int conditionStart, int end)
            => new FunParseException(252, $"if (a)  ???.  Expression is missing{Nl} Example: if (a)  a+1 ",
                conditionStart, end);

        internal static Exception ElseKeywordIsMissing(int ifelseStart, int end)
            => new FunParseException(255, $"if (a) b ???.  Else keyword is missing{Nl} Example: if (a) b else c ",
                ifelseStart, end);

        internal static Exception ElseExpressionIsMissing(int ifelseStart, int end)
            => new FunParseException(258,
                $"if (a) b else ???.  Else expression is missing{Nl} Example: if (a) b else c ", ifelseStart, end);

        internal static Exception IfKeywordIsMissing(int ifelseStart, int end)
            => new FunParseException(261, $"if (a) b (if) ...  'if' is missing{Nl} Example: if (a) b if (c) d else c ",
                ifelseStart, end);


        internal static Exception IfConditionIsNotInBrackets(int ifelseStart, int end)
            => new FunParseException(264, $"If condition is not in brackets{Nl} Example: if (a) b  else c ",
                ifelseStart, end);

        internal static Exception NewLineMissedBeforeRepeatedIf(Interval interval)
            => new FunParseException(267,
                $"Not first if has to start from new line{Nl} Example: if (a) b {Nl} if(c) d  else e ", interval);


        internal static Exception FunctionCallObrMissed(int funStart, string name, int position, ISyntaxNode pipedVal)
        {
            if (pipedVal == null)
                return new FunParseException(270,
                    $"{name}( ???. Close bracket ')' is missed{Nl} Example: {name}()",
                    funStart,
                    position);

            return new FunParseException(273,
                $"{ErrorsHelper.ToShortText(pipedVal)}.{name}( ???. Close bracket ')' is missed{Nl} Example: {ErrorsHelper.ToShortText(pipedVal)}.{name}() or {name}({ErrorsHelper.ToShortText(pipedVal)})",
                funStart,
                position);
        }

        internal static Exception TypeExpectedButWas(Tok token)
            => new FunParseException(276, $"Expected: type, but was {ErrorsHelper.ToText(token)}", token.Interval);

        internal static Exception ArrTypeCbrMissed(Interval interval)
            => new FunParseException(279, $"']' is missed on array type", interval);

        internal static Exception BracketExpressionMissed(int start, int end, IList<ISyntaxNode> arguments)
        {
            var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
            return new FunParseException(282,
                $"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})",
                start, end);
        }

        internal static Exception ExpressionListMissed(int start, int end, IList<ISyntaxNode> arguments)
        {
            var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
            return new FunParseException(285,
                $"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})",
                start, end);
        }

        internal static Exception AttributeOnFunction(FunCallSyntaxNode lexNode)
            => new FunParseException(288, $"Function cannot has attributes.", lexNode.Interval);

        internal static Exception ItIsNotAnAttribute(int start, Tok flowCurrent)
            => new FunParseException(291, $"Attribute name expected, but was '{flowCurrent}'",
                start, flowCurrent.Finish);

        internal static Exception ItIsNotCorrectAttributeValue(Tok next)
            => new FunParseException(294, $"Attribute value 'text' or 'number' or 'boolean' expected, but was '{next}'",
                next.Interval);

        internal static Exception AttributeCbrMissed(int start, TokFlow flow)
            => new FunParseException(297, $"')' is missed but was '{flow.Current}'",
                start, flow.Current.Interval.Finish);

        internal static Exception NowNewLineAfterAttribute(int start, TokFlow flow)
            => new FunParseException(300, $"Attribute needs new line after it.",
                start, flow.Current.Interval.Finish);

        internal static Exception NowNewLineBeforeAttribute(TokFlow flow)
            => new FunParseException(303, $"Attribute has to start from new line.",
                flow.Current.Interval);

        #region 3xx - hi level parsing

        internal static Exception UnexpectedExpression(ISyntaxNode lexNode)
            => new FunParseException(306, $"Unexpected expression {ErrorsHelper.ToShortText(lexNode)}",
                lexNode.Interval);

        internal static Exception FunDefTokenIsMissed(string funName, List<TypedVarDefSyntaxNode> arguments, Tok actual)
        {
            return new FunParseException(309,
                $"{ErrorsHelper.Signature(funName, arguments)} ??? . '=' def symbol is skipped but was {ErrorsHelper.ToText(actual)}{Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = ...",
                actual.Start, actual.Finish);
        }

        internal static Exception FunExpressionIsMissed(string funName, List<TypedVarDefSyntaxNode> arguments,
            Interval interval)
            => new FunParseException(312,
                $"{ErrorsHelper.Signature(funName, arguments)} = ??? . Function body is missed {Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = #place your body here",
                interval);

        internal static Exception UnknownValueAtStartOfExpression(int exprStart, Tok flowCurrent)
            => new FunParseException(315,
                $"Unexpected symbol {ErrorsHelper.ToText(flowCurrent)}. Equation, anonymous equation, function or type definition expected.",
                exprStart, flowCurrent.Finish);

        internal static Exception ExpressionBeforeTheDefinition(int exprStart, ISyntaxNode expression, Tok flowCurrent)
            => new FunParseException(318,
                $"Unexpected expression {ErrorsHelper.ToShortText(expression)} before definition. Equation, anonymous equation, function or type definition expected.",
                exprStart, flowCurrent.Finish);

        internal static Exception FunctionDefinitionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode,
            Tok flowCurrent)
            => throw new FunParseException(321,
                $"Function definition has start from new line. {Nl}Example : y:int{Nl}m(x) = x+1", exprStart,
                flowCurrent.Finish);

        internal static Exception DefinitionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode, Tok flowCurrent)
            => throw new FunParseException(324,
                $"Definition has start from new line. {Nl}Example : y:int{Nl}j = y+1 #j = y:int+1", exprStart,
                flowCurrent.Finish);


        internal static Exception AnonymousExpressionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode,
            Tok flowCurrent)
            => throw new FunParseException(327,
                $"Anonymous equation should start from new line. {Nl}Example : y:int{Nl}y+1 #out = y:int+1", exprStart,
                flowCurrent.Finish);

        internal static Exception OnlyOneAnonymousExpressionAllowed(int exprStart, ISyntaxNode lexNode, Tok flowCurrent)
            => throw new FunParseException(330, $"Only one anonymous equation allowed", exprStart, flowCurrent.Finish);

        internal static Exception UnexpectedBracketsOnFunDefinition(FunCallSyntaxNode headNode, int start, int finish)
            => new FunParseException(333,
                $"Unexpected brackets on function definition ({headNode.Id}(...))=... {Nl}Example: {headNode.Id}(...)=...",
                start, finish);

        internal static Exception WrongFunctionArgumentDefinition(FunCallSyntaxNode headNode, ISyntaxNode headNodeChild)
        {
            var sb = ErrorsHelper.ToFailureFunString(headNode, headNodeChild);
            return new FunParseException(336,
                $"{headNode.Id}({sb}) = ... {Nl} Function argument is invalid. Variable name (with optional type) expected",
                headNodeChild.Interval);
        }

        internal static Exception FunctionArgumentInBracketDefinition(FunCallSyntaxNode headNode,
            ISyntaxNode headNodeChild,
            Tok flowCurrent)
        {
            if (flowCurrent == null) throw new ArgumentNullException(nameof(flowCurrent));
            var sb = ErrorsHelper.ToFailureFunString(headNode, headNodeChild);

            return new FunParseException(339,
                $"{headNode.Id}({sb}) = ... {Nl} Function argument is in bracket. Variable name (with optional type) without brackets expected",
                headNodeChild.Interval.Start, headNodeChild.Interval.Finish);
        }

        internal static Exception VarExpressionIsMissed(int start, string id, Tok flowCurrent)
            => new FunParseException(342, $"{id} = ??? . Equation body is missed {Nl}Example: {id} = {id}+1",
                start, flowCurrent.Finish);

        internal static Exception OutputNameWithDifferentCase(string id, Interval interval)
            => new FunParseException(345, $"{id}<-  output name is same to name  {id}", interval);

        internal static Exception InputNameWithDifferentCase(string id, string actualName, Interval interval)
            => new FunParseException(348, $"{actualName}<-  input name is same to name  {id}", interval);

        internal static Exception InterpolationExpressionIsMissing(ISyntaxNode lastNode)
            => new FunParseException(252, $"  Interpolation expression is missing{Nl} Example: 'before {{...}} after' ",
                lastNode.Interval);

        #endregion

        #region 4xx - errors of lists

        internal static Exception ArrayInitializeByListError(int openBracketTokenPos, TokFlow flow)
        {
            var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.ArrOBr, TokType.ArrCBr);
            var list = res.Parsed;
            var argStubs = ErrorsHelper.CreateArgumentsStub(list);
            switch (res.Type)
            {
                case ExprListErrorType.FirstElementMissed:
                    return new FunParseException(401,
                        $"[ ??? , ..] <- First element missed {Nl}Remove ',' or place element before it", res.Interval);
                case ExprListErrorType.ElementMissed:
                    return new FunParseException(404,
                        $"[{argStubs},???, ..] <- element missed {Nl}Remove ',' or place element before it",
                        res.Interval);
                case ExprListErrorType.TotalyWrongDefinition:
                    return new FunParseException(407, "Wrong array definition ", res.Interval);
                case ExprListErrorType.SingleOpenBracket:
                    return new FunParseException(410,
                        $"[ <- unexpected array symbol{Nl} Did you mean array initialization [,], slice [::] or indexing [i]?",
                        res.Interval);
                case ExprListErrorType.SepIsMissing:
                    return new FunParseException(413,
                        $"[{argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: [{argStubs}, myArgument, ...]",
                        res.Interval);
                case ExprListErrorType.ArgumentIsInvalid:
                    return new FunParseException(416,
                        $"[{argStubs}, ??? , ...  <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]",
                        res.Interval);
                case ExprListErrorType.CloseBracketIsMissing:
                    return new FunParseException(419,
                        $"[{argStubs} ??? <- Array close bracket ']' is missing{Nl} Example: [{argStubs}]",
                        res.Interval);
                case ExprListErrorType.LastArgumentIsInvalid:
                    return new FunParseException(422,
                        $"[{ErrorsHelper.CreateArgumentsStub(list.Take(list.Length - 1))} ??? ] <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]",
                        res.Interval);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        internal static Exception FunctionArgumentError(string id, int openBracketTokenPos, TokFlow flow)
        {
            var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.Obr, TokType.Cbr);
            var list = res.Parsed;
            var argStubs = ErrorsHelper.CreateArgumentsStub(list);
            switch (res.Type)
            {
                case ExprListErrorType.FirstElementMissed:
                    return new FunParseException(425,
                        $"{id}( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it",
                        res.Interval);
                case ExprListErrorType.ElementMissed:
                    return new FunParseException(428,
                        $"{id}({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it",
                        res.Interval);
                case ExprListErrorType.TotalyWrongDefinition:
                    return new FunParseException(431, "Wrong function call", res.Interval);
                case ExprListErrorType.SingleOpenBracket:
                    return new FunParseException(434,
                        $"( <- unexpected bracket{Nl} ?",
                        res.Interval);
                case ExprListErrorType.SepIsMissing:
                    return new FunParseException(437,
                        $"{id}({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: {id}({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.ArgumentIsInvalid:
                    return new FunParseException(440,
                        $"{id}({argStubs}, ??? , ...  <- Seems like function call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.CloseBracketIsMissing:
                    return new FunParseException(443,
                        $"{id}({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example: {id}({argStubs})",
                        res.Interval);
                case ExprListErrorType.LastArgumentIsInvalid:
                    return new FunParseException(446,
                        $"{id}({ErrorsHelper.CreateArgumentsStub(list.Take(list.Length - 1))} ??? ) <- Seems like call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)",
                        res.Interval);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static Exception BracketExpressionListError(int openBracketTokenPos, TokFlow flow)
        {
            var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.Obr, TokType.Cbr);
            var list = res.Parsed;
            var argStubs = ErrorsHelper.CreateArgumentsStub(list);
            switch (res.Type)
            {
                case ExprListErrorType.FirstElementMissed:
                    return new FunParseException(449,
                        $"( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it", res.Interval);
                case ExprListErrorType.ElementMissed:
                    return new FunParseException(452,
                        $"({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it",
                        res.Interval);
                case ExprListErrorType.TotalyWrongDefinition:
                    return new FunParseException(455, "Wrong expression", res.Interval);
                case ExprListErrorType.SingleOpenBracket:
                    return new FunParseException(458,
                        $"( <- unexpected bracket{Nl} ?",
                        res.Interval);
                case ExprListErrorType.SepIsMissing:
                    return new FunParseException(461,
                        $"({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: ({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.ArgumentIsInvalid:
                    return new FunParseException(464,
                        $"({argStubs}, ??? , ...  <- Seems like invalid expressions{Nl} Example: ({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.CloseBracketIsMissing:
                    return new FunParseException(467,
                        $"({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example:({argStubs})",
                        res.Interval);
                case ExprListErrorType.LastArgumentIsInvalid:
                    return new FunParseException(470,
                        $"({ErrorsHelper.CreateArgumentsStub(list.Take(list.Length - 1))} ??? ) <- Seems like invalid expression{Nl} Example: ({argStubs}, myArgument, ...)",
                        res.Interval);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region 5xx - Interpritation exceptions

        internal static Exception CycleEquationDependencies(EquationSyntaxNode[] result)
        {
            var expression = result.First().Expression;
            return new FunParseException(500, "Cycle dependencies found: "
                                              + string.Join("->", result.Select(r => r.Id)),
                expression.Interval);
        }

        internal static Exception NotAnExpression(ISyntaxNode node)
            => new FunParseException(503, $"{node} is not an expression", node.Interval);

        internal static Exception ImpossibleCast(FunnyType from, FunnyType to, Interval interval)
            => new FunParseException(506, $"Unable to cast from {from} to {to}", interval);

        internal static Exception InvalidArgTypeDefinition(ISyntaxNode argumentNode)
            => new FunParseException(509, ErrorsHelper.ToShortText(argumentNode) + " is  not valid fun arg",
                argumentNode.Interval);

        internal static Exception AnonymousFunDefinitionIsMissing(ISyntaxNode node)
            => new FunParseException(512, "Anonymous fun definition is missing", node.Interval);

        internal static Exception AnonymousFunBodyIsMissing(ISyntaxNode node)
            => new FunParseException(515, "Anonymous fun body is missing", node.Interval);

        internal static Exception AnonymousFunArgumentIsIncorrect(ISyntaxNode node)
            => new FunParseException(518, "Invalid anonymous fun argument", node.Interval);

        internal static Exception FunctionIsNotExists(ISyntaxNode node)
        {
            if (node is FunCallSyntaxNode fc)
                return new FunParseException(521, $"Unknown function {fc.Id}", fc.Interval);
            return new FunParseException(521, $"Unknown function", node?.Interval ?? Interval.Empty);
        }


        internal static Exception AmbiguousFunctionChoise(ISyntaxNode node)
        {
            if (node is FunCallSyntaxNode fc)

                return new FunParseException(522,
                    $"Several functions with name: {fc.Id} can be used in expression. Did you mean input variable instead of function?",
                    node.Interval);
            return new FunParseException(523, $"Ambiguous function call", node?.Interval ?? Interval.Empty);
        }

        internal static Exception FunctionNameAndVariableNameConflict(VariableUsages usages)
            => new FunParseException(524,
                $"Function with name: {usages.Source.Name} can be used in expression because it's name conflict with function that exists in scope. Declare input variable",
                usages.Source.TypeSpecificationIntervalOrNull??usages.Usages.FirstOrDefault()?.Interval??Interval.Empty);

        internal static Exception AmbiguousFunctionChoise(NamedIdSyntaxNode varName)
            => new FunParseException(526,
                $"Several functions with name: {varName.Id} can be used in expression. Did you mean input variable instead of function?",
                varName.Interval);

        internal static Exception ArrayInitializerTypeMismatch(FunnyType stepType, ISyntaxNode node)
            => new FunParseException(527,
                $"Array initializator step has to be int type only but was '{stepType}'. Example: [1..5..2]",
                node.Interval);

        internal static Exception CannotParseNumber(string val, Interval interval)
            => new FunParseException(530, $"Cannot parse number '{val}'", interval);

        internal static Exception FunctionOverloadNotFound(FunCallSyntaxNode node, IFunctionDictionary functions)
        {
            var candidates = functions.SearchAllFunctionsIgnoreCase(node.Id, node.Args.Length);
            StringBuilder msg =
                new StringBuilder(
                    $"Function '{node.Id}({string.Join(",", node.Args.Select(_ => "_"))})' is not found. ");
            if (candidates.Any())
            {
                var candidate = candidates.First();
                msg.Append(
                    $"\r\nDid you mean function '{TypeHelper.GetFunSignature(candidate.Name, candidate.ReturnType, candidate.ArgTypes)}' ?");
            }

            return new FunParseException(533, msg.ToString(), node.Interval);
        }

        internal static Exception UnknownVariables(IEnumerable<VariableExpressionNode> values)
        {
            if (values.Count() == 1)
                return new FunParseException(539, $"Unknown variable \"{values.First()}\"", values.First().Interval);
            return new FunParseException(542, $"Unknown variables \"{string.Join(", ", values)}\"",
                values.First().Interval);
        }

        internal static Exception FunctionAlreadyExist(UserFunctionDefinitionSyntaxNode userFun)
            => new FunParseException(545, $"Function  {ErrorsHelper.Signature(userFun.Id, userFun.Args)} already exist",
                new Interval(userFun.Head.Interval.Start, userFun.Body.Interval.Finish));

        internal static Exception InvalidOutputType(IFunctionSignature function, Interval interval)
            => new FunParseException(551,
                $"'{function.ReturnType}' is not supported as output parameter of {function.Name}()", interval);

        internal static Exception FunctionArgumentDuplicates(UserFunctionDefinitionSyntaxNode lexFunction,
            TypedVarDefSyntaxNode lexFunctionArg)
            => new FunParseException(554,
                $"'Argument name '{lexFunctionArg.Id}' duplicates at  {ErrorsHelper.Signature(lexFunction.Id, lexFunction.Args)} ",
                lexFunction.Head.Interval);

        internal static Exception AnonymousFunctionArgumentDuplicates(FunArgumentExpressionNode argNode,
            ISyntaxNode funDefinition)
            => new FunParseException(557, $"'Argument name '{argNode.Name}' of anonymous fun duplicates ",
                argNode.Interval);

        internal static Exception AnonymousFunctionArgumentDuplicates(NamedIdSyntaxNode argNode,
            ISyntaxNode funDefinition)
            => new FunParseException(560, $"'Argument name '{argNode.Id}' of anonymous fun duplicates ",
                argNode.Interval);

        internal static Exception AnonymousFunctionArgumentDuplicates(TypedVarDefSyntaxNode argNode,
            ISyntaxNode funDefinition)
            => new FunParseException(563, $"'Argument '{argNode.Id}:{argNode.FunnyType}' of anonymous fun duplicates ",
                argNode.Interval);

        internal static Exception AnonymousFunctionArgumentConflictsWithOuterScope(string argName, Interval defInterval)
            => new FunParseException(566,
                $"'Argument name '{argName}' of anonymous fun conflicts with outer scope variable. It is denied for your safety.",
                defInterval);

        internal static Exception AnonymousFunDefinitionIsIncorrect(ArrowAnonymFunctionSyntaxNode arrowAnonymFunNode)
            => new FunParseException(569, $"'Anonym fun definition is incorrect ", arrowAnonymFunNode.Interval);

        internal static Exception ComplexRecursion(UserFunctionDefinitionSyntaxNode[] functionSolveOrder)
        {
            var callOrder = string.Join("->", functionSolveOrder.Select(s => s.Id + "(..)"));
            return new FunParseException(572, $"Complex recursion found: {callOrder} ",
                functionSolveOrder.First().Interval);
        }

        #endregion

        #region InvalidFluentUsage

        internal static FunParseException UnknownInputs(IEnumerable<VariableUsages> variableUsage) =>
            new(605, "Some inputs are unknown", Interval.Empty);

        internal static FunParseException NoOutputVariablesSetted(
            Memory<(string, IOutputFunnyConverter, PropertyInfo)> expectedOutputs)
            => new(609, "No output values were setted", Interval.Empty);

        internal static FunParseException OutputIsUnset(FunnyType expectedOutputType)
            => new(612,
                $"Output is not set. Anonymous of type '{expectedOutputType.ToString()}' equation or '{Parser.AnonymousEquationId}' variable expected"
                , Interval.Empty);

        internal static FunParseException OutputIsUnset()
            => new(615,
                $"Output is not set. Anonymous equation or '{Parser.AnonymousEquationId}' variable expected", Interval
                    .Empty);

        internal static FunParseException TypeCannotBeUsedAsOutputNfunType(FunnyType funnyType)
            => new(618, $"type {funnyType} is not supported for dynamic convertion", Interval.Empty);

        #endregion

        internal static Exception VariousIfElementTypes(IfThenElseSyntaxNode ifThenElse)
        {
            var allExpressions = ifThenElse.Ifs
                .Select(i => i.Expression)
                .Append(ifThenElse.ElseExpr)
                .ToArray();

            //Search first failed interval
            Interval failedInterval = ifThenElse.Interval;

            //Lca defined only in TI. It is kind of hack
            var hmTypes = allExpressions.Select(a => a.OutputType.ConvertToTiType()).ToArray();

            return new FunParseException(575, $"'If-else expressions contains different type. " +
                                              $"Specify toAny() cast if the result should be of 'any' type. " +
                                              $"Actual types: {string.Join(",", hmTypes.Select(m => m.Description))}",
                failedInterval);
        }

        internal static Exception VariousArrayElementTypes(ArraySyntaxNode arraySyntaxNode)
        {
            return new FunParseException(578, $"'Various array element types. " +
                                              $"{arraySyntaxNode.OutputType} = [{string.Join(",", arraySyntaxNode.Expressions.Select(e=>e.OutputType))}]", arraySyntaxNode.Interval);
        }

        internal static Exception VariousArrayElementTypes(ISyntaxNode failedArrayElement)
        {
            return new FunParseException(581, $"'Various array element types", failedArrayElement.Interval);
        }

        internal static Exception VariousArrayElementTypes(IExpressionNode[] elements, int failureIndex)
        {
            var firstType = elements[0].Type;
            var failureType = elements[failureIndex].Type;
            return new FunParseException(584, $"'Not equal array element types: {firstType} and {failureType}",
                new Interval(elements[failureIndex - 1].Interval.Start, elements[failureIndex].Interval.Finish));
        }

        internal static Exception CannotUseOutputValueBeforeItIsDeclared(VariableUsages usages)
        {
            var interval = (usages.Usages.FirstOrDefault()?.Interval)
                           ?? usages.Source.TypeSpecificationIntervalOrNull
                           ?? new Interval();

            return new FunParseException(587, $"Cannot use output value '{usages.Source.Name}' before it is declared'",
                interval);
        }

        internal static Exception VariableIsDeclaredAfterUsing(VariableUsages usages)
            => new FunParseException(590, $"Variable '{usages.Source.Name}' used before it is declared'",
                usages.Usages.First().Interval);

        #endregion

        #region typeSolving

        internal static Exception TypesNotSolved(ISyntaxNode syntaxNode)
            => new FunParseException(600, $"Types cannot be solved ", syntaxNode.Interval);

        internal static Exception FunctionTypesNotSolved(UserFunctionDefinitionSyntaxNode node)
            => new FunParseException(603,
                $"Function {node.GetFunAlias()} has invalid arguments or output type. Check function body expression",
                new Interval(node.Head.Interval.Start, node.Body.Interval.Start));

        internal static Exception OutputDefinitionDuplicates(EquationSyntaxNode node)
            => new FunParseException(606, $"Output variable {node.Id} definition duplicates", node.Interval);

        internal static Exception OutputDefinitionTypeIsNotSolved(EquationSyntaxNode node)
            => new FunParseException(609, $"Output variable '{node.Id}' type is incorrect", node.Interval);

        #endregion


        internal static Exception TranslateTicError(TicException ticException, ISyntaxNode syntaxNodeToSearch)
        {
            if (ticException is IncompatibleAncestorSyntaxNodeException syntaxNodeEx)
            {
                var concreteNode =
                    SyntaxTreeDeepFieldSearch.FindNodeByOrderNumOrNull(syntaxNodeToSearch, syntaxNodeEx.SyntaxNodeId);
                if (concreteNode != null)
                    return new FunParseException(601, $"Types cannot be solved: {ticException.Message} ",
                        concreteNode.Interval);
            }
            else if (ticException is IncompatibleAncestorNamedNodeException namedNodeEx)
            {
                var concreteNode =
                    SyntaxTreeDeepFieldSearch.FindVarDefinitionOrNull(syntaxNodeToSearch, namedNodeEx.NodeName);
                if (concreteNode != null)
                    return new FunParseException(602, $"Types cannot be solved: {ticException.Message} ",
                        concreteNode.Interval);
            }
            else if (ticException is RecursiveTypeDefinitionException e)
            {
                foreach (var nodeName in e.NodeNames)
                {
                    var concreteNode =
                        SyntaxTreeDeepFieldSearch.FindVarDefinitionOrNull(syntaxNodeToSearch, nodeName);
                    if (concreteNode != null)
                    {
                        return new FunParseException(603,
                            $"Recursive type definition: {string.Join("->", e.NodeNames)} ", concreteNode.Interval);
                    }
                }

                foreach (var nodeId in e.NodeIds)
                {
                    var concreteNode = SyntaxTreeDeepFieldSearch.FindNodeByOrderNumOrNull(syntaxNodeToSearch, nodeId);
                    if (concreteNode != null)
                        return new FunParseException(603, $"Recursive type definition detected", concreteNode.Interval);
                }
            }

            return TypesNotSolved(syntaxNodeToSearch);
        }


        internal static Exception UndoneAnonymousFunction(int anonymousStart, int anonymousFinish)
            => FunParseException.ErrorStubToDo("SuperAnonymousFunctionIsNotClose");

        internal static Exception VariableIsAlreadyDeclared(string nodeId, Interval nodeInterval)
            => FunParseException.ErrorStubToDo($"Variable {nodeId} is already declared");
    }
}