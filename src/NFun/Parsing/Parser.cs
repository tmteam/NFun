using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public static class Parser
    {
        public static LexTree Parse(TokenFlow flow)
        {
            var reader = new LexNodeReader(flow);
            var equations = new List<LexEquation>();
            var funs = new List<LexFunction>();
            var varSpecifications = new List<VariableInfo>();
            while (true)
            {
                flow.SkipNewLines();

                if (flow.IsDone || flow.IsCurrent(TokType.Eof))
                    break;
                var startOfTheString = flow.IsStart || flow.IsPrevious(TokType.NewLine);

                var exprStart = flow.Current.Start;
                var e = reader.ReadExpressionOrNull();
                if (e == null)
                    throw ErrorFactory.UnknownValueAtStartOfExpression(exprStart, flow.Current);
                if(e.Is(LexNodeType.TypedVar))
                {
                    //Input typed var specification
                    varSpecifications.Add(new VariableInfo(e.Value, (VarType)e.AdditionalContent));                        
                }
                else if (flow.IsCurrent(TokType.Def) || flow.IsCurrent(TokType.Colon))
                {
                    if (e.Is(LexNodeType.Var))
                    {
                        //equatation
                        flow.MoveNext();
                        equations.Add(ReadEquation(flow, reader, e.Value));
                    }
                    //Todo Make operator fun as separate node type
                    else if (e.Is(LexNodeType.Fun) && e.AdditionalContent == null)
                        //userFun
                        funs.Add(ReadUserFunction(exprStart,e, flow, reader));
                    else
                        throw ErrorFactory.ExpressionBeforeTheDefenition(exprStart, e, flow.Current);
                }
                else
                {

                    if (equations.Any())
                    {
                        if (startOfTheString && equations[0].Id=="out")
                            throw ErrorFactory.OnlyOneAnonymousExpressionAllowed(exprStart, e, flow.Current);
                        else
                            throw ErrorFactory.UnexpectedExpression(e);
                    }

                    if(!startOfTheString)
                        throw ErrorFactory.AnonymousExpressionHasToStartFromNewLine(exprStart, e, flow.Current);
                        
                    //anonymous
                    equations.Add(new LexEquation("out", e));
                }
            }

            return new LexTree
            {
                UserFuns = funs.ToArray(),
                Equations = equations.ToArray(),
                VarSpecifications = varSpecifications.ToArray(),
            };
        }
       
        private static LexFunction ReadUserFunction(int start, LexNode headNode, TokenFlow flow, LexNodeReader reader)
        {
            var id = headNode.Value;
            if (headNode.IsBracket)
                throw ErrorFactory.UnexpectedBracketsOnFunDefenition( headNode, start,flow.Previous.Finish);

            var arguments = new List<VariableInfo>();
            foreach (var headNodeChild in headNode.Children)
            {
                if(headNodeChild.Value==null)
                    throw ErrorFactory.WrongFunctionArgumentDefenition(start, headNode, headNodeChild, flow.Current);
                if (headNodeChild.Type != LexNodeType.Var && headNodeChild.Type != LexNodeType.TypedVar)
                    throw ErrorFactory.WrongFunctionArgumentDefenition(start, headNode, headNodeChild, flow.Current);
                if(headNodeChild.IsBracket)    
                    throw ErrorFactory.FunctionArgumentInBracketDefenition(start, headNode, headNodeChild, flow.Current);

                arguments.Add(new VariableInfo(headNodeChild.Value, (VarType)(headNodeChild.AdditionalContent ?? VarType.Real)));
            }

            VarType outputType;
            if (flow.MoveIf(TokType.Colon, out _))
                outputType = flow.ReadVarType();
            else
                outputType = VarType.Real;

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

            return new LexFunction
            {
                Args = arguments.ToArray(), 
                Head = headNode,
                Node = expression, 
                OutputType = outputType
            };
        }
        private static LexEquation ReadEquation(TokenFlow flow, LexNodeReader reader, string id)
        {
            flow.SkipNewLines();
            var start = flow.Position;
            var exNode = reader.ReadExpressionOrNull();
            if (exNode == null)
                throw ErrorFactory.VarExpressionIsMissed(start, id, flow.Current);
            return new LexEquation(id, exNode);
        }
    }
}
