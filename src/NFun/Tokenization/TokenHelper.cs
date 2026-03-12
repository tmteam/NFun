using System;
using System.Collections.Generic;
using System.Globalization;
using NFun.Exceptions;
using NFun.ParseErrors;

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

    private static FunnyType ToFunnyType(this Tok token) =>
        token.Type switch {
            TokType.Int16Type                    => FunnyType.Int16,
            TokType.Int32Type                    => FunnyType.Int32,
            TokType.Int64Type                    => FunnyType.Int64,
            TokType.UInt8Type                    => FunnyType.UInt8,
            TokType.UInt16Type                   => FunnyType.UInt16,
            TokType.UInt32Type                   => FunnyType.UInt32,
            TokType.UInt64Type                   => FunnyType.UInt64,
            TokType.RealType                     => FunnyType.Real,
            TokType.BoolType                     => FunnyType.Bool,
            TokType.CharType                     => FunnyType.Char,
            TokType.TextType                     => FunnyType.Text,
            TokType.AnythingType                 => FunnyType.Any,
            TokType.Id when token.Value == "any" => FunnyType.Any,
            TokType.Id when token.Value == "ip"  => FunnyType.Ip,
            _                                    => throw Errors.TypeExpectedButWas(token)
        };

    public static FunnyType ReadType(this TokFlow flow) {
        FunnyType readType;
        int lastPosition;
        int typeStart;

        if (flow.IsCurrent(TokType.FiObr))
        {
            var openBrace = flow.Current;
            typeStart = openBrace.Start;
            flow.MoveNext(); // consume {
            readType = ReadStructType(flow, openBrace);
            lastPosition = flow.Previous.Finish; // end of } token
        }
        else
        {
            var cur = flow.Current;
            typeStart = cur.Start;
            readType = ToFunnyType(cur);
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
                readType = FunnyType.ArrayOf(readType);
            }
            else if (flow.IsCurrent(TokType.Question))
            {
                if (flow.Current.Start != lastPosition)
                    break; // space before ? means it's not a type suffix
                lastPosition = flow.Current.Finish;
                flow.MoveNext();
                readType = FunnyType.OptionalOf(readType);
            }
            else
            {
                break;
            }
        }

        return readType;
    }


    private static FunnyType ReadStructType(TokFlow flow, Tok openBrace) {
        var fields = new List<(string, FunnyType)>();
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

            var fieldType = flow.ReadType(); // recursive — handles nested structs, arrays, optionals
            fields.Add((idToken.Value, fieldType));

            hasAnyDelimiter = false;
            if (flow.MoveIf(TokType.Sep))
                hasAnyDelimiter = true;
            if (flow.SkipNewLines())
                hasAnyDelimiter = true;
        }

        return FunnyType.StructOf(fields.ToArray());
    }

    public static bool MoveIf(this TokFlow flow, TokType tokType) {
        if (!flow.IsCurrent(tokType)) return false;
        flow.MoveNext();
        return true;
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
