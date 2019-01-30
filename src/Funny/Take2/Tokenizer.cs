using System.Collections.Generic;

namespace Funny.Take2
{
    class Tokenizer
    {
        public static IEnumerable<Tok> ToTokens(string input)
        {
            var reader = new TokenReader();
            for (int i = 0; i<input.Length; )
            {
                var res = reader.TryReadNext(input, i);
                yield return res;
                if (res.Is(TokType.Eof)) 
                    yield break;
                i = res.Finish;
            }
        }
    }
}