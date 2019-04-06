using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Tokenization
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
            throw new FunParseException($"Expected: type, but was {tokType}");

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
        
        public static bool MoveIf(this TokenFlow flow, TokType tokType)
        {
            if (flow.IsCurrent(tokType))
            {
                flow.MoveNext();
                return true;
            }
            return false;
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
            if (cur == null)
            {
                var prev = flow.Previous;
                if (prev == null)
                    throw new FunParseException(000, $"\"{tokType}\" is missing at end of stream",
                        -1,
                        -1);
                else
                    throw new FunParseException(001, $"\"{tokType}\" is missing at end of stream",
                        prev.StartInString, prev.FinishInString);
            }

            if (!cur.Is(tokType))
                throw new FunParseException(002,
                    $"\"{tokType}\" is missing but was \"{cur}\"",
                    cur.StartInString, cur.FinishInString);
            
            flow.MoveNext();
            return cur;
        }
    }
}