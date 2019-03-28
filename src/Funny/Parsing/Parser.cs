using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;
using Funny.Runtime;
using Funny.Tokenization;
using Funny.Types;

namespace Funny.Parsing
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

                var e = reader.ReadExpressionOrNull();
                if(e==null)
                    throw new FunParseException("Unexpected token "+ flow.Current);
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
                    else if (e.Is(LexNodeType.Fun))
                        //userFun
                       funs.Add(ReadUserFunction(e, flow, reader));
                    else
                        throw  new FunParseException("Unexpected expression "+ e);
                }
                else
                {
                    if (equations.Any())
                        throw new FunParseException("Unexpected expression " + e);
                    if(!flow.IsStart && !flow.IsPrevious(TokType.NewLine) )
                        throw new FunParseException("Anonymous expression should start from new line" );
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
       
        private static LexFunction ReadUserFunction(LexNode headNode, TokenFlow flow, LexNodeReader reader)
        {
            var id = headNode.Value;
            
            var arguments = new List<VariableInfo>();
            foreach (var headNodeChild in headNode.Children)
            {
                if(headNodeChild.Value==null)
                    throw new FunParseException("Invalid function argument");
                arguments.Add(new VariableInfo(headNodeChild.Value, (VarType)(headNodeChild.AdditionalContent ?? VarType.Real)));
            }

            VarType outputType;
            if (flow.MoveIf(TokType.Colon, out _))
                outputType = flow.ReadVarType();
            else
                outputType = VarType.Real;

            flow.SkipNewLines();
            flow.MoveIfOrThrow(TokType.Def, "\'=\' expected");
            var expression =reader.ReadExpressionOrNull();
            if(expression==null)
                throw new FunParseException("Function contains no body");
            
            return new LexFunction
            {
                Args = arguments.ToArray(), 
                Id= id, 
                Node = expression, 
                OutputType = outputType
            };
        }
        private static LexEquation ReadEquation(TokenFlow flow, LexNodeReader reader, string id)
        {
            flow.SkipNewLines();

            var exNode = reader.ReadExpressionOrNull();
            if(exNode==null)
                throw new FunParseException("Epxression is wrong");
            return new LexEquation(id, exNode);
        }
    }
}
