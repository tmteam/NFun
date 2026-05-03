using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly FunValue[] _stack;
    private readonly CallFrame[] _callStack;
    // Single lookup: name → (slot, type). Eliminates double-dictionary overhead in SetInput/GetOutput.
    private readonly Dictionary<string, (int Slot, FunnyType Type)> _variables;

    private VMRuntime(CompiledProgram program) {
        _program = program;
        _locals = new FunValue[program.LocalsCount];
        _stack = new FunValue[Math.Max(program.MaxStackDepth, 8)];
        _callStack = program.UserFunctions.Length > 0 ? new CallFrame[32] : Array.Empty<CallFrame>();
        _variables = new Dictionary<string, (int, FunnyType)>(program.Variables.Length, StringComparer.OrdinalIgnoreCase);
        foreach (var v in program.Variables)
            _variables[v.Name] = (v.Slot, v.Type);
    }

    public void Run() => VirtualMachine.Execute(_program, _locals, _stack, _callStack);

    public void SetInput(string name, object value) {
        if (!_variables.TryGetValue(name, out var v))
            throw new KeyNotFoundException($"Variable '{name}' not found");
        _locals[v.Slot] = FunValue.Unbox(value, v.Type);
    }

    public object GetOutput(string name) {
        if (!_variables.TryGetValue(name, out var v))
            throw new KeyNotFoundException($"Variable '{name}' not found");
        var val = _locals[v.Slot];
        if (val.Ref != null && val.Ref is not FunnyNone) {
            if (v.Type.BaseType >= BaseFunnyType.UInt8 && v.Type.BaseType <= BaseFunnyType.Real)
                return val.Ref;
            if (v.Type.BaseType == BaseFunnyType.Bool && val.Ref is bool)
                return val.Ref;
        }
        return val.Box(v.Type);
    }

    public IEnumerable<string> VariableNames => _variables.Keys;

    internal static VMRuntime Build(
        string script,
        IFunctionRegistry functionRegistry,
        DialectSettings dialect,
        IConstantList constants = null,
        IAprioriTypesMap aprioriTypesMap = null,
        ICustomTypeRegistry customTypes = null) {

        // Ensure constants is never null (same as RuntimeBuilder — TicSetupVisitor needs it)
        constants ??= dialect.Converter.TypeBehaviour is Types.RealIsDoubleTypeBehaviour
            ? BuiltInConstantList.Double
            : BuiltInConstantList.Decimal;

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
        Dictionary<string, TypeInferenceResults> perFunctionTypeResults = null;
        if (functionSolveOrder.Length > 0) {
            perFunctionTypeResults = new Dictionary<string, TypeInferenceResults>();
            var scope = new ScopeFunctionRegistry(functionRegistry, functionSolveOrder.Length);
            bodyRegistry = scope;
            for (int i = 0; i < functionSolveOrder.Length; i++) {
                RuntimeBuilder.BuildFunctionAndPutItToDictionary(
                    functionSolveOrder[i], constants, scope, dialect,
                    customTypes, namedTypeFieldRegistry, functionSolveOrder);
                // Also solve types for the VM's BytecodeCompiler
                var funcResults = SolveUserFunctionTypes(
                    functionSolveOrder[i], scope, constants, dialect, customTypes, namedTypeFieldRegistry, functionSolveOrder);
                if (funcResults != null)
                    perFunctionTypeResults[$"{functionSolveOrder[i].Id}/{functionSolveOrder[i].Args.Count}"] = funcResults;
            }
        }

        // 5. TIC + apply types (reuse RuntimeBuilder.SolveBodyTypes)
        var typeInferenceResults = RuntimeBuilder.SolveBodyTypes(
            syntaxTree, constants, bodyRegistry,
            aprioriTypesMap ?? EmptyAprioriTypesMap.Instance,
            customTypes, dialect, namedTypeFieldRegistry);

        // 6. Pre-build tree-walker nodes for lambda/hi-order-containing expressions
        var preBuilt = new Dictionary<int, Interpretation.Nodes.IExpressionNode>();
        Runtime.VariableDictionary variables = null;

        // Check if any equation needs tree-walker fallback (lambdas, ResultFunCall, etc.)
        bool hasTreeWalkerEquations = false;
        foreach (var treeNode in syntaxTree.Nodes) {
            if (treeNode is SyntaxParsing.SyntaxNodes.EquationSyntaxNode eq
                && NeedsTreeWalkerFallback(eq.Expression)) {
                hasTreeWalkerEquations = true;
                break;
            }
        }

        if (hasTreeWalkerEquations) {
            variables = new Runtime.VariableDictionary();

            // First pass: build non-lambda equations to populate variables (for captured vars)
            foreach (var treeNode in syntaxTree.Nodes) {
                if (treeNode is SyntaxParsing.SyntaxNodes.EquationSyntaxNode eq
                    && !NeedsTreeWalkerFallback(eq.Expression)) {
                    try {
                        Interpretation.ExpressionBuilderVisitor.BuildExpression(
                            eq.Expression, bodyRegistry, eq.OutputType, variables,
                            typeInferenceResults, TicTypesConverter.Concrete, dialect);
                    } catch { /* skip */ }
                }
            }

            // Second pass: build lambda-containing equations (can now reference captured vars)
            foreach (var treeNode in syntaxTree.Nodes) {
                if (treeNode is SyntaxParsing.SyntaxNodes.EquationSyntaxNode eq
                    && NeedsTreeWalkerFallback(eq.Expression)) {
                    try {
                        var expr = Interpretation.ExpressionBuilderVisitor.BuildExpression(
                            eq.Expression, bodyRegistry, eq.OutputType, variables,
                            typeInferenceResults, TicTypesConverter.Concrete, dialect);
                        preBuilt[eq.Expression.OrderNumber] = expr;
                    } catch { /* skip if tree-walker can't build it */ }
                }
            }
        }

        // 7. BytecodeCompiler (with pre-built lambda expressions and per-function type results)
        var program = BytecodeCompiler.Compile(
            syntaxTree, bodyRegistry, typeInferenceResults, TicTypesConverter.Concrete, dialect, preBuilt, perFunctionTypeResults);

        var runtime = new VMRuntime(program);

        // Wire captured variables: tree-walker variables → VM locals bridge
        if (variables != null && variables.GetAll().Any()) {
            var bridges = new List<(Runtime.VariableSource, int, FunnyType)>();
            foreach (var varSource in variables.GetAll()) {
                if (runtime._variables.TryGetValue(varSource.Name, out var vi))
                    bridges.Add((varSource, vi.Slot, vi.Type));
            }
            if (bridges.Count > 0) {
                var bridgeArray = bridges.ToArray();
                // Set locals ref and bridges on all tree-walker wrappers
                foreach (var ext in program.ExternFunctions) {
                    if (ext.Function is BytecodeCompiler.TreeWalkerWrapper tw) {
                        tw.Locals = runtime._locals;
                        tw.CapturedVarBridges = bridgeArray;
                    }
                }
            }
        }

        return runtime;
    }

    private static TypeInferenceResults SolveUserFunctionTypes(
        SyntaxParsing.SyntaxNodes.UserFunctionDefinitionSyntaxNode funcNode,
        IFunctionRegistry functions,
        IConstantList constants,
        DialectSettings dialect,
        ICustomTypeRegistry customTypes,
        INamedTypeFieldRegistry namedTypeFieldRegistry,
        SyntaxParsing.SyntaxNodes.UserFunctionDefinitionSyntaxNode[] allUserFunctions) {
        try {
            var graph = new Tic.GraphBuilder();
            var resultsBuilder = new TypeInferenceAdapter.TypeInferenceResultsBuilder();
            if (!TypeInferenceAdapter.TicSetupVisitor.SetupTicForUserFunction(
                funcNode, graph, functions, constants, resultsBuilder, dialect,
                customTypes, namedTypeFieldRegistry, allUserFunctions))
                return null;
            var types = graph.Solve(ignorePrefered: true);
            resultsBuilder.SetResults(types);
            var result = resultsBuilder.Build();
            // Apply types to syntax nodes
            funcNode.ComeOver(new TypeInferenceAdapter.ApplyTiResultEnterVisitor(result, TicTypesConverter.Concrete));
            return result;
        } catch {
            return null;
        }
    }

    internal static bool NeedsTreeWalkerFallback(ISyntaxNode node) {
        if (node is SyntaxParsing.SyntaxNodes.AnonymFunctionSyntaxNode
            || node is SyntaxParsing.SyntaxNodes.SuperAnonymFunctionSyntaxNode
            || node is SyntaxParsing.SyntaxNodes.ResultFunCallSyntaxNode)
            return true;
        foreach (var child in node.Children)
            if (NeedsTreeWalkerFallback(child)) return true;
        return false;
    }
}
