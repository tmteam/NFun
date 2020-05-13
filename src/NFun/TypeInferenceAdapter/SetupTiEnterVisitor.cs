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
            //Мы должны найти сигнатуру функции для указанного узла на входе
            //для того чтобы вложенные аргументы
            //знали что выбирать - переменную или функцию - исходя из сигнатуры 
            //внешней функции

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
            //Функция может быть рекурсивной. 
            //Обработка вызовов функций должна проверить - не является ли она вызовом самого себя,
            //Вместо того что бы лезть в словарь функций. мы можем посмотреть нет ли в выведение типов такой переменной
            //
            //Сигнатуру получившейся функции можно будет посмотреть в результатах вывода типов

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