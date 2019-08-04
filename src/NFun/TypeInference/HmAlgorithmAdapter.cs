using NFun.Interpritation;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.Visitors;
using NFun.TypeInference.Solving;

namespace NFun.HindleyMilner
{
    public class HmAlgorithmAdapter
    {
        public HmAlgorithmAdapter(
            FunctionsDictionary dictionary, 
            HmVisitorState state = null)
        {
            _solver = state?.CurrentSolver??new HmHumanizerSolver();
            var visitorState = state??new HmVisitorState(_solver);
            EnterVisitor = new EnterHmVisitor(visitorState);
            ExitVisitor = new ExitHmVisitor(visitorState, dictionary);
        }

        private readonly HmHumanizerSolver _solver;
        public ISyntaxNodeVisitor<VisitorResult> EnterVisitor { get; } 
        public ISyntaxNodeVisitor<bool> ExitVisitor { get; }
        
        public bool ComeOver(ISyntaxNode node) => node.ComeOver(EnterVisitor, ExitVisitor);

        public FunTypeSolving Solve()
        {
            var solving = _solver.Solve();
            return new FunTypeSolving(solving);
        }
    }
}