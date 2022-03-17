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

namespace NFun.ParseErrors {

internal static class Errors {
    #region 1xx read tokens

    internal static Exception QuoteAtEndOfString(char quoteSymbol, int start, int end) =>
        new FunnyParseException(
            110, $"Single '{quoteSymbol}' at end of string.",
            start, end);

    internal static Exception BackslashAtEndOfString(int start, int end)
        => new FunnyParseException(113, $"Single '\\' at end of string.", start, end);

    internal static Exception UnknownEscapeSequence(string sequence, int start, int end) =>
        new FunnyParseException(
            116, $"Unknown escape sequence \\{sequence}",
            start, end);

    internal static Exception ClosingQuoteIsMissed(char quoteSymbol, int start, int end)
        => new FunnyParseException(
            119, $"Closing {quoteSymbol} is missed at end of string",
            start, end);

    #endregion
    
    #region 2xx-3xx parsing

    private static readonly string Nl = Environment.NewLine;

    internal static Exception UnaryArgumentIsMissing(Tok operatorTok)
        => throw new FunnyParseException(
            201,
            $"{ErrorsHelper.ToText(operatorTok)} ???{Nl} right expression is missed{Nl} Example: {ErrorsHelper.ToText(operatorTok)} a",
            operatorTok.Interval);

    internal static Exception MinusDuplicates(Tok previousTok, Tok currentTok)
        => throw new FunnyParseException(
            204, $"'--' is not allowed",
            previousTok.Start, currentTok.Finish);

    internal static Exception LeftBinaryArgumentIsMissing(Tok token)
        => throw new FunnyParseException(
            207, $"expression is missed before '{ErrorsHelper.ToText(token)}'",
            token.Interval);

    internal static Exception RightBinaryArgumentIsMissing(ISyntaxNode leftNode, Tok @operator)
        => throw new FunnyParseException(
            210,
            $"{ErrorsHelper.ToShortText(leftNode)} {ErrorsHelper.ToText(@operator)} ???. Right expression is missed{Nl} Example: {ErrorsHelper.ToShortText(leftNode)} {ErrorsHelper.ToText(@operator)} e",
            leftNode.Interval.Start, @operator.Finish);

    internal static Exception OperatorIsUnknown(Tok token)
        => throw new FunnyParseException(
            213, $"operator '{ErrorsHelper.ToText(token)}' is unknown",
            token.Interval);

    internal static Exception NotAToken(Tok token)
        => throw new FunnyParseException(
            216, $"'{token.Value}' is not valid fun element. What did you mean?",
            token.Interval);

    internal static Exception FunctionOrStructMemberNameIsMissedAfterDot(Tok token)
        => throw new FunnyParseException(
            219, $"Function name expected after '.'{Nl} Example: [1,2].myFunction()",
            token.Interval);

    internal static Exception ArrayIndexOrSliceExpected(Tok openBraket)
        => new FunnyParseException(
            222, $"Array index or slice expected after '['{Nl} Example: a[1] or a[1:3:2]",
            openBraket.Interval);

    internal static Exception ArrayIndexExpected(Tok openBraket, Tok closeBracket)
        => new FunnyParseException(
            225, $"Array index expected inside of '[]'{Nl} Example: a[1]", openBraket.Start,
            closeBracket.Finish);

    internal static Exception ArrayInitializeSecondIndexMissed(Tok openBracket, Tok lastToken, Tok missedVal) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        if (string.IsNullOrWhiteSpace(missedVal?.Value))

            return new FunnyParseException(
                228,
                $"'[x,..???]. Array hi bound expected but was nothing'{Nl} Example: a[1..2] or a[1..5 step 2]", start,
                finish);
        else
            return new FunnyParseException(
                231,
                $"'[x,..???]. Array hi bound expected but was {ErrorsHelper.ToText(missedVal)}'{Nl} Example: a[1..2] or a[1..5 step 2]",
                start, finish);
    }

