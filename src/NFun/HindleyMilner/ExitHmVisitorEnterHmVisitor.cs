using System.Linq;
using NFun.BuiltInFunctions;
using NFun.HindleyMilner;
using NFun.HindleyMilner.Tyso;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Parsing;
using NFun.SyntaxParsing.Visitors;

namespace NFun.SyntaxParsing
{
    class ExitHmVisitor: ISyntaxNodeVisitor<bool>
    {
        private NsHumanizerSolver _solver;
        private readonly FunctionsDictionary _dictionary;

        public ExitHmVisitor(NsHumanizerSolver solver, FunctionsDictionary dictionary)
        {
            _solver = solver;
            _dictionary = dictionary;
        }

        public bool Visit(ArraySyntaxNode node)=> false;
        public bool Visit(UserFunctionDefenitionSyntaxNode node)=> false;
        public bool Visit(ProcArrayInit node)=> false;
        public bool Visit(AnonymCallSyntaxNode node) => false;
        
        public bool Visit(EquationSyntaxNode node)
           => _solver.SetDefenition(node.Id, node.NodeNumber, node.Expression.NodeNumber);

        public bool Visit(FunCallSyntaxNode node)
        {

            if (node.IsOperator)
            {
                switch (node.Value)
                {
                    case CoreFunNames.Multiply:
                    case CoreFunNames.Add:
                    case CoreFunNames.Substract:
                    case CoreFunNames.Remainder:
                        return _solver.SetArithmeticalOp(node.NodeNumber, node.Args[0].NodeNumber,
                            node.Args[1].NodeNumber);
                    case CoreFunNames.BitShiftLeft:
                    case CoreFunNames.BitShiftRight:
                        return _solver.SetBitShiftOperator(node.NodeNumber, node.Args[0].NodeNumber,
                            node.Args[1].NodeNumber);
                }
            }

            var argsCount = node.Args.Length;
            var candidates = _dictionary.GetNonGeneric(node.Value).Where(n=>n.ArgTypes.Length == argsCount).ToList();

            if (candidates.Count == 0)
            {
                var genericCandidate = _dictionary.GetGenericOrNull(node.Value, argsCount);
                if (genericCandidate == null)
                    return false;
                var callDef = ToCallDef(node, genericCandidate);
                return _solver.SetCall(callDef);

            }
            if (candidates.Count == 1)
            {
                var fun = candidates[0];
                var callDef = ToCallDef(node, fun);
                return _solver.SetCall(callDef);
            }

            return _solver.SetOverloadCall(candidates.Select(ToFunSignature).ToArray(), node.NodeNumber,
                node.Args.Select(a => a.NodeNumber).ToArray());
        }

        private static FunSignature ToFunSignature(FunctionBase fun) 
            =>
            new FunSignature( AdpterHelper.ConvertToHmType(fun.OutputType), 
                fun.ArgTypes.Select(AdpterHelper.ConvertToHmType).ToArray());

        private static CallDef ToCallDef(FunCallSyntaxNode node, FunctionBase fun)
        {
            var ids = new[] {node.NodeNumber}.Concat(node.Args.Select(a => a.NodeNumber)).ToArray();
            var types = new[] {fun.OutputType}.Concat(fun.ArgTypes).Select(AdpterHelper.ConvertToHmType).ToArray();

            var callDef = new CallDef(types, ids);
            return callDef;
        }
        private static CallDef ToCallDef(FunCallSyntaxNode node, GenericFunctionBase fun)
        {
            var ids = new[] {node.NodeNumber}.Concat(node.Args.Select(a => a.NodeNumber)).ToArray();
            var types = new[] {fun.OutputType}.Concat(fun.ArgTypes).Select(AdpterHelper.ConvertToHmType).ToArray();

            var callDef = new CallDef(types, ids);
            return callDef;
        }

        public bool Visit(IfThenElseSyntaxNode node)
        {
            return _solver.ApplyLcaIf(node.NodeNumber,
                node.Ifs.Select(i => i.Condition.NodeNumber).ToArray(),
                node.Ifs.Select(i => i.Expr.NodeNumber).Append(node.ElseExpr.NodeNumber).ToArray());
        }
        public bool Visit(IfThenSyntaxNode node)=> true;
        public bool Visit(ListOfExpressionsSyntaxNode node)=> true;

        public bool Visit(NumberSyntaxNode node)
        {
            var valueExpression = SingleExpressionReader.GetValueNode(node);
            return _solver.SetConst(node.NodeNumber, 
                AdpterHelper.ConvertToHmType(valueExpression.Type));
        }

        public bool Visit(SyntaxTree node)=> true;
        public bool Visit(TextSyntaxNode node) 
            => _solver.SetConst(node.NodeNumber, FType.Text);

        public bool Visit(TypedVarDefSyntaxNode node)
            => _solver.SetVarType(node.Id, AdpterHelper.ConvertToHmType(node.VarType));
            
        public bool Visit(VarDefenitionSyntaxNode node)
            => _solver.SetVarType(node.Id, AdpterHelper.ConvertToHmType(node.VarType));
        public bool Visit(VariableSyntaxNode node)
            => _solver.SetVar(node.NodeNumber, node.Id);

    }
}