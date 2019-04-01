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
                new InvertFunction(), 
                new AndFunction(), 
                new OrFunction(), 
                new XorFunction(), 
                new EqualFunction(), 
                new NotEqualFunction(), 
                new LessIntFunction(), 
                new LessRealFunction(), 
                new LessOrEqualIntFunction(), 
                new LessOrEqualRealFunction(), 
                new MoreIntFunction(), 
                new MoreRealFunction(), 
                new MoreOrEqualIntFunction(), 
                new MoreOrEqualRealFunction(), 
                new BitShiftLeftFunction(), 
                new BitShiftRightFunction(), 
                new AbsOfRealFunction(),
                new AbsOfIntFunction(),
                new AddRealFunction(),
                new AddIntFunction(),
                new AddTextFunction(),
                new SubstractIntFunction(), 
                new SubstractRealFunction(), 
                new BitAndIntFunction(),
                new BitOrIntFunction(),
                new BitXorIntFunction(),
                new BitInverseIntFunction(), 
                new PowRealFunction(), 
                new MultiplyIntFunction(), 
                new MultiplyRealFunction(), 
                new DivideRealFunction(), 
                new RemainderRealFunction(), 
                new RemainderIntFunction(), 
                    
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
            };
            var predefinedGenerics = new GenericFunctionBase[]
            {
                new IsInSingleGenericFunctionDefenition(), 
                new IsInMultipleGenericFunctionDefenition(), 
                new ReiterateGenericFunctionDefenition(),
                new UniqueGenericFunctionDefenition(), 
                new UniteGenericFunctionDefenition(), 
                new IntersectGenericFunctionDefenition(), 
                new SubstractArraysGenericFunctionDefenition(), 

                new ConcatArraysGenericFunctionDefenition(), 
                new SetGenericFunctionDefenition(),
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
            };
            return ExpressionReader.Interpritate(lexTree, predefinedfunctions, predefinedGenerics);
        }
    }
    
}