    internal static Exception ArrayInitializeStepMissed(Tok openBracket, Tok lastToken, Tok missedVal) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        if (string.IsNullOrWhiteSpace(missedVal?.Value))
            return new FunnyParseException(
                234,
                $"'[x..y step ???]. Array step expected but was nothing'{Nl} Example: a[1..5 step 2]", start,
                finish);
        else
            return new FunnyParseException(
                237,
                $"'[x..y step ???]. Array step expected but was {ErrorsHelper.ToText(missedVal)}'{Nl} Example: a[1..5 step 2]",
                start, finish);
    }

    internal static Exception ArrayIntervalInitializeCbrMissed(Tok openBracket, Tok lastToken, bool hasStep) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        return new FunnyParseException(
            240,
            $"{(hasStep ? "[x..y step ???]" : "[x..y ???]")}. ']' was missed'{Nl} Example: a[1..5 step 2]", start,
            finish);
    }

    internal static Exception ArrayIndexCbrMissed(Tok openBracket, Tok lastToken) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        return new FunnyParseException(
            243,
            $"a[x ??? <- was missed ']'{Nl} Example: a[1] or a[1:2] or a[1:5:2]", start,
            finish);
    }

    internal static Exception ArraySliceCbrMissed(Tok openBracket, Tok lastToken, bool hasStep) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        return new FunnyParseException(
            246,
            $"a{(hasStep ? "[x:y:step]" : "[x:y]")} <- ']' was missed{Nl} Example: a[1:5:2]",
            start,
            finish);
    }

    internal static Exception ConditionIsMissing(int conditionStart, int end)
        => new FunnyParseException(
            249,
            $"if (???) {Nl} Condition expression is missing{Nl} Example: if (a>b)  ... ", conditionStart, end);

    internal static Exception ThenExpressionIsMissing(int conditionStart, int end)
        => new FunnyParseException(
            252, $"if (a)  ???.  Expression is missing{Nl} Example: if (a)  a+1 ",
            conditionStart, end);

    internal static Exception ElseKeywordIsMissing(int ifelseStart, int end)
        => new FunnyParseException(
            255, $"if (a) b ???.  Else keyword is missing{Nl} Example: if (a) b else c ",
            ifelseStart, end);

    internal static Exception ElseExpressionIsMissing(int ifelseStart, int end)
        => new FunnyParseException(
            258,
            $"if (a) b else ???.  Else expression is missing{Nl} Example: if (a) b else c ", ifelseStart, end);

    internal static Exception IfKeywordIsMissing(int ifelseStart, int end)
        => new FunnyParseException(
            261,
            $"if (a) b (if) ...  'if' is missing{Nl} Example: if (a) b if (c) d else c ",
            ifelseStart, end);


    internal static Exception IfConditionIsNotInBrackets(int ifelseStart, int end)
        => new FunnyParseException(
            264, $"If condition is not in brackets{Nl} Example: if (a) b  else c ",
            ifelseStart, end);

    internal static Exception NewLineMissedBeforeRepeatedIf(Interval interval)
        => new FunnyParseException(
            267,
            $"Not first if has to start from new line{Nl} Example: if (a) b {Nl} if(c) d  else e ", interval);


    internal static Exception FunctionCallObrMissed(int funStart, string name, int position, ISyntaxNode pipedVal) {
        if (pipedVal == null)
            return new FunnyParseException(
                270,
                $"{name}( ???. Close bracket ')' is missed{Nl} Example: {name}()",
                funStart,
                position);

        return new FunnyParseException(
            273,
            $"{ErrorsHelper.ToShortText(pipedVal)}.{name}( ???. Close bracket ')' is missed{Nl} Example: {ErrorsHelper.ToShortText(pipedVal)}.{name}() or {name}({ErrorsHelper.ToShortText(pipedVal)})",
            funStart,
            position);
    }

    internal static Exception TypeExpectedButWas(Tok token)
        => new FunnyParseException(276, $"Expected: type, but was {ErrorsHelper.ToText(token)}", token.Interval);

    internal static Exception ArrTypeCbrMissed(Interval interval)
        => new FunnyParseException(279, $"']' is missed on array type", interval);

    internal static Exception BracketExpressionMissed(int start, int end, IList<ISyntaxNode> arguments) {
        var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
        return new FunnyParseException(
            282,
            $"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})",
            start, end);
    }

    internal static Exception ExpressionListMissed(int start, int end, IList<ISyntaxNode> arguments) {
        var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
        return new FunnyParseException(
            285,
            $"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})",
            start, end);
    }

    internal static Exception AttributeOnFunction(FunCallSyntaxNode lexNode)
        => new FunnyParseException(288, $"Function cannot has attributes.", lexNode.Interval);

    internal static Exception ItIsNotAnAttribute(int start, Tok flowCurrent)
        => new FunnyParseException(
            291, $"Attribute name expected, but was '{flowCurrent}'",
            start, flowCurrent.Finish);

    internal static Exception ItIsNotCorrectAttributeValue(Tok next)
        => new FunnyParseException(
            294,
            $"Attribute value 'text' or 'number' or 'boolean' expected, but was '{next}'",
            next.Interval);

    internal static Exception AttributeCbrMissed(int start, TokFlow flow)
        => new FunnyParseException(
            297, $"')' is missed but was '{flow.Current}'",
            start, flow.Current.Interval.Finish);

    internal static Exception NowNewLineAfterAttribute(int start, TokFlow flow)
        => new FunnyParseException(
            300, $"Attribute needs new line after it.",
            start, flow.Current.Interval.Finish);

    internal static Exception NowNewLineBeforeAttribute(TokFlow flow)
        => new FunnyParseException(
            303, $"Attribute has to start from new line.",
            flow.Current.Interval);


    #region 3xx - hi level parsing

    internal static Exception UnexpectedExpression(ISyntaxNode lexNode)
        => new FunnyParseException(
            306, $"Unexpected expression {ErrorsHelper.ToShortText(lexNode)}",
            lexNode.Interval);

    internal static Exception FunDefTokenIsMissed(string funName, List<TypedVarDefSyntaxNode> arguments, Tok actual) {
        return new FunnyParseException(
            309,
            $"{ErrorsHelper.Signature(funName, arguments)} ??? . '=' def symbol is skipped but was {ErrorsHelper.ToText(actual)}{Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = ...",
            actual.Start, actual.Finish);
    }

    internal static Exception FunExpressionIsMissed(
        string funName, List<TypedVarDefSyntaxNode> arguments,
        Interval interval)
        => new FunnyParseException(
            312,
            $"{ErrorsHelper.Signature(funName, arguments)} = ??? . Function body is missed {Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = #place your body here",
            interval);

    internal static Exception UnknownValueAtStartOfExpression(int exprStart, Tok flowCurrent)
        => new FunnyParseException(
            315,
            $"Unexpected symbol {ErrorsHelper.ToText(flowCurrent)}. Equation, anonymous equation, function or type definition expected.",
            exprStart, flowCurrent.Finish);

    internal static Exception ExpressionBeforeTheDefinition(int exprStart, ISyntaxNode expression, Tok flowCurrent)
        => new FunnyParseException(
            318,
            $"Unexpected expression {ErrorsHelper.ToShortText(expression)} before definition. Equation, anonymous equation, function or type definition expected.",
            exprStart, flowCurrent.Finish);

    internal static Exception FunctionDefinitionHasToStartFromNewLine(
        int exprStart, ISyntaxNode lexNode,
        Tok flowCurrent)
        => throw new FunnyParseException(
            321,
            $"Function definition has start from new line. {Nl}Example : y:int{Nl}m(x) = x+1", exprStart,
            flowCurrent.Finish);

    internal static Exception DefinitionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode, Tok flowCurrent)
        => throw new FunnyParseException(
            324,
            $"Definition has start from new line. {Nl}Example : y:int{Nl}j = y+1 #j = y:int+1", exprStart,
            flowCurrent.Finish);


    internal static Exception AnonymousExpressionHasToStartFromNewLine(
        int exprStart, ISyntaxNode lexNode,
        Tok flowCurrent)
        => throw new FunnyParseException(
            327,
            $"Anonymous equation should start from new line. {Nl}Example : y:int{Nl}y+1 #out = y:int+1", exprStart,
            flowCurrent.Finish);

    internal static Exception OnlyOneAnonymousExpressionAllowed(int exprStart, ISyntaxNode lexNode, Tok flowCurrent)
        => throw new FunnyParseException(
            330, $"Only one anonymous equation allowed", exprStart,
            flowCurrent.Finish);

    internal static Exception UnexpectedBracketsOnFunDefinition(FunCallSyntaxNode headNode, int start, int finish)
        => new FunnyParseException(
            333,
            $"Unexpected brackets on function definition ({headNode.Id}(...))=... {Nl}Example: {headNode.Id}(...)=...",
            start, finish);

    internal static Exception WrongFunctionArgumentDefinition(FunCallSyntaxNode headNode, ISyntaxNode headNodeChild) {
        var sb = ErrorsHelper.ToFailureFunString(headNode, headNodeChild);
        return new FunnyParseException(
            336,
            $"{headNode.Id}({sb}) = ... {Nl} Function argument is invalid. Variable name (with optional type) expected",
            headNodeChild.Interval);
    }

    internal static Exception FunctionArgumentInBracketDefinition(
        FunCallSyntaxNode headNode,
        ISyntaxNode headNodeChild,
        Tok flowCurrent) {
        if (flowCurrent == null) throw new ArgumentNullException(nameof(flowCurrent));
        var sb = ErrorsHelper.ToFailureFunString(headNode, headNodeChild);

        return new FunnyParseException(
            339,
            $"{headNode.Id}({sb}) = ... {Nl} Function argument is in bracket. Variable name (with optional type) without brackets expected",
            headNodeChild.Interval.Start, headNodeChild.Interval.Finish);
    }

    internal static Exception VarExpressionIsMissed(int start, string id, Tok flowCurrent)
        => new FunnyParseException(
            342, $"{id} = ??? . Equation body is missed {Nl}Example: {id} = {id}+1",
            start, flowCurrent.Finish);

    internal static Exception OutputNameWithDifferentCase(string id, Interval interval)
        => new FunnyParseException(345, $"{id}<-  output name is same to name  {id}", interval);

    internal static Exception InputNameWithDifferentCase(string id, string actualName, Interval interval)
        => new FunnyParseException(348, $"{actualName}<-  input name is same to name  {id}", interval);

    internal static Exception InterpolationExpressionIsMissing(ISyntaxNode lastNode)
        => new FunnyParseException(
            252,
            $"  Interpolation expression is missing{Nl} Example: 'before {{...}} after' ",
            lastNode.Interval);

    #endregion


    #region 4xx - errors of lists

    internal static Exception ArrayInitializeByListError(int openBracketTokenPos, TokFlow flow) {
        var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.ArrOBr, TokType.ArrCBr);
        var list = res.Parsed;
        var argStubs = ErrorsHelper.CreateArgumentsStub(list);
        return res.Type switch {
                   ExprListErrorType.FirstElementMissed => new FunnyParseException(
                       401,
                       $"[ ??? , ..] <- First element missed {Nl}Remove ',' or place element before it", res.Interval),
                   ExprListErrorType.ElementMissed => new FunnyParseException(
                       404,
                       $"[{argStubs},???, ..] <- element missed {Nl}Remove ',' or place element before it",
                       res.Interval),
                   ExprListErrorType.TotalyWrongDefinition => new FunnyParseException(
                       407, "Wrong array definition ",
                       res.Interval),
                   ExprListErrorType.SingleOpenBracket => new FunnyParseException(
                       410,
                       $"[ <- unexpected array symbol{Nl} Did you mean array initialization [,], slice [::] or indexing [i]?",
                       res.Interval),
                   ExprListErrorType.SepIsMissing => new FunnyParseException(
                       413,
                       $"[{argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: [{argStubs}, myArgument, ...]",
                       res.Interval),
                   ExprListErrorType.ArgumentIsInvalid => new FunnyParseException(
                       416,
                       $"[{argStubs}, ??? , ...  <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]",
                       res.Interval),
                   ExprListErrorType.CloseBracketIsMissing => new FunnyParseException(
                       419,
                       $"[{argStubs} ??? <- Array close bracket ']' is missing{Nl} Example: [{argStubs}]",
                       res.Interval),
                   ExprListErrorType.LastArgumentIsInvalid => new FunnyParseException(
                       422,
                       $"[{ErrorsHelper.CreateArgumentsStub(list.Take(list.Length - 1))} ??? ] <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]",
                       res.Interval),
                   _ => throw new ArgumentOutOfRangeException()
               };
    }


    internal static Exception FunctionArgumentError(string id, int openBracketTokenPos, TokFlow flow) {
        var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.Obr, TokType.Cbr);
        var list = res.Parsed;
        var argStubs = ErrorsHelper.CreateArgumentsStub(list);
        return res.Type switch {
                   ExprListErrorType.FirstElementMissed => new FunnyParseException(
                       425,
                       $"{id}( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it",
                       res.Interval),
                   ExprListErrorType.ElementMissed => new FunnyParseException(
                       428,
                       $"{id}({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it",
                       res.Interval),
                   ExprListErrorType.TotalyWrongDefinition => new FunnyParseException(
                       431, "Wrong function call",
                       res.Interval),
                   ExprListErrorType.SingleOpenBracket => new FunnyParseException(
                       434, $"( <- unexpected bracket{Nl} ?",
                       res.Interval),
                   ExprListErrorType.SepIsMissing => new FunnyParseException(
                       437,
                       $"{id}({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: {id}({argStubs}, myArgument, ...)",
                       res.Interval),
                   ExprListErrorType.ArgumentIsInvalid => new FunnyParseException(
                       440,
                       $"{id}({argStubs}, ??? , ...  <- Seems like function call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)",
                       res.Interval),
                   ExprListErrorType.CloseBracketIsMissing => new FunnyParseException(
                       443,
                       $"{id}({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example: {id}({argStubs})",
                       res.Interval),
                   ExprListErrorType.LastArgumentIsInvalid => new FunnyParseException(
                       446,
                       $"{id}({ErrorsHelper.CreateArgumentsStub(list.Take(list.Length - 1))} ??? ) <- Seems like call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)",
                       res.Interval),
                   _ => throw new ArgumentOutOfRangeException()
               };
    }

    internal static Exception BracketExpressionListError(int openBracketTokenPos, TokFlow flow) {
        var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.Obr, TokType.Cbr);
        var list = res.Parsed;
        var argStubs = ErrorsHelper.CreateArgumentsStub(list);
        return res.Type switch {
                   ExprListErrorType.FirstElementMissed => new FunnyParseException(
                       449,
                       $"( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it", res.Interval),
                   ExprListErrorType.ElementMissed => new FunnyParseException(
                       452,
                       $"({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it",
                       res.Interval),
                   ExprListErrorType.TotalyWrongDefinition => new FunnyParseException(
                       455, "Wrong expression",
                       res.Interval),
                   ExprListErrorType.SingleOpenBracket => new FunnyParseException(
                       458, $"( <- unexpected bracket{Nl} ?",
                       res.Interval),
                   ExprListErrorType.SepIsMissing => new FunnyParseException(
                       461,
                       $"({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: ({argStubs}, myArgument, ...)",
                       res.Interval),
                   ExprListErrorType.ArgumentIsInvalid => new FunnyParseException(
                       464,
                       $"({argStubs}, ??? , ...  <- Seems like invalid expressions{Nl} Example: ({argStubs}, myArgument, ...)",
                       res.Interval),
                   ExprListErrorType.CloseBracketIsMissing => new FunnyParseException(
                       467,
                       $"({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example:({argStubs})", res.Interval),
                   ExprListErrorType.LastArgumentIsInvalid => new FunnyParseException(
                       470,
                       $"({ErrorsHelper.CreateArgumentsStub(list.Take(list.Length - 1))} ??? ) <- Seems like invalid expression{Nl} Example: ({argStubs}, myArgument, ...)",
                       res.Interval),
                   _ => throw new ArgumentOutOfRangeException()
               };
    }

    #endregion


    #region 5xx - Interpritation exceptions

    internal static Exception NotAnExpression(ISyntaxNode node)
        => new FunnyParseException(503, $"{node} is not an expression", node.Interval);

    internal static Exception ImpossibleCast(FunnyType from, FunnyType to, Interval interval)
        => new FunnyParseException(506, $"Unable to cast from {from} to {to}", interval);

    internal static Exception InvalidArgTypeDefinition(ISyntaxNode argumentNode)
        => new FunnyParseException(
            509, ErrorsHelper.ToShortText(argumentNode) + " is  not valid fun arg",
            argumentNode.Interval);

    internal static Exception AnonymousFunDefinitionIsMissing(ISyntaxNode node)
        => new FunnyParseException(512, "Anonymous fun definition is missing", node.Interval);

    internal static Exception AnonymousFunBodyIsMissing(Interval interval)
        => new FunnyParseException(515, "Anonymous fun body is missing. Did you forget '=' symbol?", interval);

    internal static Exception AnonymousFunArgumentIsIncorrect(ISyntaxNode node)
        => new FunnyParseException(518, "Invalid anonymous fun argument", node.Interval);
    
    internal static Exception FunctionNameAndVariableNameConflict(VariableUsages usages)
        => new FunnyParseException(
            524,
            $"Function with name: {usages.Source.Name} can be used in expression because it's name conflict with function that exists in scope. Declare input variable",
            usages.Source.TypeSpecificationIntervalOrNull ??
            usages.Usages.FirstOrDefault()?.Interval ?? Interval.Empty);

    internal static Exception FunctionOverloadNotFound(FunCallSyntaxNode node, IFunctionDictionary functions) {
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

        return new FunnyParseException(533, msg.ToString(), node.Interval);
    }

    internal static Exception UnknownVariables(IEnumerable<VariableExpressionNode> values) {
        if (values.Count() == 1)
            return new FunnyParseException(539, $"Unknown variable \"{values.First()}\"", values.First().Interval);
        return new FunnyParseException(
            542, $"Unknown variables \"{string.Join(", ", values)}\"",
            values.First().Interval);
    }

    internal static Exception FunctionAlreadyExist(UserFunctionDefinitionSyntaxNode userFun)
        => new FunnyParseException(
            545,
            $"Function  {ErrorsHelper.Signature(userFun.Id, userFun.Args)} already exist",
            new Interval(userFun.Head.Interval.Start, userFun.Body.Interval.Finish));

    internal static Exception InvalidOutputType(IFunctionSignature function, Interval interval)
        => new FunnyParseException(
            551,
            $"'{function.ReturnType}' is not supported as output parameter of {function.Name}()", interval);

    internal static Exception FunctionArgumentDuplicates(
        UserFunctionDefinitionSyntaxNode lexFunction,
        TypedVarDefSyntaxNode lexFunctionArg)
        => new FunnyParseException(
            554,
            $"'Argument name '{lexFunctionArg.Id}' duplicates at  {ErrorsHelper.Signature(lexFunction.Id, lexFunction.Args)} ",
            lexFunction.Head.Interval);

    internal static Exception AnonymousFunctionArgumentDuplicates(
        FunArgumentExpressionNode argNode,
        ISyntaxNode funDefinition)
        => new FunnyParseException(
            557, $"'Argument name '{argNode.Name}' of anonymous fun duplicates ",
            argNode.Interval);

    internal static Exception AnonymousFunctionArgumentConflictsWithOuterScope(string argName, Interval defInterval)
        => new FunnyParseException(
            566,
            $"'Argument name '{argName}' of anonymous fun conflicts with outer scope variable. It is denied for your safety.",
            defInterval);

    internal static Exception ComplexRecursion(UserFunctionDefinitionSyntaxNode[] functionSolveOrder) {
        var callOrder = string.Join("->", functionSolveOrder.Select(s => s.Id + "(..)"));
        return new FunnyParseException(
            572, $"Complex recursion found: {callOrder} ",
            functionSolveOrder.First().Interval);
    }

    #endregion


    #region InvalidFluentUsage

    internal static FunnyParseException UnknownInputs(IEnumerable<VariableUsages> variableUsage) =>
        new(605, "Some inputs are unknown", Interval.Empty);

    internal static FunnyParseException NoOutputVariablesSetted(
        Memory<(string, IOutputFunnyConverter, PropertyInfo)> expectedOutputs)
        => new(609, "No output values were setted", Interval.Empty);

    internal static FunnyParseException OutputIsUnset()
        => new(
            615,
            $"Output is not set. Anonymous equation or '{Parser.AnonymousEquationId}' variable expected", Interval
                .Empty);

    internal static FunnyParseException TypeCannotBeUsedAsOutputNfunType(FunnyType funnyType)
        => new(618, $"type {funnyType} is not supported for dynamic convertion", Interval.Empty);

    #endregion


    internal static Exception VariousIfElementTypes(IfThenElseSyntaxNode ifThenElse) {
        var allExpressions = ifThenElse.Ifs
                                       .Select(i => i.Expression)
                                       .Append(ifThenElse.ElseExpr)
                                       .ToArray();

        //Search first failed interval
        Interval failedInterval = ifThenElse.Interval;

        //Lca defined only in TI. It is kind of hack
        var hmTypes = allExpressions.Select(a => a.OutputType.ConvertToTiType()).ToArray();

        return new FunnyParseException(
            575, $"'If-else expressions contains different type. " +
                 $"Specify toAny() cast if the result should be of 'any' type. " +
                 $"Actual types: {string.Join(",", hmTypes.Select(m => m.Description))}",
            failedInterval);
    }

    internal static Exception VariousArrayElementTypes(ArraySyntaxNode arraySyntaxNode) => 
        new FunnyParseException(
        578, $"'Various array element types. " +
             $"{arraySyntaxNode.OutputType} = [{string.Join(",", arraySyntaxNode.Expressions.Select(e => e.OutputType))}]",
        arraySyntaxNode.Interval);

    internal static Exception CannotUseOutputValueBeforeItIsDeclared(VariableUsages usages) {
        var interval = (usages.Usages.FirstOrDefault()?.Interval) ??
                       usages.Source.TypeSpecificationIntervalOrNull ?? new Interval();

        return new FunnyParseException(
            587,
            $"Cannot use output value '{usages.Source.Name}' before it is declared'",
            interval);
    }

    internal static Exception VariableIsDeclaredAfterUsing(VariableUsages usages)
        => new FunnyParseException(
            590, $"Variable '{usages.Source.Name}' used before it is declared'",
            usages.Usages.First().Interval);

    #endregion
    
    #region typeSolving

    internal static Exception TypesNotSolved(ISyntaxNode syntaxNode)
        => new FunnyParseException(600, $"Types cannot be solved ", syntaxNode.Interval);

    internal static Exception FunctionTypesNotSolved(UserFunctionDefinitionSyntaxNode node)
        => new FunnyParseException(
            603,
            $"Function {node.GetFunAlias()} has invalid arguments or output type. Check function body expression",
            new Interval(node.Head.Interval.Start, node.Body.Interval.Start));

    internal static Exception OutputDefinitionDuplicates(EquationSyntaxNode node)
        => new FunnyParseException(606, $"Output variable {node.Id} definition duplicates", node.Interval);

    internal static Exception OutputDefinitionTypeIsNotSolved(EquationSyntaxNode node)
        => new FunnyParseException(609, $"Output variable '{node.Id}' type is incorrect", node.Interval);

    #endregion
    
    internal static Exception TranslateTicError(TicException ticException, ISyntaxNode syntaxNodeToSearch) {
        if (ticException is IncompatibleAncestorSyntaxNodeException syntaxNodeEx)
        {
            var concreteNode =
                SyntaxTreeDeepFieldSearch.FindNodeByOrderNumOrNull(syntaxNodeToSearch, syntaxNodeEx.SyntaxNodeId);
            if (concreteNode != null)
                return new FunnyParseException(
                    601, $"Types cannot be solved: {ticException.Message} ",
                    concreteNode.Interval);
        }
        else if (ticException is IncompatibleAncestorNamedNodeException namedNodeEx)
        {
            var concreteNode =
                SyntaxTreeDeepFieldSearch.FindVarDefinitionOrNull(syntaxNodeToSearch, namedNodeEx.NodeName);
            if (concreteNode != null)
                return new FunnyParseException(
                    602, $"Types cannot be solved: {ticException.Message} ",
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
                    return new FunnyParseException(
                        603,
                        $"Recursive type definition: {string.Join("->", e.NodeNames)} ", concreteNode.Interval);
                }
            }

            foreach (var nodeId in e.NodeIds)
            {
                var concreteNode = SyntaxTreeDeepFieldSearch.FindNodeByOrderNumOrNull(syntaxNodeToSearch, nodeId);
                if (concreteNode != null)
                    return new FunnyParseException(
                        603, $"Recursive type definition detected",
                        concreteNode.Interval);
            }
        }

        return TypesNotSolved(syntaxNodeToSearch);
    }
    
  
    internal static Exception NumberOverflow(Interval interval, FunnyType type) 
        => ErrorStubToDo($"{type} overflow", interval);
    
    internal static Exception CannotUseSuperAnonymousVariableHere(Interval interval) 
        => ErrorStubToDo("'it' variable can be used only as arguments in rules", interval);
    
    internal static Exception CannotParseDecimalNumber(Interval interval) 
        => ErrorStubToDo("Cannot parse decimal number", interval);
    
    internal static Exception IfElseExpressionIsDenied(Interval interval) 
        => ErrorStubToDo("If-else expressions are denied for the dialect", interval);

    internal static Exception FieldIsMissed(string name, Interval interval)  
        => ErrorStubToDo($"Field {name} is missed in struct", interval);
    
    internal static Exception FieldNotExists(string name, Interval interval)  
        => ErrorStubToDo($"Access to non exist field {name}", interval);

    internal static Exception UndoneAnonymousFunction(int anonymousStart, int anonymousFinish)
        => ErrorStubToDo("SuperAnonymousFunctionIsNotClose", new Interval(anonymousStart, anonymousFinish));

    internal static Exception VariableIsAlreadyDeclared(string nodeId, Interval nodeInterval)
        => ErrorStubToDo($"Variable {nodeId} is already declared",nodeInterval);
    
    internal static Exception UnexpectedTokenEqualAfterRule(Interval nodeInterval)
        => ErrorStubToDo(
            "unexpected '=' symbol. Did you forgot brackets after 'rule' keyword?",nodeInterval);
    internal static Exception StructFieldDelimiterIsMissed(Interval interval)
        => ErrorStubToDo(
            "There is no separator between the fields of structures. " +
            "Use ',' or new line to separate fields",interval);
    internal static Exception StructFieldIdIsMissed(Tok tok)
        => ErrorStubToDo($"{tok} found instead of the structure field name",tok.Interval);
    internal static Exception StructFieldSpecificationIsNotSupportedYet(Interval interval)
        => ErrorStubToDo($"Struct field type specification is not supported yet",interval);
    internal static Exception StructFielDefenitionTokenIsMissed(Tok tok)
        => ErrorStubToDo($"{tok} found instead of the '=' symbol", tok.Interval);
    internal static Exception StructFieldBodyIsMissed(Tok id)
        => ErrorStubToDo($"Field value is missed '{id} = ???'", id.Interval);
    
    internal static Exception EmptyStructsAreNotSupported(Interval interval)
        => ErrorStubToDo($"Struct has to have at least one field", interval);
    internal static Exception UnexpectedSpaceBeforeArrayTypeBrackets(FunnyType elementType, Interval interval)
        => ErrorStubToDo($"there should be no space before the square brackets when defining the array type. Example: 'a:{elementType}[]'", interval);
    internal static Exception TokenIsMissed(Tok previous, TokType tokType)
        => ErrorStubToDo($"'{tokType}' is missing after '{previous}'", Interval.Position(previous.Finish));

    internal static Exception TokenIsMissed(int position, TokType tokType)
        => ErrorStubToDo($"'{tokType}' is missing at end of stream", Interval.Position(position));
    
    internal static Exception TokenIsMissed(TokType tokType, Tok actualToken)
        => ErrorStubToDo($"'{tokType}' is missing but was '{actualToken}'", actualToken.Interval);

    internal static Exception FunctionIsNotSolved(UserFunctionDefinitionSyntaxNode function)
        => ErrorStubToDo($"Cannot calculate types for function '{function.Head}'. Check the expressions and/or add types to arguments/return", function.Interval);
    
    internal static Exception TokenIsReserved(Interval interval, string word) 
        => ErrorStubToDo($"Symbol '{word}' is reserved for future use and cannot be used in script", interval);

    private static Exception ErrorStubToDo(string message, Interval interval)
        => FunnyParseException.ErrorStubToDo(message);
}

}