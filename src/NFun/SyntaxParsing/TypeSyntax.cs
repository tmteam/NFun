using NFun.Tokenization;

namespace NFun.SyntaxParsing;

/// <summary>
/// Syntactic representation of a type annotation.
/// Built by parser from tokens. Contains no semantic resolution —
/// just what the user wrote. Resolved to FunnyType later by TicSetupVisitor.
/// </summary>
public abstract class TypeSyntax {
    public static readonly TypeSyntax Empty = new EmptyType();

    public sealed class EmptyType : TypeSyntax {
        public override string ToString() => "";
    }

    public sealed class Named : TypeSyntax {
        public string Name { get; }
        public Interval Interval { get; }
        public Named(string name, Interval interval) { Name = name; Interval = interval; }
        public override string ToString() => Name;
    }

    public sealed class ArrayOf : TypeSyntax {
        public TypeSyntax Element { get; }
        public ArrayOf(TypeSyntax element) => Element = element;
        public override string ToString() => Element + "[]";
    }

    public sealed class OptionalOf : TypeSyntax {
        public TypeSyntax Element { get; }
        public OptionalOf(TypeSyntax element) => Element = element;
        public override string ToString() => Element + "?";
    }

    public sealed class StructOf : TypeSyntax {
        public (string FieldName, TypeSyntax FieldType)[] Fields { get; }
        public bool IsFrozen { get; }
        public StructOf((string, TypeSyntax)[] fields, bool isFrozen = false) {
            Fields = fields;
            IsFrozen = isFrozen;
        }
        public override string ToString() =>
            "{" + string.Join("; ", System.Array.ConvertAll(Fields, f => f.FieldName + ":" + f.FieldType)) + "}";
    }
}
