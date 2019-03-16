using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;
using Funny.Runtime;
using Funny.Tokenization;

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
                else if(flow.IsCurrent(TokType.IsTypeOf))
                {
                    flow.MoveNext();
                    varSpecifications.Add(ReadVarSpecification(flow, id));
                }
                else
                    throw new ParseException("Unexpected token "+ flow.Current);
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
            if (flow.MoveIf(TokType.IsTypeOf, out _))
                return ReadVarType(flow);
            else
                return VarType.RealType;
        }
        private static VariableInfo ReadVarSpecification(TokenFlow flow, string id) 
            => new VariableInfo(id, ReadVarType(flow));

        private static VarType ReadVarType(TokenFlow flow)
        {
            var cur = flow.Current;
            switch (cur.Type)
            {
                case TokType.IntType:
                    flow.MoveNext();
                    return  VarType.IntType;
                case TokType.RealType:
                    flow.MoveNext();
                    return  VarType.RealType;
                case TokType.BoolType:
                    flow.MoveNext();
                    return  VarType.BoolType;
                case TokType.TextType:
                    flow.MoveNext();
                    return  VarType.TextType;
            }
            throw new ParseException("Expected: type, but was "+ cur.Type);
        }
        
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
                throw new ParseException("Function contains no body");
            
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
                throw new ParseException("Epxression is wrong");
            return new LexEquation(id, exNode);
        }
    }
}
