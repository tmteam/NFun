using NFun.HindleyMilner;
using NFun.HindleyMilner.Tyso;
using NFun.Interpritation;
using NFun.Parsing;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.SyntaxParsing
{
    public class HmAlgorithmAdapter
    {
        public HmAlgorithmAdapter(FunctionsDictionary dictionary)
        {
            _solver = new NsHumanizerSolver();
            EnterVisitor = new EnterHmVisitor();
            ExitVisitor = new ExitHmVisitor(_solver, dictionary);
        }

        private NsHumanizerSolver _solver;
        public ISyntaxNodeVisitor<VisitorResult> EnterVisitor { get; } 
        public ISyntaxNodeVisitor<bool> ExitVisitor { get; }

        public FunTypeSolving Apply(ISyntaxNode tree)
        {
            var res = tree.ComeOver(EnterVisitor, ExitVisitor);
            if(!res)
                return new FunTypeSolving(NsResult.NotSolvedResult());
            
            var solving = _solver.Solve();
            return new FunTypeSolving(solving);
        }
    }

    public class FunTypeSolving
    {
        private readonly NsResult _result;

        public FunTypeSolving(NsResult result)
        {
            _result = result;
        }

        public int GenericsCount => _result.GenericsCount;
        public bool IsSolved => _result.IsSolved;

        public VarType GetVarType(string varId)
            => AdpterHelper.ConvertToSimpleTypes( _result.GetVarType(varId));

        public VarType GetNodeType(int nodeId) =>
            AdpterHelper.ConvertToSimpleTypes(_result.GetNodeType(nodeId));

    }
}