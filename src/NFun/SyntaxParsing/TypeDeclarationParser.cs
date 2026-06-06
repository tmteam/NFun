using System;
using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.SyntaxParsing;

/// <summary>
/// Parses `type Name = {...}` struct declarations and `type Name = alias` type aliases.
/// Shared between expression mode and lang mode — identical grammar in both.
/// </summary>
internal static class TypeDeclarationParser {
    /// <summary>
    /// Entry point. Caller positions cursor at the `type` keyword.
    /// </summary>
    internal static TypeDeclarationSyntaxNode Parse(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // skip 'type'

        if (!flow.MoveIf(TokType.Id, out var nameToken))
            throw Errors.TypeNameExpected(flow.Current);

        if (!flow.MoveIf(TokType.Def, out _))
            throw Errors.TypeDefTokenIsMissed(nameToken.Value, flow.Current);

        // type name = {...}  → struct type
        // type name = type   → type alias (int, int[], text?, other_name, etc.)
        if (!flow.IsCurrent(TokType.FiObr))
        {
            var aliasType = flow.ReadTypeSyntax();
            if (aliasType is TypeSyntax.EmptyType)
                throw Errors.TypeBodyExpected(nameToken.Value, flow.Current);
            return new TypeDeclarationSyntaxNode(nameToken.Value, aliasType,
                new Interval(start, flow.CurrentTokenFinishPosition));
        }

        flow.MoveNext(); // skip '{'

        var fields = new List<TypeFieldDefinition>();
        bool hasAnyDelimiter = true;
        flow.SkipNewLines();

        while (true)
        {
            if (flow.MoveIf(TokType.FiCbr))
                break;

            if (!hasAnyDelimiter)
                throw Errors.StructFieldDelimiterIsMissed(new Interval(flow.CurrentTokenStartPosition - 1,
                    flow.CurrentTokenFinishPosition));

            if (!flow.MoveIfFieldName(out var fieldId))
                throw Errors.StructFieldIdIsMissed(flow.Current);

            var fieldStart = fieldId.Start;

            var typeSyntax = TypeSyntax.Empty;
            if (flow.IsCurrent(TokType.Colon))
            {
                flow.MoveNext();
                typeSyntax = flow.ReadTypeSyntax();
                if (typeSyntax is TypeSyntax.EmptyType)
                    throw Errors.TypeExpectedButWas(flow.Current);
            }

            ISyntaxNode defaultValue = null;
            if (flow.MoveIf(TokType.Def))
            {
                flow.SkipNewLines();
                defaultValue = ExpressionParser.ReadNodeOrNull(flow);
                if (defaultValue == null)
                    throw Errors.StructFieldBodyIsMissed(fieldId);
            }

            if (fields.Any(f => string.Equals(f.Name, fieldId.Value, StringComparison.OrdinalIgnoreCase)))
                throw Errors.NamedTypeDuplicateField(nameToken.Value, fieldId.Value, fieldId.Interval);

            var fieldFinish = defaultValue?.Interval.Finish ?? flow.CurrentTokenFinishPosition;
            fields.Add(new TypeFieldDefinition(
                fieldId.Value, typeSyntax, defaultValue, new Interval(fieldStart, fieldFinish)));

            hasAnyDelimiter = flow.Previous.Type == TokType.NewLine;
            if (flow.MoveIf(TokType.Sep))
                hasAnyDelimiter = true;
            if (flow.SkipNewLines())
                hasAnyDelimiter = true;
            if (flow.IsDoneOrEof())
                throw Errors.StructIsUndone(flow.CurrentTokenFinishPosition);
        }

        return new TypeDeclarationSyntaxNode(nameToken.Value, fields,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }
}
