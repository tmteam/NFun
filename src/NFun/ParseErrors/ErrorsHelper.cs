using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Parsing;
using NFun.Tokenization;

namespace NFun.ParseErrors
{
    public static class ErrorsHelper
    {
        public static string CreateArgumentsStub(IEnumerable<LexNode> arguments)
        {
            var argumentsStub = string.Join(",", arguments.Select(ToString));
            return argumentsStub;
        }

        public static string Signature(string funName, IEnumerable<LexVarDefenition> arguments) 
            => $"{funName}({Join(arguments)})";

        public static string Join(IEnumerable<LexVarDefenition> arguments) 
            => string.Join(",", arguments);
        public  static string ToString(LexNode node)
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
        public static string ToString(Tok tok)
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
                case TokType.Attribute: return "@";
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
                case TokType.Int32Type:return "int32";
                case TokType.Int64Type:return "int64";
                case TokType.RealType:return "real";
                case TokType.BoolType:return "bool";
                case TokType.AnythingType:return "anything";
                case TokType.PipeForward:return ".";
                case TokType.AnonymFun:return "=>";
                default:
                    return tok.Type.ToString().ToLower();
            }
        }
        
        public static ExprListError GetExpressionListError(
            int openBracketTokenPos, TokenFlow flow, TokType openBrack, TokType closeBrack)
        {
            flow.Move(openBracketTokenPos);
            var obrStart = flow.Current.Start;
            flow.MoveIfOrThrow(openBrack);
            
            var list = new List<LexNode>();
            int currentToken = flow.CurrentTokenPosition;
            do
            {
                if (flow.IsCurrent(TokType.Sep))
                {
                    //[,] <-first element missed
                    if(!list.Any())
                        return new ExprListError(
                            ExprListErrorType.FirstElementMissed,
                            list,
                            new Interval(flow.Current.Start, flow.Current.Finish));
                        
                    //[x, ,y] <- element missed 
                    return new ExprListError(
                        ExprListErrorType.ElementMissed,
                        list,
                        new Interval(list.Last().Finish, flow.Current.Finish));
                }
                
                var exp = flow.ReadExpressionOrNull();
                if (exp != null)
                    list.Add(exp);
                if (exp == null && list.Any())
                {
                    flow.Move(currentToken);
                    //[x,y, {no or bad expression here} ...
                    return SpecifyArrayInitError(list, flow, openBrack, closeBrack);
                }
                currentToken = flow.CurrentTokenPosition;
            } while (flow.MoveIf(TokType.Sep));

            if (flow.Current.Is(closeBrack))
                //everything seems fine...
                return new ExprListError(
                    ExprListErrorType.TotalyWrongDefenition, 
                    list, new Interval(obrStart, flow.Current.Finish));
            
            if (!list.Any())
                return new ExprListError(ExprListErrorType.SingleOpenBracket, list, 
                    new Interval(obrStart, obrStart+1));

            var nextExpression = flow.TryReadExpressionAndReturnBack();
            if (nextExpression != null)//[x y] <- separator is missed
                return new ExprListError(ExprListErrorType.SepIsMissing, 
                    list, new Interval(list.Last().Finish, nextExpression.Start));
            //[x {some crappy crap here}]
            return SpecifyArrayInitError(list, flow, openBrack, closeBrack);
        }
        private static ExprListError SpecifyArrayInitError(
            IList<LexNode> arguments, TokenFlow flow, TokType openBrack, TokType closeBrack)
        {
            var firstToken = flow.Current;
            int lastArgPosition = arguments.LastOrDefault()?.Finish ?? flow.Position;

            flow.SkipNewLines();
           
            var hasAnyBeforeStop = flow.MoveUntilOneOfThe(
                    TokType.Sep, openBrack, closeBrack, TokType.NewLine, TokType.Eof)
                .Any();

            if (firstToken.Is(TokType.Sep))
            {
                //[x,y, {someshit} , ... 
                return new ExprListError(
                    ExprListErrorType.ArgumentIsInvalid, 
                    arguments,
                    new Interval(firstToken.Start, flow.Position));
            }
            
           
            var errorStart = lastArgPosition;
            if (flow.Position == errorStart)
                errorStart = arguments.Last().Start;
            //[x, {y someshit} , ... 
            if (!hasAnyBeforeStop)
            {
                //[x,y {no ']' here}
                return new ExprListError(
                    ExprListErrorType.CloseBracketIsMissing, 
                    arguments,
                    new Interval(errorStart, flow.Position));
            }   
            //LastArgument is a part of error
            var argumentsWithoutLastStub = CreateArgumentsStub(arguments.Take(arguments.Count-1));
            return new ExprListError(
                ExprListErrorType.LastArgumentIsInvalid, 
                arguments,
                new Interval(arguments.Last().Start , flow.Position));
        }



    }

    public class ExprListError
    {
        public readonly ExprListErrorType Type;
        public readonly LexNode[] Parsed;
        public readonly Interval Interval;

        public ExprListError(ExprListErrorType type, IEnumerable<LexNode> parsed, Interval interval)
        {
            Type = type;
            Parsed = parsed.ToArray();
            Interval = interval;
        }
    }
    public enum ExprListErrorType
    {
        FirstElementMissed = 538,
        ElementMissed = 539,
        TotalyWrongDefenition = 541,
        SingleOpenBracket = 542,
        SepIsMissing = 543,
        ArgumentIsInvalid = 544,
        CloseBracketIsMissing = 545,
        LastArgumentIsInvalid = 546   
    }
}