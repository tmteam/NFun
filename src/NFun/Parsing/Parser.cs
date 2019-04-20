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
                VarAttribute[] attributes = new VarAttribute[0];
                if (flow.IsCurrent(TokType.Attribute))
                    attributes = ReadAttributes(flow);
                
                var exprStart = flow.Current.Start;
                var e = reader.ReadExpressionOrNull();
                if (e == null)
                    throw ErrorFactory.UnknownValueAtStartOfExpression(exprStart, flow.Current);
                
                if(e.Is(LexNodeType.TypedVar))
                {
                    //Input typed var specification
                    varSpecifications.Add(
                        new VariableInfo(e.Value, (VarType)e.AdditionalContent, attributes));                        
                }
                else if (flow.IsCurrent(TokType.Def) || flow.IsCurrent(TokType.Colon))
                {
                    if (e.Is(LexNodeType.Var))
                    {
                        //equatation
                        flow.MoveNext();
                        equations.Add(ReadEquation(flow, reader, e.Value,attributes));
                    }
                    //Todo Make operator fun as separate node type
                    else if (e.Is(LexNodeType.Fun) && e.AdditionalContent == null)
                    {
                        if (attributes.Any())
                            throw ErrorFactory.AttributeOnFunction(exprStart, e);
                        //userFun
                        funs.Add(ReadUserFunction(exprStart, e, flow, reader));
                    }
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
                    equations.Add(new LexEquation("out", e, attributes));
                }
            }

            return new LexTree
            {
                UserFuns = funs.ToArray(),
                Equations = equations.ToArray(),
                VarSpecifications = varSpecifications.ToArray(),
            };
        }

        private static VarAttribute[] ReadAttributes(TokenFlow flow)
        {
            var ans = new List<VarAttribute>();
            while (flow.IsCurrent(TokType.Attribute))
            {
                ans.Add(ReadAttribute(flow));
                flow.SkipNewLines();
            }
            return ans.ToArray();
        }

        private static VarAttribute ReadAttribute(TokenFlow flow)
        {
            var start = flow.Current.Start;
            flow.MoveNext();
            if (!flow.MoveIf(TokType.Id, out var id))
                throw ErrorFactory.ItIsNotAnAttribute(start, flow.Current);
            object val = null;
            if (flow.MoveIf(TokType.Obr))
            {
                flow.MoveNext();
                var next = flow.Current;
                switch (next.Type)
                {
                    case TokType.False:
                    case TokType.True:
                    case TokType.Number:
                    case TokType.Text:
                        val = next.Value;
                        break;
                    default:
                        throw ErrorFactory.ItIsNotCorrectAttributeValue(next);
                }
                if(!flow.MoveIf(TokType.Cbr))                
                    throw ErrorFactory.AttributeCbrMissed(start, flow);
            }
            if(!flow.MoveIf(TokType.NewLine))
                throw ErrorFactory.NowNewLineAfterAttribute(start, flow);

            return new VarAttribute(id.Value, val);
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
        private static LexEquation ReadEquation(TokenFlow flow, LexNodeReader reader, string id, VarAttribute[] attributes)
        {
            flow.SkipNewLines();
            var start = flow.Position;
            var exNode = reader.ReadExpressionOrNull();
            if (exNode == null)
                throw ErrorFactory.VarExpressionIsMissed(start, id, flow.Current);
            return new LexEquation(id, exNode, attributes);
        }
    }
}
