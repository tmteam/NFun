using System.Linq;
using NFun.Functions;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun; 

internal static class BaseFunctions {
    /// <summary>
    /// Returns the appropriate registry impl per dialect: pipe-independent single-dict
    /// under <see cref="ExtensionFunctionsSeparation.Disabled"/>, pipe-aware dual-dict
    /// under <see cref="ExtensionFunctionsSeparation.Enabled"/>.
    /// </summary>
    internal static IFunctionRegistry GetFunctions(TypeBehaviour typeBehaviour, ExtensionFunctionsSeparation separation)
        => separation == ExtensionFunctionsSeparation.Enabled
            ? (IFunctionRegistry)typeBehaviour.RealTypeSelect(DualDoubleFunctions, DualDecimalFunctions)
            : typeBehaviour.RealTypeSelect(SingleDoubleFunctions, SingleDecimalFunctions);

    private static SingleDictFunctionRegistry SingleDoubleFunctions { get; }
    private static SingleDictFunctionRegistry SingleDecimalFunctions { get; }
    private static DualDictFunctionRegistry DualDoubleFunctions { get; }
    private static DualDictFunctionRegistry DualDecimalFunctions { get; }
    private static GenericFunctionBase[] GenericFunctions { get; }
    private static IConcreteFunction[] ConcreteFunctions { get; }
    private static IConcreteFunction[] ConcreteDoubleFunctions { get; }
    private static IConcreteFunction[] ConcreteDecimalFunctions { get; }

    static BaseFunctions() {
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
            new SignFunction(),

            new ToHexTextFunction(),
            new ToBinTextFunction(),

            new BitOrFunction(),
            new BitAndFunction(),
            new BitXorFunction(),
            new BitInverseFunction(),
            new BitShiftLeftFunction(),
            new BitShiftRightFunction(),

            new NegateFunction(),
            new AbsFunction(),
            new RemainderFunction(),
            new DivideIntFunction(),
            new AddFunction(),
            new SubstractFunction(),
            new MultiplyFunction(),
            new PowFunction(),
            new IsInSingleGenericFunctionDefinition(),
            new UniqueGenericFunctionDefinition(),
            new UniteGenericFunctionDefinition(),
            new IntersectGenericFunctionDefinition(),
            new SubstractArraysGenericFunctionDefinition(),

            new MedianFunction(),
            new SortFunction(),
            new SortDescendingFunction(),
            new SortMapFunction(),
            new SortMapDescendingFunction(),
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
            new MultiMapSumFunction(),
            new LastFunction(),
            new FirstFunction(),
            new CountFunction(),
            new CountOfGenericFunctionDefinition(),
            new AllGenericFunctionDefinition(),
            new HasAnyGenericFunctionDefinition(),
            new AnyGenericFunctionDefinition(),
            new ReverseGenericFunctionDefinition(),

            new NullCoalesceFunction(),
            new ForceUnwrapFunction(),
            new SafeGetElementFunction(),
            new FilterNotNullFunction(),
            new ThrowErrorFunction(),
            new OopsFunction0(),
            new OopsFunction1(),
            new OopsFunction2()
        };

        ConcreteFunctions = new IConcreteFunction[] {
            new NotFunction(),
            new AndFunction(),
            new OrFunction(),
            new XorFunction(),

            new ToTextFunction(),
            new ToNumTextFunction(),
            new ToSciTextFunction(),
            new PadLeftTextFunction(),
            new PadRightTextFunction(),
            new PadCenterTextFunction(),

            new TrimFunction(),
            new TrimStartFunction(),
            new TrimEndFunction(),
            new ToUpperFunction(),
            new ToLowerFunction(),
            new SplitFunction(),
            new JoinFunction(),

            //Interpolation functions:
            new ConcatArrayOfTextsFunction(),
            new Concat2TextsFunction(),
            new Concat3TextsFunction(),
        };

        ConcreteDoubleFunctions = new IConcreteFunction[] {
            new AverageDoubleFunction(),

            new DivideDoubleFunction(),
            new SqrtDoubleFunction(),
            new SinDoubleFunction(),
            new CosDoubleFunction(),
            new TanDoubleFunction(),
            new AtanDoubleFunction(),
            new Atan2DoubleFunction(),
            new AsinDoubleFunction(),
            new AcosDoubleFunction(),
            new ExpDoubleFunction(),
            new LogDoubleFunction(),
            new LogEDoubleFunction(),
            new Log10DoubleFunction(),
            new RoundToDoubleFunction(),
            new CeilDoubleFunction(),
            new FloorDoubleFunction(),
        };

        ConcreteDecimalFunctions = new IConcreteFunction[] {
            new AverageDecimalFunction(),

            new DivideDecimalFunction(),
            new SqrtDecimalFunction(),
            new SinDecimalFunction(),
            new CosDecimalFunction(),
            new TanDecimalFunction(),
            new AtanDecimalFunction(),
            new Atan2DecimalFunction(),
            new AsinDecimalFunction(),
            new AcosDecimalFunction(),
            new ExpDecimalFunction(),
            new LogDecimalFunction(),
            new LogEDecimalFunction(),
            new Log10DecimalFunction(),
            new RoundToDecimalFunction(),
            new CeilDecimalFunction(),
            new FloorDecimalFunction(),
        };
        var allDoubleConcretes = ConcreteFunctions.Concat(ConcreteDoubleFunctions).ToArray();
        var allDecimalConcretes = ConcreteFunctions.Concat(ConcreteDecimalFunctions).ToArray();
        SingleDoubleFunctions = new SingleDictFunctionRegistry(allDoubleConcretes, GenericFunctions);
        SingleDecimalFunctions = new SingleDictFunctionRegistry(allDecimalConcretes, GenericFunctions);
        DualDoubleFunctions = new DualDictFunctionRegistry(allDoubleConcretes, GenericFunctions);
        DualDecimalFunctions = new DualDictFunctionRegistry(allDecimalConcretes, GenericFunctions);
    }
}