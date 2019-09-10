using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.ParseErrors
{
    public static class ErrorsHelper
    {
        public static string ToFailureFunString(FunCallSyntaxNode headNode, ISyntaxNode headNodeChild)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var child in headNode.Args)
            {
                if (child == headNodeChild)
                    sb.Append("???");
                if (child is VarDefenitionSyntaxNode varDef)
                    sb.Append(varDef.Id);
                else if (child is VariableSyntaxNode varSyntax)
                    sb.Append(varSyntax.Id);

                if (headNode.Args.Last() != child)
                    sb.Append(",");
            }

            return sb.ToString();
        }
       
       
        public static string CreateArgumentsStub(IEnumerable<ISyntaxNode> arguments)
        {
            var argumentsStub = string.Join(",", arguments.Select(ToShortText));
            return argumentsStub;
        }
        
        
        public static string Signature(string funName, IEnumerable<ISyntaxNode> arguments) 
            => $"{funName}({Join(arguments)})";

        
        public static string Join(IEnumerable<ISyntaxNode> arguments) 
            => string.Join(",", arguments.Select(ToShortText));


        public static string ToShortText(ISyntaxNode node) => node.Accept(new ShortDescritpionVisitor());
        public static string ToText(Tok tok)
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
            int openBracketTokenPos, TokFlow flow, TokType openBrack, TokType closeBrack)
        {
            flow.Move(openBracketTokenPos);
            var obrStart = flow.Current.Start;
            flow.MoveIfOrThrow(openBrack);
            
            var list = new List<ISyntaxNode>();
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
                        new Interval(list.Last().Interval.Finish, flow.Current.Finish));
                }
                
                var exp = SyntaxNodeReader.ReadNodeOrNull(flow);
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

            var nextExpression = SyntaxNodeReader.ReadNodeAndFinallyReturnBack(flow);
            if (nextExpression != null)//[x y] <- separator is missed
                return new ExprListError(ExprListErrorType.SepIsMissing, 
                    list, new Interval(list.Last().Interval.Finish, nextExpression.Interval.Start));
            //[x {some crappy crap here}]
            return SpecifyArrayInitError(list, flow, openBrack, closeBrack);
        }
        private static ExprListError SpecifyArrayInitError(
            IList<ISyntaxNode> arguments, TokFlow flow, TokType openBrack, TokType closeBrack)
        {
            var firstToken = flow.Current;
            int lastArgPosition = arguments.LastOrDefault()?.Interval.Finish ?? flow.Position;

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
                errorStart = arguments.Last().Interval.Start;
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
                new Interval(arguments.Last().Interval.Start , flow.Position));
        }



    }

    public class ExprListError
    {
        public readonly ExprListErrorType Type;
        public readonly ISyntaxNode[] Parsed;
        public readonly Interval Interval;

        public ExprListError(ExprListErrorType type, IEnumerable<ISyntaxNode> parsed, Interval interval)
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