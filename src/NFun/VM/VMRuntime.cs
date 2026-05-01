using System;
using System.Collections.Generic;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// VM-based runtime. Replaces FunnyRuntime for the bytecode execution path.
/// Same variable read/write interface, but executes bytecode instead of tree-walking.
/// </summary>
public class VMRuntime {
    private readonly CompiledProgram _program;
    private readonly FunValue[] _locals;
    private readonly Dictionary<string, int> _variableSlots;
    private readonly Dictionary<string, FunnyType> _variableTypes;
    private readonly HashSet<string> _outputNames;

    private VMRuntime(CompiledProgram program) {
        _program = program;
        _locals = new FunValue[program.LocalsCount];
        _variableSlots = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        _variableTypes = new Dictionary<string, FunnyType>(StringComparer.OrdinalIgnoreCase);
        _outputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var v in program.Variables) {
            _variableSlots[v.Name] = v.Slot;
            _variableTypes[v.Name] = v.Type;
            if (v.IsOutput) _outputNames.Add(v.Name);
        }
    }

    /// <summary>Run the compiled program.</summary>
    public void Run() => VirtualMachine.Execute(_program, _locals);

    /// <summary>Set input variable value.</summary>
    public void SetInput(string name, object value) {
        if (!_variableSlots.TryGetValue(name, out var slot))
            throw new KeyNotFoundException($"Variable '{name}' not found");
        _locals[slot] = FunValue.Unbox(value, _variableTypes[name]);
    }

    /// <summary>Get output variable value.</summary>
    public object GetOutput(string name) {
        if (!_variableSlots.TryGetValue(name, out var slot))
            throw new KeyNotFoundException($"Variable '{name}' not found");
        return _locals[slot].Box(_variableTypes[name]);
    }

    /// <summary>All variable names.</summary>
    public IEnumerable<string> VariableNames => _variableSlots.Keys;

    /// <summary>
    /// Build VM runtime from source script.
    /// Reuses Tokenizer → Parser → TIC, then BytecodeCompiler.
    /// </summary>
    internal static VMRuntime Build(
        string script,
        IFunctionRegistry functionRegistry,
        DialectSettings dialect,
        IConstantList constants = null,
        IAprioriTypesMap aprioriTypesMap = null,
        ICustomTypeRegistry customTypes = null) {

        // Tokenize + Parse (same as tree-walker path)
        var flow = Tokenizer.ToFlow(script, dialect.AllowNewlineInStrings == AllowNewlineInStrings.Deny);
        var syntaxTree = Parser.Parse(flow);

        // Set node numbers
        syntaxTree.ComeOver(new SetNodeNumberVisitor(0));

        // TIC type inference (reuse existing helper)
        bool typesApplied;
        var typeInferenceResults = RuntimeBuilderHelper.SolveBodyOrThrow(
            syntaxTree, functionRegistry, constants,
            aprioriTypesMap ?? EmptyAprioriTypesMap.Instance,
            customTypes, dialect, out typesApplied,
            namedTypeFieldRegistry: null);

        // Bytecode compilation
        var typesConverter = TicTypesConverter.Concrete;
        var program = BytecodeCompiler.Compile(
            syntaxTree, functionRegistry, typeInferenceResults, typesConverter, dialect);

        return new VMRuntime(program);
    }
}
