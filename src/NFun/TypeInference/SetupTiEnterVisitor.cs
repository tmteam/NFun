using System.Collections.Generic;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.TypeInference.Solving;
using NFun.Types;

namespace NFun.TypeInference
{
    class SetupTiEnterVisitor: EnterVisitorBase
    {
        private readonly SetupTiState _setupTiState;

        public SetupTiEnterVisitor(SetupTiState setupTiState)
        {
            _setupTiState = setupTiState;
        }

        public override VisitorResult Visit(UserFunctionDefenitionSyntaxNode node) 
            => VisitorResult.Skip;
        
        public override VisitorResult Visit(AnonymCallSyntaxNode anonymFunNode)
        {
            var argTypes = new List<SolvingNode>();
            _setupTiState.EnterScope(anonymFunNode.OrderNumber);
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
                    {
                        type = _setupTiState.CurrentSolver.SetNewVarOrNull(anonymName);
                        if (type == null)
                            throw ErrorFactory.AnonymousFunctionArgumentDuplicates(typed, anonymFunNode);
                    }
                    else
                    {
                        _setupTiState.CurrentSolver.SetVarType(anonymName, typed.VarType.ConvertToTiType());
                        type = _setupTiState.CurrentSolver.GetOrCreate(anonymName);
                    }
                }
                else if (syntaxNode is VariableSyntaxNode varNode)
                {
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(anonymFunNode, originName);
                    if (_setupTiState.CurrentSolver.HasVariable(anonymName))
                        throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, anonymFunNode);
                    type = _setupTiState.CurrentSolver.SetNewVarOrNull(anonymName);
                    if (type == null)
                        throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, anonymFunNode);
                }
                else 
                    throw ErrorFactory.AnonymousFunArgumentIsIncorrect(syntaxNode);
                
                _setupTiState.AddVariableAliase(originName, anonymName);
                argTypes.Add(type);
            }

            var lambdaRes = _setupTiState.CurrentSolver.InitLambda(anonymFunNode.OrderNumber,
                anonymFunNode.Body.OrderNumber, argTypes.ToArray());
            if (!lambdaRes.IsSuccesfully)
                throw ErrorFactory.AnonymousFunDefenitionIsIncorrect(anonymFunNode);
            
            return VisitorResult.Continue;
        }

        
        private static string MakeAnonVariableName(AnonymCallSyntaxNode node, string id) 
            => LangTiHelper.GetArgAlias("anonymous_"+node.OrderNumber, id);
    }
}