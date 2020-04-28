using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.TypeInference;
using NFun.Types;

namespace NFun.TypeInferenceAdapter
{
    public class SetupTiEnterVisitor: EnterVisitorBase
    {
        private readonly SetupTiState _setupTiState;

        public SetupTiEnterVisitor(SetupTiState setupTiState)
        {
            _setupTiState = setupTiState;
        }

        public override VisitorEnterResult Visit(UserFunctionDefenitionSyntaxNode node) 
            => VisitorEnterResult.Skip;
        
        public override VisitorEnterResult Visit(AnonymCallSyntaxNode anonymFunNode)
        {
            _setupTiState.EnterScope(anonymFunNode.OrderNumber);
            foreach (var syntaxNode in anonymFunNode.ArgumentsDefenition)
            {
                string originName;
                string anonymName;
                if (syntaxNode is TypedVarDefSyntaxNode typed)
                {
                    originName = typed.Id;
                    anonymName = MakeAnonVariableName(anonymFunNode, originName);
                    if (typed.VarType.Equals(VarType.Empty))
                    {
                        //type = _setupTiState.CurrentSolver.SetVarType();
                        //if (type == null)
                        //    throw ErrorFactory.AnonymousFunctionArgumentDuplicates(typed, anonymFunNode);
                    }
                    else
                    {
                        var ticType = typed.VarType.ConvertToTiType();
                        _setupTiState.CurrentSolver.SetVarType(anonymName, ticType);
                    }
                }
                else if (syntaxNode is VariableSyntaxNode varNode)
                {
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(anonymFunNode, originName);
                    //if (_setupTiState.CurrentSolver.HasVariable(anonymName))
                    //    throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, anonymFunNode);
                    //type = _setupTiState.CurrentSolver.SetNewVarOrNull(anonymName);
                    //if (type == null)
                    //    throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, anonymFunNode);
                }
                else 
                    throw ErrorFactory.AnonymousFunArgumentIsIncorrect(syntaxNode);

                _setupTiState.AddVariableAliase(originName, anonymName);
            }

            //var lambdaRes = _setupTiState.CurrentSolver.InitLambda(anonymFunNode.OrderNumber,
            //    anonymFunNode.Body.OrderNumber, argTypes.ToArray());
            //if (!lambdaRes.IsSuccesfully)
            //    throw ErrorFactory.AnonymousFunDefenitionIsIncorrect(anonymFunNode);

            return VisitorEnterResult.Continue;

            //throw new InvalidOperationException();

            //var argTypes = new List<SolvingNode>();
            //_setupTiState.EnterScope(anonymFunNode.OrderNumber);
            //foreach (var syntaxNode in anonymFunNode.ArgumentsDefenition)
            //{
            //    SolvingNode type;
            //    string originName;
            //    string anonymName;
            //    if (syntaxNode is TypedVarDefSyntaxNode typed)
            //    {
            //        originName = typed.Id;
            //        anonymName = MakeAnonVariableName(anonymFunNode, originName);
            //        if (typed.VarType.Equals(VarType.Empty))
            //        {
            //            type = _setupTiState.CurrentSolver.SetNewVarOrNull(anonymName);
            //            if (type == null)
            //                throw ErrorFactory.AnonymousFunctionArgumentDuplicates(typed, anonymFunNode);
            //        }
            //        else
            //        {
            //            _setupTiState.CurrentSolver.SetVarType(anonymName, typed.VarType.ConvertToTiType());
            //            type = _setupTiState.CurrentSolver.GetOrCreate(anonymName);
            //        }
            //    }
            //    else if (syntaxNode is VariableSyntaxNode varNode)
            //    {
            //        originName = varNode.Id;
            //        anonymName = MakeAnonVariableName(anonymFunNode, originName);
            //        if (_setupTiState.CurrentSolver.HasVariable(anonymName))
            //            throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, anonymFunNode);
            //        type = _setupTiState.CurrentSolver.SetNewVarOrNull(anonymName);
            //        if (type == null)
            //            throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, anonymFunNode);
            //    }
            //    else 
            //        throw ErrorFactory.AnonymousFunArgumentIsIncorrect(syntaxNode);

            //    _setupTiState.AddVariableAliase(originName, anonymName);
            //    argTypes.Add(type);
            //}

            //var lambdaRes = _setupTiState.CurrentSolver.InitLambda(anonymFunNode.OrderNumber,
            //    anonymFunNode.Body.OrderNumber, argTypes.ToArray());
            //if (!lambdaRes.IsSuccesfully)
            //    throw ErrorFactory.AnonymousFunDefenitionIsIncorrect(anonymFunNode);

            //return VisitorEnterResult.Continue;
        }

        private static string MakeAnonVariableName(AnonymCallSyntaxNode node, string id) 
            => LangTiHelper.GetArgAlias("anonymous_"+node.OrderNumber, id);
    }
}