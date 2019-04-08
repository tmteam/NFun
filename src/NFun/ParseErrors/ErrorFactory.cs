using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using NFun.Interpritation.Nodes;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.ParseErrors
{
    public static class ErrorFactory
    {
        private static readonly string Nl = Environment.NewLine;
        public static Exception UnaryArgumentIsMissing(Tok operatorTok)
            => throw new FunParseException(101, $"{ToString(operatorTok)} ???{Nl} right expression is missed{Nl} Example: {ToString(operatorTok)} a",
                operatorTok.Interval);
        public static Exception MinusDuplicates(Tok previousTok, Tok currentTok)
            => throw new FunParseException(104,$"'--' is not allowed",
                previousTok.Start, currentTok.Finish);
        public static Exception LeftBinaryArgumentIsMissing(Tok token)
            => throw new FunParseException(107,$"expression is missed before '{ToString(token)}'",
                token.Interval);
        public static Exception RightBinaryArgumentIsMissing(LexNode leftNode, Tok @operator)
            => throw new FunParseException(110,$"{ToString(leftNode)} {ToString(@operator)} ???. Right expression is missed{Nl} Example: {ToString(leftNode)} {ToString(@operator)} e",
                leftNode.Start, @operator.Finish);
        
        public static Exception OperatorIsUnknown(Tok token)
            => throw new FunParseException(113,$"operator '{ToString(token)}' is unknown",token.Interval);

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
                    $"'[x,..???]. Array hi bound expected but was {ToString(missedVal)}'{Nl} Example: a[1..2] or a[1..5..2]", start, finish);
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
                    $"'[x..y..???]. Array step expected but was {ToString(missedVal)}'{Nl} Example: a[1..5..2]", start, finish);
        }
        
        public static Exception ArrayIntervalInitializeCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(137,
                $"{(hasStep?"[x..y..step ???]":"[x..y ???]")}. ']' was missed'{Nl} Example: a[1..5..2]", start,
                finish);
        }
        public static Exception ArrayEnumInitializeError2(int openBracketTokenPos, TokenFlow flow)
        {
            flow.Move(openBracketTokenPos);
            var obrStart = flow.Current.Start;
            flow.MoveIfOrThrow(TokType.ArrOBr);
            
            
            var list = new List<LexNode>();
            int currentToken = flow.CurrentTokenPosition;
            do
            {
                if (flow.IsCurrent(TokType.Sep))
                {
                    //[,] <-first element missed
                    if(!list.Any())
                        return new FunParseException(538,
                            $"[, ..] <- element missed {Nl}Remove ',' or place element before it", 
                            new Interval(flow.Current.Start, flow.Current.Finish));

                    //[x,,y] <- element missed 
                    return new FunParseException(539,
                            $"[, ..] <- element missed {Nl}Remove ',' or place element before it", 
                            new Interval(list.Last().Finish, flow.Current.Finish));
                                                
                }
                var exp = flow.ReadExpressionOrNull();
                if (exp != null)
                    list.Add(exp);
                if (exp == null && list.Any())
                {
                    flow.Move(currentToken);
                    //[x,y, {no or bad expression here} ...
                    return ArrayEnumInitializeError(obrStart, list, flow);
                }
                currentToken = flow.CurrentTokenPosition;
            } while (flow.MoveIf(TokType.Sep));

            if (flow.Current.Is(TokType.ArrCBr))
                //everything seems fine...
                return new FunParseException(541, "Wrong array defenition ", obrStart, flow.Current.Finish);
            
            if (!list.Any())
            {
                return new FunParseException(542,
                    $"[ <- unexpected array symbol{Nl} Did you mean array initialization [,], slice [::] or indexing [i]?", 
                    new Interval(obrStart, obrStart+1));
            }

            var argumentsStub = CreateArgumentsStub(list);
            var nextExpression = flow.TryReadExpressionAndReturnBack();
            if (nextExpression != null)
            {
                //[x y] <- separator is missed
                return new FunParseException(543,
                    $"[{argumentsStub}, ??? , ...  <- Seems like ',' is missing{Nl} Example: [{argumentsStub}, {ToString(nextExpression)} ...]",
                    new Interval(list.Last().Finish, nextExpression.Start));
            }
            
            return ArrayEnumInitializeError(obrStart, list, flow);
        }
        public static Exception ArrayEnumInitializeError(int openBracketTokenPos, TokenFlow flow)
        {
            flow.Move(openBracketTokenPos);
            var obrStart = flow.Current.Start;
            var list = new List<LexNode>();
            int currentToken = flow.CurrentTokenPosition;
            do
            {
                var exp = flow.ReadExpressionOrNull();
                if (exp != null)
                    list.Add(exp);
                if (exp == null && list.Any())
                {
                    flow.Move(currentToken);
                    //[x,y, {no expression here} ...
                    return ArrayEnumInitializeError(obrStart, list, flow);
                }
                currentToken = flow.CurrentTokenPosition;
            } while (flow.MoveIf(TokType.Sep));
            
            
            if (!flow.Current.Is(TokType.ArrCBr))
                return ArrayEnumInitializeError(obrStart, list, flow);
            else
                return new FunParseException(14, "Wrong array defenition ", obrStart, flow.Current.Finish);
        }
        public static Exception ArrayEnumInitializeError(int obrStart, IList<LexNode> arguments, TokenFlow flow)
        {
            var next = flow.Current;
            if(!arguments.Any() && !next.Is(TokType.Sep)) 
                // [ #and noting else 
                return new FunParseException(140,
                    $"[ <- unexpected array symbol{Nl} Did you mean array initialization [,], slice [::] or indexing [i]?", 
                    new Interval(obrStart, obrStart+1));
            
            int lastArgPosition = arguments.LastOrDefault()?.Finish ?? flow.Position;
            int firstArgPostion = arguments.FirstOrDefault()?.Start?? obrStart;

            flow.SkipNewLines();
           
            var argumentsStub = CreateArgumentsStub(arguments);
            bool hasSeparator = next.Is(TokType.Sep);
            
            if (!hasSeparator)
            {
                var nextExpression = flow.TryReadExpressionAndReturnBack();
                if (nextExpression != null) { //next expression exists
                    //[a b...
                    //  ^ sep is missing
                    return new FunParseException(141,
                        $"[{argumentsStub}, ??? , ...  <- Seems like ',' is missing{Nl} Example: [{argumentsStub}, {ToString(nextExpression)} ...]",
                        new Interval(lastArgPosition, nextExpression.Start));
                }
            }
            var hasAnyBeforeStop = flow.MoveUntilOneOfThe(
                TokType.Sep, TokType.ArrCBr, TokType.ArrOBr, TokType.NewLine, TokType.NewLine, TokType.Eof)
                .Any();
            
            if (hasSeparator)
            {
                //[x,y, {someshit} , ... 
                return new FunParseException(142,
                    $"[{argumentsStub}, ??? , ...  <- Seems like array argument is invalid{Nl} Example: [{argumentsStub}, myArgument, ...]",
                    new Interval(next.Start, flow.Position));
            }
            else
            {
                var errorStart = lastArgPosition;
                if (flow.Position == errorStart)
                    errorStart = arguments.Last().Start;
                //[x, {y someshit} , ... 
                if (!hasAnyBeforeStop)
                {
                    //[x,y {no ']' here}
                    return new FunParseException(143,
                        $"[{argumentsStub} ??? <- Array close bracket ']' is missing{Nl} Example: [{argumentsStub}]",
                        new Interval(errorStart, flow.Position));
                }   
                //LastArgument is a part of error
                var argumentsWithoutLastStub = CreateArgumentsStub(arguments.Take(arguments.Count-1));

                return new FunParseException(144,
                    $"[{argumentsWithoutLastStub} ??? ] <- Seems like array argument is invalid{Nl} Example: [{argumentsStub}, myArgument, ...]",
                    new Interval(arguments.Last().Start , flow.Position));
            }
        }


        public static Exception ArrayIndexCbrMissed(Tok openBracket, Tok lastToken)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(145,
                $"a[x ??? <- was missed ']'{Nl} Example: a[1] or a[1:2] or a[1:5:2]", start,
                finish);        
        }
        public static Exception ArraySliceCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(146,
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
                $"{ToString(pipedVal)}.{name}( ???. Close bracket ')' is missed{Nl} Example: {ToString(pipedVal)}.{name}() or {name}({ToString(pipedVal)})", 
                funStart, 
                position);
        }

        public static Exception FunctionCallCbrOrSeparatorMissed(int funStart, string name,  IList<LexNode> arguments, Tok current,LexNode pipedVal)
        {
            if (pipedVal == null)
            {
                var argumentsStub = CreateArgumentsStub(arguments);
                return new FunParseException(173,$"{name}({argumentsStub} ??? <- Close bracket ')' or separator ',' are missed{Nl} Example: {name}({argumentsStub})", 
                    funStart,current.Finish);
            }

            var pipedArgsStub = CreateArgumentsStub(arguments);
            return new FunParseException(176,$"{ToString(pipedVal)}.{name}({pipedArgsStub} ??? <- Close bracket ')' or separator ',' are missed{Nl} Example: {ToString(pipedVal)}.{name}({pipedArgsStub}) or {name}(x,{pipedArgsStub})", 
                funStart, current.Finish);
        }


        public static Exception BracketExprCbrOrSeparatorMissed(int start,int end,  IList<LexNode> arguments)
        {
            var argumentsStub = CreateArgumentsStub(arguments);
            return new FunParseException(179,$"({argumentsStub})<-??? {Nl}Close bracket ')' is missed{Nl} Example: ({argumentsStub})", 
                start,end);
        }

        public static Exception BracketExpressionMissed(int start, int end, IList<LexNode> arguments)
        {
             var argumentsStub = CreateArgumentsStub(arguments);
             return new FunParseException(182,$"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", 
                            start,end);
        }
        
        public static Exception ExpressionListMissed(int start, int end, IList<LexNode> arguments)
        {
            var argumentsStub = CreateArgumentsStub(arguments);
            return new FunParseException(183,$"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", 
                start,end);
        }
        private static string CreateArgumentsStub(IEnumerable<LexNode> arguments)
        {
            var argumentsStub = string.Join(",", arguments.Select(ToString));
            return argumentsStub;
        }

        
        public static Exception FunDefTokenIsMissed(string funName, List<VariableInfo> arguments, Tok actual)
        {
            return new FunParseException(201, $"{Signature(funName, arguments)} ??? . '=' def symbol is skipped but was {ToString(actual)}{Nl}Example: {Signature(funName, arguments)} = ...", 
                actual.Start, actual.Finish);
        }
        public static Exception FunExpressionIsMissed(string funName, List<VariableInfo> arguments, Interval interval) 
            => new FunParseException(204,
                $"{Signature(funName, arguments)} = ??? . Function body is missed {Nl}Example: {Signature(funName, arguments)} = #place your body here", 
                interval);

        public static Exception UnknownValueAtStartOfExpression(int exprStart, Tok flowCurrent) 
            => new FunParseException(207,$"Unexpected symbol {ToString(flowCurrent)}. Equation, anonymous equation, function or type defenition expected.", exprStart, flowCurrent.Finish);
        public static Exception ExpressionBeforeTheDefenition(int exprStart, LexNode expression, Tok flowCurrent)
            => new FunParseException(210,$"Unexpected expression {ToString(expression)} before defenition. Equation, anonymous equation, function or type defenition expected.", exprStart, flowCurrent.Finish);

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

        private static string Signature(string funName, List<VariableInfo> arguments) 
            => $"{funName}({Join(arguments)})";

        private static string Join(List<VariableInfo> arguments) 
            => string.Join(",", arguments);
        private static string ToString(LexNode node)
        {
            switch (node.Type)
            {
                case LexNodeType.Number: return node.Value;
                case LexNodeType.Var: return node.Value;
                case LexNodeType.Fun: return node.Value + "(...)";
                case LexNodeType.IfThen: return "if...then...";
                case LexNodeType.IfThanElse: return "if...then....else...";
                case LexNodeType.Text: return $"\"{(node.Value.Length>20?(node.Value.Substring(17)+"..."):node.Value)}\"";
                case LexNodeType.ArrayInit: return "[...]";
                case LexNodeType.AnonymFun: return "(..)=>..";
                case LexNodeType.TypedVar: return node.Value;
                case LexNodeType.ListOfExpressions: return "(,)";
                case LexNodeType.ProcArrayInit: return "[ .. ]";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private static string ToString(Tok tok)
        {
            if (!string.IsNullOrWhiteSpace(tok.Value))
                return tok.Value;
            switch (tok.Type)
            {
                case TokType.If: return "if";
                case TokType.Else:return "else";
                case TokType.Then:return "then";
                case TokType.Plus: return "+";
                case TokType.Minus: return "-";
                case TokType.Div: return "/";
                case TokType.Rema: return "%";
                case TokType.Mult: return "*";
                case TokType.Pow: return "**";
                case TokType.Obr: return "(";
                case TokType.Cbr: return ")";
                case TokType.ArrOBr: return "[";
                case TokType.ArrCBr: return "]";
                case TokType.ArrConcat: return "@";
                case TokType.In: return "in";
                case TokType.BitOr: return "|";
                case TokType.BitAnd: return "&";
                case TokType.BitXor: return "^";
                case TokType.BitShiftLeft: return "<<";
                case TokType.BitShiftRight: return ">>";
                case TokType.BitInverse: return "~";
                case TokType.Def: return "=";
                case TokType.Equal: return "==";
                case TokType.NotEqual: return "!=";
                case TokType.And:return "and";
                case TokType.Or:return "or";
                case TokType.Xor:return "xor";
                case TokType.Not:return "not";
                case TokType.Less:return "<";
                case TokType.More:return ">";
                case TokType.LessOrEqual:return "<=";
                case TokType.MoreOrEqual:return ">=";
                case TokType.Sep:return ",";
                case TokType.True:return "true";
                case TokType.False:return "false";
                case TokType.Colon:return ":";
                case TokType.TwoDots:return "..";
                case TokType.TextType:return "text";
                case TokType.IntType:return "int";
                case TokType.RealType:return "real";
                case TokType.BoolType:return "bool";
                case TokType.AnythingType:return "anything";
                case TokType.PipeForward:return ".";
                case TokType.AnonymFun:return "=>";
                default:
                    return tok.Type.ToString().ToLower();
            }
        }

    }
}