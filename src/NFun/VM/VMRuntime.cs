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
    private readonly FunValue[] _stack;
    private readonly CallFrame[] _callStack;
    private readonly Dictionary<string, (int Slot, FunnyType Type)> _variables;
    // Register VM (null when using stack VM)
    private byte[] _regCode;
    private FunValue[] _regConstants;

    private VMRuntime(CompiledProgram program) {
        _program = program;
        _locals = new FunValue[program.LocalsCount];
        _stack = new FunValue[Math.Max(program.MaxStackDepth, 8)];
        _callStack = program.UserFunctions.Length > 0 ? new CallFrame[32] : Array.Empty<CallFrame>();
        _variables = new Dictionary<string, (int, FunnyType)>(program.Variables.Length, StringComparer.OrdinalIgnoreCase);
        foreach (var v in program.Variables)
            _variables[v.Name] = (v.Slot, v.Type);
    }

    public void Run() {
        if (_regCode != null)
            RegisterVM.Execute(_regCode, _locals, _regConstants, _program,
                _program.ExternFunctions, _program.UserFunctions, 0);
        else
            VirtualMachine.Execute(_program, _locals, _stack, _callStack);
    }

    /// <summary>Direct access to register/local slots. Zero overhead for hot loops.</summary>
    public FunValue[] Locals => _locals;

    /// <summary>Get slot index for a variable name. Cache this for hot loops.</summary>
    public int GetSlot(string name) =>
        _variables.TryGetValue(name, out var v) ? v.Slot : -1;

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
            var scope = new ScopeFunctionRegistry(functionRegistry, functionSolveOrder.Length);
            bodyRegistry = scope;
            for (int i = 0; i < functionSolveOrder.Length; i++) {
                var funcDef = functionSolveOrder[i];
                bool hasLambda = NeedsTreeWalkerFallback(funcDef.Body);
                // For non-lambda functions: skip expression tree building (VM compiles from AST)
                // For lambda functions: need full tree-walker build (VM uses CALL_EXTERN)
                RuntimeBuilder.BuildFunctionAndPutItToDictionary(
                    funcDef, constants, scope, dialect,
                    customTypes, namedTypeFieldRegistry, functionSolveOrder,
                    out var funcTypeResults,
                    skipExpressionBuild: !hasLambda);
                if (!hasLambda && funcTypeResults != null) {
                    perFunctionTypeResults ??= new Dictionary<string, TypeInferenceResults>();
                    perFunctionTypeResults[$"{funcDef.Id}/{funcDef.Args.Count}"] = funcTypeResults;
                }
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

        // Check if any equation needs tree-walker fallback (ResultFunCall, try-catch — NOT lambdas)
        bool hasTreeWalkerEquations = false;
        foreach (var treeNode in syntaxTree.Nodes) {
            if (treeNode is SyntaxParsing.SyntaxNodes.EquationSyntaxNode eq
                && NeedsTreeWalkerFallbackStrict(eq.Expression)) {
                hasTreeWalkerEquations = true;
                break;
            }
        }

        if (hasTreeWalkerEquations) {
            variables = new Runtime.VariableDictionary();

            // Lightweight variable registration: create VariableSource entries for all
            // equation outputs + referenced inputs WITHOUT building expression trees.
            // This replaces the expensive ExpressionBuilderVisitor pass for non-lambda equations.
            foreach (var treeNode in syntaxTree.Nodes) {
                if (treeNode is SyntaxParsing.SyntaxNodes.EquationSyntaxNode eq) {
                    var eqType = eq.OutputType.BaseType != BaseFunnyType.Empty
                        ? eq.OutputType : eq.Expression.OutputType;
                    if (eqType.BaseType != BaseFunnyType.Empty)
                        variables.TryAdd(Runtime.VariableSource.CreateWithoutStrictTypeLabel(
                            eq.Id, eqType, Runtime.FunnyVarAccess.Output, dialect.Converter));
                }
            }
            // Also register input variables referenced in the tree
            RegisterInputVariables(syntaxTree, variables, dialect);

            // Build lambda-containing equations (can now reference captured vars)
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

        // 7. Try register VM first — supports all features except try-catch/hi-order calls.
        //    Falls back to stack VM on NotSupportedException.
        try {
            if (!hasTreeWalkerEquations) {
                var (regCode, regConsts, regLocals, regSlots, regExternFuncs, regStructLayouts, regTypeTable, regUserFuncs) =
                    RegisterCompiler.Compile(syntaxTree, typeInferenceResults, TicTypesConverter.Concrete,
                        bodyRegistry, dialect, perFunctionTypeResults);

                // Build a minimal CompiledProgram for variable metadata
                var regVars = new List<VariableSlot>();
                foreach (var (name, slot) in regSlots) {
                    var type = FunnyType.Any;
                    // Determine type from syntax tree equations
                    foreach (var n in syntaxTree.Nodes) {
                        if (n is SyntaxParsing.SyntaxNodes.EquationSyntaxNode eq && string.Equals(eq.Id, name, StringComparison.OrdinalIgnoreCase)) {
                            type = eq.OutputType.BaseType != BaseFunnyType.Empty ? eq.OutputType : eq.Expression.OutputType;
                            // If still unresolved, try TIC results for the expression node
                            if (type.BaseType == BaseFunnyType.Empty || type.BaseType == BaseFunnyType.Any) {
                                var ticState = typeInferenceResults.GetSyntaxNodeTypeOrNull(eq.Expression.OrderNumber);
                                if (ticState != null)
                                    type = TicTypesConverter.Concrete.Convert(ticState);
                            }
                            break;
                        }
                    }
                    // If not an equation output, it's an input — get type from syntax tree
                    if (type.BaseType == BaseFunnyType.Any || type.BaseType == BaseFunnyType.Empty) {
                        foreach (var n in syntaxTree.Nodes)
                            if (n is SyntaxParsing.SyntaxNodes.EquationSyntaxNode eq2)
                                ScanForVariableType(eq2.Expression, name, ref type);
                    }
                    bool isOutput = false;
                    foreach (var n in syntaxTree.Nodes)
                        if (n is SyntaxParsing.SyntaxNodes.EquationSyntaxNode eq3 && string.Equals(eq3.Id, name, StringComparison.OrdinalIgnoreCase))
                            isOutput = true;
                    regVars.Add(new VariableSlot { Name = name, Slot = slot, Type = type, IsOutput = isOutput });
                }

                var minimalProgram = new CompiledProgram {
                    Code = regCode,
                    Constants = regConsts,
                    Variables = regVars.ToArray(),
                    StructLayouts = regStructLayouts,
                    ExternFunctions = regExternFuncs,
                    UserFunctions = regUserFuncs,
                    ExceptionHandlers = Array.Empty<ExceptionHandler>(),
                    TypeTable = regTypeTable,
                    LocalsCount = regLocals,
                    MaxStackDepth = 0,
                    HasExceptionHandlers = false,
                    IsRegisterBytecode = true,
                };
                var regRuntime = new VMRuntime(minimalProgram);
                regRuntime._regCode = regCode;
                regRuntime._regConstants = regConsts;
                return regRuntime;
            }
        } catch (NotSupportedException) {
            // Register compiler doesn't support this expression — use stack VM
        }

        // 7b. Stack VM fallback
        var program = BytecodeCompiler.Compile(
            syntaxTree, bodyRegistry, typeInferenceResults, TicTypesConverter.Concrete, dialect, preBuilt, perFunctionTypeResults);

        var runtime = new VMRuntime(program);

        // Wire captured variables: tree-walker variables → VM locals bridge
        if (variables != null && variables.Count > 0) {
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


    /// <summary>Scan syntax tree for input variables and register them in the dictionary.</summary>
    private static void RegisterInputVariables(SyntaxTree tree, Runtime.VariableDictionary variables, DialectSettings dialect) {
        foreach (var node in tree.Nodes) {
            if (node is SyntaxParsing.SyntaxNodes.UserFunctionDefinitionSyntaxNode) continue;
            ScanAndRegisterVars(node, variables, dialect);
        }
    }

    private static void ScanAndRegisterVars(ISyntaxNode node, Runtime.VariableDictionary variables, DialectSettings dialect) {
        // Skip lambda bodies — their arguments are local, not outer scope
        if (node is SyntaxParsing.SyntaxNodes.AnonymFunctionSyntaxNode
            || node is SyntaxParsing.SyntaxNodes.SuperAnonymFunctionSyntaxNode)
            return;
        if (node is SyntaxParsing.SyntaxNodes.NamedIdSyntaxNode named
            && named.IdType == SyntaxParsing.SyntaxNodes.NamedIdNodeType.Variable
            && named.OutputType.BaseType != BaseFunnyType.Empty
            && variables.GetOrNull(named.Id) == null) {
            variables.TryAdd(Runtime.VariableSource.CreateWithoutStrictTypeLabel(
                named.Id, named.OutputType, Runtime.FunnyVarAccess.Input, dialect.Converter));
        }
        foreach (var child in node.Children)
            ScanAndRegisterVars(child, variables, dialect);
    }

    /// <summary>Recursively scan a syntax node tree for a named variable and capture its type.</summary>
    private static void ScanForVariableType(ISyntaxNode node, string name, ref FunnyType type) {
        if (node is SyntaxParsing.SyntaxNodes.NamedIdSyntaxNode named
            && string.Equals(named.Id, name, StringComparison.OrdinalIgnoreCase)
            && named.OutputType.BaseType != BaseFunnyType.Empty) {
            type = named.OutputType;
            return;
        }
        foreach (var child in node.Children)
            ScanForVariableType(child, name, ref type);
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

    /// <summary>Strict version: only ResultFunCall and TryCatch need tree-walker.
    /// Lambdas are handled natively by register compiler.</summary>
    internal static bool NeedsTreeWalkerFallbackStrict(ISyntaxNode node) {
        if (node is SyntaxParsing.SyntaxNodes.ResultFunCallSyntaxNode
            || node is SyntaxParsing.SyntaxNodes.TryCatchSyntaxNode)
            return true;
        // Don't recurse into lambda bodies — register compiler handles them
        if (node is SyntaxParsing.SyntaxNodes.AnonymFunctionSyntaxNode
            || node is SyntaxParsing.SyntaxNodes.SuperAnonymFunctionSyntaxNode)
            return false;
        foreach (var child in node.Children)
            if (NeedsTreeWalkerFallbackStrict(child)) return true;
        return false;
    }
}
