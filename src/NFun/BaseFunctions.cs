using System.Linq;
using NFun.Functions;
using NFun.Functions.Collections;
using NFun.Functions.Lang;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun; 

internal static class BaseFunctions {
    /// <summary>
    /// ee-mode registry, per dialect: pipe-independent single-dict under
    /// <see cref="ExtensionFunctionsSeparation.Disabled"/>, pipe-aware dual-dict
    /// under <see cref="ExtensionFunctionsSeparation.Enabled"/>. Uses strict
    /// <see cref="MapFunction"/>.
    /// </summary>
    internal static IFunctionRegistry GetFunctions(TypeBehaviour typeBehaviour, ExtensionFunctionsSeparation separation)
        => separation == ExtensionFunctionsSeparation.Enabled
            ? (IFunctionRegistry)typeBehaviour.RealTypeSelect(DualDoubleFunctions, DualDecimalFunctions)
            : typeBehaviour.RealTypeSelect(SingleDoubleFunctions, SingleDecimalFunctions);

    /// <summary>Lang-mode registry — swaps <see cref="MapFunction"/> for
    /// <see cref="MapEnumerableFunction"/>; all other functions are shared.</summary>
    internal static IFunctionRegistry GetFunctionsLang(TypeBehaviour typeBehaviour, ExtensionFunctionsSeparation separation)
        => separation == ExtensionFunctionsSeparation.Enabled
            ? (IFunctionRegistry)typeBehaviour.RealTypeSelect(DualDoubleFunctionsLang, DualDecimalFunctionsLang)
            : typeBehaviour.RealTypeSelect(SingleDoubleFunctionsLang, SingleDecimalFunctionsLang);

    private static SingleDictFunctionRegistry SingleDoubleFunctions { get; }
    private static SingleDictFunctionRegistry SingleDecimalFunctions { get; }
    private static DualDictFunctionRegistry DualDoubleFunctions { get; }
    private static DualDictFunctionRegistry DualDecimalFunctions { get; }
    private static SingleDictFunctionRegistry SingleDoubleFunctionsLang { get; }
    private static SingleDictFunctionRegistry SingleDecimalFunctionsLang { get; }
    private static DualDictFunctionRegistry DualDoubleFunctionsLang { get; }
    private static DualDictFunctionRegistry DualDecimalFunctionsLang { get; }
    private static GenericFunctionBase[] GenericFunctions { get; }
    private static IConcreteFunction[] ConcreteFunctions { get; }
    private static IConcreteFunction[] ConcreteDoubleFunctions { get; }
    private static IConcreteFunction[] ConcreteDecimalFunctions { get; }

