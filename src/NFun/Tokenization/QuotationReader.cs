using System.Text;
using NFun.ParseErrors;

namespace NFun.Tokenization
{
    public static class QuotationReader
    {
        /// <summary>
        /// Convert escaped string until ' or  "  or { symbols
        /// </summary>
        /// <param name="rawString"></param>
        /// <param name="startPosition">open quote position</param>
        /// <returns>result: escaped string, resultPosition: index of closing quote symbol. -1 if no close quote symbol found</returns>
        public static (string result, int resultPosition) ReadQuotation(string rawString, int startPosition)
        {
            var sb = new StringBuilder();
            int lastNonEscaped = startPosition + 1;

            int i = lastNonEscaped;
            var closeQuotationPosition = 0;
            for (; i < rawString.Length; i++)
            {
                var current = rawString[i];
                if (current == '\'' || current == '"' || current == '{')
                {
                    closeQuotationPosition = i;
                    break;
                }

                if (rawString[i] != '\\')
                    continue;

                if (lastNonEscaped != i)
                {
                    var prev = rawString.Substring(lastNonEscaped, i - lastNonEscaped);
                    sb.Append(prev);
                }

                if (i == rawString.Length - 1)
                    throw ErrorFactory.BackslashAtEndOfString(i, i + 1);

                var next = rawString[i + 1];
                var symbol = next switch
                {
                    '\\' => '\\',
                    'n' => '\n',
                    'r' => '\r',
                    '\'' => '\'',
                    '"' => '"',
                    't' => '\t',
                    'f' => '\f',
                    'v' => '\v',
                    '{' => '{',
                    '}' => '}',
                    _ => throw ErrorFactory.UnknownEscapeSequence(next.ToString(), i, i + 2)
                };
                sb.Append(symbol);
                i++;
                lastNonEscaped = i + 1;
            }

            if (closeQuotationPosition == 0)
                return ("", -1);

            if (lastNonEscaped == startPosition + 1)
                return (rawString.Substring(startPosition + 1, i - startPosition - 1), i);

            if (lastNonEscaped <= rawString.Length - 1)
            {
                var prev = rawString.Substring(lastNonEscaped, i - lastNonEscaped);
                sb.Append(prev);
            }

            return (sb.ToString(), closeQuotationPosition);
        }
    }
}