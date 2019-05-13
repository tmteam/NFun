using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Parsing;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.HindleyMilner
{
    class EnterHmVisitor: ISyntaxNodeVisitor<VisitorResult>
    {
        private HmVisitorState _hmVisitorState;

        public EnterHmVisitor(HmVisitorState hmVisitorState)
        {
            _hmVisitorState = hmVisitorState;
        }

        public VisitorResult Visit(UserFunctionDefenitionSyntaxNode node)
        {
            _hmVisitorState.EnterUserFunction(node.Id, node.Args.Count);
            foreach (var arg in node.Args)
            {
                if(!arg.VarType.Equals(VarType.Empty))
                    _hmVisitorState.CurrentSolver.SetVarType(arg.Id, AdpterHelper.ConvertToHmType(arg.VarType));
            }

            return VisitorResult.Continue;
        }
        
        public VisitorResult Visit(ProcArrayInit node)=> VisitorResult.Continue;
        public VisitorResult Visit(ArraySyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(AnonymCallSyntaxNode node) => VisitorResult.Continue;
        public VisitorResult Visit(EquationSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(FunCallSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(IfThenElseSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(IfThenSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(ListOfExpressionsSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(NumberSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(SyntaxTree node)=> VisitorResult.Continue;
        public VisitorResult Visit(TextSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(TypedVarDefSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(VarDefenitionSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(VariableSyntaxNode node)=> VisitorResult.Continue;

    }
}