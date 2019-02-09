using Funny.BuiltInFunctions;
using Funny.Interpritation;
using Funny.Parsing;
using Funny.Tokenization;

namespace Funny
{
    public class Interpreter
    {
        public static Runtime.FunRuntime BuildOrThrow(string text)
        {
            var flow = Tokenizer.ToFlow(text);
            var lexTree =    Parser.Parse(flow);
            var predefinedfunctions = new FunctionBase[]
            {
                new AbsFunction(),
                new AddFunction(),
                new SinFunction(), 
                new CosFunction(), 
                new EFunction(), 
                new PiFunction(), 
            };
            return ExpressionReader.Interpritate(lexTree, predefinedfunctions);
        }
    }
    
}