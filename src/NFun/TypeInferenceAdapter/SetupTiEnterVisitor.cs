using System;
using System.Linq;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;
using NFun.Types;

namespace NFun.TypeInferenceAdapter
{
    public class SetupTiEnterVisitor: EnterVisitorBase
    {
        private readonly SetupTiState _setupTiState;
        private readonly IFunctionDicitionary _dictionary;
        private readonly TypeInferenceResultsBuilder _resultsBuilder;

        public SetupTiEnterVisitor(
            SetupTiState setupTiState,
            IFunctionDicitionary dictionary, 
            TypeInferenceResultsBuilder resultsBuilder)
        {
            _setupTiState = setupTiState;
            _dictionary = dictionary;
            _resultsBuilder = resultsBuilder;
        }

        public override VisitorEnterResult Visit(MetaInfoSyntaxNode node) => VisitorEnterResult.Skip;

        public override VisitorEnterResult Visit(FunCallSyntaxNode node)
        {
            var signature = _dictionary.GetOrNull(node.Id, node.Args.Length);
            if (signature is GenericMetafunction)
            {
                //Если сигнатура - метафункциальная - нужно найти оригинальную функцию и перестроить дерево
                var firstArg = node.Args[0] as VariableSyntaxNode;
                if(firstArg==null)
                    throw FunParseException.ErrorStubToDo("first arg should be variable");
                node.TransformToMetafunction(firstArg);
            }
            
            if (signature != null)
                _resultsBuilder.RememberFunctionCall(node.OrderNumber, signature);

            return VisitorEnterResult.Continue;
        }
        public override VisitorEnterResult Visit(UserFunctionDefenitionSyntaxNode node)
        {
            var argNames = new string[node.Args.Count];
            int i = 0;
            foreach (var arg in node.Args)
            {
                argNames[i] = arg.Id;
                i++;
                if (arg.VarType != VarType.Empty)
                    _setupTiState.CurrentSolver.SetVarType(arg.Id, arg.VarType.ConvertToTiType());
            }

            IType returnType = null;
            if (node.ReturnType != VarType.Empty)
                returnType = (IType)node.ReturnType.ConvertToTiType();
            
            TraceLog.WriteLine($"Enter {node.OrderNumber}. UFun {node.Id}({string.Join(",",argNames)})->{node.Body.OrderNumber}:{returnType?.ToString()??"empty"}");
            var fun =_setupTiState.CurrentSolver.SetFunDef(
                name: node.Id+"'"+ node.Args.Count, 
                returnId: node.Body.OrderNumber, 
                returnType: returnType, 
                varNames: argNames);
            _resultsBuilder.RememberUserFunctionSignature(node.Id, fun);
            return VisitorEnterResult.Continue;
        }

        public override VisitorEnterResult Visit(SuperAnonymCallSyntaxNode node)
        {
            throw new NotImplementedException();
        }
        public override VisitorEnterResult Visit(ArrowAnonymCallSyntaxNode arrowAnonymFunNode)
        {
            _setupTiState.EnterScope(arrowAnonymFunNode.OrderNumber);
            foreach (var syntaxNode in arrowAnonymFunNode.ArgumentsDefenition)
            {
                string originName;
                string anonymName;
                if (syntaxNode is TypedVarDefSyntaxNode typed)
                {
                    originName = typed.Id;
                    anonymName = MakeAnonVariableName(arrowAnonymFunNode, originName);
                    if (!typed.VarType.Equals(VarType.Empty))
                    {
                        var ticType = typed.VarType.ConvertToTiType();
                        _setupTiState.CurrentSolver.SetVarType(anonymName, ticType);
                    }
                }
                else if (syntaxNode is VariableSyntaxNode varNode)
                {
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(arrowAnonymFunNode, originName);
                }
                else 
                    throw ErrorFactory.AnonymousFunArgumentIsIncorrect(syntaxNode);

                _setupTiState.AddVariableAliase(originName, anonymName);
            }

            return VisitorEnterResult.Continue;
        }

        private static string MakeAnonVariableName(ArrowAnonymCallSyntaxNode node, string id) 
            => LangTiHelper.GetArgAlias("anonymous_"+node.OrderNumber, id);
    }
}