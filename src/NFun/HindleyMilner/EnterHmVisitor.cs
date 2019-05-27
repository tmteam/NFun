using System.Collections.Generic;
using System.Linq;
using NFun.HindleyMilner.Tyso;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.HindleyMilner
{
    class EnterHmVisitor: EnterVisitorBase
    {
        private readonly HmVisitorState _hmVisitorState;

        public EnterHmVisitor(HmVisitorState hmVisitorState)
        {
            _hmVisitorState = hmVisitorState;
        }

        public override VisitorResult Visit(UserFunctionDefenitionSyntaxNode node) 
            => VisitorResult.Skip;
        
        public override VisitorResult Visit(AnonymCallSyntaxNode anonymFunNode)
        {
            var argTypes = new List<SolvingNode>();
            _hmVisitorState.EnterScope(anonymFunNode.OrderNumber);
            foreach (var syntaxNode in anonymFunNode.ArgumentsDefenition)
            {
                SolvingNode type;
                string originName;
                string anonymName;
                if (syntaxNode is TypedVarDefSyntaxNode typed)
                {
                    originName = typed.Id;
                    anonymName = MakeAnonVariableName(anonymFunNode, originName);
                    if (typed.VarType.Equals(VarType.Empty))
                        type = _hmVisitorState.CurrentSolver.SetNewVar(anonymName);
                    else
                    {
                        _hmVisitorState.CurrentSolver.SetVarType(anonymName, typed.VarType.ConvertToHmType());
                        type = _hmVisitorState.CurrentSolver.GetOrCreate(anonymName);
                    }
                }
                else if (syntaxNode is VariableSyntaxNode varNode)
                {
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(anonymFunNode, originName);
                    if (_hmVisitorState.CurrentSolver.HasVariable(anonymName))
                        throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, anonymFunNode);
                    type = _hmVisitorState.CurrentSolver.SetNewVar(anonymName);
                }
                else 
                    throw ErrorFactory.AnonymousFunArgumentIsIncorrect(syntaxNode);
                
                _hmVisitorState.AddVariableAliase(originName, anonymName);
                argTypes.Add(type);
            }

            var lambdaRes = _hmVisitorState.CurrentSolver.InitLambda(anonymFunNode.OrderNumber,
                anonymFunNode.Body.OrderNumber, argTypes.ToArray());
            if (!lambdaRes.IsSuccesfully)
                throw ErrorFactory.AnonymousFunDefenitionIsIncorrect(anonymFunNode);
            
            return VisitorResult.Continue;
        }

        
        private static string MakeAnonVariableName(AnonymCallSyntaxNode node, string id) 
            => AdpterHelper.GetArgAlias("anonymous_"+node.OrderNumber, id);
    }
}