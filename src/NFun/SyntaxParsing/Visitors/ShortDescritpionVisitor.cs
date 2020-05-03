using System.Linq;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Types;

namespace NFun.SyntaxParsing.Visitors
{
    public class ShortDescritpionVisitor: ISyntaxNodeVisitor<string>
    {
        public void SetChildrenNumber(ISyntaxNode parent, int num) { }

        public string Visit(AnonymCallSyntaxNode anonymFunNode) => "(..)=>..";
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
            if (node.OutputType.Equals(VarType.Text))
            {
                var str = node.Value.ToString();
                return $"'{(str.Length > 20 ? (str.Substring(17) + "...") : str)}'";
            }
            return $"{node.Value}";
        }

        public string Visit(ProcArrayInit node)
        {
            var from = node.From.Accept(this);
            var to = node.To.Accept(this);

            if (node.Step == null)
                return $"[{from}..{to}]";
            else
                return $"[{from}..{to}..{node.Step.Accept(this)}]";
        }

        public string Visit(SyntaxTree node) => "Fun equations";
        public string Visit(TypedVarDefSyntaxNode node)
            => $"'{node.Id}:{node.VarType}";


        public string Visit(UserFunctionDefenitionSyntaxNode node) => $"{node.Id}(...) = ...";
        public string Visit(VarDefenitionSyntaxNode node) => $"'{node.Id}:{node.VarType}";

        public string Visit(VariableSyntaxNode node) => node.Id;
        public string Visit(GenericIntSyntaxNode node) => node.Value.ToString();
    }
}