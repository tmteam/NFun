using System;
using System.Collections.Generic;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// VM-based runtime. Executes bytecode instead of tree-walking.
/// </summary>
public class VMRuntime {
    private readonly CompiledProgram _program;
    private readonly FunValue[] _locals;
    private readonly Dictionary<string, int> _variableSlots;
    private readonly Dictionary<string, FunnyType> _variableTypes;

    private VMRuntime(CompiledProgram program) {
        _program = program;
        _locals = new FunValue[program.LocalsCount];
        _variableSlots = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        _variableTypes = new Dictionary<string, FunnyType>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in program.Variables) {
            _variableSlots[v.Name] = v.Slot;
            _variableTypes[v.Name] = v.Type;
        }
    }

    public void Run() => VirtualMachine.Execute(_program, _locals);

    public void SetInput(string name, object value) {
        if (!_variableSlots.TryGetValue(name, out var slot))
            throw new KeyNotFoundException($"Variable '{name}' not found");
        _locals[slot] = FunValue.Unbox(value, _variableTypes[name]);
    }

    public object GetOutput(string name) {
        if (!_variableSlots.TryGetValue(name, out var slot))
            throw new KeyNotFoundException($"Variable '{name}' not found");
        var val = _locals[slot];
        var type = _variableTypes[name];
        // When value is stored in Ref (from Any-returning functions) but type is numeric,
        // re-unbox through the Ref to get the correct representation.
        if (val.Ref != null && val.Ref is not FunnyNone && type.BaseType >= BaseFunnyType.UInt8 && type.BaseType <= BaseFunnyType.Real)
            return val.Ref; // Already boxed correctly
        return val.Box(type);
    }

    public IEnumerable<string> VariableNames => _variableSlots.Keys;

    internal static VMRuntime Build(
        string script,
        IFunctionRegistry functionRegistry,
        DialectSettings dialect,
        IConstantList constants = null,
        IAprioriTypesMap aprioriTypesMap = null,
        ICustomTypeRegistry customTypes = null) {

        // 1. Tokenize + Parse
        var flow = Tokenizer.ToFlow(script, dialect.AllowNewlineInStrings == AllowNewlineInStrings.Deny);
        var syntaxTree = Parser.Parse(flow);

        // 2. Named types
        INamedTypeFieldRegistry namedTypeFieldRegistry = null;
        if (dialect.NamedTypesSupport == NamedTypesSupport.Enabled)
            syntaxTree = NamedTypeElaborator.Elaborate(syntaxTree, out _);

        // 3. Node numbers
        var visitor = new SetNodeNumberVisitor(0);
        syntaxTree.ComeOver(visitor);
        syntaxTree.MaxNodeId = visitor.LastUsedNumber;
        syntaxTree.IsSimpleBody = visitor.IsSimpleBody;

        // 4. Build user functions (reuse RuntimeBuilder infrastructure)
        var functionSolveOrder = syntaxTree.FindFunctionSolvingOrderOrThrow();
        IFunctionRegistry bodyRegistry = functionRegistry;
        if (functionSolveOrder.Length > 0) {
            var scope = new ScopeFunctionRegistry(functionRegistry, functionSolveOrder.Length);
            bodyRegistry = scope;
            for (int i = 0; i < functionSolveOrder.Length; i++) {
                RuntimeBuilder.BuildFunctionAndPutItToDictionary(
                    functionSolveOrder[i], constants, scope, dialect,
                    customTypes, namedTypeFieldRegistry, functionSolveOrder);
            }
        }

        // 5. TIC + apply types (reuse RuntimeBuilder.SolveBodyTypes)
        var typeInferenceResults = RuntimeBuilder.SolveBodyTypes(
            syntaxTree, constants, bodyRegistry,
            aprioriTypesMap ?? EmptyAprioriTypesMap.Instance,
            customTypes, dialect, namedTypeFieldRegistry);

        // 6. BytecodeCompiler
        var program = BytecodeCompiler.Compile(
            syntaxTree, bodyRegistry, typeInferenceResults, TicTypesConverter.Concrete, dialect);

        return new VMRuntime(program);
    }
}
