using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.ParseErrors
{
    public static class ErrorFactory
    {
        #region 1xx read tokens
        public static Exception QuoteAtEndOfString(char quoteSymbol, int start, int end) =>
            new FunParseException(130, $"Single '{quoteSymbol}' at end of string.", 
                start, end);

        public static Exception BackslashAtEndOfString(int start, int end) 
            => new FunParseException(133, $"Single '\\' at end of string.", start, end);

        public static Exception UnknownEscapeSequence(string sequence, int start, int end) =>
            new FunParseException(136, $"Unknown escape sequence \\{sequence}", 
                start, end);

        public static Exception ClosingQuoteIsMissed(char quoteSymbol, int start, int end)
            => new FunParseException(139, $"Closing {quoteSymbol} is missed at end of string" , 
                start, end);
        #endregion
       
        #region 2xx lex node parsing

        private static readonly string Nl = Environment.NewLine;
        public static Exception UnaryArgumentIsMissing(Tok operatorTok)
            => throw new FunParseException(301, $"{ErrorsHelper.ToString(operatorTok)} ???{Nl} right expression is missed{Nl} Example: {ErrorsHelper.ToString(operatorTok)} a",
                operatorTok.Interval);
        public static Exception MinusDuplicates(Tok previousTok, Tok currentTok)
            => throw new FunParseException(304,$"'--' is not allowed",
                previousTok.Start, currentTok.Finish);
        public static Exception LeftBinaryArgumentIsMissing(Tok token)
            => throw new FunParseException(307,$"expression is missed before '{ErrorsHelper.ToString(token)}'",
                token.Interval);
        public static Exception RightBinaryArgumentIsMissing(LexNode leftNode, Tok @operator)
            => throw new FunParseException(310,$"{ErrorsHelper.ToString(leftNode)} {ErrorsHelper.ToString(@operator)} ???. Right expression is missed{Nl} Example: {ErrorsHelper.ToString(leftNode)} {ErrorsHelper.ToString(@operator)} e",
                leftNode.Start, @operator.Finish);
        public static Exception RightBinaryArgumentIsMissing(ISyntaxNode leftNode, Tok @operator)
            => throw new FunParseException(310,$"{ErrorsHelper.ToString(leftNode)} {ErrorsHelper.ToString(@operator)} ???. Right expression is missed{Nl} Example: {ErrorsHelper.ToString(leftNode)} {ErrorsHelper.ToString(@operator)} e",
                leftNode.Interval.Start, @operator.Finish);

        public static Exception OperatorIsUnknown(Tok token)
            => throw new FunParseException(313,$"operator '{ErrorsHelper.ToString(token)}' is unknown",token.Interval);

        public static Exception NotAToken(Tok token)
            => throw new FunParseException(314,$"'{token.Value}' is not valid fun element. What did you mean?", token.Interval);
        
        public static Exception FunctionNameIsMissedAfterPipeForward(Tok token)
            => throw new FunParseException(316,$"Function name expected after '.'{Nl} Example: [1,2].myFunction()",token.Interval);

        public static Exception ArrayIndexOrSliceExpected(Tok openBraket)
            => new FunParseException(219,$"Array index or slice expected after '['{Nl} Example: a[1] or a[1:3:2]",openBraket.Interval);

        public static Exception ArrayIndexExpected(Tok openBraket, Tok closeBracket)
            =>  new FunParseException(222,$"Array index expected inside of '[]'{Nl} Example: a[1]",openBraket.Start,closeBracket.Finish);

        public static Exception ArrayInitializeSecondIndexMissed(Tok openBracket, Tok lastToken, Tok missedVal)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            if (string.IsNullOrWhiteSpace(missedVal?.Value))
                
                return new FunParseException(225,
                    $"'[x,..???]. Array hi bound expected but was nothing'{Nl} Example: a[1..2] or a[1..5..2]", start,
                    finish);
            else
                return new FunParseException(228,
                    $"'[x,..???]. Array hi bound expected but was {ErrorsHelper.ToString(missedVal)}'{Nl} Example: a[1..2] or a[1..5..2]", start, finish);
        }
        public static Exception ArrayInitializeStepMissed(Tok openBracket, Tok lastToken, Tok missedVal)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            if (string.IsNullOrWhiteSpace(missedVal?.Value))
                return new FunParseException(231,
                    $"'[x..y..???]. Array step expected but was nothing'{Nl} Example: a[1..5..2]", start,
                    finish);
            else
                return new FunParseException(234,
                    $"'[x..y..???]. Array step expected but was {ErrorsHelper.ToString(missedVal)}'{Nl} Example: a[1..5..2]", start, finish);
        }
        
        public static Exception ArrayIntervalInitializeCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(237,
                $"{(hasStep?"[x..y..step ???]":"[x..y ???]")}. ']' was missed'{Nl} Example: a[1..5..2]", start,
                finish);
        }
        
       
        
        public static Exception ArrayIndexCbrMissed(Tok openBracket, Tok lastToken)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(247,
                $"a[x ??? <- was missed ']'{Nl} Example: a[1] or a[1:2] or a[1:5:2]", start,
                finish);        
        }
        public static Exception ArraySliceCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(248,
                $"a{(hasStep?"[x:y:step]":"[x:y]")} <- ']' was missed{Nl} Example: a[1:5:2]", 
                start,
                finish);        
        }
        public static Exception ConditionIsMissing(int conditionStart, int end)
            => new FunParseException(249,
                $"if (???) {Nl} Condition expression is missing{Nl} Example: if (a>b)  ... ", conditionStart, end);
        
        public static Exception ThenExpressionIsMissing(int conditionStart, int end)
            => new FunParseException(252,$"if (a)  ???.  Expression is missing{Nl} Example: if (a)  a+1 ", conditionStart, end);

        public static Exception ElseKeywordIsMissing(int ifelseStart, int end)
            => new FunParseException(255,$"if (a) b ???.  Else keyword is missing{Nl} Example: if (a) b else c ", ifelseStart, end);

        public static Exception ElseExpressionIsMissing(int ifelseStart, int end)
            => new FunParseException(258,$"if (a) b else ???.  Else expression is missing{Nl} Example: if (a) b else c ", ifelseStart, end);

        public static Exception IfKeywordIsMissing(int ifelseStart, int end)
            => new FunParseException(261,$"if (a) b (if) ...  'if' is missing{Nl} Example: if (a) b if (c) d else c ", ifelseStart, end);

        
        public static Exception IfConditionIsNotInBrackets(int ifelseStart, int end)
            => new FunParseException(264,$"If condition is not in brackets{Nl} Example: if (a) b  else c ", ifelseStart, end);
        
        public static Exception NewLineMissedBeforeRepeatedIf(Interval interval)
            => new FunParseException(265,$"Not first if has to start from new line{Nl} Example: if (a) b {Nl} if(c) d  else e ", interval);

        
        public static Exception FunctionCallObrMissed(int funStart, string name, int position,LexNode pipedVal)
        {
            if(pipedVal==null)
                return new FunParseException(267,
                    $"{name}( ???. Close bracket ')' is missed{Nl} Example: {name}()", 
                    funStart, 
                    position);

            return new FunParseException(270,
                $"{ErrorsHelper.ToString(pipedVal)}.{name}( ???. Close bracket ')' is missed{Nl} Example: {ErrorsHelper.ToString(pipedVal)}.{name}() or {name}({ErrorsHelper.ToString(pipedVal)})", 
                funStart, 
                position);
        }
        
        public static Exception FunctionCallObrMissed(int funStart, string name, int position,ISyntaxNode pipedVal)
        {
            if(pipedVal==null)
                return new FunParseException(267,
                    $"{name}( ???. Close bracket ')' is missed{Nl} Example: {name}()", 
                    funStart, 
                    position);

            return new FunParseException(270,
                $"{ErrorsHelper.ToString(pipedVal)}.{name}( ???. Close bracket ')' is missed{Nl} Example: {ErrorsHelper.ToString(pipedVal)}.{name}() or {name}({ErrorsHelper.ToString(pipedVal)})", 
                funStart, 
                position);
        }

        public static Exception TypeExpectedButWas(Tok token) 
            => new FunParseException(271, $"Expected: type, but was {ErrorsHelper.ToString(token)}", token.Interval);

        public static Exception ArrTypeCbrMissed(Interval interval)
            => new FunParseException(274, $"']' is missed on array type", interval);
        
        public static Exception BracketExpressionMissed(int start, int end, IList<ISyntaxNode> arguments)
        {
            var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
            return new FunParseException(282,$"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", 
                start,end);
        }
        public static Exception BracketExpressionMissed(int start, int end, IList<LexNode> arguments)
        {
            var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
            return new FunParseException(282,$"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", 
                start,end);
        }
        
        public static Exception ExpressionListMissed(int start, int end, IList<ISyntaxNode> arguments)
        {
            var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
            return new FunParseException(283,$"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", 
                start,end);
        }
        public static Exception ExpressionListMissed(int start, int end, IList<LexNode> arguments)
        {
            var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
            return new FunParseException(283,$"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", 
                start,end);
        }
        
        
        public static Exception AttributeOnFunction(int exprStart, FunCallSyntaxNode lexNode) 
            => new FunParseException(286,$"Function cannot has attributes.", lexNode.Interval);

        public static Exception AttributeOnFunction(int exprStart, LexNode lexNode) 
            => new FunParseException(286,$"Function cannot has attributes.", lexNode.Interval);

        public static Exception ItIsNotAnAttribute(int start, Tok flowCurrent)
            => new FunParseException(289,$"Attribute name expected, but was '{flowCurrent}'", 
                start, flowCurrent.Finish);


        public static Exception ItIsNotCorrectAttributeValue(Tok next)
            => new FunParseException(292,$"Attribute value 'text' or 'number' or 'boolean' expected, but was '{next}'", 
                next.Interval);

        public static Exception AttributeCbrMissed(int start, TokenFlow flow)
            => new FunParseException(295,$"')' is missed but was '{flow.Current}'", 
                start, flow.Current.Interval.Finish);

        public static Exception NowNewLineAfterAttribute(int start, TokenFlow flow)
            => new FunParseException(298,$"Attribute needs new line after it.", 
                    start, flow.Current.Interval.Finish);
        public static Exception NowNewLineBeforeAttribute(TokenFlow flow)
            => new FunParseException(299,$"Attribute has to start from new line.", 
                flow.Current.Interval);
        #endregion

        #region  3xx - hi level parsing
        
        public static Exception UnexpectedExpression(LexNode lexNode) 
            => new FunParseException(300,$"Unexpected expression {ErrorsHelper.ToString(lexNode)}", lexNode.Interval);
        public static Exception UnexpectedExpression(ISyntaxNode lexNode) 
            => new FunParseException(300,$"Unexpected expression {ErrorsHelper.ToString(lexNode)}", lexNode.Interval);

        public static Exception FunDefTokenIsMissed(string funName, List<VarDefenitionSyntaxNode> arguments, Tok actual)
        {
            return new FunParseException(301, $"{ErrorsHelper.Signature(funName, arguments)} ??? . '=' def symbol is skipped but was {ErrorsHelper.ToString(actual)}{Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = ...", 
                actual.Start, actual.Finish);
        }
        
        public static Exception FunDefTokenIsMissed(string funName, List<LexVarDefenition> arguments, Tok actual)
        {
            return new FunParseException(301, $"{ErrorsHelper.Signature(funName, arguments)} ??? . '=' def symbol is skipped but was {ErrorsHelper.ToString(actual)}{Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = ...", 
                actual.Start, actual.Finish);
        }
        
        public static Exception FunExpressionIsMissed(string funName, List<VarDefenitionSyntaxNode> arguments, Interval interval) 
            => new FunParseException(304,
                $"{ErrorsHelper.Signature(funName, arguments)} = ??? . Function body is missed {Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = #place your body here", 
                interval);
            
        public static Exception FunExpressionIsMissed(string funName, List<LexVarDefenition> arguments, Interval interval) 
            => new FunParseException(304,
                $"{ErrorsHelper.Signature(funName, arguments)} = ??? . Function body is missed {Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = #place your body here", 
                interval);

        public static Exception UnknownValueAtStartOfExpression(int exprStart, Tok flowCurrent) 
            => new FunParseException(307,$"Unexpected symbol {ErrorsHelper.ToString(flowCurrent)}. Equation, anonymous equation, function or type defenition expected.", exprStart, flowCurrent.Finish);
        
        public static Exception ExpressionBeforeTheDefenition(int exprStart, ISyntaxNode expression, Tok flowCurrent)
            => new FunParseException(310,$"Unexpected expression {ErrorsHelper.ToString(expression)} before defenition. Equation, anonymous equation, function or type defenition expected.", exprStart, flowCurrent.Finish);
        
        public static Exception ExpressionBeforeTheDefenition(int exprStart, LexNode expression, Tok flowCurrent)
            => new FunParseException(310,$"Unexpected expression {ErrorsHelper.ToString(expression)} before defenition. Equation, anonymous equation, function or type defenition expected.", exprStart, flowCurrent.Finish);

        public static Exception AnonymousExpressionHasToStartFromNewLine(int exprStart, ISyntaxNode lexNode, Tok flowCurrent)
            =>   throw new FunParseException(313,$"Anonymous equation should start from new line. {Nl}Example : y:int{Nl}y+1 #out = y:int+1", exprStart, flowCurrent.Finish);
        
        public static Exception AnonymousExpressionHasToStartFromNewLine(int exprStart, LexNode lexNode, Tok flowCurrent)
            =>   throw new FunParseException(313,$"Anonymous equation should start from new line. {Nl}Example : y:int{Nl}y+1 #out = y:int+1", exprStart, flowCurrent.Finish);
        
        public static Exception OnlyOneAnonymousExpressionAllowed(int exprStart, ISyntaxNode lexNode, Tok flowCurrent)
            =>   throw new FunParseException(316,$"Only one anonymous equation allowed", exprStart, flowCurrent.Finish);
        public static Exception OnlyOneAnonymousExpressionAllowed(int exprStart, LexNode lexNode, Tok flowCurrent)
            =>   throw new FunParseException(316,$"Only one anonymous equation allowed", exprStart, flowCurrent.Finish);
        public static Exception UnexpectedBracketsOnFunDefenition(FunCallSyntaxNode headNode, int start, int finish)
            => new FunParseException(319, $"Unexpected brackets on function defenition ({headNode.Value}(...))=... {Nl}Example: {headNode.Value}(...)=...", 
                start, finish);
        public static Exception UnexpectedBracketsOnFunDefenition(LexNode headNode, int start, int finish)
            => new FunParseException(319, $"Unexpected brackets on function defenition ({headNode.Value}(...))=... {Nl}Example: {headNode.Value}(...)=...", 
                start, finish);

        
        public static Exception WrongFunctionArgumentDefenition(int start, FunCallSyntaxNode headNode, ISyntaxNode headNodeChild,
            Tok flowCurrent)
        {
            var sb = ErrorsHelper.ToFailureFunString(headNode, headNodeChild);
            return new FunParseException(322,
                    $"{headNode.Value}({sb}) = ... {Nl} Function argument is invalid. Variable name (with optional type) expected", 
                    headNodeChild.Interval);
        }
        
        public static Exception WrongFunctionArgumentDefenition(int start, LexNode headNode, LexNode headNodeChild,
            Tok flowCurrent)
        {
            var sb = ErrorsHelper.ToFailureFunString(headNode, headNodeChild);
            return new FunParseException(322,
                $"{headNode.Value}({sb}) = ... {Nl} Function argument is invalid. Variable name (with optional type) expected", 
                headNodeChild.Interval);
        }


        public static Exception FunctionArgumentInBracketDefenition(int start, FunCallSyntaxNode headNode, ISyntaxNode headNodeChild,
            Tok flowCurrent)
        {
            var sb = ErrorsHelper.ToFailureFunString(headNode, headNodeChild);

            return new FunParseException(325,
                $"{headNode.Value}({sb.ToString()}) = ... {Nl} Function argument is in bracket. Variable name (with optional type) without brackets expected", 
                headNodeChild.Interval.Start, headNodeChild.Interval.Finish);
        }
        public static Exception FunctionArgumentInBracketDefenition(int start, LexNode headNode, LexNode headNodeChild,
            Tok flowCurrent)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var child in headNode.Children)
            {
                if (child == headNodeChild)
                    sb.Append("(???)");
                else if (child.Is(LexNodeType.Var))
                    sb.Append(child.Value);
                if (headNode.Children.Last() != child)
                    sb.Append(",");
            }
            return new FunParseException(325,
                $"{headNode.Value}({sb.ToString()}) = ... {Nl} Function argument is in bracket. Variable name (with optional type) without brackets expected", 
                headNodeChild.Start, headNodeChild.Finish);
        }
        
        public static Exception VarExpressionIsMissed(int start, string id, Tok flowCurrent)
            => new FunParseException(328, $"{id} = ??? . Equation body is missed {Nl}Example: {id} = {id}+1", 
                start, flowCurrent.Finish);
       
        public static Exception OutputNameDuplicates(string id, LexNode lexEquationExpression)
            => new FunParseException(331, $"{id}<-  output name duplicates ", lexEquationExpression.Interval);

        public static Exception OutputNameWithDifferentCase(string id, Interval interval)
            => new FunParseException(334, $"{id}<-  output name is same to name  {id}", interval);
        
        public static Exception InputNameWithDifferentCase(string id, LexNode lexEquationExpression)
            => new FunParseException(334, $"{lexEquationExpression.Value}<-  input name is same to name  {id}", lexEquationExpression.Interval);

        #endregion

        #region  4xx - errors of lists

         public static Exception ArrayInitializeByListError(int openBracketTokenPos, TokenFlow flow)
        {
            var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.ArrOBr, TokType.ArrCBr);
            var list = res.Parsed;
            var argStubs = ErrorsHelper.CreateArgumentsStub(list);
            switch (res.Type)
            {
                case ExprListErrorType.FirstElementMissed:
                    return new FunParseException(418,
                        $"[ ??? , ..] <- First element missed {Nl}Remove ',' or place element before it", res.Interval);
                case ExprListErrorType.ElementMissed:
                    return new FunParseException(419,
                        $"[{argStubs},???, ..] <- element missed {Nl}Remove ',' or place element before it", 
                        res.Interval);
                case ExprListErrorType.TotalyWrongDefenition:
                    return new FunParseException(421, "Wrong array defenition ", res.Interval);
                case ExprListErrorType.SingleOpenBracket:
                    return new FunParseException(422,
                        $"[ <- unexpected array symbol{Nl} Did you mean array initialization [,], slice [::] or indexing [i]?", 
                        res.Interval);
                case ExprListErrorType.SepIsMissing:
                    return new FunParseException(423,
                        $"[{argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: [{argStubs}, myArgument, ...]",
                        res.Interval);
                case ExprListErrorType.ArgumentIsInvalid:
                    return new FunParseException(424,
                        $"[{argStubs}, ??? , ...  <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]",
                        res.Interval);
                case ExprListErrorType.CloseBracketIsMissing:
                    return new FunParseException(425,
                        $"[{argStubs} ??? <- Array close bracket ']' is missing{Nl} Example: [{argStubs}]",
                        res.Interval);
                case ExprListErrorType.LastArgumentIsInvalid:
                    return new FunParseException(426,
                        $"[{ErrorsHelper.CreateArgumentsStub(list.Take(list.Length-1))} ??? ] <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]",
                        res.Interval);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        
        public static Exception FunctionArgumentError(string id, int openBracketTokenPos, TokenFlow flow)
        {
            var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.Obr, TokType.Cbr);
            var list = res.Parsed;
            var argStubs = ErrorsHelper.CreateArgumentsStub(list);
            switch (res.Type)
            {
                case ExprListErrorType.FirstElementMissed:
                    return new FunParseException(438,
                        $"{id}( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it", res.Interval);
                case ExprListErrorType.ElementMissed:
                    return new FunParseException(439,
                        $"{id}({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it", 
                        res.Interval);
                case ExprListErrorType.TotalyWrongDefenition:
                    return new FunParseException(441, "Wrong function call", res.Interval);
                case ExprListErrorType.SingleOpenBracket:
                    return new FunParseException(442,
                        $"( <- unexpected bracket{Nl} ?", 
                        res.Interval);
                case ExprListErrorType.SepIsMissing:
                    return new FunParseException(443,
                        $"{id}({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: {id}({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.ArgumentIsInvalid:
                    return new FunParseException(444,
                        $"{id}({argStubs}, ??? , ...  <- Seems like function call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.CloseBracketIsMissing:
                    return new FunParseException(445,
                        $"{id}({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example: {id}({argStubs})",
                        res.Interval);
                case ExprListErrorType.LastArgumentIsInvalid:
                    return new FunParseException(446,
                        $"{id}({ErrorsHelper.CreateArgumentsStub(list.Take(list.Length-1))} ??? ) <- Seems like call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)",
                        res.Interval);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        
        public static Exception BracketExpressionListError(int openBracketTokenPos, TokenFlow flow)
        {
            var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.Obr, TokType.Cbr);
            var list = res.Parsed;
            var argStubs = ErrorsHelper.CreateArgumentsStub(list);
            switch (res.Type)
            {
                case ExprListErrorType.FirstElementMissed:
                    return new FunParseException(468,
                        $"( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it", res.Interval);
                case ExprListErrorType.ElementMissed:
                    return new FunParseException(469,
                        $"({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it", 
                        res.Interval);
                case ExprListErrorType.TotalyWrongDefenition:
                    return new FunParseException(471, "Wrong expression", res.Interval);
                case ExprListErrorType.SingleOpenBracket:
                    return new FunParseException(472,
                        $"( <- unexpected bracket{Nl} ?", 
                        res.Interval);
                case ExprListErrorType.SepIsMissing:
                    return new FunParseException(473,
                        $"({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: ({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.ArgumentIsInvalid:
                    return new FunParseException(474,
                        $"({argStubs}, ??? , ...  <- Seems like invalid expressions{Nl} Example: ({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.CloseBracketIsMissing:
                    return new FunParseException(475,
                        $"({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example:({argStubs})",
                        res.Interval);
                case ExprListErrorType.LastArgumentIsInvalid:
                    return new FunParseException(476,
                        $"({ErrorsHelper.CreateArgumentsStub(list.Take(list.Length-1))} ??? ) <- Seems like invalid expression{Nl} Example: ({argStubs}, myArgument, ...)",
                        res.Interval);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region 5xx - Interpritation exceptions

        public static Exception CycleEquationDependencies(LexEquation[] result)
        {
            var expression = result.First().Expression;
            return new FunParseException(501,"Cycle dependencies found: " 
                                         + string.Join("->", result.Select(r=>r.Id)), 
                expression.Interval);
        }
        public static Exception NotAnExpression(LexNode node) 
            => new FunParseException(503,$"{node} is not an expression", node.Interval);
  
        public static Exception ImpossibleCast(VarType from,VarType to, Interval interval)
            => new FunParseException(506, $"Unable to cast from {from} to {to}", interval);
      
        public static Exception InvalidArgTypeDefenition(LexNode argumentNode) 
            => new FunParseException(506, ErrorsHelper.ToString(argumentNode) + " is  not valid fun arg", argumentNode.Interval);

        public static Exception AnonymousFunDefenitionIsMissing(LexNode node)
            => new FunParseException(509, "Anonymous fun defenition is missing", node.Interval);

        public static Exception AnonymousFunBodyIsMissing(LexNode node)
            => new FunParseException(512, "Anonymous fun body is missing", node.Interval);

        public static Exception AmbiguousCallOfFunction(IList<FunctionBase> funVars, LexNode varName)
            => throw new FunParseException(515,$"Ambiguous call of function with name: {varName.Value}", varName.Interval);

        public static Exception AmbiguousFunctionChoise(IList<FunctionBase> funVars, LexNode varName)
            => throw new FunParseException(516,$"Several functions with name: {varName.Value} can be used in expression. Did you mean input variable instead of function?", varName.Interval);
        
        public static Exception ArrayInitializerTypeMismatch(VarType stepType, LexNode node)
            => throw new FunParseException(518,$"Array initializator step has to be int type only but was '{stepType}'. Example: [1..5..2]", node.Interval);

        public static Exception CannotParseNumber(LexNode node)
            => new FunParseException(521, $"Cannot parse number '{node.Value}'", node.Interval);

        public static Exception FunctionNotFound(LexNode node, List<IExpressionNode> children,
            FunctionsDictionary functions)
        {
            return new FunParseException(524,
                $"Function {node.Value}({string.Join(", ", children.Select(c => c.Type))}) is not defined",
                node.Interval);
        }

        public static Exception UnknownVariables(IEnumerable<VariableExpressionNode> values)
        {
            if (values.Count() == 1)
                return new FunParseException(527,$"Unknown variable \"{values.First()}\"",values.First().Interval);
            return new FunParseException(530,$"Unknown variables \"{string.Join(", ", values)}\"",values.First().Interval);
        }

        public static Exception FunctionAlreadyExist(LexFunction userFun) 
            => new FunParseException(533,$"Function  {ErrorsHelper.Signature(userFun.Id, userFun.Args)} already exist", 
                new Interval( userFun.Head.Interval.Start,userFun.Node.Interval.Finish));


        public static Exception NoCommonCast(IEnumerable<IExpressionNode> nodes) 
            => new FunParseException(536,
                "There are no common convertion between types "+ string.Join(",", nodes.Select(n=>n.Type)), 
                new Interval(nodes.Min(n=>n.Interval.Start),nodes.Max(n=>n.Interval.Finish)));

        public static Exception IfConditionIsNotBool(IExpressionNode condition) 
            => new FunParseException(539,"if Condition has to be boolean but was "+ condition.Type, condition.Interval);

        public static Exception InvalidOutputType(FunctionBase function, Interval interval) 
            => new FunParseException(542, $"'{function.OutputType}' is not supported as output parameter of {function.Name}()", interval);

        public static Exception FunctionArgumentDuplicates(LexFunction lexFunction, LexVarDefenition lexFunctionArg) 
            => new FunParseException(545, $"'Argument name '{lexFunctionArg.Id}' duplicates at  {ErrorsHelper.Signature(lexFunction.Id, lexFunction.Args)} ", lexFunction.Head.Interval);

        public static Exception AnonymousFunctionArgumentDuplicates(FunArgumentExpressionNode argNode,LexNode funDefenition)
            => new FunParseException(548, $"'Argument name '{argNode.Name}' of anonymous fun duplicates ", funDefenition.Interval);

        public static Exception AnonymousFunctionArgumentConflictsWithOuterScope(FunArgumentExpressionNode argNode, LexNode funDefenition)
            => new FunParseException(551, $"'Argument name '{argNode.Name}' of anonymous fun conflicts with outer scope variable. It is denied for your safety.", funDefenition.Interval);
        public static Exception VariousArrayElementTypes(IExpressionNode[] elements, int failureIndex) {
            var firstType = elements[0].Type;
            var failureType = elements[failureIndex].Type;
            return new FunParseException(554, $"'Not equal array element types: {firstType} and {failureType}",
                new Interval(elements[failureIndex-1].Interval.Start, elements[failureIndex].Interval.Finish));
        }

        public static Exception CannotUseOutputValueBeforeItIsDeclared(VariableUsages usages, string equationId) 
            => new FunParseException(560, $"Cannot use output value '{equationId}' before it is declared'",
                usages.Nodes.First().Interval);

        public static Exception VariableIsDeclaredAfterUsing(VariableUsages usages)
            => new FunParseException(563, $"Variable '{usages.Source.Name}' used before it is declared'",
                usages.Nodes.First().Interval);       
        #endregion

       
    }
}