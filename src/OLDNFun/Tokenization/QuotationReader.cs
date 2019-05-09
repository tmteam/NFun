using System;
using System.Text;
using NFun.ParseErrors;

namespace NFun.Tokenization
{
    public static class QuotationReader
    {
        public static (string result, int resultPosition) ReadQuotation(string rawString, int position)
        {
            var quoteSymbol = rawString[position];
            if(quoteSymbol!= '\'' && quoteSymbol!= '\"')
                throw new InvalidOperationException("Current symbol is not \"open-quote\" symbol");
            if (position == rawString.Length - 1)
                throw ErrorFactory.QuoteAtEndOfString(quoteSymbol, position, position + 1);
            
            StringBuilder sb = new StringBuilder();
            int lastNonEscaped = position+1;

            int i = lastNonEscaped;
            int closeQuotationPosition = 0;
            for (; i < rawString.Length; i++)
            {
                if (rawString[i] == quoteSymbol)
                {
                    closeQuotationPosition = i;
                    break;
                }

                if(rawString[i]!= '\\')
                    continue;
                
                if (lastNonEscaped != i) {
                    var prev = rawString.Substring(lastNonEscaped, i - lastNonEscaped);
                    sb.Append(prev);
                }

                if (i == rawString.Length - 1)
                    throw ErrorFactory.BackslashAtEndOfString(i, i + 1);
                
                var next = rawString[i + 1];
                char symbol;
                switch (next)
                {
                    case '\\': symbol = '\\'; break;
                    case 'n':  symbol = '\n'; break;
                    case 'r':  symbol = '\r'; break;
                    case '\'': symbol = '\''; break;
                    case '"': symbol = '"'; break;
                    case 't': symbol = '\t'; break;
                    case 'f': symbol = '\f'; break;
                    case 'v': symbol = '\v'; break;
                    case '{': symbol = '{'; break;
                    case '}': symbol = '}'; break;
                    default:
                        throw ErrorFactory.UnknownEscapeSequence(next.ToString(), i, i+2); 
                }
                sb.Append(symbol);
                i++;
                lastNonEscaped = i+1;
            }

            if (closeQuotationPosition == 0)
                throw ErrorFactory.ClosingQuoteIsMissed(quoteSymbol, position + 1, i);
            
            if (lastNonEscaped == position+1)
                return (rawString.Substring(position + 1, i - position-1), i + 1);
            
            if (lastNonEscaped <= rawString.Length-1)
            {
                var prev = rawString.Substring(lastNonEscaped,i - lastNonEscaped);
                sb.Append(prev);
            }
            return (sb.ToString(), i+1);
        }
    }
}