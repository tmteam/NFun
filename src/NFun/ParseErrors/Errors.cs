using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.ParseErrors;

internal static partial class Errors {

    private static readonly string Nl = Environment.NewLine;

    private static string ToFailureFunString(FunCallSyntaxNode headNode, ISyntaxNode headNodeChild) {
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


    private static string CreateArgumentsStub(IEnumerable<ISyntaxNode> arguments)
        => string.Join(",", arguments.Select(a => a.ToShortText()));

    private static string Signature(string funName, IEnumerable<ISyntaxNode> arguments)
        => $"{funName}({Join(arguments)})";

    private static string Signature(IFunctionSignature signature)
        => $"{signature.Name}({Join(signature.ArgTypes)})->{signature.ReturnType}";

    private static string Join(FunnyType[] arguments)
        => string.Join(", ", arguments);


    private static string Join(IEnumerable<ISyntaxNode> arguments)
        => string.Join(", ", arguments.Select(a => a.ToShortText()));

    private static string ToText(Tok tok) =>
        !string.IsNullOrWhiteSpace(tok.Value)
            ? tok.Value
            : tok.Type switch {
                TokType.FiObr => "{",
                TokType.FiCbr => "}",
                TokType.ParenthObr => "(",
                TokType.ParenthCbr => ")",
                TokType.NewLine => "\n",
                TokType.If => "if",
                TokType.Else => "else",
                TokType.Then => "then",
                TokType.Plus => "+",
                TokType.Minus => "-",
                TokType.Div => "\\",
                TokType.DivInt => "\\\\",
                TokType.Rema => "%",
                TokType.Mult => "*",
                TokType.Pow => "**",
                TokType.ArrOBr => "[",
                TokType.ArrCBr => "]",
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
                TokType.In => "in",
                TokType.Sep => ",",
                TokType.True => "true",
                TokType.False => "false",
                TokType.Colon => ":",
                TokType.MetaInfo => "@",
                TokType.TwoDots => "..",
                TokType.Step => "step",
                TokType.TextType => "text",
                TokType.Int16Type => "int16",
                TokType.Int32Type => "int32",
                TokType.Int64Type => "int64",
                TokType.UInt8Type => "byte",
                TokType.UInt16Type => "uint16",
                TokType.UInt32Type => "uint32",
                TokType.UInt64Type => "uint64",
                TokType.RealType => "real",
                TokType.BoolType => "bool",
                TokType.CharType => "char",
                TokType.AnythingType => "any",
                TokType.Dot => ".",
                TokType.Rule => "rule",
                TokType.Default => "default",
                _ => tok.Type.ToString()
            };

    private static ExprListError GetExpressionListError(
        int openBracketTokenPos, TokFlow flow, TokType openBrack, TokType closeBrack) {
        flow.Move(openBracketTokenPos);
        var obrStart = flow.Current.Start;
        flow.AssertAndMove(openBrack);

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
                //[x,y, { bad expression here} ...
                return SpecifyArrayInitError(list, flow, openBrack, closeBrack);
            }

            currentToken = flow.CurrentTokenPosition;
        } while (flow.MoveIf(TokType.Sep));

        if (flow.Current.Is(closeBrack))
            //everything seems fine...
            return new ExprListError(
                ExprListErrorType.TotallyWrongDefinition,
                list, new Interval(obrStart, flow.Current.Finish));

        if (list.IsEmpty())
            return new ExprListError(
                ExprListErrorType.SingleOpenBracket, list,
                new Interval(obrStart, obrStart + 1));

        var position = flow.CurrentTokenPosition;
        var nextExpression = SyntaxNodeReader.ReadNodeOrNull(flow);
        flow.Move(position);

        if (nextExpression != null) //[x y] <- separator is missed
            return new ExprListError(
                ExprListErrorType.SepIsMissing,
                list, new Interval(list.Last().Interval.Finish, nextExpression.Interval.Start));
        //[x {some crappy crap here}]
        return SpecifyArrayInitError(list, flow, openBrack, closeBrack);
    }

    private static ExprListError SpecifyArrayInitError(
        IList<ISyntaxNode> arguments, TokFlow flow, TokType openBrack, TokType closeBrack) {
        var firstToken = flow.Current;
        int lastArgPosition = arguments.LastOrDefault()?.Interval.Finish ?? flow.CurrentTokenFinishPosition;

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
                new Interval(firstToken.Start, flow.CurrentTokenFinishPosition));
        }


        var errorStart = lastArgPosition;
        if (flow.CurrentTokenFinishPosition == errorStart)
            errorStart = arguments.Last().Interval.Start;
        //[x, {y someshit} , ...
        if (!hasAnyBeforeStop)
        {
            //[x,y {no ']' here}
            return new ExprListError(
                ExprListErrorType.CloseBracketIsMissing,
                arguments,
                new Interval(errorStart, flow.CurrentTokenFinishPosition));
        }

        //Last argument is a part of error
        return new ExprListError(
            ExprListErrorType.LastArgumentIsInvalid,
            arguments,
            new Interval(arguments.Last().Interval.Start, flow.CurrentTokenFinishPosition));
    }

    private class ExprListError {
        public readonly ExprListErrorType Type;
        public readonly ISyntaxNode[] Parsed;
        public readonly Interval Interval;

        public ExprListError(ExprListErrorType type, IEnumerable<ISyntaxNode> parsed, Interval interval) {
            Type = type;
            Parsed = parsed.ToArray();
            Interval = interval;
        }
    }

    private enum ExprListErrorType {
        FirstElementMissed,
        ElementMissed,
        TotallyWrongDefinition,
        SingleOpenBracket,
        SepIsMissing,
        ArgumentIsInvalid,
        CloseBracketIsMissing,
        LastArgumentIsInvalid
    }
}
