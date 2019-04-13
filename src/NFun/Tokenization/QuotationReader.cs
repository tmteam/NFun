using System;
using System.Text;

namespace NFun.Tokenization
{
    public static class QuotationReader
    {
        public static (string result, Interval error) TryReplaceEscaped(string rawString)
        {
            StringBuilder sb = new StringBuilder();
            int lastNonEscaped = 0;
            
            for (int i = 0; i < rawString.Length; i++)
            {
                if(rawString[i]!= '\\')
                    continue;
                
                if (lastNonEscaped != i) {
                    var prev = rawString.Substring(lastNonEscaped, i - lastNonEscaped);
                    sb.Append(prev);
                }

                if (i == rawString.Length - 1)
                    return (null, new Interval(i, i+1));
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
                         
                    default: 
                        return (null, new Interval(i, i+2));
                }
                sb.Append(symbol);
                i++;
                lastNonEscaped = i+1;
            }
            if (lastNonEscaped == 0)
                return (rawString, Interval.Empty);
            
            if (lastNonEscaped <= rawString.Length-1)
            {
                var prev = rawString.Substring(lastNonEscaped);
                sb.Append(prev);
            }
            return (sb.ToString(), Interval.Empty);
        }
        
        
        public static string ReplaceEscaped(string rawString)
        {
            StringBuilder sb = new StringBuilder();
            int lastNonEscaped = 0;
            
            for (int i = 0; i < rawString.Length; i++)
            {
                if(rawString[i]!= '\\')
                    continue;
                
                if (lastNonEscaped != i) {
                    var prev = rawString.Substring(lastNonEscaped, i - lastNonEscaped);
                    sb.Append(prev);
                }

                if(i == rawString.Length-1)
                    throw  new ArgumentException("unone escaped sequence");
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
                         
                    default: throw  new ArgumentException("not supported escaped sequence");
                }
                sb.Append(symbol);
                i++;
                lastNonEscaped = i+1;
            }
            if (lastNonEscaped == 0)
                return rawString;
            
            if (lastNonEscaped <= rawString.Length-1)
            {
                var prev = rawString.Substring(lastNonEscaped);
                sb.Append(prev);
            }
            return sb.ToString();
        }
    }
}