using System;
using System.Collections.Generic;
using System.Globalization;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.SyntaxParsing;

namespace NFun.Tokenization;

public static class TokenHelper {
    /// <exception cref="SystemException">Throws if string contains invalid format</exception>
    public static (object, FunnyType) ToConstant(string val) {
        val = val.Replace("_", null);

        if (val.Contains("."))
        {
            if (val.EndsWith("."))
                throw new FormatException();
            return (double.Parse(val, CultureInfo.InvariantCulture), FunnyType.Real);
        }

        var longVal = ParseLongValue(val);

        return longVal switch {
                   < Int32.MinValue => (longVal, FunnyType.Int64),
                   > Int32.MaxValue => (longVal, FunnyType.Int64),
                   _                => (longVal, FunnyType.Int32)
               };
    }

    private static long ParseLongValue(string val) {
        if (val.Length > 2)
        {
            if (val[1] == 'b')
            {
                var uval = Convert.ToUInt64(val[2..], 2);
                if (uval > long.MaxValue)
                    throw new OverflowException();
                return (long)uval;
            }
            else if (val[1] == 'x')
            {
                var uval = Convert.ToUInt64(val, 16);
                if (uval > long.MaxValue)
                    throw new OverflowException();
                return (long)uval;
            }
        }

        return long.Parse(val);
    }

    private static string ToTypeName(this Tok token) => token.Type switch {
        TokType.Int16Type    => "int16",
        TokType.Int32Type    => "int32",
        TokType.Int64Type    => "int64",
        TokType.UInt8Type    => "uint8",
        TokType.UInt16Type   => "uint16",
        TokType.UInt32Type   => "uint32",
        TokType.UInt64Type   => "uint64",
        TokType.RealType     => "real",
        TokType.BoolType     => "bool",
        TokType.CharType     => "char",
        TokType.TextType     => "text",
        TokType.AnythingType => "any",
        TokType.Id           => token.Value,
        _ => throw Errors.TypeExpectedButWas(token)
    };

    public static TypeSyntax ReadTypeSyntax(this TokFlow flow) {
        TypeSyntax readType;
        int lastPosition;
        int typeStart;

        if (flow.IsCurrent(TokType.FiObr))
        {
            var openBrace = flow.Current;
            typeStart = openBrace.Start;
            flow.MoveNext();
            readType = ReadStructTypeSyntax(flow, openBrace);
            lastPosition = flow.Previous.Finish;
        }
        else
        {
            var cur = flow.Current;
            typeStart = cur.Start;
            readType = new TypeSyntax.Named(cur.ToTypeName(), cur.Interval);
            flow.MoveNext();
            lastPosition = cur.Finish;
        }

        while (true)
        {
            if (flow.IsCurrent(TokType.ArrOBr))
            {
                if (flow.Current.Start != lastPosition)
                    throw Errors.UnexpectedSpaceBeforeArrayTypeBrackets(readType, new Interval(lastPosition, flow.Current.Start));

                flow.MoveNext();
                lastPosition = flow.Current.Finish;
                if (!flow.MoveIf(TokType.ArrCBr))
                    throw Errors.ArrTypeCbrMissed(new Interval(typeStart, flow.Current.Start));
                readType = new TypeSyntax.ArrayOf(readType);
            }
            else if (flow.IsCurrent(TokType.Question))
            {
                if (flow.Current.Start != lastPosition)
                    break;
                lastPosition = flow.Current.Finish;
                flow.MoveNext();
                readType = new TypeSyntax.OptionalOf(readType);
            }
            else
            {
                break;
            }
        }

        return readType;
    }

    private static TypeSyntax ReadStructTypeSyntax(TokFlow flow, Tok openBrace) {
        var fields = new List<(string, TypeSyntax)>();
        bool hasAnyDelimiter = true;
        flow.SkipNewLines();

        while (true)
        {
            if (flow.MoveIf(TokType.FiCbr))
                break;
            if (!hasAnyDelimiter)
                throw Errors.StructTypeSeparatorExpected(flow.Current);

            if (!flow.MoveIf(TokType.Id, out var idToken))
                throw Errors.StructTypeFieldNameExpected(flow.Current);
            if (!flow.MoveIf(TokType.Colon))
                throw Errors.StructTypeColonExpected(idToken, flow.Current);

            var fieldType = flow.ReadTypeSyntax(); // recursive
            fields.Add((idToken.Value, fieldType));

            hasAnyDelimiter = false;
            if (flow.MoveIf(TokType.Sep))
                hasAnyDelimiter = true;
            if (flow.SkipNewLines())
                hasAnyDelimiter = true;
        }

        return new TypeSyntax.StructOf(fields.ToArray());
    }

    public static bool MoveIf(this TokFlow flow, TokType tokType) {
        if (!flow.IsCurrent(tokType)) return false;
        flow.MoveNext();
        return true;
    }

    /// <summary>Moves if current token is Id with the given value (contextual keyword).</summary>
    public static bool MoveIfIdEquals(this TokFlow flow, string value, out Tok tok) {
        if (flow.IsCurrent(TokType.Id) && flow.Current.Value == value)
        {
            tok = flow.Current;
            flow.MoveNext();
            return true;
        }
        tok = null;
        return false;
    }

    public static bool MoveIf(this TokFlow flow, TokType tokType, out Tok tok) {
        if (flow.IsCurrent(tokType))
        {
            tok = flow.Current;
            flow.MoveNext();
            return true;
        }
        else
        {
            tok = null;
            return false;
        }
    }

    public static Tok AssertAndMove(this TokFlow flow, TokType tokType) {
        var cur = flow.Current.NotNull($"{tokType}' is missing");

        if(!cur!.Is(tokType))
            AssertChecks.Panic($"{tokType}' is missing");
        flow.MoveNext();
        return cur;
    }
}
