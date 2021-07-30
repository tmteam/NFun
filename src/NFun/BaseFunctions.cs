using NFun.Functions;
using NFun.Interpretation;
using NFun.Interpretation.Functions;

namespace NFun
{
    public static class BaseFunctions
    {
        internal static readonly ImmutableFunctionDictionary DefaultDictionary;

        public static IFunctionDictionary DefaultFunctions => DefaultDictionary;

        private static GenericFunctionBase[] GenericFunctions { get; }

        private static IConcreteFunction[] ConcreteFunctions { get; }

        static BaseFunctions()
        {
            GenericFunctions = new GenericFunctionBase[] {
                new ConvertFunction(),

                new EqualFunction(),
                new NotEqualFunction(),
                new MoreFunction(),
                new MoreOrEqualFunction(),
                new LessFunction(),
                new LessOrEqualFunction(),
                new MinFunction(),
                new MaxFunction(),

                new BitOrFunction(),
                new BitAndFunction(),
                new BitXorFunction(),
                new BitInverseFunction(),
                new BitShiftLeftFunction(),
                new BitShiftRightFunction(),

                new InvertFunction(),
                new AbsFunction(),
                new RemainderFunction(),
                new AddFunction(),
                new SubstractFunction(),
                new MultiplyFunction(),
                new IsInSingleGenericFunctionDefinition(),
                new UniqueGenericFunctionDefinition(),
                new UniteGenericFunctionDefinition(),
                new IntersectGenericFunctionDefinition(),
                new SubstractArraysGenericFunctionDefinition(),

                new MedianFunction(),
                new SortFunction(),
                new MinElementFunction(),
                new MaxElementFunction(),

                new RangeFunction(),
                new RangeStepFunction(),
                new MultiSumFunction(),
                new ConcatArraysGenericFunctionDefinition(),
                new AppendGenericFunctionDefinition(),
                new SetGenericFunctionDefinition(),
                new GetGenericFunctionDefinition(),
                new SliceGenericFunctionDefinition(),
                new SliceWithStepGenericFunctionDefinition(),
                new FindGenericFunctionDefinition(),
                new FoldWithDefaultsGenericFunctionDefinition(),
                new FoldGenericFunctionDefinition(),
                new TakeGenericFunctionDefinition(),
                new SkipGenericFunctionDefinition(),
                new RepeatGenericFunctionDefinition(),
                new FilterGenericFunctionDefinition(),
                new FlatGenericFunctionDefinition(),
                new ChunkGenericFunctionDefinition(),
                new MapFunction(),
                new LastFunction(),
                new FirstFunction(),
                new CountFunction(),
                new CountOfGenericFunctionDefinition(),
                new AllGenericFunctionDefinition(),
                new HasAnyGenericFunctionDefinition(),
                new AnyGenericFunctionDefinition(),
                new ReverseGenericFunctionDefinition()
            };
            ConcreteFunctions = new IConcreteFunction[] {
                new NotFunction(),
                new AndFunction(),
                new OrFunction(),
                new XorFunction(),

                //new BitShiftLeftInt32Function(),
                //new BitShiftLeftInt64Function(),
                //new BitShiftRightInt32Function(),
                //new BitShiftRightInt64Function(),

                new PowRealFunction(),


                new DivideRealFunction(),
                new SqrtFunction(),

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
                new ToTextFunction(),


                new RoundToRealFunction(),

                //We need these function to allow user convert numbers.
                //a = toInt16(b) #no matter what is b - it will be casted

                //Safe converters
                //new ToInt16FromInt16Function(),
                //new ToInt32FromInt32Function(),
                //new ToInt32FromInt32Function("toInt"),
                //new ToInt64FromInt64Function(),
                //new ToUint16FromUint16Function(),
                //new ToUint32FromUint32Function(),
                //new ToUint64FromUint64Function(),
                //new ToRealFromRealFunction(),

                //Unsafe converters
                //new ToInt16FromInt64Function(),
                //new ToInt32FromInt64Function(),
                //new ToInt32FromInt64Function("toInt"),
                //new ToInt64FromInt64Function(),
                //new ToInt16FromUInt64Function(),
                //new ToInt32FromUInt64Function(),
                //new ToInt32FromUInt64Function("toInt"),
                //new ToInt64FromUInt64Function(),
                //new ToUint8FromInt64Function("toByte"),
                //new ToUint8FromInt64Function(),
                //new ToUint16FromInt64Function(),
                //new ToUint32FromInt64Function(),
                //new ToUint64FromInt64Function(),
                //new ToUint8FromUint64Function("toByte"),
                //new ToUint8FromUint64Function(),
                //new ToUint16FromUint64Function(),
                //new ToUint32FromUint64Function(),
                //new ToUint64FromUint64Function(),

                //new EFunction(),
                //new PiFunction(),
                new AverageFunction(),
                //new SortTextFunction(),
                new TrimFunction(),
                new TrimStartFunction(),
                new TrimEndFunction(),
                new SplitFunction(),
                new JoinFunction(),

                //Interpolation functions:
                new ConcatArrayOfTextsFunction(),
                new Concat2TextsFunction(),
                new Concat3TextsFunction()
            };
            DefaultDictionary = new ImmutableFunctionDictionary(ConcreteFunctions, GenericFunctions);
        }
    }
}