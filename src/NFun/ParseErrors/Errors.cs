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

    internal static FunnyParseException QuoteAtEndOfString(char quoteSymbol, int start, int end) => new(
        110, $"Single '{quoteSymbol}' at end of string.",
        start, end);

    internal static FunnyParseException BackslashAtEndOfString(int start, int end)
        => new(113, $"Single '\\' at end of string.", start, end);

    internal static FunnyParseException UnknownEscapeSequence(string sequence, int start, int end) =>
        new(
            116, $"Unknown escape sequence \\{sequence}",
            start, end);

    internal static FunnyParseException ClosingQuoteIsMissed(char quoteSymbol, int start, int end)
        => new(
            119, $"Closing {quoteSymbol} is missed at end of string",
            start, end);

    internal static FunnyParseException TokenIsReserved(Interval interval, string word)
        => new(122, $"Symbol '{word}' is reserved for future use and cannot be used in script", interval);

    internal static FunnyParseException TokenIsMissed(Tok previous, TokType tokType)
        => new(125, $"'{tokType}' is missing after '{previous}'", Interval.Position(previous.Finish));

    internal static FunnyParseException TokenIsMissed(int position, TokType tokType)
        => new(125, $"'{tokType}' is missing at end of stream", Interval.Position(position));

    internal static FunnyParseException TokenIsMissed(TokType tokType, Tok actualToken)
        => new(125, $"'{tokType}' is missing but was '{actualToken}'", actualToken.Interval);


    #endregion

    #region 2xx-3xx parsing

    private static readonly string Nl = Environment.NewLine;

    internal static FunnyParseException UnaryArgumentIsMissing(Tok operatorTok) => new(
        201, $"{ErrorsHelper.ToText(operatorTok)} ???{Nl} right expression is missed{Nl} Example: {ErrorsHelper.ToText(operatorTok)} a",
        operatorTok.Interval);

    internal static FunnyParseException MinusDuplicates(Tok previousTok, Tok currentTok) => new(
        204, $"'--' is not allowed",
        previousTok.Start, currentTok.Finish);

    internal static FunnyParseException LeftBinaryArgumentIsMissing(Tok token) => new(
        207, $"expression is missed before '{ErrorsHelper.ToText(token)}'",
        token.Interval);

    internal static FunnyParseException RightBinaryArgumentIsMissing(ISyntaxNode leftNode, Tok @operator) => new(
        210,
        $"{ErrorsHelper.ToShortText(leftNode)} {ErrorsHelper.ToText(@operator)} ???. Right expression is missed{Nl} Example: {ErrorsHelper.ToShortText(leftNode)} {ErrorsHelper.ToText(@operator)} e",
        leftNode.Interval.Start, @operator.Finish);

    internal static FunnyParseException OperatorIsUnknown(Tok token) => new(
        213, $"operator '{ErrorsHelper.ToText(token)}' is unknown", token.Interval);

    internal static FunnyParseException NotAToken(Tok token) => new(
        216, $"'{token.Value}' is not valid fun element. What did you mean?", token.Interval);

    internal static FunnyParseException FunctionOrStructMemberNameIsMissedAfterDot(Tok token) => new(
        219, $"Function name expected after '.'{Nl} Example: [1,2].myFunction()", token.Interval);

    #region slices and indexing

    internal static FunnyParseException ArrayIndexOrSliceExpected(Tok openBraket) => new(
        222, $"Array index or slice expected after '['{Nl} Example: a[1] or a[1:3:2]", openBraket.Interval);

    internal static FunnyParseException ArrayIndexExpected(Tok openBraket, Tok closeBracket) => new(
        225, $"Array index expected inside of '[]'{Nl} Example: a[1]", openBraket.Start, closeBracket.Finish);

    internal static FunnyParseException ArrayInitializeSecondIndexMissed(Tok openBracket, Tok lastToken, Tok missedVal) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        if (string.IsNullOrWhiteSpace(missedVal?.Value))
            return new(
                228,
                $"'[x,..???]. Array hi bound expected but was nothing'{Nl} Example: a[1..2] or a[1..5 step 2]", start,
                finish);
        else
            return new(
                231,
                $"'[x,..???]. Array hi bound expected but was {ErrorsHelper.ToText(missedVal)}'{Nl} Example: a[1..2] or a[1..5 step 2]",
                start, finish);
    }

    internal static FunnyParseException ArrayInitializeStepMissed(Tok openBracket, Tok lastToken, Tok missedVal) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        if (string.IsNullOrWhiteSpace(missedVal?.Value))
            return new(
                234,
                $"'[x..y step ???]. Array step expected but was nothing'{Nl} Example: a[1..5 step 2]", start,
                finish);
        else
            return new(
                237,
                $"'[x..y step ???]. Array step expected but was {ErrorsHelper.ToText(missedVal)}'{Nl} Example: a[1..5 step 2]",
                start, finish);
    }

    internal static FunnyParseException ArrayIntervalInitializeCbrMissed(Tok openBracket, Tok lastToken, bool hasStep) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        return new(
            240,
            $"{(hasStep ? "[x..y step ???]" : "[x..y ???]")}. ']' was missed'{Nl} Example: a[1..5 step 2]", start,
            finish);
    }

    internal static FunnyParseException ArrayIndexCbrMissed(Tok openBracket, Tok lastToken) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        return new(
            243,
            $"a[x ??? <- was missed ']'{Nl} Example: a[1] or a[1:2] or a[1:5:2]", start,
            finish);
    }

    internal static FunnyParseException ArraySliceCbrMissed(Tok openBracket, Tok lastToken, bool hasStep) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        return new(
            246,
            $"a{(hasStep ? "[x:y step]" : "[x:y]")} <- ']' was missed{Nl} Example: a[1:5 step 2]",
            start,
            finish);
    }

    #endregion
    #region if-else



    internal static FunnyParseException ConditionIsMissing(int conditionStart, int end) => new(
        249, $"if (???) {Nl} Condition expression is missing{Nl} Example: if (a>b)  ... ", conditionStart, end);

    internal static FunnyParseException ThenExpressionIsMissing(int conditionStart, int end) => new(
        252, $"if (a)  ???.  Expression is missing{Nl} Example: if (a)  a+1 ", conditionStart, end);

    internal static FunnyParseException ElseKeywordIsMissing(int ifelseStart, int end) => new(
        255, $"if (a) b ???.  Else keyword is missing{Nl} Example: if (a) b else c ", ifelseStart, end);

    internal static FunnyParseException ElseExpressionIsMissing(int ifelseStart, int end) => new(
        258, $"if (a) b else ???.  Else expression is missing{Nl} Example: if (a) b else c ", ifelseStart, end);

    internal static FunnyParseException IfKeywordIsMissing(int ifelseStart, int end) => new(
        261, $"if (a) b (if) ...  'if' is missing{Nl} Example: if (a) b if (c) d else c ", ifelseStart, end);

    internal static FunnyParseException IfConditionIsNotInBrackets(int ifelseStart, int end) => new(
        264, $"If condition is not in brackets{Nl} Example: if (a) b  else c ", ifelseStart, end);

    internal static FunnyParseException NewLineMissedBeforeRepeatedIf(Interval interval) => new(
        267, $"Not first if has to start from new line{Nl} Example: if (a) b {Nl} if(c) d  else e ", interval);

    internal static FunnyParseException IfElseExpressionIsDenied(Interval interval) => new(
        268, "If-else expressions are denied for the dialect", interval);

    #endregion

    internal static FunnyParseException FunctionCallObrMissed(int funStart, string name, int position, ISyntaxNode pipedVal) {
        if (pipedVal == null)
            return new(
                270, $"{name}( ???. Close bracket ')' is missed{Nl} Example: {name}()", funStart, position);

        return new(
            273,
            $"{ErrorsHelper.ToShortText(pipedVal)}.{name}( ???. Close bracket ')' is missed{Nl} Example: {ErrorsHelper.ToShortText(pipedVal)}.{name}() or {name}({ErrorsHelper.ToShortText(pipedVal)})",
            funStart, position);
    }

    internal static FunnyParseException TypeExpectedButWas(Tok token) => new(
        276, $"Expected: type, but was {ErrorsHelper.ToText(token)}", token.Interval);

    internal static FunnyParseException ArrTypeCbrMissed(Interval interval) => new(
        279, $"']' is missed on array type", interval);
    internal static FunnyParseException UnexpectedSpaceBeforeArrayTypeBrackets(FunnyType elementType, Interval interval) => new(
        280, $"there should be no space before the square brackets when defining the array type. Example: 'a:{elementType}[]'", interval);

    internal static FunnyParseException BracketExpressionMissed(int start, int end, IList<ISyntaxNode> arguments) {
        var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
        return new(
            282, $"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", start, end);
    }

    internal static FunnyParseException ExpressionListMissed(int start, int end, IList<ISyntaxNode> arguments) {
        var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
        return new(
            285, $"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", start, end);
    }

    internal static FunnyParseException AttributeOnFunction(FunCallSyntaxNode lexNode) => new(
        288, $"Function cannot has attributes.", lexNode.Interval);

    internal static FunnyParseException ItIsNotAnAttribute(int start, Tok flowCurrent) => new(
        291, $"Attribute name expected, but was '{flowCurrent}'", start, flowCurrent.Finish);

    internal static FunnyParseException ItIsNotCorrectAttributeValue(Tok next) => new(
        294, $"Attribute value 'text' or 'number' or 'boolean' expected, but was '{next}'", next.Interval);

    internal static FunnyParseException AttributeCbrMissed(int start, TokFlow flow) => new(
        297, $"')' is missed but was '{flow.Current}'", start, flow.Current.Interval.Finish);


    internal static FunnyParseException NowNewLineAfterAttribute(int start, TokFlow flow) => new(
        300, $"Attribute needs new line after it.", start, flow.Current.Interval.Finish);

    internal static FunnyParseException NowNewLineBeforeAttribute(TokFlow flow) => new(
        303, $"Attribute has to start from new line.", flow.Current.Interval);

    internal static FunnyParseException NumberOverflow(Interval interval, FunnyType type) => new(
        306, $"{type} overflow", interval);

    internal static FunnyParseException CannotParseDecimalNumber(Interval interval) => new(
        309, "Cannot parse decimal number", interval);

    internal static FunnyParseException UnexpectedExpression(ISyntaxNode lexNode) => new(
        312, $"Unexpected expression {ErrorsHelper.ToShortText(lexNode)}", lexNode.Interval);

    internal static FunnyParseException FunDefTokenIsMissed(string funName, List<TypedVarDefSyntaxNode> arguments, Tok actual) => new(
        315, $"{ErrorsHelper.Signature(funName, arguments)} ??? . '=' def sym, ol is skipped but was {ErrorsHelper.ToText(actual)}{Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = ...",
        actual.Start, actual.Finish);

    internal static FunnyParseException FunExpressionIsMissed(string funName, List<TypedVarDefSyntaxNode> arguments, Interval interval) => new(
        318, $"{ErrorsHelper.Signature(funName, arguments)} = ??? . Function body is missed {Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = #place your body here", interval);

    internal static FunnyParseException UnknownValueAtStartOfExpression(int exprStart, Tok flowCurrent) => new(
        321, $"Unexpected symbol {ErrorsHelper.ToText(flowCurrent)}. Equation, anonymous equation, function or type definition expected.",
        exprStart, flowCurrent.Finish);

    internal static FunnyParseException ExpressionBeforeTheDefinition(int exprStart, ISyntaxNode expression, Tok flowCurrent) => new(
        324, $"Unexpected expression {ErrorsHelper.ToShortText(expression)} before definition. Equation, anonymous equation, function or type definition expected.",
        exprStart, flowCurrent.Finish);

    internal static FunnyParseException FunctionDefinitionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode, Tok flowCurrent) => new(
        327, $"Function definition has start from new line. {Nl}Example : y:int{Nl}m(x) = x+1", exprStart, flowCurrent.Finish);

    internal static FunnyParseException DefinitionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode, Tok flowCurrent) => new(
        330, $"Definition has start from new line. {Nl}Example : y:int{Nl}j = y+1 #j = y:int+1", exprStart, flowCurrent.Finish);


    internal static FunnyParseException AnonymousExpressionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode, Tok flowCurrent) => new(
        333, $"Anonymous equation should start from new line. {Nl}Example : y:int{Nl}y+1 #out = y:int+1", exprStart, flowCurrent.Finish);

    internal static FunnyParseException OnlyOneAnonymousExpressionAllowed(int exprStart, ISyntaxNode lexNode, Tok flowCurrent) => new(
        336, $"Only one anonymous equation allowed", exprStart, flowCurrent.Finish);

    internal static FunnyParseException UnexpectedBracketsOnFunDefinition(FunCallSyntaxNode headNode, int start, int finish) => new(
        339, $"Unexpected brackets on function definition ({headNode.Id}(...))=... {Nl}Example: {headNode.Id}(...)=...", start, finish);

    internal static FunnyParseException WrongFunctionArgumentDefinition(FunCallSyntaxNode headNode, ISyntaxNode headNodeChild) {
        var sb = ErrorsHelper.ToFailureFunString(headNode, headNodeChild);
        return new(
            342, $"{headNode.Id}({sb}) = ... {Nl} Function argument is invalid. Variable name (with optional type) expected", headNodeChild.Interval);
    }

    internal static FunnyParseException FunctionArgumentInBracketDefinition(
        FunCallSyntaxNode headNode,
        ISyntaxNode headNodeChild,
        Tok flowCurrent) {
        if (flowCurrent == null) throw new ArgumentNullException(nameof(flowCurrent));
        var sb = ErrorsHelper.ToFailureFunString(headNode, headNodeChild);

        return new(
            345, $"{headNode.Id}({sb}) = ... {Nl} Function argument is in bracket. Variable name (with optional type) without brackets expected",
            headNodeChild.Interval.Start, headNodeChild.Interval.Finish);
    }

    internal static FunnyParseException VarExpressionIsMissed(int start, string id, Tok flowCurrent) => new(
        348, $"{id} = ??? . Equation body is missed {Nl}Example: {id} = {id}+1", start, flowCurrent.Finish);

    internal static FunnyParseException OutputNameWithDifferentCase(string id, Interval interval) => new(
        351, $"{id}<-  output name is same to name  {id}", interval);

    internal static FunnyParseException InputNameWithDifferentCase(string id, string actualName, Interval interval) => new(
        351, $"{actualName}<-  input name is same to name  {id}", interval);

    internal static FunnyParseException InterpolationExpressionIsMissing(ISyntaxNode lastNode) => new(
        354, $"  Interpolation expression is missing{Nl} Example: 'before {{...}} after' ", lastNode.Interval);

    #endregion


    #region 4xx - errors of lists

    internal static FunnyParseException ArrayInitializeByListError(int openBracketTokenPos, TokFlow flow) {
        var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.ArrOBr, TokType.ArrCBr);
        var list = res.Parsed;
        var argStubs = ErrorsHelper.CreateArgumentsStub(list);
        return res.Type switch {
                   ExprListErrorType.FirstElementMissed => new(
                       401,
                       $"[ ??? , ..] <- First element missed {Nl}Remove ',' or place element before it", res.Interval),
                   ExprListErrorType.ElementMissed => new(
                       404,
                       $"[{argStubs},???, ..] <- element missed {Nl}Remove ',' or place element before it",
                       res.Interval),
                   ExprListErrorType.TotalyWrongDefinition => new(
                       407, "Wrong array definition ",
                       res.Interval),
                   ExprListErrorType.SingleOpenBracket => new(
                       410,
                       $"[ <- unexpected array symbol{Nl} Did you mean array initialization [,], slice [::] or indexing [i]?",
                       res.Interval),
                   ExprListErrorType.SepIsMissing => new(
                       413,
                       $"[{argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: [{argStubs}, myArgument, ...]",
                       res.Interval),
                   ExprListErrorType.ArgumentIsInvalid => new(
                       416,
                       $"[{argStubs}, ??? , ...  <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]",
                       res.Interval),
                   ExprListErrorType.CloseBracketIsMissing => new(
                       419,
                       $"[{argStubs} ??? <- Array close bracket ']' is missing{Nl} Example: [{argStubs}]",
                       res.Interval),
                   ExprListErrorType.LastArgumentIsInvalid => new(
                       422,
                       $"[{ErrorsHelper.CreateArgumentsStub(list.Take(list.Length - 1))} ??? ] <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]",
                       res.Interval),
                   _ => throw new ArgumentOutOfRangeException()
               };
    }


    internal static FunnyParseException FunctionArgumentError(string id, int openBracketTokenPos, TokFlow flow) {
        var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.Obr, TokType.Cbr);
        var list = res.Parsed;
        var argStubs = ErrorsHelper.CreateArgumentsStub(list);
        return res.Type switch {
                   ExprListErrorType.FirstElementMissed => new(
                       425,
                       $"{id}( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it",
                       res.Interval),
                   ExprListErrorType.ElementMissed => new(
                       428,
                       $"{id}({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it",
                       res.Interval),
                   ExprListErrorType.TotalyWrongDefinition => new(
                       431, "Wrong function call",
                       res.Interval),
                   ExprListErrorType.SingleOpenBracket => new(
                       434, $"( <- unexpected bracket{Nl} ?",
                       res.Interval),
                   ExprListErrorType.SepIsMissing => new(
                       437,
                       $"{id}({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: {id}({argStubs}, myArgument, ...)",
                       res.Interval),
                   ExprListErrorType.ArgumentIsInvalid => new(
                       440,
                       $"{id}({argStubs}, ??? , ...  <- Seems like function call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)",
                       res.Interval),
                   ExprListErrorType.CloseBracketIsMissing => new(
                       443,
                       $"{id}({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example: {id}({argStubs})",
                       res.Interval),
                   ExprListErrorType.LastArgumentIsInvalid => new(
                       446,
                       $"{id}({ErrorsHelper.CreateArgumentsStub(list.Take(list.Length - 1))} ??? ) <- Seems like call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)",
                       res.Interval),
                   _ => throw new ArgumentOutOfRangeException()
               };
    }

    internal static FunnyParseException BracketExpressionListError(int openBracketTokenPos, TokFlow flow) {
        var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.Obr, TokType.Cbr);
        var list = res.Parsed;
        var argStubs = ErrorsHelper.CreateArgumentsStub(list);
        return res.Type switch {
                   ExprListErrorType.FirstElementMissed => new(
                       449,
                       $"( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it", res.Interval),
                   ExprListErrorType.ElementMissed => new(
                       452,
                       $"({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it",
                       res.Interval),
                   ExprListErrorType.TotalyWrongDefinition => new(
                       455, "Wrong expression",
                       res.Interval),
                   ExprListErrorType.SingleOpenBracket => new(
                       458, $"( <- unexpected bracket{Nl} ?",
                       res.Interval),
                   ExprListErrorType.SepIsMissing => new(
                       461,
                       $"({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: ({argStubs}, myArgument, ...)",
                       res.Interval),
                   ExprListErrorType.ArgumentIsInvalid => new(
                       464,
                       $"({argStubs}, ??? , ...  <- Seems like invalid expressions{Nl} Example: ({argStubs}, myArgument, ...)",
                       res.Interval),
                   ExprListErrorType.CloseBracketIsMissing => new(
                       467,
                       $"({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example:({argStubs})", res.Interval),
                   ExprListErrorType.LastArgumentIsInvalid => new(
                       470,
                       $"({ErrorsHelper.CreateArgumentsStub(list.Take(list.Length - 1))} ??? ) <- Seems like invalid expression{Nl} Example: ({argStubs}, myArgument, ...)",
                       res.Interval),
                   _ => throw new ArgumentOutOfRangeException()
               };
    }

    #region structs



    internal static FunnyParseException FieldIsMissed(string name, Interval interval) => new(
        473, $"Field {name} is missed in struct", interval);

    internal static FunnyParseException FieldNotExists(string name, Interval interval) => new(
        476, $"Access to non exist field {name}", interval);


    internal static FunnyParseException StructFieldDelimiterIsMissed(Interval interval) => new(
        479, "There is no separator between the fields of structures. Use ',' or new line to separate fields", interval);
    internal static FunnyParseException StructFieldIdIsMissed(Tok tok) => new(
        482, $"{tok} found instead of the structure field name", tok.Interval);
    internal static FunnyParseException StructFieldSpecificationIsNotSupportedYet(Interval interval) => new(
        485, $"Struct field type specification is not supported yet", interval);
    internal static FunnyParseException StructFielDefenitionTokenIsMissed(Tok tok) => new(
        488, $"{tok} found instead of the '=' symbol", tok.Interval);
    internal static FunnyParseException StructFieldBodyIsMissed(Tok id) => new(
        491, $"Field value is missed '{id} = ???'", id.Interval);

    internal static FunnyParseException EmptyStructsAreNotSupported(Interval interval) => new(
        494, $"Struct has to have at least one field", interval);

    #endregion


    #endregion


    #region 5xx - Interpritation exceptions

    internal static FunnyParseException NotAnExpression(ISyntaxNode node) => new(
        503, $"{node} is not an expression", node.Interval);

    internal static FunnyParseException ImpossibleCast(FunnyType from, FunnyType to, Interval interval) => new(
        506, $"Unable to cast from {from} to {to}", interval);



    #region rules and arrows

    internal static FunnyParseException AnonymousFunDefinitionIsMissing(ISyntaxNode node) => new(
        512, "Anonymous fun definition is missing", node.Interval);

    internal static FunnyParseException AnonymousFunBodyIsMissing(Interval interval) => new(
        515, "Anonymous fun body is missing. Did you forget '=' symbol?", interval);

    internal static FunnyParseException AnonymousFunArgumentIsIncorrect(ISyntaxNode node) => new(
        518, "Invalid anonymous fun argument", node.Interval);

    internal static FunnyParseException CannotUseSuperAnonymousVariableHere(Interval interval) => new(
        521, "'it' variable can be used only as arguments in rules", interval);

    internal static FunnyParseException UnexpectedTokenEqualAfterRule(Interval nodeInterval) => new(
        527, "unexpected '=' symbol. Did you forgot brackets after 'rule' keyword?", nodeInterval);

    internal static FunnyParseException AnonymousFunctionArgumentDuplicates(FunArgumentExpressionNode argNode, ISyntaxNode funDefinition) => new(
        530, $"'Argument name '{argNode.Name}' of anonymous fun duplicates ", argNode.Interval);

    internal static FunnyParseException AnonymousFunctionArgumentConflictsWithOuterScope(string argName, Interval defInterval) => new(
        533, $"'Argument name '{argName}' of anonymous fun conflicts with outer scope variable. It is denied for your safety.", defInterval);

    #endregion

    internal static FunnyParseException FunctionNameAndVariableNameConflict(VariableUsages usages) => new(
        560, $"Function with name: {usages.Source.Name} can be used in expression because it's name conflict with function that exists in scope. Declare input variable",
        usages.Source.TypeSpecificationIntervalOrNull ??
        usages.Usages.FirstOrDefault()?.Interval ?? Interval.Empty);

    internal static FunnyParseException FunctionOverloadNotFound(FunCallSyntaxNode node, IFunctionDictionary functions) {
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

        return new(563, msg.ToString(), node.Interval);
    }

    internal static FunnyParseException UnknownVariables(IEnumerable<VariableExpressionNode> values) {
        if (values.Count() == 1)
            return new(566, $"Unknown variable \"{values.First()}\"", values.First().Interval);
        return new(
            566, $"Unknown variables \"{string.Join(", ", values)}\"",
            values.First().Interval);
    }

    #region user functions

    internal static FunnyParseException FunctionAlreadyExist(UserFunctionDefinitionSyntaxNode userFun) => new(
        569, $"Function  {ErrorsHelper.Signature(userFun.Id, userFun.Args)} already exist",
        new Interval(userFun.Head.Interval.Start, userFun.Body.Interval.Finish));

    internal static FunnyParseException InvalidOutputType(IFunctionSignature function, Interval interval) => new(
        572, $"'{function.ReturnType}' is not supported as output parameter of {function.Name}()", interval);

    internal static FunnyParseException FunctionArgumentDuplicates(UserFunctionDefinitionSyntaxNode lexFunction, TypedVarDefSyntaxNode lexFunctionArg) => new(
        575, $"'Argument name '{lexFunctionArg.Id}' duplicates at  {ErrorsHelper.Signature(lexFunction.Id, lexFunction.Args)} ", lexFunction.Head.Interval);

    internal static FunnyParseException InvalidArgTypeDefinition(ISyntaxNode argumentNode) => new(
        578, ErrorsHelper.ToShortText(argumentNode) + " is  not valid fun arg", argumentNode.Interval);

    internal static FunnyParseException ComplexRecursion(UserFunctionDefinitionSyntaxNode[] functionSolveOrder) => new(
        584, $"Complex recursion found: {string.Join("->", functionSolveOrder.Select(s => s.Id + "(..)"))} ", functionSolveOrder.First().Interval);

    #endregion

    #endregion


    #region InvalidFluentUsage

    internal static FunnyParseException UnknownInputs(IEnumerable<VariableUsages> variableUsage) => new(
        605, "Some inputs are unknown", Interval.Empty);

    internal static FunnyParseException NoOutputVariablesSetted(Memory<(string, IOutputFunnyConverter, PropertyInfo)> expectedOutputs) => new(
        609, "No output values were setted", Interval.Empty);

    internal static FunnyParseException OutputIsUnset() => new(
        615, $"Output is not set. Anonymous equation or '{Parser.AnonymousEquationId}' variable expected", Interval.Empty);

    internal static FunnyParseException TypeCannotBeUsedAsOutputNfunType(FunnyType funnyType) => new(
        618, $"type {funnyType} is not supported for dynamic convertion", Interval.Empty);

    #endregion


    internal static FunnyParseException VariousIfElementTypes(IfThenElseSyntaxNode ifThenElse) {
        var allExpressions = ifThenElse.Ifs
                                       .Select(i => i.Expression)
                                       .Append(ifThenElse.ElseExpr)
                                       .ToArray();

        //Search first failed interval
        Interval failedInterval = ifThenElse.Interval;

        //Lca defined only in TI. It is kind of hack
        var hmTypes = allExpressions.Select(a => a.OutputType.ConvertToTiType()).ToArray();

        return new(
            575, $"'If-else expressions contains different type. " +
                 $"Specify toAny() cast if the result should be of 'any' type. " +
                 $"Actual types: {string.Join(",", hmTypes.Select(m => m.Description))}",
            failedInterval);
    }

    internal static FunnyParseException VariousArrayElementTypes(ArraySyntaxNode arraySyntaxNode) =>
        new(
            578, $"'Various array element types. " +
                 $"{arraySyntaxNode.OutputType} = [{string.Join(",", arraySyntaxNode.Expressions.Select(e => e.OutputType))}]",
            arraySyntaxNode.Interval);

    internal static FunnyParseException CannotUseOutputValueBeforeItIsDeclared(VariableUsages usages) {
        var interval = (usages.Usages.FirstOrDefault()?.Interval) ??
                       usages.Source.TypeSpecificationIntervalOrNull ?? new Interval();

        return new(
            587,
            $"Cannot use output value '{usages.Source.Name}' before it is declared'",
            interval);
    }

    internal static FunnyParseException VariableIsDeclaredAfterUsing(VariableUsages usages) => new(
        590, $"Variable '{usages.Source.Name}' used before it is declared'", usages.Usages.First().Interval);

    internal static FunnyParseException VariableIsAlreadyDeclared(string nodeId, Interval nodeInterval) => new(
        593, $"Variable {nodeId} is already declared", nodeInterval);

    #region typeSolving 6xx

    internal static FunnyParseException TypesNotSolved(ISyntaxNode syntaxNode) => new(
        600, $"Types cannot be solved ", syntaxNode.Interval);

    internal static FunnyParseException FunctionTypesNotSolved(UserFunctionDefinitionSyntaxNode node) => new(
        603, $"Function {node.GetFunAlias()} has invalid arguments or output type. Check function body expression", new Interval(node.Head.Interval.Start, node.Body.Interval.Start));

    internal static FunnyParseException OutputDefinitionDuplicates(EquationSyntaxNode node) => new(
        606, $"Output variable {node.Id} definition duplicates", node.Interval);

    internal static FunnyParseException OutputDefinitionTypeIsNotSolved(EquationSyntaxNode node) => new(
        609, $"Output variable '{node.Id}' type is incorrect", node.Interval);

    internal static FunnyParseException FunctionIsNotSolved(UserFunctionDefinitionSyntaxNode function) => new(
        612, $"Cannot calculate types for function '{function.Head}'. Check the expressions and/or add types to arguments/return", function.Interval);

    #endregion

    internal static FunnyParseException TranslateTicError(TicException ticException, ISyntaxNode syntaxNodeToSearch) {
        if (ticException is IncompatibleAncestorSyntaxNodeException syntaxNodeEx)
        {
            var concreteNode =
                SyntaxTreeDeepFieldSearch.FindNodeByOrderNumOrNull(syntaxNodeToSearch, syntaxNodeEx.SyntaxNodeId);
            if (concreteNode != null)
                return new(
                    601, $"Types cannot be solved: {ticException.Message} ",
                    concreteNode.Interval);
        }
        else if (ticException is IncompatibleAncestorNamedNodeException namedNodeEx)
        {
            var concreteNode =
                SyntaxTreeDeepFieldSearch.FindVarDefinitionOrNull(syntaxNodeToSearch, namedNodeEx.NodeName);
            if (concreteNode != null)
                return new(
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
                    return new(
                        603,
                        $"Recursive type definition: {string.Join("->", e.NodeNames)} ", concreteNode.Interval);
                }
            }

            foreach (var nodeId in e.NodeIds)
            {
                var concreteNode = SyntaxTreeDeepFieldSearch.FindNodeByOrderNumOrNull(syntaxNodeToSearch, nodeId);
                if (concreteNode != null)
                    return new(
                        603, $"Recursive type definition detected",
                        concreteNode.Interval);
            }
        }

        return TypesNotSolved(syntaxNodeToSearch);
    }
}

}