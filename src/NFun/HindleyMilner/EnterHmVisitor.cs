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

        public override VisitorResult Visit(UserFunctionDefenitionSyntaxNode node) => VisitorResult.Skip;
        
        public override VisitorResult Visit(AnonymCallSyntaxNode node)
        {
            var argTypes = new List<SolvingNode>();
            _hmVisitorState.EnterScope();
            foreach (var syntaxNode in node.ArgumentsDefenition)
            {
                SolvingNode type;
                string originName;
                string anonymName;
                if (syntaxNode is TypedVarDefSyntaxNode typed)
                {
                    originName = typed.Id;
                    anonymName = MakeAnonVariableName(node, originName);
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
                    anonymName = MakeAnonVariableName(node, originName);
                    if (_hmVisitorState.CurrentSolver.HasVariable(anonymName))
                        throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, node);
                    type = _hmVisitorState.CurrentSolver.SetNewVar(anonymName);
                }
                else 
                    throw FunParseException.ErrorStubToDo("Unexpected lambda defention");
                
                _hmVisitorState.AddVariableAliase(originName, anonymName);
                argTypes.Add(type);
            }

           if(!_hmVisitorState.CurrentSolver.InitLambda(node.NodeNumber, node.Body.NodeNumber, argTypes.ToArray()))
               throw FunParseException.ErrorStubToDo("LambdaCannot be iniited");
            
            return VisitorResult.Continue;
        }

        public override VisitorResult Visit(FunCallSyntaxNode funCallnode)
        {
            
            return VisitorResult.Continue;
        }
        
        private static string MakeAnonVariableName(AnonymCallSyntaxNode node, string id)
        {
            var anonName = "=" + node.NodeNumber + ":" + id;
            return anonName;
        }
    }
}