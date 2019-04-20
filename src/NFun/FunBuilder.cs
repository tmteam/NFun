using System.Collections.Generic;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun
{
    public  class FunBuilder
    {
        private readonly string _text;

        public static FunBuilder With(string text) => new FunBuilder(text);

        private FunBuilder(string text)
        {
            _text = text;
        }

        readonly List<FunctionBase> _functions = new List<FunctionBase>();
        readonly List<GenericFunctionBase> _genericFunctions= new List<GenericFunctionBase>();
        public FunBuilder WithFunctions(params FunctionBase[] functions)
        {
            _functions.AddRange(functions);
            return this;
        }
        public FunBuilder WithFunctions(params GenericFunctionBase[] functions)
        {
            _genericFunctions.AddRange(functions);
            return this;
        }

        public FunRuntime Build()
        {
            var flow = Tokenizer.ToFlow(_text);
            var lexTree =    Parser.Parse(flow);
             
            return ExpressionReader.Interpritate(
                lexTree, 
                _functions.Concat(FunBuilder.PredefinedFunctions), 
                _genericFunctions.Concat(FunBuilder.predefinedGenerics));
        }
        public static IEnumerable<FunctionBase> PredefinedFunctions => _predefinedFunctions;
        public static IEnumerable<GenericFunctionBase> PredefinedGenericFunctions => predefinedGenerics;

        internal static readonly GenericFunctionBase[] predefinedGenerics =
        {
            new IsInSingleGenericFunctionDefenition(), 
            new IsInMultipleGenericFunctionDefenition(), 
            new ReiterateGenericFunctionDefenition(),
            new UniqueGenericFunctionDefenition(), 
            new UniteGenericFunctionDefenition(), 
            new IntersectGenericFunctionDefenition(), 
            new SubstractArraysGenericFunctionDefenition(), 
            
            new ConcatArraysGenericFunctionDefenition(CoreFunNames.ArrConcat), 
            new ConcatArraysGenericFunctionDefenition("concat"), 

            new SetGenericFunctionDefenition(),
            new GetGenericFunctionDefenition(),
            new SliceGenericFunctionDefenition(), 
            new SliceWithStepGenericFunctionDefenition(), 
            new FindGenericFunctionDefenition(), 
            new ReduceWithDefaultsGenericFunctionDefenition(),
            new ReduceGenericFunctionDefenition(),
            new TakeGenericFunctionDefenition(),
            new SkipGenericFunctionDefenition(),
            new RepeatGenericFunctionDefenition(),
            new FilterGenericFunctionDefenition(),
            new FlatGenericFunctionDefenition(),
            new ChunkGenericFunctionDefenition(),
            new MapGenericFunctionDefenition(),
            new AllGenericFunctionDefenition(), 
            new AnyGenericFunctionDefenition(), 
            new ReverseGenericFunctionDefenition(),
        };
        internal static readonly FunctionBase[] _predefinedFunctions = 
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
                
                new AddRealFunction(CoreFunNames.Add),
                new AddIntFunction(CoreFunNames.Add),
                new AddTextFunction(CoreFunNames.Add),
                
                new AddRealFunction("sum"),
                new AddIntFunction("sum"),
                new AddInt64Function("sum"), 
                new AddTextFunction("concat"),

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
                new TanFunction(),
                new AtanFunction(),
                new Atan2Function(),
                new AsinFunction(), 
                new AcosFunction(), 
                new ExpFunction(), 
                new LogFunction(), 
                new LogEFunction(), 
                new Log10Function(), 
                new FloorFunction(), 
                new CeilFunction(), 
                new RoundToIntFunction(), 
                new RoundToRealFunction(), 
                new SignFunction(),
                new ToTextFunction(), 
                new ToIntFromRealFunction(), 
                new ToIntFromTextFunction(), 
                new ToIntFromBytesFunction(),
                new ToRealFromIntFunction(), 
                new ToRealFromTextFunction(), 
                new ToUtf8Function(), 
                new ToUnicodeFunction(), 
                new ToBytesFromIntFunction(), 
                new ToBitsFromIntFunction(), 

                new EFunction(), 
                new PiFunction(),
                new CountFunction(),
                new AverageFunction(),
                new MaxOfIntFunction(), 
                new MaxOfInt64Function(),
                new MaxOfRealFunction(), 
                new MinOfIntFunction(), 
                new MinOfInt64Function(),
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
        public static FunRuntime BuildDefault(string text)
            => FunBuilder.With(text).Build();
    }
}