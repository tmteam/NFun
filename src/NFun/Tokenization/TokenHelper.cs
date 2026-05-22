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
        else if (flow.IsCurrent(TokType.Rule) && flow.Peek?.Type == TokType.ParenthObr)
        {
            // Function type: rule(argTypes...)->returnType
            typeStart = flow.Current.Start;
            flow.MoveNext(); // skip 'rule'
            flow.MoveNext(); // skip '('

            var argTypes = new List<TypeSyntax>();
            if (!flow.IsCurrent(TokType.ParenthCbr)) {
                argTypes.Add(ReadTypeSyntax(flow));
                while (flow.MoveIf(TokType.Sep, out _))
                    argTypes.Add(ReadTypeSyntax(flow));
            }
            if (!flow.MoveIf(TokType.ParenthCbr, out _))
                throw new FunnyParseException(
                    541, "Expected ')' after rule type arguments", flow.Current.Start, flow.Current.Finish);

            // Arrow is required for function type syntax
            if (!flow.MoveIf(TokType.Arrow, out _))
                throw new FunnyParseException(
                    540, "Expected '->' after rule type arguments", flow.Current.Start, flow.Current.Finish);

            var returnType = ReadTypeSyntax(flow);
            readType = new TypeSyntax.FunOf(argTypes.ToArray(), returnType);
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

        // User-written struct annotations are strict-width: `b:{x:int, y:int} = a` where
        // a:{x,y,z} must NOT widen b's static type to include z. The Pull Struct ancestor
        // width-propagation only fires when ancStruct.IsFrozen=false, so marking user
        // annotations as frozen here pins the declared shape. (MR5Bug6.)
        return new TypeSyntax.StructOf(fields.ToArray(), isFrozen: true);
    }

    public static bool MoveIf(this TokFlow flow, TokType tokType) {
        if (!flow.IsCurrent(tokType)) return false;
        flow.MoveNext();
        return true;
    }

    /// <summary>
    /// Moves if current token can be used as a struct field name: an Id, or any
    /// primitive-type keyword (real, int, bool, text, …). Type-keywords carry
    /// their lexeme in <see cref="Tok.Value"/>, so callers that only consume
    /// <c>.Value</c> get the right string. This unblocks scripts like
    /// <c>type c = {real: real, imag: real}</c> where a field happens to share
    /// a primitive-type name. The relaxation is safe in field-name positions
    /// (struct def / struct literal / dot-access) because the grammar there
    /// always expects an identifier next — no ambiguity with a type slot.
    /// </summary>
    public static bool MoveIfFieldName(this TokFlow flow, out Tok tok) {
        if (flow.IsCurrent(TokType.Id) || IsTypeKeyword(flow.Current.Type))
        {
            tok = flow.Current;
            flow.MoveNext();
            return true;
        }
        tok = null;
        return false;
    }

    private static bool IsTypeKeyword(TokType type) => type
        is TokType.TextType
        or TokType.BoolType
        or TokType.CharType
        or TokType.RealType
        or TokType.Int16Type
        or TokType.Int32Type
        or TokType.Int64Type
        or TokType.UInt8Type
        or TokType.UInt16Type
        or TokType.UInt32Type
        or TokType.UInt64Type
        or TokType.AnythingType;

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
