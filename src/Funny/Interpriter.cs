using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Take2;

namespace Funny
{
    public class Interpriter
    {
        public static Runtime BuildOrThrow(string text)
        {
            var tokens = Tokenizer.ToTokens(text);
            var flow = new TokenFlow(tokens);
            var eq = new Parser(flow).Parse();
            return ExpressionReader.Interpritate(eq);
        }

    }
}