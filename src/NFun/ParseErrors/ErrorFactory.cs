using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.LexAnalyze;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.ParseErrors
{
    public static class ErrorFactory
    {
        #region 1xx lex node parsing

        private static readonly string Nl = Environment.NewLine;
        public static Exception UnaryArgumentIsMissing(Tok operatorTok)
            => throw new FunParseException(101, $"{ErrorsHelper.ToString(operatorTok)} ???{Nl} right expression is missed{Nl} Example: {ErrorsHelper.ToString(operatorTok)} a",
                operatorTok.Interval);
        public static Exception MinusDuplicates(Tok previousTok, Tok currentTok)
            => throw new FunParseException(104,$"'--' is not allowed",
                previousTok.Start, currentTok.Finish);
        public static Exception LeftBinaryArgumentIsMissing(Tok token)
            => throw new FunParseException(107,$"expression is missed before '{ErrorsHelper.ToString(token)}'",
                token.Interval);
        public static Exception RightBinaryArgumentIsMissing(LexNode leftNode, Tok @operator)
            => throw new FunParseException(110,$"{ErrorsHelper.ToString(leftNode)} {ErrorsHelper.ToString(@operator)} ???. Right expression is missed{Nl} Example: {ErrorsHelper.ToString(leftNode)} {ErrorsHelper.ToString(@operator)} e",
                leftNode.Start, @operator.Finish);
        
        public static Exception OperatorIsUnknown(Tok token)
            => throw new FunParseException(113,$"operator '{ErrorsHelper.ToString(token)}' is unknown",token.Interval);

        public static Exception NotAToken(Tok token)
            => throw new FunParseException(114,$"'{token.Value}' is not valid fun element. What did you mean?", token.Interval);
        
        public static Exception FunctionNameIsMissedAfterPipeForward(Tok token)
            => throw new FunParseException(116,$"Function name expected after '.'{Nl} Example: [1,2].myFunction()",token.Interval);

        public static Exception ArrayIndexOrSliceExpected(Tok openBraket)
            => new FunParseException(119,$"Array index or slice expected after '['{Nl} Example: a[1] or a[1:3:2]",openBraket.Interval);

        public static Exception ArrayIndexExpected(Tok openBraket, Tok closeBracket)
            =>  new FunParseException(122,$"Array index expected inside of '[]'{Nl} Example: a[1]",openBraket.Start,closeBracket.Finish);

        public static Exception ArrayInitializeSecondIndexMissed(Tok openBracket, Tok lastToken, Tok missedVal)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            if (string.IsNullOrWhiteSpace(missedVal?.Value))
                
                return new FunParseException(125,
                    $"'[x,..???]. Array hi bound expected but was nothing'{Nl} Example: a[1..2] or a[1..5..2]", start,
                    finish);
            else
                return new FunParseException(128,
                    $"'[x,..???]. Array hi bound expected but was {ErrorsHelper.ToString(missedVal)}'{Nl} Example: a[1..2] or a[1..5..2]", start, finish);
        }
        public static Exception ArrayInitializeStepMissed(Tok openBracket, Tok lastToken, Tok missedVal)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            if (string.IsNullOrWhiteSpace(missedVal?.Value))
                return new FunParseException(131,
                    $"'[x..y..???]. Array step expected but was nothing'{Nl} Example: a[1..5..2]", start,
                    finish);
            else
                return new FunParseException(134,
                    $"'[x..y..???]. Array step expected but was {ErrorsHelper.ToString(missedVal)}'{Nl} Example: a[1..5..2]", start, finish);
        }
        
        public static Exception ArrayIntervalInitializeCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(137,
                $"{(hasStep?"[x..y..step ???]":"[x..y ???]")}. ']' was missed'{Nl} Example: a[1..5..2]", start,
                finish);
        }
        
       
        
        public static Exception ArrayIndexCbrMissed(Tok openBracket, Tok lastToken)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(147,
                $"a[x ??? <- was missed ']'{Nl} Example: a[1] or a[1:2] or a[1:5:2]", start,
                finish);        
        }
        public static Exception ArraySliceCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(148,
                $"a{(hasStep?"[x:y:step]":"[x:y]")} <- ']' was missed{Nl} Example: a[1:5:2]", 
                start,
                finish);        
        }
        public static Exception ConditionIsMissing(int conditionStart, int end)
            => new FunParseException(149,
                $"if ??? then{Nl} Condition expression is missing{Nl} Example: if a>b then ... ", conditionStart, end);
        
        public static Exception ThenExpressionIsMissing(int conditionStart, int end)
            => new FunParseException(152,$"if a then ???.  Expression is missing{Nl} Example: if a then a+1 ", conditionStart, end);

        public static Exception ElseKeywordIsMissing(int ifelseStart, int end)
            => new FunParseException(155,$"if a then b ???.  Else keyword is missing{Nl} Example: if a then b else c ", ifelseStart, end);

        public static Exception ElseExpressionIsMissing(int ifelseStart, int end)
            => new FunParseException(158,$"if a then b else ???.  Else expression is missing{Nl} Example: if a then b else c ", ifelseStart, end);

        public static Exception IfKeywordIsMissing(int ifelseStart, int end)
            => new FunParseException(161,$"if a then b (if) ...  'if' is missing{Nl} Example: if a then b if c then d else c ", ifelseStart, end);

        public static Exception ThenKeywordIsMissing(int ifelseStart, int end)
            => new FunParseException(164,$"if a (then) b  ...  'then' is missing{Nl} Example: if a then b  else c ", ifelseStart, end);
       
        public static Exception FunctionCallObrMissed(int funStart, string name, int position,LexNode pipedVal)
        {
            if(pipedVal==null)
                return new FunParseException(167,
                    $"{name}( ???. Close bracket ')' is missed{Nl} Example: {name}()", 
                    funStart, 
                    position);

            return new FunParseException(170,
                $"{ErrorsHelper.ToString(pipedVal)}.{name}( ???. Close bracket ')' is missed{Nl} Example: {ErrorsHelper.ToString(pipedVal)}.{name}() or {name}({ErrorsHelper.ToString(pipedVal)})", 
                funStart, 
                position);
        }

        public static Exception TypeExpectedButWas(Tok token) 
            => new FunParseException(171, $"Expected: type, but was {ErrorsHelper.ToString(token)}", token.Interval);

        public static Exception ArrTypeCbrMissed(Interval interval)
            => new FunParseException(174, $"']' is missed on array type", interval);
        
        public static Exception BracketExpressionMissed(int start, int end, IList<LexNode> arguments)
        {
            var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
            return new FunParseException(182,$"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", 
                start,end);
        }
        public static Exception ExpressionListMissed(int start, int end, IList<LexNode> arguments)
        {
            var argumentsStub = ErrorsHelper.CreateArgumentsStub(arguments);
            return new FunParseException(183,$"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", 
                start,end);
        }
        #endregion

        #region  2xx - hi level parsing
        
        public static Exception UnexpectedExpression(LexNode lexNode) 
            => new FunParseException(200,$"Unexpected expression {ErrorsHelper.ToString(lexNode)}", lexNode.Interval);
        public static Exception FunDefTokenIsMissed(string funName, List<VariableInfo> arguments, Tok actual)
        {
            return new FunParseException(201, $"{ErrorsHelper.Signature(funName, arguments)} ??? . '=' def symbol is skipped but was {ErrorsHelper.ToString(actual)}{Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = ...", 
                actual.Start, actual.Finish);
        }
        public static Exception FunExpressionIsMissed(string funName, List<VariableInfo> arguments, Interval interval) 
            => new FunParseException(204,
                $"{ErrorsHelper.Signature(funName, arguments)} = ??? . Function body is missed {Nl}Example: {ErrorsHelper.Signature(funName, arguments)} = #place your body here", 
                interval);

        public static Exception UnknownValueAtStartOfExpression(int exprStart, Tok flowCurrent) 
            => new FunParseException(207,$"Unexpected symbol {ErrorsHelper.ToString(flowCurrent)}. Equation, anonymous equation, function or type defenition expected.", exprStart, flowCurrent.Finish);
        public static Exception ExpressionBeforeTheDefenition(int exprStart, LexNode expression, Tok flowCurrent)
            => new FunParseException(210,$"Unexpected expression {ErrorsHelper.ToString(expression)} before defenition. Equation, anonymous equation, function or type defenition expected.", exprStart, flowCurrent.Finish);

        public static Exception AnonymousExpressionHasToStartFromNewLine(int exprStart, LexNode lexNode, Tok flowCurrent)
            =>   throw new FunParseException(213,$"Anonymous equation should start from new line. {Nl}Example : y:int{Nl}y+1 #out = y:int+1", exprStart, flowCurrent.Finish);

        public static Exception OnlyOneAnonymousExpressionAllowed(int exprStart, LexNode lexNode, Tok flowCurrent)
            =>   throw new FunParseException(216,$"Only one anonymous equation allowed", exprStart, flowCurrent.Finish);
        public static Exception UnexpectedBracketsOnFunDefenition(LexNode headNode, int start, int finish)
            => new FunParseException(219, $"Unexpected brackets on function defenition ({headNode.Value}(...))=... {Nl}Example: {headNode.Value}(...)=...", 
                start, finish);

        
        public static Exception WrongFunctionArgumentDefenition(int start, LexNode headNode, LexNode headNodeChild,
            Tok flowCurrent)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var child in headNode.Children)
            {
                if (child == headNodeChild)
                    sb.Append("???");
                else if (child.Is(LexNodeType.Var))
                    sb.Append(child.Value);
                if (headNode.Children.Last() != child)
                    sb.Append(",");
            }
            return new FunParseException(222,
                    $"{headNode.Value}({sb}) = ... {Nl} Function argument is invalid. Variable name (with optional type) expected", 
                    headNodeChild.Interval);
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
            return new FunParseException(225,
                $"{headNode.Value}({sb.ToString()}) = ... {Nl} Function argument is in bracket. Variable name (with optional type) without brackets expected", 
                headNodeChild.Start, headNodeChild.Finish);
        }

       
        public static Exception VarExpressionIsMissed(int start, string id, Tok flowCurrent)
            => new FunParseException(228, $"{id} = ??? . Equation body is missed {Nl}Example: {id} = {id}+1", 
                start, flowCurrent.Finish);
       
        #endregion

        #region  3xx - errors of lists

         public static Exception ArrayInitializeByListError(int openBracketTokenPos, TokenFlow flow)
        {
            var res = ErrorsHelper.GetExpressionListError(openBracketTokenPos, flow, TokType.ArrOBr, TokType.ArrCBr);
            var list = res.Parsed;
            var argStubs = ErrorsHelper.CreateArgumentsStub(list);
            switch (res.Type)
            {
                case ExprListErrorType.FirstElementMissed:
                    return new FunParseException(318,
                        $"[ ??? , ..] <- First element missed {Nl}Remove ',' or place element before it", res.Interval);
                case ExprListErrorType.ElementMissed:
                    return new FunParseException(319,
                        $"[{argStubs},???, ..] <- element missed {Nl}Remove ',' or place element before it", 
                        res.Interval);
                case ExprListErrorType.TotalyWrongDefenition:
                    return new FunParseException(321, "Wrong array defenition ", res.Interval);
                case ExprListErrorType.SingleOpenBracket:
                    return new FunParseException(322,
                        $"[ <- unexpected array symbol{Nl} Did you mean array initialization [,], slice [::] or indexing [i]?", 
                        res.Interval);
                case ExprListErrorType.SepIsMissing:
                    return new FunParseException(323,
                        $"[{argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: [{argStubs}, myArgument, ...]",
                        res.Interval);
                case ExprListErrorType.ArgumentIsInvalid:
                    return new FunParseException(324,
                        $"[{argStubs}, ??? , ...  <- Seems like array argument is invalid{Nl} Example: [{argStubs}, myArgument, ...]",
                        res.Interval);
                case ExprListErrorType.CloseBracketIsMissing:
                    return new FunParseException(325,
                        $"[{argStubs} ??? <- Array close bracket ']' is missing{Nl} Example: [{argStubs}]",
                        res.Interval);
                case ExprListErrorType.LastArgumentIsInvalid:
                    return new FunParseException(326,
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
                    return new FunParseException(338,
                        $"{id}( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it", res.Interval);
                case ExprListErrorType.ElementMissed:
                    return new FunParseException(339,
                        $"{id}({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it", 
                        res.Interval);
                case ExprListErrorType.TotalyWrongDefenition:
                    return new FunParseException(341, "Wrong function call", res.Interval);
                case ExprListErrorType.SingleOpenBracket:
                    return new FunParseException(342,
                        $"( <- unexpected bracket{Nl} ?", 
                        res.Interval);
                case ExprListErrorType.SepIsMissing:
                    return new FunParseException(343,
                        $"{id}({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: {id}({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.ArgumentIsInvalid:
                    return new FunParseException(344,
                        $"{id}({argStubs}, ??? , ...  <- Seems like function call argument is invalid{Nl} Example: {id}({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.CloseBracketIsMissing:
                    return new FunParseException(345,
                        $"{id}({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example: {id}({argStubs})",
                        res.Interval);
                case ExprListErrorType.LastArgumentIsInvalid:
                    return new FunParseException(346,
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
                    return new FunParseException(368,
                        $"( ??? , ..) <- First element missed {Nl}Remove ',' or place element before it", res.Interval);
                case ExprListErrorType.ElementMissed:
                    return new FunParseException(369,
                        $"({argStubs},???, ..) <- element missed {Nl}Remove ',' or place element before it", 
                        res.Interval);
                case ExprListErrorType.TotalyWrongDefenition:
                    return new FunParseException(371, "Wrong expression", res.Interval);
                case ExprListErrorType.SingleOpenBracket:
                    return new FunParseException(372,
                        $"( <- unexpected bracket{Nl} ?", 
                        res.Interval);
                case ExprListErrorType.SepIsMissing:
                    return new FunParseException(373,
                        $"({argStubs}, ??? , ...  <- Seems like ',' is missing{Nl} Example: ({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.ArgumentIsInvalid:
                    return new FunParseException(374,
                        $"({argStubs}, ??? , ...  <- Seems like invalid expressions{Nl} Example: ({argStubs}, myArgument, ...)",
                        res.Interval);
                case ExprListErrorType.CloseBracketIsMissing:
                    return new FunParseException(375,
                        $"({argStubs}, ??? <- Close bracket ')' is missing{Nl} Example:({argStubs})",
                        res.Interval);
                case ExprListErrorType.LastArgumentIsInvalid:
                    return new FunParseException(376,
                        $"({ErrorsHelper.CreateArgumentsStub(list.Take(list.Length-1))} ??? ) <- Seems like invalid expression{Nl} Example: ({argStubs}, myArgument, ...)",
                        res.Interval);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region 4xx - Interpritation exceptions

        public static Exception CycleEquatationDependencies(LexEquation[] result)
        {
            var expression = result.First().Expression;
            return new FunParseException(401,"Cycle dependencies found: " 
                                         + string.Join("->", result.Select(r=>r.Id)), 
                expression.Interval);
        }
        
        public static Exception ImpossibleCast(VarType from,VarType to, Interval interval)
            => new FunParseException(406, $"Unable to cast from {from} to {to}", interval);
        public static Exception NotAnExpression(LexNode node) 
            => new FunParseException(403,$"{node} is not an expression", node.Interval);

        public static Exception InvalidArgTypeDefenition(LexNode argumentNode) 
            => new FunParseException(406, ErrorsHelper.ToString(argumentNode) + " is  not valid fun arg", argumentNode.Interval);

        public static Exception AnonymousFunDefenitionIsMissing(LexNode node)
            => new FunParseException(409, "Anonymous fun defenition is missing", node.Interval);

        public static Exception AnonymousFunBodyIsMissing(LexNode node)
            => new FunParseException(412, "Anonymous fun body is missing", node.Interval);

        public static Exception AmbiguousCallOfFunction(IList<FunctionBase> funVars, LexNode varName)
            => throw new FunParseException(415,$"Ambiguous call of function with name: {varName.Value}", varName.Interval);

        public static Exception ArrayInitializerTypeMismatch(VarType stepType, LexNode node)
            => throw new FunParseException(418,$"Array initializator step has to be int type only but was '{stepType}'. Example: [1..5..2]", node.Interval);

        public static Exception CannotParseNumber(LexNode node)
            => throw new FunParseException(421, $"Cannot parse number '{node.Value}'", node.Interval);

        public static Exception FunctionNotFound(LexNode node, List<IExpressionNode> children, FunctionsDictionary functions)
        => throw new FunParseException(424, $"Function {node.Value}({string.Join(", ", children.Select(c=>c.Type))}) is not defined", node.Interval);
        #endregion

    }
}