using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;
using Funny.Runtime;
using Funny.Tokenization;

namespace Funny.Parsing
{
    public class VariableTypeSpecification
    {
        public readonly string Id;
        public readonly VarType Type;

        public VariableTypeSpecification(string id, VarType type)
        {
            Id = id;
            Type = type;
        }
    }
    
    public static class Parser
    {
        public static LexTree Parse(TokenFlow flow)
        {
            var reader = new LexNodeReader(flow);
            var equations = new List<LexEquation>();
            var funs = new List<LexFunction>();
            var varSpecifications = new List<VariableTypeSpecification>();
            while (true)
            {
                flow.SkipNewLines();

                if (flow.IsDone || flow.IsCurrent(TokType.Eof))
                    break;

                var id = reader.MoveIfOrThrow(TokType.Id).Value;
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
                else if(flow.IsCurrent(TokType.Is))
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

        private static VariableTypeSpecification ReadVarSpecification(TokenFlow flow, string id)
        {
            var cur = flow.Current;
            switch (cur.Type)
            {
                case TokType.IntType:
                    flow.MoveNext();
                    return new VariableTypeSpecification(id, VarType.IntType);
                case TokType.RealType:
                    flow.MoveNext();
                    return new VariableTypeSpecification(id, VarType.RealType);
                case TokType.BoolType:
                    flow.MoveNext();
                    return new VariableTypeSpecification(id, VarType.BoolType);
                case TokType.TextType:
                    flow.MoveNext();
                    return new VariableTypeSpecification(id, VarType.TextType);
                
            }
            throw new ParseException("Expected: type, but was "+ cur.Type);
        }

        private static LexFunction ReadUserFunction(TokenFlow flow, LexNodeReader reader, string id)
        {
            var arguments = new List<string>();
            while (true)
            {
                if (reader.MoveIf(TokType.Cbr, out _))
                    break;
                if (arguments.Any())
                    reader.MoveIfOrThrow(TokType.Sep, "\",\" or \")\" expected");
                var argId = reader.MoveIfOrThrow(TokType.Id, "Argument name expected");
                arguments.Add(argId.Value);
            }
            flow.SkipNewLines();
            reader.MoveIfOrThrow(TokType.Def, "\'=\' expected");
            var expression =reader.ReadExpressionOrNull();
            if(expression==null)
                throw new ParseException("Function contains no body");
            return new LexFunction{Args = arguments.ToArray(), Id= id, Node = expression};
        }


        private static LexEquation ReadEquation(TokenFlow flow, LexNodeReader reader, string id)
        {
            flow.SkipNewLines();

            var exNode = reader.ReadExpressionOrNull();
            return new LexEquation(id, exNode);
        }
    }
}
