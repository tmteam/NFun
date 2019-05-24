using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing
{
    public static class TopLevelParser
    {
        public const string AnonymousEquationId = "out";
        public static SyntaxTree Parse(TokFlow flow)
        {
            var reader = new SyntaxNodeReader(flow);
            var nodes = new List<ISyntaxNode>();
            var equationNames = new List<string>();
            bool hasAnonymousEquation = false;
            bool hasFuctions = false;
            while (true)
            {
                flow.SkipNewLines();
                if (flow.IsDone || flow.IsCurrent(TokType.Eof))
                    break;
                VarAttribute[] attributes = new VarAttribute[0];
                if (flow.IsCurrent(TokType.Attribute))
                    attributes = ReadAttributes(flow);

                var startOfTheString = flow.IsStart || flow.IsPrevious(TokType.NewLine);

                var exprStart = flow.Current.Start;
                var e = reader.ReadExpressionOrNull();
                if (e == null)
                    throw ErrorFactory.UnknownValueAtStartOfExpression(exprStart, flow.Current);
                
                if(e is TypedVarDefSyntaxNode typed)
                {
                    //Input typed var specification
                    nodes.Add(
                        new VarDefenitionSyntaxNode(typed, attributes));                        
                }
                else if (flow.IsCurrent(TokType.Def) || flow.IsCurrent(TokType.Colon))
                {
                    if (e is VariableSyntaxNode variable)
                    {
                        //equatation
                        flow.MoveNext();
                        var equation = ReadEquation(flow, reader, variable.Id, attributes);
                        nodes.Add(equation);
                        equationNames.Add(equation.Id);
                    }
                    //Todo Make operator fun as separate node type
                    else if (e is FunCallSyntaxNode fun && !fun.IsOperator)
                    {
                        //fun
                        if (attributes.Any())
                            throw ErrorFactory.AttributeOnFunction(exprStart, fun);
                        nodes.Add(ReadUserFunction(exprStart, fun, flow, reader));
                        hasFuctions = true;
                    }
                    else
                        throw ErrorFactory.ExpressionBeforeTheDefenition(exprStart, e, flow.Current);
                }
                else
                {
                    //anonymous equation
                    if (equationNames.Any())
                    {
                        if (startOfTheString && hasAnonymousEquation)
                            throw ErrorFactory.OnlyOneAnonymousExpressionAllowed(exprStart, e, flow.Current);
                        else
                            throw ErrorFactory.UnexpectedExpression(e);
                    }

                    if(!startOfTheString)
                        throw ErrorFactory.AnonymousExpressionHasToStartFromNewLine(exprStart, e, flow.Current);
                        
                    //todo start
                    //anonymous
                    var equation = new EquationSyntaxNode(AnonymousEquationId,0, e, attributes);
                    hasAnonymousEquation = true;
                    equationNames.Add(equation.Id);
                    nodes.Add(equation);
                }
            }
            
            return new SyntaxTree(nodes.ToArray());
            
        }
        private static VarAttribute[] ReadAttributes(TokFlow flow)
        {
            bool newLine = flow.IsStart || flow.Previous.Is(TokType.NewLine);
            var ans = new List<VarAttribute>();
            while (flow.IsCurrent(TokType.Attribute))
            {
                if (!newLine)
                    throw ErrorFactory.NowNewLineBeforeAttribute(flow);

                ans.Add(ReadAttribute(flow));
                flow.SkipNewLines();
            }
            return ans.ToArray();
        }
        private static VarAttribute ReadAttribute(TokFlow flow)
        {
            var start = flow.Current.Start;
            flow.MoveNext();
            if (!flow.MoveIf(TokType.Id, out var id))
                throw ErrorFactory.ItIsNotAnAttribute(start, flow.Current);
            object val = null;
            if (flow.MoveIf(TokType.Obr))
            {
                var next = flow.Current;
                switch (next.Type)
                {
                    case TokType.False:
                        val = false;
                        break;
                    case TokType.True:
                        val = true;
                        break;
                    case TokType.Number:
                        val = TokenHelper.ToConstant(next.Value).Item1;
                        break;
                    case TokType.Text:
                        val = next.Value;
                        break;
                    default:
                        throw ErrorFactory.ItIsNotCorrectAttributeValue(next);
                }
                flow.MoveNext();
                if(!flow.MoveIf(TokType.Cbr))                
                    throw ErrorFactory.AttributeCbrMissed(start, flow);
            }
            if(!flow.MoveIf(TokType.NewLine))
                throw ErrorFactory.NowNewLineAfterAttribute(start, flow);

            return new VarAttribute(id.Value, val);
        }

        private static UserFunctionDefenitionSyntaxNode ReadUserFunction(int start, FunCallSyntaxNode headNode, TokFlow flow, SyntaxNodeReader reader)
        {
            var id = headNode.Id;
            if (headNode.IsInBrackets)
                throw ErrorFactory.UnexpectedBracketsOnFunDefenition( headNode, start,flow.Previous.Finish);

            var arguments = new List<TypedVarDefSyntaxNode>();
            foreach (var headNodeChild in headNode.Args)
            {
                if (headNodeChild is TypedVarDefSyntaxNode varDef)
                    arguments.Add(varDef);
                else if(headNodeChild is VariableSyntaxNode varSyntax)
                    arguments.Add(new TypedVarDefSyntaxNode(varSyntax.Id, headNodeChild.OutputType, headNodeChild.Interval));
                else    
                    throw ErrorFactory.WrongFunctionArgumentDefenition(start, headNode, headNodeChild, flow.Current);
              
                if(headNodeChild.IsInBrackets)    
                    throw ErrorFactory.FunctionArgumentInBracketDefenition(start, headNode, headNodeChild, flow.Current);
            }

            var outputType = VarType.Empty;
            if (flow.MoveIf(TokType.Colon, out _))
                outputType = flow.ReadVarType();
            
            flow.SkipNewLines();
            if (!flow.MoveIf(TokType.Def, out var def))
                throw ErrorFactory.FunDefTokenIsMissed(id, arguments, flow.Current);  

            var expression =reader.ReadExpressionOrNull();
            if (expression == null)
            {

                int finish = flow.Peek?.Finish ?? flow.Position;
                    
                throw ErrorFactory.FunExpressionIsMissed(id, arguments, 
                    new Interval(def.Start, finish));
            }

            return new UserFunctionDefenitionSyntaxNode(arguments, headNode, expression, outputType ); 
        }
        private static EquationSyntaxNode ReadEquation(TokFlow flow, SyntaxNodeReader reader, string id, VarAttribute[] attributes)
        {
            flow.SkipNewLines();
            var start = flow.Position;
            var exNode = reader.ReadExpressionOrNull();
            if (exNode == null)
                throw ErrorFactory.VarExpressionIsMissed(start, id, flow.Current);
            return new EquationSyntaxNode(id,start, exNode, attributes);
        }
    }
}
