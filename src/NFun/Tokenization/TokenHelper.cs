using System;
using System.Globalization;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.Types;

namespace NFun.Tokenization
{
    
    public static class TokenHelper
    {
        /// <exception cref="SystemException">Throws if string contains invalid format</exception>
        public static (object, FunnyType) ToConstant(string val)
        {
            val = val.Replace("_", null);

            if (val.Contains('.'))
            {
                if (val.EndsWith("."))
                    throw new FormatException();
                return (double.Parse(val, CultureInfo.InvariantCulture), FunnyType.Real);
            }

            var longVal = ParseLongValue(val);

        
            if( longVal < Int32.MinValue)
                return ( longVal, FunnyType.Int64);
            else if (longVal > Int32.MaxValue)
                return (longVal, FunnyType.Int64);
            else 
                return (longVal, FunnyType.Int32);
        }

        private static long ParseLongValue(string val)
        {
            if (val.Length > 2)
            {
                if (val[1] == 'b')
                {
                    var uval = Convert.ToUInt64(val[2..], 2);
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

        private static FunnyType ToFunnyType(this Tok token)
        {
            switch (token.Type)
            {
                case TokType.Int16Type:  return FunnyType.Int16;
                case TokType.Int32Type:  return FunnyType.Int32;
                case TokType.Int64Type:  return FunnyType.Int64;
                case TokType.UInt8Type:  return FunnyType.UInt8;
                case TokType.UInt16Type: return FunnyType.UInt16;
                case TokType.UInt32Type: return FunnyType.UInt32;
                case TokType.UInt64Type: return FunnyType.UInt64;
                case TokType.RealType:   return FunnyType.Real;
                case TokType.BoolType:   return FunnyType.Bool;
                case TokType.TextType:   return FunnyType.Text;
                case TokType.AnythingType:  return FunnyType.Any;
                case TokType.Id:
                    if(token.Value=="any") return FunnyType.Any;
                    break;     
            }
            throw ErrorFactory.TypeExpectedButWas(token);

        }

        public static FunnyType ReadType(this TokFlow flow)
        {
            var cur = flow.Current;
            var readType = ToFunnyType(cur);
            
            flow.MoveNext();
            var lastPosition = cur.Finish;
            
            while (flow.IsCurrent(TokType.ArrOBr))
            {
                if (flow.Current.Start != lastPosition) 
                    throw FunParseException.ErrorStubToDo("unexpected space before []");
                
                flow.MoveNext();
                lastPosition = flow.Current.Finish;
                if (!flow.MoveIf(TokType.ArrCBr))
                    throw ErrorFactory.ArrTypeCbrMissed(new Interval(cur.Start, flow.Current.Start));
                readType = FunnyType.ArrayOf(readType);
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
                    throw FunParseException.ErrorStubToDo($"\"{tokType}\" is missing at end of stream");
                else
                    throw FunParseException.ErrorStubToDo($"\"{tokType}\" is missing at end of stream");
            }

            if (!cur.Is(tokType))
                throw FunParseException.ErrorStubToDo(
                    $"\"{tokType}\" is missing but was \"{cur}\"");
            
            flow.MoveNext();
            return cur;
        }
    }
}