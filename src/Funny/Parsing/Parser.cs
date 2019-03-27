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

                var id = flow.MoveIfOrThrow(TokType.Id).Value;
                flow.SkipNewLines();
                
                if (flow.IsCurrent(TokType.Def))
                {
                    flow.MoveNext();
                    equations.Add(ReadEquation(flow, reader, id));
                }
                else if (flow.IsCurrent(TokType.Obr))
                {
                    flow.MoveNext();
                    funs.Add(ReadUserFunction(flow, reader, id));
                }
                else if(flow.IsCurrent(TokType.Colon))
                {
                    flow.MoveNext();
                    varSpecifications.Add(ReadVarSpecification(flow, id));
                }
                else
                    throw new FunParseException("Unexpected token "+ flow.Current);
            }

            return new LexTree
            {
                UserFuns = funs.ToArray(),
                Equations = equations.ToArray(),
                VarSpecifications = varSpecifications.ToArray(),
            };
        }

        private static VarType ReadType(TokenFlow flow)
        {
            if (flow.MoveIf(TokType.Colon, out _))
                return flow.ReadVarType();
            else
                return VarType.Real;
        }
        
        private static VariableInfo ReadVarSpecification(TokenFlow flow, string id) 
            => new VariableInfo(id, flow.ReadVarType());

        private static LexFunction ReadUserFunction(TokenFlow flow, LexNodeReader reader, string id)
        {
            var arguments = new List<VariableInfo>();
            while (true)
            {
                if (flow.MoveIf(TokType.Cbr, out _))
                    break;
                
                if (arguments.Any())
                    flow.MoveIfOrThrow(TokType.Sep, "\",\" or \")\" expected");
                var argId = flow.MoveIfOrThrow(TokType.Id, "Argument name expected");

                var type =  ReadType(flow);
                arguments.Add(new VariableInfo(argId.Value, type));
            }

            var outputType = ReadType(flow);
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
