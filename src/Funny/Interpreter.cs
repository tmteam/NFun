using Funny.BuiltInFunctions;
using Funny.Interpritation;
using Funny.Interpritation.Functions;
using Funny.Parsing;
using Funny.Tokenization;

namespace Funny
{
    public static class Interpreter
    {
        public static Runtime.FunRuntime BuildOrThrow(string text)
        {
            var flow = Tokenizer.ToFlow(text);
            var lexTree =    Parser.Parse(flow);
            var predefinedfunctions = new FunctionBase[]
            {
                new AbsOfRealFunction(),
                new AbsOfIntFunction(),
                new AddFunction(),
                new SinFunction(), 
                new CosFunction(), 
                new EFunction(), 
                new PiFunction(),
                new CountFunction(),
                new AverageFunction(),
                new MaxOfIntFunction(), 
                new MaxOfRealFunction(), 
                new MinOfIntFunction(), 
                new MinOfRealFunction(), 
                new MultiMaxIntFunction(), 
                new MultiMaxRealFunction(),
                new MultiMinIntFunction(), 
                new MultiMinRealFunction(),
                new MultiSumIntFunction(), 
                new MultiSumRealFunction(), 
                new MedianIntFunction(), 
                new MedianRealFunction(),
                new AnyFunction(), 
                new SortIntFunction(), 
                new SortRealFunction(), 
                new SortTextFunction(), 
            };
            var predefinedGenerics = new GenericFunctionBase[]
            {
                new FoldGenericFunctionDefenition(),
                new TakeGenericFunctionDefenition(),
                new SkipGenericFunctionDefenition(),
                new ConcatGenericFunctionDefenition(),
                new RepeatGenericFunctionDefenition(),
                new FilterGenericFunctionDefenition(),
                new MapGenericFunctionDefenition(),
                new AllGenericFunctionDefenition(), 
                new AnyGenericFunctionDefenition(), 
                new ReverseGenericFunctionDefenition(),
            };
            return ExpressionReader.Interpritate(lexTree, predefinedfunctions, predefinedGenerics);
        }
    }
    
}