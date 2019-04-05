using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Parsing;
using NFun.Tokenization;

namespace NFun.ParseErrors
{
    public static class ErrorFactory
    {
        public static Exception UnaryArgumentIsMissing(Tok operatorTok)
            => throw new FunParseException($"{operatorTok.Value} ???\r right expression is missed \rExample: {operatorTok.Value} a",operatorTok.Start, operatorTok.Finish);
        public static Exception MinusDuplicates(Tok previousTok, Tok currentTok)
            => throw new FunParseException($"'--' is not allowed",previousTok.Start, currentTok.Finish);
        public static Exception LeftBinaryArgumentIsMissing(Tok token)
            => throw new FunParseException($"expression is missed before '{token.Value}'",token.Start, token.Finish);
        public static Exception RightBinaryArgumentIsMissing(Tok token)
            => throw new FunParseException($"expression is missed after '{token.Value}'",token.Start, token.Finish);

        public static Exception OperatorIsUnknown(Tok token)
            => throw new FunParseException($"operator '{token.Value}' is unknown",token.Start, token.Finish);

        public static Exception FunctionNameIsMissedAfterPipeForward(Tok token)
            => throw new FunParseException($"Function name expected after '.' \rExample: [1,2].myFunction()",token.Start, token.Finish);

        public static Exception ArrayIndexOrSliceExpected(Tok openBraket)
            => new FunParseException($"Array index or slice expected after '[' \rExample: a[1] or a[1:3:2]",openBraket.Start,openBraket.Finish);

        public static Exception ArrayIndexExpected(Tok openBraket, Tok closeBracket)
            =>  new FunParseException($"Array index expected inside of '[]' \rExample: a[1]",openBraket.Start,closeBracket.Finish);

        public static Exception ArrayInitializeSecondIndexMissed(Tok openBracket, Tok lastToken, Tok missedVal)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            if (string.IsNullOrWhiteSpace(missedVal?.Value))
                return new FunParseException(
                    $"'[x,..???]. Array hi bound expected but was nothing' \rExample: a[1..2] or a[1..5..2]", start,
                    finish);
            else
                return new FunParseException(
                    $"'[x,..???]. Array hi bound expected but was {missedVal.Value}' \rExample: a[1..2] or a[1..5..2]", start, finish);
        }
        public static Exception ArrayInitializeStepMissed(Tok openBracket, Tok lastToken, Tok missedVal)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            if (string.IsNullOrWhiteSpace(missedVal?.Value))
                return new FunParseException(
                    $"'[x..y..???]. Array step expected but was nothing' \rExample: a[1..5..2]", start,
                    finish);
            else
                return new FunParseException(
                    $"'[x..y..???]. Array step expected but was {missedVal.Value}' \rExample: a[1..5..2]", start, finish);
        }
        
        public static Exception ArrayIntervalInitializeCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(
                $"{(hasStep?"[x..y..step ???]":"[x..y ???]")}. ']' was missed' \rExample: a[1..5..2]", start,
                finish);
        }
        
        public static Exception ArrayEnumInitializeCbrMissed(Tok openBracket, IList<LexNode> arguments,Tok lastToken)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            var argumentsStub = CreateArgumentsStub(arguments);
            return new FunParseException(
                $"[{argumentsStub} ???. ']' was missed' \rExample: [{argumentsStub}]", start,
                finish);
        }

     
        public static Exception ArrayIndexCbrMissed(Tok openBracket, Tok lastToken)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(
                $"a[x ???. ']' was missed' \rExample: a[1] or a[1:2] or a[1:5:2]", start,
                finish);        
        }
        public static Exception ArraySliceCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.Finish;
            return new FunParseException(
                $"a{(hasStep?"[x:y:step]":"[x:y]")} <- ']' was missed' \rExample: a[1:5:2]", start,
                finish);        
        }
        public static Exception ConditionIsMissing(int conditionStart, Tok flowCurrent)
            => new FunParseException("if ??? then\r Condition expression is missing\r Example: if a>b then ... ", conditionStart, flowCurrent.Finish);
        
        public static Exception ThanExpressionIsMissing(int conditionStart, Tok flowCurrent)
            => new FunParseException("if a then ???.  Expression is missing\r Example: if a then a+1 ", conditionStart, flowCurrent.Finish);

        public static Exception ElseKeywordIsMissing(int ifelseStart, Tok flowCurrent)
            => new FunParseException("if a then b ???.  Else keyword is missing\r Example: if a then b else c ", ifelseStart, flowCurrent.Finish);

        public static Exception ElseExpressionIsMissing(int ifelseStart, Tok flowCurrent)
            => new FunParseException("if a then b else ???.  Else expression is missing\r Example: if a then b else c ", ifelseStart, flowCurrent.Finish);

        public static Exception IfKeywordIsMissing(int ifelseStart, Tok flowCurrent)
            => new FunParseException("if a then b (if) ...  'if' is missing\r Example: if a then b if c then d else c ", ifelseStart, flowCurrent.Finish);

        public static Exception ThenKeywordIsMissing(int ifelseStart, Tok flowCurrent)
            => new FunParseException("if a (then) b  ...  'then' is missing\r Example: if a then b  else c ", ifelseStart, flowCurrent.Finish);
        public static Exception FunctionCallObrMissed(int funStart, string name, Tok flowCurrent,LexNode pipedVal)
        {
            if(pipedVal==null)
                return new FunParseException($"{name}(??? \r.Close bracket ')' is missed\r Example: {name}()", funStart, flowCurrent.Finish);
            
            return new FunParseException($"x.{name}(??? \r.Close bracket ')' is missed\r Example: x.{name}() or {name}(x)", funStart, flowCurrent.Finish);

        }

        public static Exception FunctionCallCbrOrSeparatorMissed(int funStart, string name,  IList<LexNode> arguments, Tok current,LexNode pipedVal)
        {

            if (pipedVal == null)
            {
                var argumentsStub = CreateArgumentsStub(arguments);
                return new FunParseException($"{name}({argumentsStub} ??? \r.Close bracket ')' is missed\r Example: {name}({argumentsStub})", 
                    funStart,current.Finish);
            }

            var pipedArgsStub = CreateArgumentsStub(arguments);
            return new FunParseException($"x.{name}({pipedArgsStub}??? \r.Close bracket ')' or ',' is missed\r Example: x.{name}({pipedArgsStub}) or {name}(x,{pipedArgsStub})", 
                funStart, pipedVal.Finish);
        }


        public static Exception BracketExprCbrOrSeparatorMissed(int start, IList<LexNode> arguments, Tok current)
        {
            var argumentsStub = CreateArgumentsStub(arguments);
            return new FunParseException($"({argumentsStub})<-??? \rClose bracket ')' is missed\r Example: ({argumentsStub})", 
                start,current.Finish);
        }

        public static Exception BracketExpressionMissed(int start, IList<LexNode> arguments, Tok current)
        {
             var argumentsStub = CreateArgumentsStub(arguments);
             return new FunParseException($"({argumentsStub}???) \r.Expression inside the brackets is missed\r Example: (a)", 
                            start,current.Finish);
        }
        private static string CreateArgumentsStub(IList<LexNode> arguments)
        {
            var argumentsStub = string.Join(",", Enumerable.Repeat("x", arguments.Count));
            return argumentsStub;
        }


    }
}