using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;
using Funny.Tokenization;

namespace Funny.Parsing
{
    public static class Parser
    {
        public static LexTree Parse(TokenFlow flow)
        {
            var reader = new LexNodeReader(flow);
            var equatations = new List<LexEquatation>();
            var funs = new List<UserFunDef>();
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
                    equatations.Add(ReadEquatation(flow, reader, id));
                }
                else if (flow.IsCurrent(TokType.Obr))
                {
                    flow.MoveNext();
                    funs.Add(ReadUserFunction(flow, reader, id));

                }
                else
                    throw new ParseException("has no =");

            }


            return new LexTree
            {
                UserFuns = funs.ToArray(),
                Equatations = equatations.ToArray()
            };
        }

        private static UserFunDef ReadUserFunction(TokenFlow flow, LexNodeReader reader, string id)
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
            var expression =reader.ReadExpression();
            
            return new UserFunDef{Args = arguments.ToArray(), Id= id, Node = expression};
        }


        private static LexEquatation ReadEquatation(TokenFlow flow, LexNodeReader reader, string id)
        {
            flow.SkipNewLines();

            var exNode = reader.ReadExpression();
            return new LexEquatation(id, exNode);
        }
    }
}
