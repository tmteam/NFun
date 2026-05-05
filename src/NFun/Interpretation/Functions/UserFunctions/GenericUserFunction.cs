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

    private readonly IReadOnlyList<ConstraintsState> _constrainsMap;
    public int BuiltCount { get; private set; }

    internal static GenericUserFunction Create(
        TypeInferenceResults typeInferenceResults,
        UserFunctionDefinitionSyntaxNode syntaxNode,
        IFunctionRegistry dictionary,
        DialectSettings dialect) {
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
        for (var i = 0; i < ticSignature.ArgNodes.Length; i++)
            argTypes[i] = signatureConverter.Convert(ticSignature.ArgNodes[i].State);
        var retType = signatureConverter.Convert(ticSignature.ReturnType);

        var langConstrains = new GenericConstrains[extendedGenerics.Count];
        for (int i = 0; i < ticGenerics.Count; i++)
            langConstrains[i] = GenericConstrains.FromTicConstrains(ticGenerics[i]);

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
            dialect);
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
    /// open struct merging. The bug only manifests when the return type
    /// is the same struct as the arg's element type.
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
        DialectSettings dialect) : base(syntaxNode.Id, constrains, returnType, argTypes) {
        _typeInferenceResults = typeInferenceResults;
        _constrainsMap = constrainsMap;
        _structGenericMap = structGenericMap;
        _syntaxNode = syntaxNode;
        _dictionary = dictionary;
        _dialect = dialect;
    }

    readonly Dictionary<string, IConcreteFunction> _concreteFunctionsCache = new();

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) {
        BuiltCount++;

        var id = string.Join(",", concreteTypes);
        if (_concreteFunctionsCache.TryGetValue(id, out var alreadyExists))
            return alreadyExists;
        //set types to nodes
        var converter = TicTypesConverter.ReplaceGenericTypesConverter(_constrainsMap, concreteTypes, _structGenericMap);
        var ticSignature = _typeInferenceResults.GetVariableType(_syntaxNode.Id + "'" + _syntaxNode.Args.Count);
        var funType = converter.Convert(ticSignature);

        var returnType = funType.FunTypeSpecification.Output;
        var argTypes = funType.FunTypeSpecification.Inputs;

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
}
