namespace NFun.SyntaxParsing;

using System.Text;
using SyntaxNodes;
using Visitors;

public class SyntaxTreePrinter {
    private StringBuilder _sb = new();
    private int __indent;

    private SyntaxTreePrinter() { }

    public static string Print(ISyntaxNode node) {
        var printer = new SyntaxTreePrinter();
        printer.PrintReq(node);
        return printer._sb.ToString();
    }

    private void PrintReq(ISyntaxNode node) {
        _sb.AppendLine(new string(' ', __indent * 3) +   node.Accept(SyntaxNodePrinterVisitor.Instance));
        __indent++;
        foreach (var child in node.Children)
            PrintReq(child);
        __indent--;
    }
}

class SyntaxNodePrinterVisitor : ISyntaxNodeVisitor<string> {

    public static readonly SyntaxNodePrinterVisitor Instance = new();

    public string Visit(AnonymFunctionSyntaxNode node) => "anonym-fun";

    public string Visit(ArraySyntaxNode node) => "array";

    public string Visit(EquationSyntaxNode node) => $"equation '{node.Id}'";

    public string Visit(FunCallSyntaxNode node) => $"call '{node.Id}'";

    public string Visit(ComparisonChainSyntaxNode node) => $"chain-comp ";

    public string Visit(IfThenElseSyntaxNode node) => "if-else";

    public string Visit(IfCaseSyntaxNode node) => "if";

    public string Visit(ListOfExpressionsSyntaxNode node) => "list";

    public string Visit(ConstantSyntaxNode node) => $"constant '{node.Value}'";

    public string Visit(GenericIntSyntaxNode node) => $"gint '{node.Value}'";

    public string Visit(IpAddressConstantSyntaxNode node) => $"ip '{node.Value}'";

    public string Visit(SyntaxTree node) => "syntax-tree";

    public string Visit(TypedVarDefSyntaxNode node) => $"typed-var-def '{node.Id}:{node.FunnyType}'";

    public string Visit(UserFunctionDefinitionSyntaxNode node) => $"fun '{node.Id}'";

    public string Visit(VarDefinitionSyntaxNode node) => $"var-def '{node.Id}'";

    public string Visit(NamedIdSyntaxNode node) => $"id '{node.Id}'";

    public string Visit(ResultFunCallSyntaxNode node) => "hi-call";

    public string Visit(SuperAnonymFunctionSyntaxNode node) => "super-anonym-def";

    public string Visit(StructFieldAccessSyntaxNode node) => $"field-access '{node.FieldName}";

    public string Visit(StructInitSyntaxNode node) => "struct";

    public string Visit(DefaultValueSyntaxNode node) => "default";
}
