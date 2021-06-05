using System.Linq;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Types;

namespace NFun.SyntaxParsing.Visitors
{
    public class ShortDescritpionVisitor: ISyntaxNodeVisitor<string>
    {
        public void OnEnterNode(ISyntaxNode parent, int childNum) { }

        public string Visit(ArrowAnonymFunctionSyntaxNode arrowAnonymFunNode) => "(..)=>..";
        public string Visit(ArraySyntaxNode node) =>"[...]";
        public string Visit(EquationSyntaxNode node) => $"{node.Id} = ... ";
        public string Visit(FunCallSyntaxNode node) => $"{node.Id}(...)";
        public string Visit(IfThenElseSyntaxNode node) => "if (...) ... else ...";
        public string Visit(IfCaseSyntaxNode node) => "if (...) ...";
        public string Visit(ListOfExpressionsSyntaxNode node)
        {
            var strings = node.Expressions.Select(e => e.Accept(this));
            return $"{string.Join(",", strings)}";
        }

        public string Visit(ConstantSyntaxNode node)
        {
            if (node.OutputType.Equals(FunnyType.Text))
            {
                var str = node.Value.ToString();
                return $"'{(str.Length > 20 ? (str.Substring(17) + "...") : str)}'";
            }
            return $"{node.Value}";
        }
        public string Visit(SyntaxTree node) => "Fun equations";
        public string Visit(TypedVarDefSyntaxNode node)
            => $"'{node.Id}:{node.FunnyType}";
        public string Visit(UserFunctionDefinitionSyntaxNode node) => $"{node.Id}(...) = ...";
        public string Visit(VarDefinitionSyntaxNode node) => $"'{node.Id}:{node.FunnyType}";
        public string Visit(NamedIdSyntaxNode node) => node.Id;
        public string Visit(ResultFunCallSyntaxNode node) => $"{node.ResultExpression.Accept(this)}(...)";
        public string Visit(SuperAnonymFunctionSyntaxNode node) => "{" + node.Body.Accept(this) + "}";
        public string Visit(StructFieldAccessSyntaxNode node) => $".{node.FieldName}";
        public string Visit(StructInitSyntaxNode node)
            => $"{{ {string.Join("; ", node.Fields.Select(f => $"{f.Name}={f.Node.Accept(this)}"))}}}";

        public string Visit(GenericIntSyntaxNode node) => node.Value.ToString();
    }
}