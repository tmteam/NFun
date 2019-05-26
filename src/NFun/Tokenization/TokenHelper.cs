using System;
using System.Globalization;
using System.Linq;
using NFun.ParseErrors;
using NFun.Types;

namespace NFun.Tokenization
{
    
    public static class TokenHelper
    {
        /// <exception cref="SystemException">Throws if string contains invalid format</exception>
        public static (object, VarType) ToConstant(string val)
        {
            val = val.Replace("_", null);

            if (val.Contains('.'))
            {
                if (val.EndsWith("."))
                    throw new FormatException();
                //todo надо написать тесты на разные культуры
                return (double.Parse(val, CultureInfo.InvariantCulture), VarType.Real);
            }

            var longVal = ParseLongValue(val);
            if (longVal > Int32.MaxValue || longVal < Int32.MinValue)
                return (longVal, VarType.Int64);
            return ((int) longVal, VarType.Int32);
        }

        private static long ParseLongValue(string val)
        {
            if (val.Length > 2)
            {

                if (val[1] == 'b')
                {
                    var uval = Convert.ToUInt64(val.Substring(2), 2);
                    if(uval> long.MaxValue)
                        throw new OverflowException();
                    return (long)uval;
                }
                else if (val[1] == 'x')
                {
                    var uval =  Convert.ToUInt64(val, 16);
                    if(uval> long.MaxValue)
                        throw new OverflowException();
                    return (long)uval;
                }
            }
            return long.Parse(val);
        }

        private static VarType ToVarType(this Tok token)
        {
            switch (token.Type)
            {
                case TokType.Int32Type:
                    return  VarType.Int32;
                case TokType.Int64Type:
                    return VarType.Int64;
                case TokType.RealType:
                    return  VarType.Real;
                case TokType.BoolType:
                    return  VarType.Bool;
                case TokType.TextType:
                    return  VarType.Text;
                case TokType.AnythingType:
                    return  VarType.Anything;
            }
            throw ErrorFactory.TypeExpectedButWas(token);
        }
        public static VarType ReadVarType(this TokFlow flow)
        {
            var cur = flow.Current;
            var readType = ToVarType(cur);
            flow.MoveNext();

            while (flow.IsCurrent(TokType.ArrOBr))
            {
                flow.MoveNext();
                if (!flow.MoveIf(TokType.ArrCBr))
                    throw ErrorFactory.ArrTypeCbrMissed(new Interval(cur.Start, flow.Current.Start));
                readType = VarType.ArrayOf(readType);
            }
            return readType;
        }
        
        public static bool MoveIf(this TokFlow flow, TokType tokType)
        {
            if (flow.IsCurrent(tokType))
            {
                flow.MoveNext();
                return true;
            }
            return false;
        }
        public static bool MoveIf(this TokFlow flow, TokType tokType, out Tok tok)
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
        
        public static Tok MoveIfOrThrow(this TokFlow flow, TokType tokType)
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
                        prev.Start, prev.Finish);
            }

            if (!cur.Is(tokType))
                throw new FunParseException(002,
                    $"\"{tokType}\" is missing but was \"{cur}\"",
                    cur.Start, cur.Finish);
            
            flow.MoveNext();
            return cur;
        }
    }
}