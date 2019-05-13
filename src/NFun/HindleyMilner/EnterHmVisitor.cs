using System.Collections.Generic;
using System.Linq;
using NFun.HindleyMilner.Tyso;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.Parsing;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.HindleyMilner
{
    class EnterHmVisitor: ISyntaxNodeVisitor<VisitorResult>
    {
        private readonly HmVisitorState _hmVisitorState;

        public EnterHmVisitor(HmVisitorState hmVisitorState)
        {
            _hmVisitorState = hmVisitorState;
        }

        public VisitorResult Visit(UserFunctionDefenitionSyntaxNode node)
        {
            _hmVisitorState.EnterUserFunction(node.Id, node.Args.Count);
            foreach (var arg in node.Args)
            {
                if(!arg.VarType.Equals(VarType.Empty))
                    _hmVisitorState.CurrentSolver.SetVarType(arg.Id, AdpterHelper.ConvertToHmType(arg.VarType));
            }

            return VisitorResult.Continue;
        }
        
        public VisitorResult Visit(ProcArrayInit node)=> VisitorResult.Continue;
        public VisitorResult Visit(ArraySyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(AnonymCallSyntaxNode node)
        {
            List<SolvingNode> argTypes = new List<SolvingNode>();
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
                        type = _hmVisitorState.CurrentSolver.GetByVar(anonymName);
                    }
                }
                else if (syntaxNode is VariableSyntaxNode varNode)
                {
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(node, originName);
                    type = _hmVisitorState.CurrentSolver.SetNewVar(anonymName);
                }
                else 
                    throw new FunParseException(-4, "Unexpected lambda defention",0,0);

                _hmVisitorState.AddAnonymVariablesAliase(originName, anonymName);
                argTypes.Add(type);
            }

           if(!_hmVisitorState.CurrentSolver.InitLambda(node.NodeNumber, node.Body.NodeNumber, argTypes.ToArray()))
               throw new FunParseException(-3, "LambdaCannot be iniited", 0,0);
            
            return VisitorResult.Continue;
        }

        private static string MakeAnonVariableName(AnonymCallSyntaxNode node, string id)
        {
            var anonName = "=" + node.NodeNumber + ":" + id;
            return anonName;
        }

        public VisitorResult Visit(EquationSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(FunCallSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(IfThenElseSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(IfThenSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(ListOfExpressionsSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(NumberSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(SyntaxTree node)=> VisitorResult.Continue;
        public VisitorResult Visit(TextSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(TypedVarDefSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(VarDefenitionSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(VariableSyntaxNode node)=> VisitorResult.Continue;

    }
}