namespace Funny.Tokenization
{
    public static class TokenFlowExtensions
    {
        public static bool MoveIf(this TokenFlow flow, TokType tokType, out Tok tok)
        {
            if (flow.IsCurrent(tokType))
            {
                tok = flow.Current;
                flow.MoveNext();
                return true;
            }

            tok = null;
            return false;
        }

        public static Tok MoveIfOrThrow(this TokenFlow flow, TokType tokType)
        {
            var cur = flow.Current;
            if(cur==null)
                throw new ParseException($"\"{tokType}\" is missing");

            if (!cur.Is(tokType))
                throw new ParseException($"\"{tokType}\" is missing but was \"{flow.Current}\"");
            
            flow.MoveNext();
            return cur;
        }

        public static Tok MoveIfOrThrow(this TokenFlow flow, TokType tokType, string error)
        {
            var cur = flow.Current;
            if (cur?.Is(tokType)!= true)
                throw new ParseException(error);
            flow.MoveNext();
            return cur;
        }

    }
}