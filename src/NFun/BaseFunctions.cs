using System.Security.Cryptography;
using NFun.BuiltInFunctions;
using NFun.Interpritation;
using NFun.Interpritation.Functions;

namespace NFun
{
    public static class BaseFunctions
    {
        public static FunctionDictionary GetDefaultDictionary()
        {
            var functionsDictionary = new FunctionDictionary();
            foreach (var predefinedFunction in ConcreteFunctions)
                functionsDictionary.Add(predefinedFunction);
            foreach (var genericFunctionBase in GenericFunctions)
                functionsDictionary.Add(genericFunctionBase);
            return functionsDictionary;
        }
        public static GenericFunctionBase[] GenericFunctions { get; } =
        {
            new GetValOrDefault(),
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
            new IsInSingleGenericFunctionDefenition(), 
            //new IsInMultipleGenericFunctionDefenition(), 
            new UniqueGenericFunctionDefenition(),
            new UniteGenericFunctionDefenition(),
            new IntersectGenericFunctionDefenition(),
            new SubstractArraysGenericFunctionDefenition(),

            new MedianFunction(),
            new SortFunction(),
            new MinElementFunction(),
            new MaxElementFunction(),

            new RangeFunction(),
            new RangeStepFunction(),
            new MultiSumFunction(),
            // new ConcatArraysGenericFunctionDefenition(CoreFunNames.ArrConcat), 
            new ConcatArraysGenericFunctionDefenition(),
            new AppendGenericFunctionDefenition(),
            new SetGenericFunctionDefenition(),
            new GetGenericFunctionDefenition(),
            new SliceGenericFunctionDefenition(),
            new SliceWithStepGenericFunctionDefenition(),
            new FindGenericFunctionDefenition(),
            new foldWithDefaultsGenericFunctionDefenition(),
            new FoldGenericFunctionDefenition(),
            new TakeGenericFunctionDefenition(),
            new SkipGenericFunctionDefenition(),
            new RepeatGenericFunctionDefenition(),
            new FilterGenericFunctionDefenition(),
            new FlatGenericFunctionDefenition(),
            new ChunkGenericFunctionDefenition(),
            new MapFunction(),
            new LastFunction(),
            new FirstFunction(),
            new CountFunction(),
            new CountOfGenericFunctionDefenition(),
            new AllGenericFunctionDefenition(),
            new HasAnyGenericFunctionDefenition(),
            new AnyGenericFunctionDefenition(),
            new ReverseGenericFunctionDefenition(),
        };

        public static FunctionBase[] ConcreteFunctions { get; } =
        {
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

            //new FloorFunction(),
            //new CeilFunction(),
            //new RoundToIntFunction(),
            //new RoundToRealFunction(),
            //new SignFunction(),

            //new ToIntFromRealFunction("toInt"),
            //new ToIntFromRealFunction(),
            //new ToIntFromTextFunction("toInt"),
            //new ToIntFromTextFunction(),

            //new ToIntFromBytesFunction(),
            //new ToRealFromTextFunction(),
            //new ToUtf8Function(),
            //new ToUnicodeFunction(),
            //new ToBytesFromIntFunction(),
            //new ToBitsFromIntFunction(), 
                
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
            new ConcatTextsFunction(),
            new GetVarTextInfoMetafunction()
        };
    }
}