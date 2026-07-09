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
    internal static IFunctionRegistry GetFunctions(TypeBehaviour typeBehaviour, ExtensionFunctionsSeparation separation) {
        // Float-family dialect gets its own registries: they additionally contain
        // the float-family-only names (toFloat32/toFloat64). RealTypeSelect cannot
        // discriminate here — F32F64TypeBehaviour selects the double arm.
        if (typeBehaviour.SupportsFloatFamily)
            return separation == ExtensionFunctionsSeparation.Enabled
                ? (IFunctionRegistry)DualFloatFamilyFunctions
                : SingleFloatFamilyFunctions;
        return separation == ExtensionFunctionsSeparation.Enabled
            ? (IFunctionRegistry)typeBehaviour.RealTypeSelect(DualDoubleFunctions, DualDecimalFunctions)
            : typeBehaviour.RealTypeSelect(SingleDoubleFunctions, SingleDecimalFunctions);
    }

    private static SingleDictFunctionRegistry SingleDoubleFunctions { get; }
    private static SingleDictFunctionRegistry SingleDecimalFunctions { get; }
    private static SingleDictFunctionRegistry SingleFloatFamilyFunctions { get; }
    private static DualDictFunctionRegistry DualDoubleFunctions { get; }
    private static DualDictFunctionRegistry DualDecimalFunctions { get; }
    private static DualDictFunctionRegistry DualFloatFamilyFunctions { get; }
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
            new OopsFunction2(),

            // Math fns generic over Floats (Float32 + Real).
            new SqrtFunction(),
            new SinFunction(),
            new CosFunction(),
            new TanFunction(),
            new AsinFunction(),
            new AcosFunction(),
            new AtanFunction(),
            new Atan2Function(),
            new ExpFunction(),
            new LogEFunction(),
            new LogFunction(),
            new Log10Function(),
            new CeilFunction(),
            new FloorFunction(),
            new RoundFunction(),
            new AverageFunction(),
            new DivideFunction(),
        }
        // issue #135
        .Concat(ToNumericFunctions.CreateBaseFamily()).ToArray();

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

        // Math fns dispatch via GenericFunctions (Floats constraint).
        ConcreteDoubleFunctions = System.Array.Empty<IConcreteFunction>();
        ConcreteDecimalFunctions = System.Array.Empty<IConcreteFunction>();
        var allDoubleConcretes = ConcreteFunctions.Concat(ConcreteDoubleFunctions).ToArray();
        var allDecimalConcretes = ConcreteFunctions.Concat(ConcreteDecimalFunctions).ToArray();
        // Float-family dialect (Real=double + Float32): base set + toFloat32/toFloat64.
        var floatFamilyGenerics = GenericFunctions.Concat(ToNumericFunctions.CreateFloatFamilyExtras()).ToArray();
        SingleDoubleFunctions = new SingleDictFunctionRegistry(allDoubleConcretes, GenericFunctions);
        SingleDecimalFunctions = new SingleDictFunctionRegistry(allDecimalConcretes, GenericFunctions);
        SingleFloatFamilyFunctions = new SingleDictFunctionRegistry(allDoubleConcretes, floatFamilyGenerics);
        DualDoubleFunctions = new DualDictFunctionRegistry(allDoubleConcretes, GenericFunctions);
        DualDecimalFunctions = new DualDictFunctionRegistry(allDecimalConcretes, GenericFunctions);
        DualFloatFamilyFunctions = new DualDictFunctionRegistry(allDoubleConcretes, floatFamilyGenerics);
    }
}