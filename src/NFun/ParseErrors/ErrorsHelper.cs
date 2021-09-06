using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.ParseErrors {

internal static class ErrorsHelper {
    public static string ToFailureFunString(FunCallSyntaxNode headNode, ISyntaxNode headNodeChild) {
        StringBuilder sb = new StringBuilder();
        foreach (var child in headNode.Args)
        {
            if (child == headNodeChild)
                sb.Append("???");
            if (child is VarDefinitionSyntaxNode varDef)
                sb.Append(varDef.Id);
            else if (child is NamedIdSyntaxNode varSyntax)
                sb.Append(varSyntax.Id);

            if (headNode.Args.Last() != child)
                sb.Append(",");
        }

        return sb.ToString();
    }


    public static string CreateArgumentsStub(IEnumerable<ISyntaxNode> arguments) {
        var argumentsStub = string.Join(",", arguments.Select(ToShortText));
        return argumentsStub;
    }


    public static string Signature(string funName, IEnumerable<ISyntaxNode> arguments)
        => $"{funName}({Join(arguments)})";


    public static string Join(IEnumerable<ISyntaxNode> arguments)
        => string.Join(",", arguments.Select(ToShortText));


    public static string ToShortText(ISyntaxNode node) => node.Accept(new ShortDescritpionVisitor());

    public static string ToText(Tok tok) {
        if (!string.IsNullOrWhiteSpace(tok.Value))
            return tok.Value;
        return tok.Type switch {
            TokType.If => "if",
            TokType.Else => "else",
            TokType.Then => "then",
            TokType.Plus => "+",
            TokType.Minus => "-",
            TokType.Div => "/",
            TokType.Rema => "%",
            TokType.Mult => "*",
            TokType.Pow => "**",
            TokType.Obr => "(",
            TokType.Cbr => ")",
            TokType.ArrOBr => "[",
            TokType.ArrCBr => "]",
            TokType.MetaInfo => "@",
            TokType.In => "in",
            TokType.BitOr => "|",
            TokType.BitAnd => "&",
            TokType.BitXor => "^",
            TokType.BitShiftLeft => "<<",
            TokType.BitShiftRight => ">>",
            TokType.BitInverse => "~",
            TokType.Def => "=",
            TokType.Equal => "==",
            TokType.NotEqual => "!=",
            TokType.And => "and",
            TokType.Or => "or",
            TokType.Xor => "xor",
            TokType.Not => "not",
            TokType.Less => "<",
            TokType.More => ">",
            TokType.LessOrEqual => "<=",
            TokType.MoreOrEqual => ">=",
            TokType.Sep => ",",
            TokType.True => "true",
            TokType.False => "false",
            TokType.Colon => ":",
            TokType.TwoDots => "..",
            TokType.TextType => "text",
            TokType.Int32Type => "int32",
            TokType.Int64Type => "int64",
            TokType.RealType => "real",
            TokType.BoolType => "bool",
            TokType.AnythingType => "anything",
            TokType.Dot => ".",
            TokType.Arrow => "=>",
            _ => tok.Type.ToString().ToLower()
        };
    }

    public static ExprListError GetExpressionListError(
        int openBracketTokenPos, TokFlow flow, TokType openBrack, TokType closeBrack) {
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
                if (!list.Any())
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
                ExprListErrorType.TotalyWrongDefinition,
                list, new Interval(obrStart, flow.Current.Finish));

        if (!list.Any())
            return new ExprListError(ExprListErrorType.SingleOpenBracket, list,
                new Interval(obrStart, obrStart + 1));

        var position = flow.CurrentTokenPosition;
        var nextExpression = SyntaxNodeReader.ReadNodeOrNull(flow);
        flow.Move(position);

        if (nextExpression != null) //[x y] <- separator is missed
            return new ExprListError(ExprListErrorType.SepIsMissing,
                list, new Interval(list.Last().Interval.Finish, nextExpression.Interval.Start));
        //[x {some crappy crap here}]
        return SpecifyArrayInitError(list, flow, openBrack, closeBrack);
    }

    private static ExprListError SpecifyArrayInitError(
        IList<ISyntaxNode> arguments, TokFlow flow, TokType openBrack, TokType closeBrack) {
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
        return new ExprListError(
            ExprListErrorType.LastArgumentIsInvalid,
            arguments,
            new Interval(arguments.Last().Interval.Start, flow.Position));
    }
}

public class ExprListError {
    public readonly ExprListErrorType Type;
    public readonly ISyntaxNode[] Parsed;
    public readonly Interval Interval;

    public ExprListError(ExprListErrorType type, IEnumerable<ISyntaxNode> parsed, Interval interval) {
        Type = type;
        Parsed = parsed.ToArray();
        Interval = interval;
    }
}

public enum ExprListErrorType {
    FirstElementMissed = 538,
    ElementMissed = 539,
    TotalyWrongDefinition = 541,
    SingleOpenBracket = 542,
    SepIsMissing = 543,
    ArgumentIsInvalid = 544,
    CloseBracketIsMissing = 545,
    LastArgumentIsInvalid = 546
}

}