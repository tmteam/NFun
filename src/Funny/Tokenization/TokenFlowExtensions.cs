using Funny.Types;

namespace Funny.Tokenization
{
    public static class TokenFlowExtensions
    {
        private static VarType ToVarType(this Tok tokType)
        {
            switch (tokType.Type)
            {
                case TokType.IntType:
                    return  VarType.Int;
                case TokType.RealType:
                    return  VarType.Real;
                case TokType.BoolType:
                    return  VarType.Bool;
                case TokType.TextType:
                    return  VarType.Text;
                case TokType.AnythingType:
                    return  VarType.Anything;
            }
            throw new ParseException($"Expected: type, but was {tokType}");

        }
        public static VarType ReadVarType(this TokenFlow flow)
        {
            var cur = flow.Current;
            var readType = ToVarType(cur);
            flow.MoveNext();

            while (flow.IsCurrent(TokType.ArrOBr))
            {
                flow.MoveNext();
                flow.MoveIfOrThrow(TokType.ArrCBr);
                readType = VarType.ArrayOf(readType);
            }
            return readType;
        }
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