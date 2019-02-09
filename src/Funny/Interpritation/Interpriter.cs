using Funny.BuiltInFunctions;
using Funny.Parsing;
using Funny.Tokenization;

namespace Funny.Interpritation
{
    public class Interpriter
    {
        public static Runtime.Runtime BuildOrThrow(string text)
        {
            var tokens = Tokenizer.ToTokens(text);
            var flow = new TokenFlow(tokens);
            var eq =    Parser.Parse(flow);
            var functions = new FunctionBase[]
            {
                new AbsFunction(),
                new AddFunction(),
                new SinFunction(), 
                new CosFunction(), 
                new EFunction(), 
                new PiFunction(), 
            };
            return ExpressionReader.Interpritate(eq.Equatations, functions);
        }
    }
}