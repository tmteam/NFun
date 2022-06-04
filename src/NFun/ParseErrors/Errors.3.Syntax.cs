using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.ParseErrors; 

internal static partial class Errors {

    #region slices and indexing

    internal static FunnyParseException ArrayIndexOrSliceExpected(Tok openBraket) => new(
        310, $"Array index or slice expected after '['{Nl} Example: a[1] or a[1:3:2]", openBraket.Interval);

    internal static FunnyParseException ArrayIndexExpected(Tok openBraket, Tok closeBracket) => new(
        313, $"Array index expected inside of '[]'{Nl} Example: a[1]", openBraket.Start, closeBracket.Finish);

    internal static FunnyParseException ArrayInitializeSecondIndexMissed(Tok openBracket, Tok lastToken, Tok missedVal) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        if (string.IsNullOrWhiteSpace(missedVal?.Value))
            return new(
                316, $"'[x,..???]. Array hi bound expected but was nothing'{Nl} Example: a[1..2] or a[1..5 step 2]", start, finish);
        else
            return new(
                319, $"'[x,..???]. Array hi bound expected but was {ToText(missedVal)}'{Nl} Example: a[1..2] or a[1..5 step 2]", start, finish);
    }

    internal static FunnyParseException ArrayInitializeStepMissed(Tok openBracket, Tok lastToken, Tok missedVal) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        if (string.IsNullOrWhiteSpace(missedVal?.Value))
            return new(
                322, $"'[x..y step ???]. Array step expected but was nothing'{Nl} Example: a[1..5 step 2]", start, finish);
        else
            return new(
                325, $"'[x..y step ???]. Array step expected but was {ToText(missedVal)}'{Nl} Example: a[1..5 step 2]", start, finish);
    }

    internal static FunnyParseException ArrayIntervalInitializeCbrMissed(Tok openBracket, Tok lastToken, bool hasStep) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        return new(
            328, $"{(hasStep ? "[x..y step ???]" : "[x..y ???]")}. ']' was missed'{Nl} Example: a[1..5 step 2]", start, finish);
    }

    internal static FunnyParseException ArrayIndexCbrMissed(Tok openBracket, Tok lastToken) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        return new(
            331, $"a[x ??? <- was missed ']'{Nl} Example: a[1] or a[1:2] or a[1:5:2]", start, finish);
    }

    internal static FunnyParseException ArraySliceCbrMissed(Tok openBracket, Tok lastToken, bool hasStep) {
        int start = openBracket.Start;
        int finish = lastToken.Finish;
        return new(
            334, $"a{(hasStep ? "[x:y step]" : "[x:y]")} <- ']' was missed{Nl} Example: a[1:5 step 2]", start, finish);
    }

    #endregion


    #region arrays

    internal static FunnyParseException ArrayInitializeByListError(int openBracketTokenPos, TokFlow flow) {
        var res = GetExpressionListError(openBracketTokenPos, flow, TokType.ArrOBr, TokType.ArrCBr);
        var list = res.Parsed;
        var argStubs = CreateArgumentsStub(list);
        return res.Type switch {
                   ExprListErrorType.FirstElementMissed => new(
                       347, $"[ ??? , ..] <- First element missed {Nl}Remove ',' or place element before it", res.Interval),
                   ExprListErrorType.ElementMissed => new(
                       350, $"[{argStubs},???, ..] <- element missed {Nl}Remove ',' or place element before it", res.Interval),
                   ExprListErrorType.TotallyWrongDefinition => new(
                       353, "Wrong array definition ", res.Interval),
                   ExprListErrorType.SingleOpenBracket => new(
                       356, $"[ <- unexpected array symbol{Nl} Did you mean array initialization [,], slice [::] or indexing [i]?", res.Interval),
                   ExprListErrorType.SepIsMissing => new(
                       359, $"[{argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: [{argStubs}, myArgument, ...]", res.Interval),
                   ExprListErrorType.ArgumentIsInvalid => new(
                       362, $"[{argStubs}, ??? , ...  <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]", res.Interval),
                   ExprListErrorType.CloseBracketIsMissing => new(
                       365, $"[{argStubs} ??? <- Array close bracket ']' is missing{Nl} Example: [{argStubs}]", res.Interval),
                   ExprListErrorType.LastArgumentIsInvalid => new(
                       368, $"[{CreateArgumentsStub(list.Take(list.Length - 1))} ??? ] <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]", res.Interval),
                   _ => throw new ArgumentOutOfRangeException()
               };
    }

    #endregion


    #region if-else

    internal static FunnyParseException ConditionIsMissing(int conditionStart, int end) => new(
        378, $"if (???) {Nl} Condition expression is missing{Nl} Example: if (a>b)  ... ", conditionStart, end);

    internal static FunnyParseException ThenExpressionIsMissing(int conditionStart, int end) => new(
        381, $"if (a)  ???.  Expression is missing{Nl} Example: if (a)  a+1 ", conditionStart, end);

    internal static FunnyParseException ElseKeywordIsMissing(int ifelseStart, int end) => new(
        384, $"if (a) b ???.  Else keyword is missing{Nl} Example: if (a) b else c ", ifelseStart, end);

    internal static FunnyParseException ElseExpressionIsMissing(int ifelseStart, int end) => new(
        387, $"if (a) b else ???.  Else expression is missing{Nl} Example: if (a) b else c ", ifelseStart, end);

    internal static FunnyParseException IfKeywordIsMissing(int ifelseStart, int end) => new(
        390, $"if (a) b (if) ...  'if' is missing{Nl} Example: if (a) b if (c) d else c ", ifelseStart, end);

    internal static FunnyParseException IfConditionIsNotInBrackets(int ifelseStart, int end) => new(
        393, $"If condition is not in brackets{Nl} Example: if (a) b  else c ", ifelseStart, end);

    #endregion


    #region type defenitions

    internal static FunnyParseException TypeExpectedButWas(Tok token) => new(
        406, $"Expected: type, but was {ToText(token)}", token.Interval);

    internal static FunnyParseException ArrTypeCbrMissed(Interval interval) => new(
        409, $"']' is missed on array type", interval);

    internal static FunnyParseException UnexpectedSpaceBeforeArrayTypeBrackets(FunnyType elementType, Interval interval) => new(
        412, $"there should be no space before the square brackets when defining the array type. Example: 'a:{elementType}[]'", interval);

    #endregion


    #region attributes

    internal static FunnyParseException AttributeOnFunction(FunCallSyntaxNode lexNode) => new(
        422, $"Function cannot has attributes.", lexNode.Interval);

    internal static FunnyParseException ItIsNotAnAttribute(int start, Tok flowCurrent) => new(
        425, $"Attribute name expected, but was '{flowCurrent}'", start, flowCurrent.Finish);

    internal static FunnyParseException ItIsNotCorrectAttributeValue(Tok next) => new(
        428, $"Attribute value 'text' or 'number' or 'bool' expected, but was '{next}'", next.Interval);

    internal static FunnyParseException AttributeCbrMissed(int start, TokFlow flow) => new(
        431, $"')' is missed but was '{flow.Current}'", start, flow.Current.Interval.Finish);

    internal static FunnyParseException NowNewLineAfterAttribute(int start, TokFlow flow) => new(
        434, $"Attribute needs new line after it.", start, flow.Current.Interval.Finish);

    internal static FunnyParseException NowNewLineBeforeAttribute(TokFlow flow) => new(
        437, $"Attribute has to start from new line.", flow.Current.Interval);

    #endregion


    #region user functions

    internal static FunnyParseException FunDefTokenIsMissed(string funName, List<TypedVarDefSyntaxNode> arguments, Tok actual) => new(
        450, $"{Signature(funName, arguments)} ??? . '=' def sym, ol is skipped but was {ToText(actual)}{Nl}Example: {Signature(funName, arguments)} = ...",
        actual.Start, actual.Finish);


    internal static FunnyParseException UnexpectedBracketsOnFunDefinition(FunCallSyntaxNode headNode, int start, int finish) => new(
        453, $"Unexpected brackets on function definition ({headNode.Id}(...))=... {Nl}Example: {headNode.Id}(...)=...", start, finish);

    internal static FunnyParseException FunctionArgumentError(string id, int openBracketTokenPos, TokFlow flow) {
        var res = GetExpressionListError(openBracketTokenPos, flow, TokType.Obr, TokType.Cbr);
        var list = res.Parsed;
        var argStubs = CreateArgumentsStub(list);
        return res.Type switch {
                   ExprListErrorType.FirstElementMissed => new(
                       456, $"{id}( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it", res.Interval),
                   ExprListErrorType.ElementMissed => new(
                       458, $"{id}({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it", res.Interval),
                   ExprListErrorType.TotallyWrongDefinition => new(
                       460, "Wrong function call", res.Interval),
                   ExprListErrorType.SingleOpenBracket => new(
                       462, $"{id}( ??? <- Close bracket ')' is missing", res.Interval),
                   ExprListErrorType.SepIsMissing => new(
                       464, $"{id}({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: {id}({argStubs}, myArgument, ...)", res.Interval),
                   ExprListErrorType.ArgumentIsInvalid => new(
                       466, $"{id}({argStubs}, ??? , ...  <- Seems like function call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)", res.Interval),
                   ExprListErrorType.CloseBracketIsMissing => new(
                       468, $"{id}({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example: {id}({argStubs})", res.Interval),
                   ExprListErrorType.LastArgumentIsInvalid => new(
                       470, $"{id}({CreateArgumentsStub(list.Take(list.Length - 1))} ??? ) <- Seems like call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)", res.Interval),
                   _ => throw new ArgumentOutOfRangeException()
               };
    }

    internal static FunnyParseException WrongFunctionArgumentDefinition(FunCallSyntaxNode headNode, ISyntaxNode headNodeChild) => new(
        476, $"{headNode.Id}({ToFailureFunString(headNode, headNodeChild)}) = ... {Nl} Function argument is invalid. Variable name (with optional type) expected", headNodeChild.Interval);

    internal static FunnyParseException FunctionArgumentInBracketDefinition(FunCallSyntaxNode headNode, ISyntaxNode headNodeChild, Tok flowCurrent) {
        //todo asset
        if (flowCurrent == null) throw new ArgumentNullException(nameof(flowCurrent));
        var sb = ToFailureFunString(headNode, headNodeChild);

        return new(
            479, $"{headNode.Id}({sb}) = ... {Nl} Function argument is in bracket. Variable name (with optional type) without brackets expected", headNodeChild.Interval);
    }


    internal static FunnyParseException InvalidArgTypeDefinition(ISyntaxNode argumentNode) => new(
        482, ToShortText(argumentNode) + " is  not valid fun arg", argumentNode.Interval);

    internal static FunnyParseException FunctionDefinitionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode, Tok flowCurrent) => new(
        485, $"Function definition has start from new line. {Nl}Example : y:int{Nl}m(x) = x+1", exprStart, flowCurrent.Finish);

    internal static FunnyParseException FunExpressionIsMissed(string funName, List<TypedVarDefSyntaxNode> arguments, Interval interval) => new(
        488, $"{Signature(funName, arguments)} = ??? . Function body is missed {Nl}Example: {Signature(funName, arguments)} = #place your body here", interval);

    #endregion


    #region structs

    internal static FunnyParseException StructFieldDelimiterIsMissed(Interval interval) => new(
        501, "There is no separator between the fields of structures. Use ',' or new line to separate fields", interval);

    internal static FunnyParseException StructFieldIdIsMissed(Tok tok) => new(
        504, $"{tok} found instead of the structure field name", tok.Interval);

    internal static FunnyParseException StructFieldDefinitionTokenIsMissed(Tok tok) => new(
        507, $"{tok} found instead of the '=' symbol", tok.Interval);

    internal static FunnyParseException StructFieldBodyIsMissed(Tok id) => new(
        510, $"Field value is missed '{id} = ???'", id.Interval);

    internal static FunnyParseException StructIsUndone(int position) => new(
        510, "Struct definition is undone. Close bracket '}' is missed", Interval.Position(position));
    
    
    #endregion


    #region rules

    internal static FunnyParseException AnonymousFunBodyIsMissing(Interval interval) => new(
        520, "Anonymous fun body is missing. Did you forget '=' symbol?", interval);

    internal static FunnyParseException AnonymousFunArgumentIsIncorrect(ISyntaxNode node) => new(
        523, "Invalid anonymous fun argument", node.Interval);

    internal static FunnyParseException UnexpectedTokenEqualAfterRule(Interval nodeInterval) => new(
        526, "Unexpected '=' symbol. Did you forgot brackets after 'rule' keyword?", nodeInterval);

    #endregion


    #region flow

    internal static FunnyParseException UnknownValueAtStartOfExpression(int exprStart, Tok flowCurrent) => new(
        536, $"Unexpected symbol {ToText(flowCurrent)}. Equation, anonymous equation, function or type definition expected.", exprStart, flowCurrent.Finish);

    internal static FunnyParseException ExpressionBeforeTheDefinition(int exprStart, ISyntaxNode expression, Tok flowCurrent) => new(
        539, $"Unexpected expression {ToShortText(expression)} before definition. Equation, anonymous equation, function or type definition expected.", exprStart, flowCurrent.Finish);

    internal static FunnyParseException DefinitionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode, Tok flowCurrent) => new(
        542, $"Definition has start from new line. {Nl}Example : y:int{Nl}j = y+1", exprStart, flowCurrent.Finish);

    internal static FunnyParseException AnonymousExpressionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode, Tok flowCurrent) => new(
        545, $"Anonymous equation should start from new line. {Nl}Example : y:int{Nl}out =y+1", exprStart, flowCurrent.Finish);


    #endregion


    #region misc

    internal static FunnyParseException FunctionOrStructMemberNameIsMissedAfterDot(Tok token) => new(
        558, $"Function name or field name expected after '.'{Nl} Example: [1,2].myFunction(){Nl} Example: user.name", token.Interval);

    internal static FunnyParseException FunctionCallObrMissed(int funStart, string name, int position, ISyntaxNode pipedVal) {
        if (pipedVal == null)
            return new(
                561, $"{name}( ???. Close bracket ')' is missed{Nl} Example: {name}()", funStart, position);

        return new(
            564, $"{ToShortText(pipedVal)}.{name}( ???. Close bracket ')' is missed{Nl} Example: {ToShortText(pipedVal)}.{name}() or {name}({ToShortText(pipedVal)})", funStart, position);
    }

    internal static FunnyParseException BracketExpressionMissed(int start, int end, IList<ISyntaxNode> arguments) {
        var argumentsStub = CreateArgumentsStub(arguments);
        return new(
            567, $"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", start, end);
    }

    internal static FunnyParseException ExpressionListMissed(int start, int end, IList<ISyntaxNode> arguments) {
        var argumentsStub = CreateArgumentsStub(arguments);
        return new(
            570, $"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", start, end);
    }

    internal static FunnyParseException VarExpressionIsMissed(int start, string id, Tok flowCurrent) => new(
        573, $"{id} = ??? . Equation body is missed {Nl}Example: {id} = {id}+1", start, flowCurrent.Finish);

    internal static FunnyParseException BracketExpressionListError(int openBracketTokenPos, TokFlow flow) {
        var res = GetExpressionListError(openBracketTokenPos, flow, TokType.Obr, TokType.Cbr);
        var list = res.Parsed;
        var argStubs = CreateArgumentsStub(list);
        return res.Type switch {
                   ExprListErrorType.FirstElementMissed => new(
                       579, $"( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it", res.Interval),
                   ExprListErrorType.ElementMissed => new(
                       581, $"({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it", res.Interval),
                   ExprListErrorType.TotallyWrongDefinition => new(
                       583, "Wrong expression", res.Interval),
                   ExprListErrorType.SingleOpenBracket => new(
                       585, $"( <- unexpected open bracket without closing bracket", res.Interval),
                   ExprListErrorType.SepIsMissing => new(
                       587, $"({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: ({argStubs}, myArgument, ...)", res.Interval),
                   ExprListErrorType.ArgumentIsInvalid => new(
                       589, $"({argStubs}, ??? , ...  <- Seems like invalid expressions{Nl} Example: ({argStubs}, myArgument, ...)", res.Interval),
                   ExprListErrorType.CloseBracketIsMissing => new(
                       591, $"({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example:({argStubs})", res.Interval),
                   ExprListErrorType.LastArgumentIsInvalid => new(
                       593, $"({CreateArgumentsStub(list.Take(list.Length - 1))} ??? ) <- Seems like invalid expression{Nl} Example: ({argStubs}, myArgument, ...)", res.Interval),
                   _ => throw new ArgumentOutOfRangeException()
               };
    }

    internal static FunnyParseException NotAnExpression(ISyntaxNode node) => new(
        603, $"{GetDescription(node)} is not an expression", node.Interval);

    internal static FunnyParseException LeftBinaryArgumentIsMissing(Tok token) => new(
        606, $"expression is missed before '{ToText(token)}'", token.Interval);

    internal static FunnyParseException RightBinaryArgumentIsMissing(ISyntaxNode leftNode, Tok @operator) => new(
        609, $"{ToShortText(leftNode)} {ToText(@operator)} ???. Right expression is missed{Nl} Example: {ToShortText(leftNode)} {ToText(@operator)} e", leftNode.Interval.Start, @operator.Finish);

    #endregion

}