    /// <summary>
    /// Maximum arity for the <c>list(...)</c> factory. NFun lacks varargs so each
    /// arity is registered as its own overload. 16 covers virtually all real-world
    /// inline literals; users hitting the cap can nest with `concat(list(...), list(...))`.
    /// </summary>
    private const int MaxListFactoryArity = 16;

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
            new ContainsGenericFunctionDefinition(),
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
        };

        // Lang-mode list(...) / array(...) / fixedArray(...) factories: one
        // overload per arity (NFun lacks native varargs). MaxListFactoryArity
        // covers virtually all literals; beyond it the standard "function not
        // found" error fires.
        var listFactories = new GenericFunctionBase[MaxListFactoryArity];
        var arrayFactories = new GenericFunctionBase[MaxListFactoryArity];
        var fixedArrayFactories = new GenericFunctionBase[MaxListFactoryArity];
        var setFactories = new GenericFunctionBase[MaxListFactoryArity];
        var mapFactories = new GenericFunctionBase[MaxListFactoryArity];
        for (int i = 0; i < MaxListFactoryArity; i++) {
            listFactories[i] = new ListFactoryFunction(i + 1);
            arrayFactories[i] = new ArrayFactoryFunction(i + 1);
            fixedArrayFactories[i] = new FixedArrayFactoryFunction(i + 1);
            setFactories[i] = new SetFactoryFunction(i + 1);
            mapFactories[i] = new NFun.Functions.Collections.MapFactoryFunction(i + 1);
        }

        // Lang-mode list mutation API (Stage 3 / B.1). Signatures pin the
        // first argument to `list<T>` so ee-mode arrays / future fixedArray
        // cannot satisfy the call — uniform invariance keeps the API limited
        // to the mutable kind.
        var listMutators = new GenericFunctionBase[] {
            new ListAddFunction(),
            new ListAddAllFunction(),
            new ListRemoveFunction(),
            new ListRemoveAtFunction(),
            new ListRemoveLastFunction(),
            new ListClearFunction(),
            new NFun.Functions.Collections.SetTryAddFunction(),
            new NFun.Functions.Collections.SetTryRemoveFunction(),
            // Stage 5 / Map.2 — access + mutation API for map<K, V>.
            new NFun.Functions.Collections.MapSetKeyFunction(),
            new NFun.Functions.Collections.MapTryAddKeyFunction(),
            new NFun.Functions.Collections.MapRemoveKeyFunction(),
            new NFun.Functions.Collections.MapContainsKeyFunction(),
            new NFun.Functions.Collections.MapGetFunction(),
            new NFun.Functions.Collections.MapTryGetFunction(),
            new NFun.Functions.Collections.MapTryRemoveFunction(),
        };
        // Stage C — collection-conversion family (`toList`, `toArray`,
        // `toFixedArray`, `toSet`). Each one takes Enumerable<T> and produces a
        // fresh container of the named kind, preserving element type.
        var toXxxConverters = new GenericFunctionBase[] {
            new NFun.Functions.Collections.ToListFunction(),
            new NFun.Functions.Collections.ToArrayFunction(),
            new NFun.Functions.Collections.ToFixedArrayFunction(),
            new NFun.Functions.Collections.ToSetFunction(),
        };

        GenericFunctions = GenericFunctions
            .Concat(listFactories)
            .Concat(arrayFactories)
            .Concat(fixedArrayFactories)
            .Concat(setFactories)
            .Concat(mapFactories)
            .Concat(listMutators)
            .Concat(toXxxConverters)
            .ToArray();

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

            PrintFunction.Instance,
            PrintWithEndFunction.Instance,
            ReadLineFunction.Instance,
            ReadCharFunction.Instance,
        };

        // Math fns dispatch via GenericFunctions (Floats constraint).
        ConcreteDoubleFunctions = System.Array.Empty<IConcreteFunction>();
        ConcreteDecimalFunctions = System.Array.Empty<IConcreteFunction>();
        var allDoubleConcretes = ConcreteFunctions.Concat(ConcreteDoubleFunctions).ToArray();
        var allDecimalConcretes = ConcreteFunctions.Concat(ConcreteDecimalFunctions).ToArray();
        SingleDoubleFunctions = new SingleDictFunctionRegistry(allDoubleConcretes, GenericFunctions);
        SingleDecimalFunctions = new SingleDictFunctionRegistry(allDecimalConcretes, GenericFunctions);
        DualDoubleFunctions = new DualDictFunctionRegistry(allDoubleConcretes, GenericFunctions);
        DualDecimalFunctions = new DualDictFunctionRegistry(allDecimalConcretes, GenericFunctions);
        // Lang-mode swap: ee-mode LINQ functions that take/return `T[]`
        // (legacy StateArray) are replaced with their lang-mode counterparts
        // that take `Enumerable<T>` and return `FixedArray<T>`. This makes
        // chains like `arr?.sort().reverse() ?? [0]` produce StateCollection
        // results which the round-6 cross-Constructor LCA fix can combine
        // properly (instead of collapsing to Any via the legacy StateArray
        // path). See `NFun.Functions.Lang/`. Bug hunt round 6 #32 closure.
        var genericFunctionsLang = GenericFunctions
            .Select(f => f switch {
                MapFunction                          => (GenericFunctionBase)new MapEnumerableFunction(),
                SortFunction                         => new SortEnumerableFunction(),
                SortDescendingFunction               => new SortDescendingEnumerableFunction(),
                SortMapFunction                      => new SortMapEnumerableFunction(),
                SortMapDescendingFunction            => new SortMapDescendingEnumerableFunction(),
                ReverseGenericFunctionDefinition     => new ReverseEnumerableFunction(),
                FilterGenericFunctionDefinition      => new FilterEnumerableFunction(),
                ConcatArraysGenericFunctionDefinition => new ConcatEnumerableFunction(),
                TakeGenericFunctionDefinition         => new TakeEnumerableFunction(),
                SkipGenericFunctionDefinition         => new SkipEnumerableFunction(),
                FlatGenericFunctionDefinition         => new FlatEnumerableFunction(),
                UniteGenericFunctionDefinition        => new UniteEnumerableFunction(),
                UniqueGenericFunctionDefinition       => new UniqueEnumerableFunction(),
                IntersectGenericFunctionDefinition    => new IntersectEnumerableFunction(),
                SubstractArraysGenericFunctionDefinition => new ExceptEnumerableFunction(),
                AppendGenericFunctionDefinition       => new AppendEnumerableFunction(),
                RepeatGenericFunctionDefinition       => new RepeatEnumerableFunction(),
                SliceGenericFunctionDefinition        => new SliceEnumerableFunction(),
                SliceWithStepGenericFunctionDefinition => new SliceWithStepEnumerableFunction(),
                ChunkGenericFunctionDefinition        => new ChunkEnumerableFunction(),
                FoldGenericFunctionDefinition         => new FoldEnumerableFunction(),
                FoldWithDefaultsGenericFunctionDefinition => new FoldWithDefaultEnumerableFunction(),
                _ => f,
            })
            .ToArray();
        SingleDoubleFunctionsLang = new SingleDictFunctionRegistry(allDoubleConcretes, genericFunctionsLang);
        SingleDecimalFunctionsLang = new SingleDictFunctionRegistry(allDecimalConcretes, genericFunctionsLang);
        DualDoubleFunctionsLang = new DualDictFunctionRegistry(allDoubleConcretes, genericFunctionsLang);
        DualDecimalFunctionsLang = new DualDictFunctionRegistry(allDecimalConcretes, genericFunctionsLang);
    }
}