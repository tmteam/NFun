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
                new AddRealFunction(),
                new AddIntFunction(),
                new AddTextFunction(), 

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
                new RangeIntFunction(),
                new RangeWithStepIntFunction(),
                new RangeWithStepRealFunction(),
                new SubstractIntFunction(), 
                new SubstractRealFunction(), 

                
            };
            var predefinedGenerics = new GenericFunctionBase[]
            {
                new AddArraysGenericFunctionDefenition(), 
                new GetGenericFunctionDefenition(),
                new SliceGenericFunctionDefenition(), 
                new SliceWithStepGenericFunctionDefenition(), 
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
                new SubstractArraysGenericFunctionDefenition(), 
            };
            return ExpressionReader.Interpritate(lexTree, predefinedfunctions, predefinedGenerics);
        }
    }
    
}