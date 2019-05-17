using System.Collections.Generic;
using NFun.HindleyMilner.Tyso;
using NFun.Interpritation;
using NFun.Parsing;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.HindleyMilner
{
    public class HmAlgorithmAdapter
    {

        public HmAlgorithmAdapter(FunctionsDictionary dictionary, 
            HmVisitorState state = null)
        {
            _solver = state?.CurrentSolver??new NsHumanizerSolver();
            var visitorState = state??new HmVisitorState(_solver);
            EnterVisitor = new EnterHmVisitor(visitorState);
            ExitVisitor = new ExitHmVisitor(visitorState, dictionary);
        }

        private readonly NsHumanizerSolver _solver;
        public ISyntaxNodeVisitor<VisitorResult> EnterVisitor { get; } 
        public ISyntaxNodeVisitor<bool> ExitVisitor { get; }

        public FunTypeSolving ApplyWithFunDef(ISyntaxNode tree, string funAlias, int defNodeId, int bodyId)
        {
            var res = tree.ComeOver(EnterVisitor, ExitVisitor);
            if(!res)
                return new FunTypeSolving(NsResult.NotSolvedResult());

            if (!_solver.SetFunDefenition(funAlias, defNodeId, bodyId))
                return new FunTypeSolving(NsResult.NotSolvedResult());
            
            var solving = _solver.Solve();
            return new FunTypeSolving(solving);
        }
        
        public FunTypeSolving Apply(ISyntaxNode tree)
        {
            var res = tree.ComeOver(EnterVisitor, ExitVisitor);
            if(!res)
                return new FunTypeSolving(NsResult.NotSolvedResult());
            
            var solving = _solver.Solve();
            return new FunTypeSolving(solving);
        }
    }
}