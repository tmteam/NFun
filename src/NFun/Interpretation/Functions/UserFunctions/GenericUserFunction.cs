using System;
using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Interpretation.Functions;

public class GenericUserFunction : GenericFunctionBase, IUserFunction {
    private readonly TypeInferenceResults _typeInferenceResults;
    private readonly UserFunctionDefinitionSyntaxNode _syntaxNode;
    private readonly IFunctionRegistry _dictionary;
    private readonly DialectSettings _dialect;
    private readonly IReadOnlyList<(StateStruct Struct, ConstraintsState Wrapper)> _structGenericMap;
    private readonly INamedTypeFieldRegistry _namedTypeFieldRegistry;

    private readonly IReadOnlyList<ConstraintsState> _constrainsMap;
    public int BuiltCount { get; private set; }
    public bool IsExtension => _syntaxNode.IsExtension;
    public bool IsUserDefined => true;

    internal static GenericUserFunction Create(
        TypeInferenceResults typeInferenceResults,
        UserFunctionDefinitionSyntaxNode syntaxNode,
        IFunctionRegistry dictionary,
        DialectSettings dialect,
        INamedTypeFieldRegistry namedTypeFieldRegistry = null) {
        var ticGenerics = typeInferenceResults.Generics;

        var ticFunName = syntaxNode.Id + "'" + syntaxNode.Args.Count;
        var ticSignature = (StateFun)typeInferenceResults.GetVariableType(ticFunName);

        // Detect struct generics: struct states in the signature that contain
        // generic (ConstraintsState) fields. These should be treated as generic
        // type variables with struct constraints, not as fixed struct layouts.
        // This allows callers to pass structs with additional fields beyond
        // what the function body accesses.
        var structGenericMap = DetectStructGenerics(ticSignature, ticGenerics);

        IReadOnlyList<ConstraintsState> extendedGenerics;
        if (structGenericMap != null)
        {
            // Build extended generics list: original ConstraintsState generics
            // plus wrapper ConstraintsState for each struct generic.
            // The wrappers are used in the converter's _constrainsMap
            // to map struct states to generic indices.
            var list = new List<ConstraintsState>(ticGenerics);
            foreach (var (_, wrapper) in structGenericMap)
                list.Add(wrapper);
            extendedGenerics = list;
        }
        else
        {
            extendedGenerics = ticGenerics;
        }

        // Use a signature converter that handles both ConstraintsState and
        // StateStruct -> Generic(i) mapping
        var signatureConverter = TicTypesConverter.GenericSignatureConverter(extendedGenerics, structGenericMap);

        var argTypes = new FunnyType[ticSignature.ArgNodes.Length];
        for (var i = 0; i < ticSignature.ArgNodes.Length; i++) {
            var argState = ticSignature.ArgNodes[i].State;
            argTypes[i] = signatureConverter.Convert(argState);
            // If arg's TIC state has a cycle-rescued named type stamp, surface NamedStructOf in
            // the function's external signature so the call site matches by named identity.
            var named = TicTypesConverter.BuildNamedTypeFromTicState(argState);
            if (named.HasValue) argTypes[i] = named.Value;
        }
        var retType = signatureConverter.Convert(ticSignature.ReturnType);

        var langConstrains = new GenericConstrains[extendedGenerics.Count];
        for (int i = 0; i < ticGenerics.Count; i++)
        {
            // When a generic carries an F-bound (lifted by LiftMuTypes), encode the bound as a
            // runtime FunnyType struct with FunnyType.Generic(i) for self-references. The
            // runtime dispatcher uses this for structural Fit at call sites. Mutually exclusive
            // with primitive Ancestor/Descendant per GenericConstrains assertion.
            if (ticGenerics[i].StructBound != null)
            {
                var boundFt = TicTypesConverter.BuildStructBoundFunnyType(
                    ticGenerics[i].StructBound, ticGenerics[i], i);
                langConstrains[i] = GenericConstrains.WithStructBound(boundFt);
            }
            else
            {
                langConstrains[i] = GenericConstrains.FromTicConstrains(ticGenerics[i]);
            }
        }

        if (structGenericMap != null)
        {
            // For struct generics, store the FunnyType struct as the constraint.
            // The struct fields may reference other generics (e.g. Generic(0) for field types).
            // Use a converter WITHOUT struct generic mapping to get the actual struct layout
            // (otherwise the converter would map the struct itself to Generic(j)).
            var fieldConverter = TicTypesConverter.GenericSignatureConverter(ticGenerics);
            int idx = 0;
            foreach (var (structState, _) in structGenericMap)
            {
                var structFunnyType = fieldConverter.Convert(structState);
                langConstrains[ticGenerics.Count + idx] = GenericConstrains.WithStructDescendant(structFunnyType);
                idx++;
            }
        }

#if DEBUG
        TraceLog.WriteLine($"CREATE GENERIC FUN {syntaxNode.Id}({string.Join(",", argTypes)}):{retType}");
        TraceLog.WriteLine($"    of {string.Join(", ", langConstrains)}");
#endif
        var function = new GenericUserFunction(
            typeInferenceResults,
            syntaxNode,
            dictionary,
            langConstrains,
            retType,
            argTypes,
            extendedGenerics,
            structGenericMap,
            dialect,
            namedTypeFieldRegistry);
        return function;
    }

