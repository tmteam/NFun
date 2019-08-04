using System.Collections.Generic;
using NFun.ParseErrors;
using NFun.SyntaxParsing;

namespace NFun.Tokenization
{
    public static class TokenFlowExtensions
    {
        public static bool IsDoneOrEof(this TokFlow flow)
            => flow.IsDone || flow.IsCurrent(TokType.Eof);
        public static bool IsStartOfTheLine(this TokFlow flow) 
            => flow.IsStart || flow.IsPrevious(TokType.NewLine);

        public static VarAttribute[] ReadAttributes(this TokFlow flow)
        {
            var attributes = new VarAttribute[0];
            if (!flow.IsCurrent(TokType.Attribute))
                return attributes;

            bool newLine = flow.IsStart || flow.Previous.Is(TokType.NewLine);
            var ans = new List<VarAttribute>();
            while (flow.IsCurrent(TokType.Attribute))
            {
                if (!newLine)
                    throw ErrorFactory.NowNewLineBeforeAttribute(flow);

                ans.Add(ReadAttributeOrThrow(flow));
                flow.SkipNewLines();
            }
            return ans.ToArray();
        }
        public static VarAttribute ReadAttributeOrThrow(this TokFlow flow)
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
    }
}