using System.Collections.Generic;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.HindleyMilner.Tyso;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Parsing;
using NFun.SyntaxParsing.Visitors;

namespace NFun.HindleyMilner
{
    class ExitHmVisitor: ISyntaxNodeVisitor<bool>
    {
        private readonly HmVisitorState _state;
        private readonly FunctionsDictionary _dictionary;
        private readonly Dictionary<string, FunctionPrototype> _userFunctions = new Dictionary<string, FunctionPrototype>();
        public ExitHmVisitor(HmVisitorState state, FunctionsDictionary dictionary)
        {
            _state = state;
            _dictionary = dictionary;
        }

        public bool Visit(ArraySyntaxNode node)=> _state.CurrentSolver.SetArrayInit(node.NodeNumber, node.Expressions.Select(e=>e.NodeNumber).ToArray());
        public bool Visit(UserFunctionDefenitionSyntaxNode node)
        {
            /*
            if (!node.OutputType.Equals(VarType.Empty))
                _state.CurrentSolver.SetStrict(node.Body.NodeNumber, AdpterHelper.ConvertToHmType(node.OutputType));
            
            var functionSolvation = _state.ExitFunction();
            var solvation = functionSolvation.Solver.Solve();
            
            if (!solvation.IsSolved)
                throw new FunParseException(-1,"Function type not solved", 0, 0);
           
            var userName = functionSolvation.Name + ":" + functionSolvation.ArgsCount;
            if(_userFunctions.ContainsKey(userName))
                throw new FunParseException(-3,"User function duplicates", 0, 0);
            var solvedArgTypes = new List<VarType>();
            foreach (var var in node.Args)
            {
                var res = solvation.GetVarTypeOrNull(var.Id);
                if(res==null)
                    solvedArgTypes.Add(VarType.Anything);
                else
                    solvedArgTypes.Add(AdpterHelper.ConvertToSimpleTypes(res));
            }

            var outputType =  AdpterHelper.ConvertToSimpleTypes(solvation.GetNodeType(node.Body.NodeNumber));
            
            _userFunctions.Add(userName, new FunctionPrototype(functionSolvation.Name, outputType, solvedArgTypes.ToArray()));
            */
            return true;
        }

        public bool Visit(ProcArrayInit node)
        {
            if (node.Step == null)
            {
                return _state.CurrentSolver.SetProcArrayInit(node.NodeNumber, node.From.NodeNumber, node.To.NodeNumber);
            }
            else
            {
                return _state.CurrentSolver.SetProcArrayInit(node.NodeNumber, node.From.NodeNumber, node.To.NodeNumber,node.Step.NodeNumber);
            }
        }

        public bool Visit(AnonymCallSyntaxNode node) => true;
        
        public bool Visit(EquationSyntaxNode node)
           => _state.CurrentSolver.SetDefenition(node.Id, node.NodeNumber, node.Expression.NodeNumber);

        public bool Visit(FunCallSyntaxNode node)
        {

            if (node.IsOperator)
            {
                switch (node.Id)
                {
                    case CoreFunNames.Multiply:
                    case CoreFunNames.Add:
                    case CoreFunNames.Substract:
                    case CoreFunNames.Remainder:
                        return _state.CurrentSolver.SetArithmeticalOp(node.NodeNumber, node.Args[0].NodeNumber,
                            node.Args[1].NodeNumber);
                    case CoreFunNames.BitShiftLeft:
                    case CoreFunNames.BitShiftRight:
                        return _state.CurrentSolver.SetBitShiftOperator(node.NodeNumber, node.Args[0].NodeNumber,
                            node.Args[1].NodeNumber);
                }
            }

            var argsCount = node.Args.Length;

            var userShortName = node.Id + ":" + argsCount;
            if(_userFunctions.ContainsKey(userShortName)){
                var callDef = ToCallDef(node, _userFunctions[userShortName]);
                return _state.CurrentSolver.SetCall(callDef);
            }
            
            var candidates = _dictionary.GetNonGeneric(node.Id).Where(n=>n.ArgTypes.Length == argsCount).ToList();

            if (candidates.Count == 0)
            {
                var genericCandidate = _dictionary.GetGenericOrNull(node.Id, argsCount);
                if (genericCandidate == null)
                    return false;
                var callDef = ToCallDef(node, genericCandidate);
                return _state.CurrentSolver.SetCall(callDef);

            }
            if (candidates.Count == 1)
            {
                var fun = candidates[0];
                var callDef = ToCallDef(node, fun);
                return _state.CurrentSolver.SetCall(callDef);
            }

            return _state.CurrentSolver.SetOverloadCall(candidates.Select(ToFunSignature).ToArray(), node.NodeNumber,
                node.Args.Select(a => a.NodeNumber).ToArray());
        }

        private static FunSignature ToFunSignature(FunctionBase fun) 
            =>
            new FunSignature( AdpterHelper.ConvertToHmType(fun.SpecifiedType), 
                fun.ArgTypes.Select(AdpterHelper.ConvertToHmType).ToArray());

        private static CallDef ToCallDef(FunCallSyntaxNode node, FunctionBase fun)
        {
            var ids = new[] {node.NodeNumber}.Concat(node.Args.Select(a => a.NodeNumber)).ToArray();
            var types = new[] {fun.SpecifiedType}.Concat(fun.ArgTypes).Select(AdpterHelper.ConvertToHmType).ToArray();

            var callDef = new CallDef(types, ids);
            return callDef;
        }
        private static CallDef ToCallDef(FunCallSyntaxNode node, GenericFunctionBase fun)
        {
            var ids = new[] {node.NodeNumber}.Concat(node.Args.Select(a => a.NodeNumber)).ToArray();
            var types = new[] {fun.SpecifiedType}.Concat(fun.ArgTypes).Select(AdpterHelper.ConvertToHmType).ToArray();

            var callDef = new CallDef(types, ids);
            return callDef;
        }

        public bool Visit(IfThenElseSyntaxNode node)
        {
            return _state.CurrentSolver.ApplyLcaIf(node.NodeNumber,
                node.Ifs.Select(i => i.Condition.NodeNumber).ToArray(),
                node.Ifs.Select(i => i.Expr.NodeNumber).Append(node.ElseExpr.NodeNumber).ToArray());
        }
        public bool Visit(IfThenSyntaxNode node)=> true;
        public bool Visit(ListOfExpressionsSyntaxNode node)=> true;

        public bool Visit(NumberSyntaxNode node)
        {
            //dirty hack!!!
            var valueExpression =            
                ExpressionBuilder.GetValueNode(node);
            return _state.CurrentSolver.SetConst(node.NodeNumber, 
                AdpterHelper.ConvertToHmType(valueExpression.Type));
        }

        public bool Visit(SyntaxTree node)=> true;
        public bool Visit(TextSyntaxNode node) 
            => _state.CurrentSolver.SetConst(node.NodeNumber, FType.Text);

        public bool Visit(TypedVarDefSyntaxNode node)
            => _state.CurrentSolver.SetVarType(node.Id, AdpterHelper.ConvertToHmType(node.VarType));
            
        public bool Visit(VarDefenitionSyntaxNode node)
            => _state.CurrentSolver.SetVarType(node.Id, AdpterHelper.ConvertToHmType(node.VarType));
        public bool Visit(VariableSyntaxNode node)
        {
            var id = _state.GetActualName(node.Id);
            return _state.CurrentSolver.SetVar(node.NodeNumber, id);
        }
    }
}