    /// <summary>
    /// Detect StateStruct instances in the function signature that should be
    /// treated as generic type variables (because they contain generic field types
    /// AND appear in both arg and return positions).
    /// Returns null if no struct generics are found.
    ///
    /// Only structs shared between arg and return need this treatment.
    /// Structs that only appear in arg positions work correctly with normal
    /// open struct merging.
    /// </summary>
    private static List<(StateStruct OriginalStruct, ConstraintsState Wrapper)> DetectStructGenerics(
        StateFun signature, IReadOnlyList<ConstraintsState> ticGenerics) {
        if (ticGenerics.Count == 0) return null;

        // Collect structs reachable from arg nodes
        var argStructs = new List<StateStruct>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var argNode in signature.ArgNodes)
            CollectGenericStructs(argNode.GetNonReference().State, ticGenerics, argStructs, visited);

        if (argStructs.Count == 0) return null;

        // Collect structs reachable from return node
        var retStructs = new List<StateStruct>();
        visited.Clear();
        CollectGenericStructs(signature.RetNode.GetNonReference().State, ticGenerics, retStructs, visited);

        if (retStructs.Count == 0) return null;

        // Only include structs that appear in BOTH arg and return positions
        var result = new List<(StateStruct, ConstraintsState)>();
        foreach (var argStr in argStructs)
        {
            foreach (var retStr in retStructs)
            {
                if (ReferenceEquals(argStr, retStr) || StructFieldNamesMatch(argStr, retStr))
                {
                    var wrapper = ConstraintsState.Of(argStr);
                    result.Add((argStr, wrapper));
                    break;
                }
            }
        }
        return result.Count > 0 ? result : null;
    }

    private static bool StructFieldNamesMatch(StateStruct a, StateStruct b) {
        if (a.FieldsCount != b.FieldsCount) return false;
        foreach (var (key, _) in a.Fields)
            if (b.GetFieldOrNull(key) == null) return false;
        return true;
    }

    /// <summary>
    /// Recursively collect non-frozen StateStruct instances that contain
    /// fields with generic (ConstraintsState) types.
    /// </summary>
    private static void CollectGenericStructs(
        ITicNodeState state, IReadOnlyList<ConstraintsState> ticGenerics,
        List<StateStruct> result, HashSet<object> visited) {
        if (!visited.Add(state)) return; // prevent infinite recursion on cyclic refs
        switch (state)
        {
            case StateStruct str when !str.IsFrozen:
                bool hasGenericField = false;
                foreach (var member in str.Members)
                {
                    var memberState = member.GetNonReference().State;
                    if (memberState is ConstraintsState cs && ticGenerics.IndexOf(cs) >= 0)
                        hasGenericField = true;
                    else
                        CollectGenericStructs(memberState, ticGenerics, result, visited);
                }
                if (hasGenericField)
                    result.Add(str);
                break;
            case StateArray arr:
                CollectGenericStructs(arr.ElementNode.GetNonReference().State, ticGenerics, result, visited);
                break;
            case StateOptional opt:
                CollectGenericStructs(opt.ElementNode.GetNonReference().State, ticGenerics, result, visited);
                break;
            case StateFun fun:
                foreach (var argNode in fun.ArgNodes)
                    CollectGenericStructs(argNode.GetNonReference().State, ticGenerics, result, visited);
                CollectGenericStructs(fun.RetNode.GetNonReference().State, ticGenerics, result, visited);
                break;
        }
    }

    internal static void CreateSomeConcrete(GenericUserFunction function) {
        var varType = new FunnyType[function._constrainsMap.Count];

        for (var i = 0; i < function._constrainsMap.Count; i++)
        {
            var cs = function._constrainsMap[i];
            if (cs.Descendant is StateStruct)
            {
                // Struct generic: use Any as the concrete type
                varType[i] = FunnyType.Any;
            }
            else
            {
                // Use ancestor (widest type) for the initial concrete build.
                // Preferred is NOT used here because CreateSomeConcrete runs before
                // call sites are processed — using preferred would narrow the function
                // signature and reject valid call-site types (e.g., Real[] for Int32[]).
                var anc = cs.Ancestor ?? StatePrimitive.Any;
                varType[i] = TicTypesConverter.ToConcrete(anc.Name);
            }
        }

        function.CreateConcrete(varType, Dialects.Origin);
    }

    private GenericUserFunction(
        TypeInferenceResults typeInferenceResults,
        UserFunctionDefinitionSyntaxNode syntaxNode,
        IFunctionRegistry dictionary,
        GenericConstrains[] constrains,
        FunnyType returnType,
        FunnyType[] argTypes,
        IReadOnlyList<ConstraintsState> constrainsMap,
        IReadOnlyList<(StateStruct, ConstraintsState)> structGenericMap,
        DialectSettings dialect,
        INamedTypeFieldRegistry namedTypeFieldRegistry = null) : base(syntaxNode.Id, constrains, returnType, argTypes) {
        _typeInferenceResults = typeInferenceResults;
        _constrainsMap = constrainsMap;
        _structGenericMap = structGenericMap;
        _syntaxNode = syntaxNode;
        _dictionary = dictionary;
        _dialect = dialect;
        _namedTypeFieldRegistry = namedTypeFieldRegistry;
    }

    readonly Dictionary<string, IConcreteFunction> _concreteFunctionsCache = new();

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) {
        BuiltCount++;

        // F-bound Fit check at call site. For each generic position with a StructBound, the
        // caller's concrete type must satisfy the bound structurally (covariant width subtyping).
        // Recursive call substitution: when concreteTypes[i] is uninitialised (BaseType == Empty),
        // this is a recursive self-call from inside the body where TIC's saved generic
        // placeholders never resolved to a specific outer type. Substitute with the bound itself
        // — the body monomorphizes against its own bound, the correct self-referential semantic
        // for μ-recursion (Amadio–Cardelli rule: μX.S unfolds to S[μX.S/X]).
        for (int i = 0; i < Constrains.Length && i < concreteTypes.Length; i++)
        {
            if (!Constrains[i].HasStructBound) continue;
            if (concreteTypes[i].BaseType == BaseFunnyType.Empty)
            {
                concreteTypes[i] = Constrains[i].StructBound;
                continue;
            }
            if (!FunnyTypeFitsStructBound(concreteTypes[i], Constrains[i].StructBound, _namedTypeFieldRegistry))
                throw new Exceptions.FunnyParseException(
                    code: 783,
                    message: $"Argument {i} of '{Name}' does not satisfy the inferred recursive shape: " +
                             $"expected {Constrains[i].StructBound}, got {concreteTypes[i]}",
                    interval: _syntaxNode.Interval);
        }

        var id = string.Join(",", concreteTypes);
        if (_concreteFunctionsCache.TryGetValue(id, out var alreadyExists))
            return alreadyExists;
        //set types to nodes
        var converter = TicTypesConverter.ReplaceGenericTypesConverter(_constrainsMap, concreteTypes, _structGenericMap);
        var ticSignature = _typeInferenceResults.GetVariableType(_syntaxNode.Id + "'" + _syntaxNode.Args.Count);
        var funType = converter.Convert(ticSignature);

        var returnType = funType.FunTypeSpecification.Output;
        var argTypes = funType.FunTypeSpecification.Inputs;

        // Post-process — fold Struct→NamedStruct (recursive types only).
        if (_namedTypeFieldRegistry != null) {
            for (int i = 0; i < argTypes.Length; i++)
                argTypes[i] = FoldStructToNamedRecursive(argTypes[i], _namedTypeFieldRegistry);
            returnType = FoldStructToNamedRecursive(returnType, _namedTypeFieldRegistry);
        }

        // Create a function prototype and put it to cache for recursive cases
        // If the function is recursive - function will take recursive prototype from cache
        var concretePrototype = new ConcreteUserFunctionPrototype(Name, returnType, argTypes);
        _concreteFunctionsCache.Add(id, concretePrototype);

        _syntaxNode.ComeOver(
            enterVisitor: new ApplyTiResultEnterVisitor(
                solving: _typeInferenceResults,
                tiToLangTypeConverter: converter));

        var function = _syntaxNode.BuildConcrete(
            argTypes: argTypes,
            returnType: returnType,
            functionsRegistry: _dictionary,
            results: _typeInferenceResults,
            converter: converter,
            dialect: _dialect);

        concretePrototype.SetActual(function);
        //It is only place where we can figure out - is the function recursive or not
        RecursionKind = function.RecursionKind;
        return function;
    }
    //todo - remove it from here
    protected override object Calc(object[] args) => throw new NotImplementedException();

    public bool IsGeneric => true;
    public FunctionRecursionKind RecursionKind { get; private set; }

    /// <summary>
    /// Runtime mirror of TIC's <c>Fit(T, CS{S})</c>: a candidate FunnyType
    /// satisfies an F-bound StructBound iff:
    ///   1. candidate is a Struct (or NamedStruct expanded via registry),
    ///   2. <c>Fields(candidate) ⊇ Fields(bound)</c>,
    ///   3. for each shared field, <c>candidate.fᵢ ≤ bound.fᵢ</c> covariantly,
    ///      with self-references in <c>bound.fᵢ</c> (FunnyType.Generic(i))
    ///      resolved coinductively against <c>candidate</c> at the same depth.
    /// Coinductive cycle break (Amadio–Cardelli equirecursive subtyping):
    /// in-progress (candidate, bound) pairs return true on hit.
    /// </summary>
    private static bool FunnyTypeFitsStructBound(
        FunnyType candidate, FunnyType bound, INamedTypeFieldRegistry registry) {
        var visited = new HashSet<(string, string)>();
        return FitInner(candidate, bound, candidate, registry, visited);
    }

    /// <summary>
    /// Recursively walk a FunnyType and fold Struct→NamedStruct anywhere
    /// the field set uniquely matches a registered named type. Used in
    /// CreateConcrete to align function's expected ArgTypes with caller's
    /// NamedStruct identity.
    /// </summary>
    private static FunnyType FoldStructToNamedRecursive(FunnyType t, INamedTypeFieldRegistry registry) {
        switch (t.BaseType) {
            case BaseFunnyType.Struct: {
                var folded = TryFoldBackToNamedStruct(t, registry);
                if (folded.HasValue) return folded.Value;
                // Fold inner fields recursively.
                var spec = t.StructTypeSpecification;
                var newFields = new (string, FunnyType)[spec.Count];
                int j = 0;
                bool changed = false;
                foreach (var kv in spec) {
                    var newF = FoldStructToNamedRecursive(kv.Value, registry);
                    if (!newF.Equals(kv.Value)) changed = true;
                    newFields[j++] = (kv.Key, newF);
                }
                return changed ? FunnyType.StructOf(newFields) : t;
            }
            case BaseFunnyType.Optional:
                var inner = FoldStructToNamedRecursive(t.OptionalTypeSpecification.ElementType, registry);
                return inner.Equals(t.OptionalTypeSpecification.ElementType) ? t : FunnyType.OptionalOf(inner);
            case BaseFunnyType.ArrayOf:
                var elem = FoldStructToNamedRecursive(t.ArrayTypeSpecification.FunnyType, registry);
                return elem.Equals(t.ArrayTypeSpecification.FunnyType) ? t : FunnyType.ArrayOf(elem);
            default:
                return t;
        }
    }

    /// <summary>
    /// When a Struct candidate's outer field-set uniquely matches a registered
    /// NamedStruct's field set, fold back to NamedStructOf(name). Recovers identity
    /// lost during depth-bounded expansion in LangTiHelper.ResolveNamedStruct.
    /// Disambiguates by field types when multiple named types share field names.
    /// SCOPED: only fires when candidate has recursive structure matching registered
    /// type's recursion (avoids over-folding row-polymorphic non-recursive structs).
    /// Returns null if no unique match.
    /// </summary>
    private static FunnyType? TryFoldBackToNamedStruct(
        FunnyType candidate, INamedTypeFieldRegistry registry) {
        if (candidate.BaseType != BaseFunnyType.Struct) return null;
        var cSpec = candidate.StructTypeSpecification;
        // First pass: collect all named types matching by field-set names.
        var nameMatches = new List<string>();
        foreach (var kv in registry.All) {
            var declared = kv.Value;
            if (declared.Length != cSpec.Count) continue;
            bool allFound = true;
            foreach (var d in declared) {
                if (!cSpec.ContainsKey(d.name)) { allFound = false; break; }
            }
            if (allFound) nameMatches.Add(kv.Key);
        }
        if (nameMatches.Count == 0) return null;

        // Scope: fold only if registered type is RECURSIVE (has self-reference
        // in declared fields). For non-recursive named types (point2d = {x,y}),
        // row-polymorphic functions like length(p) = p.x*p.x + p.y*p.y MUST
        // stay generic; folding to NamedStruct(point2d) breaks polymorphism.
        nameMatches.RemoveAll(name => {
            registry.TryGetFields(name, out var declared);
            foreach (var d in declared) {
                if (TypeMentionsName(d.type, name)) return false; // keep — recursive
            }
            return true; // remove — non-recursive
        });
        if (nameMatches.Count == 0) return null;
        if (nameMatches.Count == 1) return FunnyType.NamedStructOf(nameMatches[0]);

        // Filter by primitive field-type compatibility. When the inferred type provides
        // values for the named-type's primitive fields they must agree on BaseType. Returns
        // the first compatible candidate — F-bound width-subtyping is purely structural, so
        // any structurally-indistinguishable named type satisfies the bound; the first
        // recovers a stable identity for downstream Fit / conversion.
        foreach (var name in nameMatches) {
            registry.TryGetFields(name, out var declared);
            bool primitiveTypesMatch = true;
            foreach (var d in declared) {
                if (d.type.BaseType == BaseFunnyType.NamedStruct
                    || d.type.BaseType == BaseFunnyType.Optional
                    || d.type.BaseType == BaseFunnyType.ArrayOf
                    || d.type.BaseType == BaseFunnyType.Struct)
                    continue; // recursive/composite: skip
                if (!cSpec.TryGetValue(d.name, out var cType)) { primitiveTypesMatch = false; break; }
                if (cType.BaseType != d.type.BaseType) { primitiveTypesMatch = false; break; }
            }
            if (primitiveTypesMatch) return FunnyType.NamedStructOf(name);
        }
        return null;
    }

    private static bool TypeMentionsName(FunnyType t, string name) {
        switch (t.BaseType) {
            case BaseFunnyType.NamedStruct:
                return string.Equals(t.NamedStructTypeName, name, StringComparison.OrdinalIgnoreCase);
            case BaseFunnyType.Optional:
                return TypeMentionsName(t.OptionalTypeSpecification.ElementType, name);
            case BaseFunnyType.ArrayOf:
                return TypeMentionsName(t.ArrayTypeSpecification.FunnyType, name);
            default:
                return false;
        }
    }

    private static bool FitInner(FunnyType candidate, FunnyType bound,
        FunnyType selfRefTarget, INamedTypeFieldRegistry registry,
        HashSet<(string, string)> visited) {
        // Fold Struct→NamedStruct at every depth.
        if (candidate.BaseType == BaseFunnyType.Struct && registry != null) {
            var matched = TryFoldBackToNamedStruct(candidate, registry);
            if (matched.HasValue) candidate = matched.Value;
        }
        // Self-ref in bound: FunnyType.Generic(i) means "the candidate at this generic position".
        // The candidate must satisfy its own bound coinductively.
        if (bound.BaseType == BaseFunnyType.Generic)
            return true; // coinductive — assume the recursive subgoal holds
        // candidate=Any means the call site couldn't determine a specific type for this generic
        // position (no concrete value flowed in, often because the function's body uses T only
        // structurally). The F-bound is satisfied vacuously — Any has no fields to inspect, so
        // any width requirement on Any is trivially met.
        if (candidate.BaseType == BaseFunnyType.Any)
            return true;
        // Reference-pair guard.
        var key = (candidate.ToString(), bound.ToString());
        if (!visited.Add(key)) return true;
        try {
            // Depth-bounded NamedStruct expansion produces flat Struct candidates that lost
            // identity. If a Struct's outer field-set uniquely matches a registered NamedStruct
            // AND bound is also a Struct (F-bound case), treat the candidate as if it were the
            // registered NamedStruct and use registry's typed fields. Recovers the Optional
            // wrappers that depth-1 expansion drops.
            if (candidate.BaseType == BaseFunnyType.Struct && bound.BaseType == BaseFunnyType.Struct
                && registry != null) {
                var matched = TryFoldBackToNamedStruct(candidate, registry);
                if (matched.HasValue) {
                    return FitInner(matched.Value, bound, selfRefTarget, registry, visited);
                }
            }
            // Expand NamedStruct via registry.
            if (candidate.BaseType == BaseFunnyType.NamedStruct) {
                if (registry == null) return false;
                if (!registry.TryGetFields(candidate.NamedStructTypeName, out var declared)) return false;
                // Build FunnyType.Struct from registry fields and recurse.
                var fields = new (string, FunnyType)[declared.Length];
                for (int j = 0; j < declared.Length; j++) fields[j] = (declared[j].name, declared[j].type);
                var expanded = FunnyType.StructOf(fields);
                return FitInner(expanded, bound, selfRefTarget, registry, visited);
            }
            // Optional/Array: covariant unwrap if both sides agree.
            if (bound.BaseType == BaseFunnyType.Optional)
            {
                if (candidate.BaseType == BaseFunnyType.None) return true;
                if (candidate.BaseType != BaseFunnyType.Optional) return false;
                return FitInner(candidate.OptionalTypeSpecification.ElementType,
                                bound.OptionalTypeSpecification.ElementType,
                                selfRefTarget, registry, visited);
            }
            if (bound.BaseType == BaseFunnyType.ArrayOf)
            {
                if (candidate.BaseType != BaseFunnyType.ArrayOf) return false;
                return FitInner(candidate.ArrayTypeSpecification.FunnyType,
                                bound.ArrayTypeSpecification.FunnyType,
                                selfRefTarget, registry, visited);
            }
            // Struct/Struct: width subtype + pointwise covariant.
            if (bound.BaseType == BaseFunnyType.Struct)
            {
                // GH #126 follow-up: F-bound is the recursive STRUCTURAL shape
                // (μX.S — the X is implicit), but the actual function-arg position
                // may lift it through Optional. Examples that produce this shape:
                //   loop(x, acc) = if(x==none) acc else loop(x?.next, n{next=acc})
                //   go(x) = loop(x, none)            // wrapper hides `none`
                //   r = go(n{v=1})                   // call site
                // TIC infers `loop`'s acc as `T?` where `T : {next: T?}` is the
                // F-bound. The Optional wrapper lives at the arg-position level,
                // not in the bound itself. So a candidate Optional(struct)
                // must be peeled one level before structural Fit. None is
                // trivially accepted (none ≤ T? for any T).
                if (candidate.BaseType == BaseFunnyType.Optional)
                {
                    return FitInner(candidate.OptionalTypeSpecification.ElementType,
                                    bound, selfRefTarget, registry, visited);
                }
                if (candidate.BaseType == BaseFunnyType.None)
                    return true;
                if (candidate.BaseType != BaseFunnyType.Struct) return false;
                var bSpec = bound.StructTypeSpecification;
                var cSpec = candidate.StructTypeSpecification;
                foreach (var bField in bSpec)
                {
                    bool found = false;
                    foreach (var cField in cSpec)
                    {
                        if (string.Equals(cField.Key, bField.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            // Self-ref in bound field: at recursion the candidate
                            // for this field must again satisfy the SAME bound.
                            if (bField.Value.BaseType == BaseFunnyType.Generic)
                            {
                                if (!FitInner(cField.Value, bound /* recursive bound */, selfRefTarget, registry, visited))
                                    return false;
                            }
                            else if (bField.Value.BaseType == BaseFunnyType.Optional
                                  && bField.Value.OptionalTypeSpecification.ElementType.BaseType == BaseFunnyType.Generic)
                            {
                                // opt(Generic(i)): candidate must be opt(T_self) or a NamedStruct
                                // whose declaration is opt(T_self) at this slot. When the
                                // candidate field is a bare NamedStruct (e.g. literal `intList{...}`),
                                // expand via registry — the declared type's `next: intList?` field
                                // IS Optional, and the bare NamedStruct value is implicitly the
                                // self-recursive instance carrying the bound.
                                if (cField.Value.BaseType == BaseFunnyType.None) { found = true; break; }
                                if (cField.Value.BaseType == BaseFunnyType.NamedStruct)
                                {
                                    // The NamedStruct value occupies the recursive
                                    // slot — it MUST itself satisfy the whole bound
                                    // (coinductive: visited guard prevents loop).
                                    if (!FitInner(cField.Value, bound, selfRefTarget, registry, visited))
                                        return false;
                                    found = true;
                                    break;
                                }
                                if (cField.Value.BaseType != BaseFunnyType.Optional) return false;
                                if (!FitInner(cField.Value.OptionalTypeSpecification.ElementType,
                                              bound, selfRefTarget, registry, visited))
                                    return false;
                            }
                            else
                            {
                                if (!FitInner(cField.Value, bField.Value, selfRefTarget, registry, visited))
                                    return false;
                            }
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                }
                return true;
            }
            // Concrete/concrete: standard equality (M2 will refine).
            return candidate.Equals(bound) || candidate.CanBeConvertedTo(bound);
        }
        finally
        {
            visited.Remove(key);
        }
    }
}
