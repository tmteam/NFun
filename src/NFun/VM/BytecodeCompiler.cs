using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Functions;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.TypeInferenceAdapter;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// Translates a typed syntax tree into bytecode for <see cref="VirtualMachine"/>.
/// Implements <see cref="ISyntaxNodeVisitor{T}"/> returning void (dummy byte) — each Visit
/// method emits bytecode into the shared <see cref="BytecodeEmitter"/>.
/// </summary>
internal sealed class BytecodeCompiler : ISyntaxNodeVisitor<byte> {
    private readonly BytecodeEmitter _e = new();
    private readonly IFunctionRegistry _functions;
    private TypeInferenceResults _typeInferenceResults;
    private readonly TicTypesConverter _typesConverter;
    private readonly DialectSettings _dialect;

    // Variable name → local slot
    private readonly Dictionary<string, int> _localSlots = new(StringComparer.OrdinalIgnoreCase);
    private int _nextLocalSlot;

    // Output variables (equations): name → slot
    private readonly List<VariableSlot> _variableSlots = new();

    // Extern functions registry — lazy-init (empty for pure arithmetic)
    private List<ExternFunc> _externFunctions;
    private Dictionary<string, Dictionary<int, int>> _externFuncIds;

    // User-defined functions — lazy-init
    private List<UserFunc> _userFunctions;
    private readonly List<ExceptionHandler> _exceptionHandlers = new();
    private Dictionary<int, Interpretation.Nodes.IExpressionNode> _preBuiltExpressions;
    private Dictionary<string, TypeInferenceResults> _perFunctionTypeResults;

    // Struct layouts — lazy-init
    private List<StructLayout> _structLayouts;
    private List<FunnyType> _typeTable;
    private Dictionary<FunnyType, int> _typeTableIndex;
    private Dictionary<string, int> _structLayoutIds;

    private List<ExternFunc> ExternFunctions => _externFunctions ??= new();
    private Dictionary<string, Dictionary<int, int>> ExternFuncIds => _externFuncIds ??= new();
    private List<UserFunc> UserFunctions => _userFunctions ??= new();
    private List<StructLayout> StructLayouts => _structLayouts ??= new();
    private List<FunnyType> TypeTable => _typeTable ??= new();
    private Dictionary<FunnyType, int> TypeTableIndex => _typeTableIndex ??= new();
    private Dictionary<string, int> StructLayoutIds => _structLayoutIds ??= new();

    private BytecodeCompiler(
        IFunctionRegistry functions,
        TypeInferenceResults typeInferenceResults,
        TicTypesConverter typesConverter,
        DialectSettings dialect) {
        _functions = functions;
        _typeInferenceResults = typeInferenceResults;
        _typesConverter = typesConverter;
        _dialect = dialect;
    }

    /// <summary>
    /// Compiles the syntax tree into a <see cref="CompiledProgram"/>.
    /// </summary>
    public static CompiledProgram Compile(
        SyntaxTree tree,
        IFunctionRegistry functions,
        TypeInferenceResults typeInferenceResults,
        TicTypesConverter typesConverter,
        DialectSettings dialect,
        Dictionary<int, Interpretation.Nodes.IExpressionNode> preBuiltExpressions = null,
        Dictionary<string, TypeInferenceResults> perFunctionTypeResults = null) {
        var compiler = new BytecodeCompiler(functions, typeInferenceResults, typesConverter, dialect);
        compiler._preBuiltExpressions = preBuiltExpressions ?? new(0);
        compiler._perFunctionTypeResults = perFunctionTypeResults;
        return compiler.CompileTree(tree);
    }

    private CompiledProgram CompileTree(SyntaxTree tree) {
        // First pass: allocate output slots
        foreach (var node in tree.Nodes) {
            if (node is EquationSyntaxNode eq) {
                var eqType = eq.OutputType.BaseType != BaseFunnyType.Empty
                    ? eq.OutputType : GetOutputType(eq.Expression);
                AllocateSlot(eq.Id, isOutput: true, eqType);
            }
        }

        // Allocate slots for input variables referenced in equations
        if (_preBuiltExpressions.Count > 0)
            AllocateInputVariables(tree);

        // Compile user function definitions
        foreach (var node in tree.Nodes)
            if (node is UserFunctionDefinitionSyntaxNode funcDef)
                CompileUserFunction(funcDef);

        // Compile each equation
        foreach (var node in tree.Nodes) {
            if (node is not EquationSyntaxNode eq) continue;
            // If equation has pre-built tree-walker expression (contains lambda),
            // emit CALL_EXTERN to the pre-built node instead of bytecode compilation.
            if (_preBuiltExpressions.TryGetValue(eq.Expression.OrderNumber, out var preBuilt)) {
                var wrapper = new TreeWalkerWrapper(preBuilt);
                var funcId = GetOrRegisterExternFunc(wrapper);
                _e.EmitWithArg(Op.CallExtern, (byte)funcId);
                _e.Code.Add(0); // 0 args
            } else {
                CompileExpression(eq.Expression);
                var targetType = eq.OutputType.BaseType != BaseFunnyType.Empty
                    ? eq.OutputType : GetOutputType(eq.Expression);
                EmitConvertIfNeeded(GetOutputType(eq.Expression), targetType);
            }
            var slot = _localSlots[eq.Id];
            _e.EmitWithArg(Op.StoreLocal, (byte)slot);
        }

        _e.Emit(Op.Halt);

        // Peephole optimization: fuse common opcode patterns into superinstructions
        PeepholeOptimize(_e);

        var bytecode = _e.ToArray();
        return new CompiledProgram {
            Code = bytecode,
            Constants = _e.ConstantsToArray(),
            StructLayouts = _structLayouts is { Count: > 0 } ? _structLayouts.ToArray() : Array.Empty<StructLayout>(),
            ExternFunctions = _externFunctions is { Count: > 0 } ? _externFunctions.ToArray() : Array.Empty<ExternFunc>(),
            UserFunctions = _userFunctions is { Count: > 0 } ? _userFunctions.ToArray() : Array.Empty<UserFunc>(),
            Variables = _variableSlots.ToArray(),
            ExceptionHandlers = _exceptionHandlers.Count > 0 ? _exceptionHandlers.ToArray() : Array.Empty<ExceptionHandler>(),
            HasExceptionHandlers = _exceptionHandlers.Count > 0,
            TypeTable = _typeTable is { Count: > 0 } ? _typeTable.ToArray() : Array.Empty<FunnyType>(),
            LocalsCount = _nextLocalSlot,
            MaxStackDepth = ComputeMaxStackDepth(bytecode),
        };
    }

    private void CompileUserFunction(UserFunctionDefinitionSyntaxNode funcDef) {
        // If body contains lambdas, skip VM compilation — tree-walker version
        // is already registered in the function registry by RuntimeBuilder.
        // The VM will call it via CALL_EXTERN.
        if (VMRuntime.NeedsTreeWalkerFallback(funcDef.Body))
            return;

        // Placeholder — jump over function body
        int jumpOver = _e.EmitJump(Op.Jump);

        int entryIP = _e.Position;
        var funcId = UserFunctions.Count;

        // Save current slot state — function has its own scope
        var savedSlots = new Dictionary<string, int>(_localSlots, StringComparer.OrdinalIgnoreCase);
        var savedNextSlot = _nextLocalSlot;
        _localSlots.Clear();
        _nextLocalSlot = 0;

        // Swap type inference results for user function scope
        var savedTypeResults = _typeInferenceResults;
        var funcKey = $"{funcDef.Id}/{funcDef.Args.Count}";
        if (_perFunctionTypeResults != null && _perFunctionTypeResults.TryGetValue(funcKey, out var funcResults))
            _typeInferenceResults = funcResults;

        // Allocate argument slots
        var argTypes = new FunnyType[funcDef.Args.Count];
        for (int i = 0; i < funcDef.Args.Count; i++) {
            var arg = funcDef.Args[i];
            var argType = arg.OutputType;
            argTypes[i] = argType;
            _localSlots[arg.Id] = _nextLocalSlot++;
        }

        var returnType = funcDef.OutputType;

        // Register the user function BEFORE compiling its body so that
        // recursive calls (fact(n-1) inside fact) resolve to Op.Call
        // instead of falling through to CALL_EXTERN on the prototype.
        // LocalsCount is patched after compilation.
        UserFunctions.Add(new UserFunc {
            EntryIP = entryIP,
            LocalsCount = 0, // patched below
            Name = funcDef.Id,
            ReturnType = returnType,
            ArgTypes = argTypes,
        });

        // Compile body
        CompileExpression(funcDef.Body);
        _e.Emit(Op.Return);

        // Patch LocalsCount now that we know how many locals the body used
        var uf = UserFunctions[funcId];
        uf.LocalsCount = _nextLocalSlot;
        UserFunctions[funcId] = uf;

        // Restore scope and type inference results
        _typeInferenceResults = savedTypeResults;
        _localSlots.Clear();
        foreach (var kv in savedSlots) _localSlots[kv.Key] = kv.Value;
        _nextLocalSlot = savedNextSlot;

        _e.PatchJump(jumpOver);
    }

    // ═══════════════════════════════════════════════════════════
    //  Expression compilation — dispatches to Visit methods
    // ═══════════════════════════════════════════════════════════

    private void CompileExpression(ISyntaxNode node) => node.Accept(this);

    private FunnyType GetOutputType(ISyntaxNode node) {
        if (node.OutputType.BaseType != BaseFunnyType.Empty)
            return node.OutputType;
        var ticState = _typeInferenceResults.GetSyntaxNodeTypeOrNull(node.OrderNumber);
        return ticState != null ? _typesConverter.Convert(ticState) : node.OutputType;
    }

    // ═══════════════════════════════════════════════════════════
    //  Visitor implementations (each returns dummy 0 byte)
    // ═══════════════════════════════════════════════════════════

    public byte Visit(ConstantSyntaxNode node) {
        EmitConstant(node.Value, GetOutputType(node));
        return 0;
    }

    public byte Visit(GenericIntSyntaxNode node) {
        EmitTypedIntConstant(node.Value, GetOutputType(node));
        return 0;
    }

    public byte Visit(IpAddressConstantSyntaxNode node) {
        _e.EmitWithArg(Op.LoadConstRef, (byte)_e.AddConstant(FunValue.FromRef(node.Value)));
        return 0;
    }

    public byte Visit(NamedIdSyntaxNode node) {
        if (node.IdType == NamedIdNodeType.Constant) {
            var varVal = (ConstantValueAndType)node.IdContent;
            EmitConstant(varVal.FunnyValue, varVal.Type);
            return 0;
        }

        var name = node.Id;
        var slot = GetOrAllocateSlot(name, isOutput: false, GetOutputType(node));
        _e.EmitWithArg(Op.LoadLocal, (byte)slot);
        return 0;
    }

    public byte Visit(BinOperatorSyntaxNode node) {
        var outputType = GetOutputType(node);
        var opType = ResolveBinOpType(node, outputType);

        CompileExpression(node.Left);
        EmitConvertIfNeeded(GetOutputType(node.Left), opType);

        CompileExpression(node.Right);
        EmitConvertIfNeeded(GetOutputType(node.Right), opType);

        EmitBinOp(node.Op, opType, outputType);
        return 0;
    }

    public byte Visit(UnaryOperatorSyntaxNode node) {
        var outputType = GetOutputType(node);

        CompileExpression(node.Operand);

        switch (node.Op) {
            case UnOp.Negate:
                if (IsRealType(outputType))
                    _e.Emit(Op.NegReal);
                else
                    _e.Emit(Op.NegInt);
                break;
            case UnOp.Not:
                _e.Emit(Op.Not);
                break;
            case UnOp.BitInverse:
                _e.Emit(Op.BitNot);
                break;
            default:
                throw new NotSupportedException($"VM: UnOp {node.Op} not yet supported");
        }
        return 0;
    }

    public byte Visit(IfThenElseSyntaxNode node) {
        var outputType = GetOutputType(node);
        var jumpToEnds = new List<int>();

        for (int i = 0; i < node.Ifs.Length; i++) {
            var ifCase = node.Ifs[i];

            // Compile condition
            CompileExpression(ifCase.Condition);

            // Jump if false to next branch
            int jumpToNext = _e.EmitJump(Op.JumpIfFalse);

            // Compile then-expression
            CompileExpression(ifCase.Expression);
            EmitConvertIfNeeded(GetOutputType(ifCase.Expression), outputType);

            // Jump to end
            jumpToEnds.Add(_e.EmitJump(Op.Jump));

            // Patch the "jump to next" to land here
            _e.PatchJump(jumpToNext);
        }

        // Compile else
        CompileExpression(node.ElseExpr);
        EmitConvertIfNeeded(GetOutputType(node.ElseExpr), outputType);

        // Patch all "jump to end"
        foreach (var addr in jumpToEnds)
            _e.PatchJump(addr);

        return 0;
    }

    public byte Visit(FunCallSyntaxNode node) {
        var id = node.Id;
        var args = _typeInferenceResults.GetResolvedCallArgsOrNull(node.OrderNumber) ?? node.Args;

        // Lambdas: compile as UserFunc + MakeClosure (no tree-walker fallback needed)

        // null coalesce: ??
        if (id == CoreFunNames.NullCoalesce && args.Length == 2) {
            CompileExpression(args[0]);
            CompileExpression(args[1]);
            _e.Emit(Op.Coalesce);
            return 0;
        }

        // Safe array access: ?[]
        if (id == CoreFunNames.SafeGetElementName) {
            CompileExpression(args[0]);
            CompileExpression(args[1]);
            _e.Emit(Op.GetElementSafe);
            return 0;
        }

        // Native arithmetic/comparison operators — bypass CALL_EXTERN for performance
        // Check both IsOperator (parser flag) and known operator names (SPS may not set IsOperator)
        if (args.Length == 2 && TryEmitNativeOperator(id, args, node))
            return 0;

        // Native built-in functions — bypass CALL_EXTERN for zero-boxing hot functions
        if (TryEmitNativeFunction(id, args))
            return 0;

        // User function call — check FIRST so VM-compiled user functions always
        // use Op.Call, even when node.ResolvedSignature points to a prototype.
        // This is critical for skipExpressionBuild: the prototype has no expression
        // tree, so CALL_EXTERN to it would fail at runtime.
        var ufc = _userFunctions?.Count ?? 0;
        for (int i = 0; i < ufc; i++) {
            if (string.Equals(_userFunctions[i].Name, id, StringComparison.OrdinalIgnoreCase)
                && _userFunctions[i].ArgTypes.Length == args.Length) {
                for (int j = 0; j < args.Length; j++)
                    CompileExpression(args[j]);
                _e.EmitWithArg(Op.Call, (byte)i);
                _e.Code.Add((byte)args.Length);
                return 0;
            }
        }

        // Resolve function (extern or generic)
        var someFunc = node.ResolvedSignature
            ?? _typeInferenceResults.GetResolvedCallSignatureOrNull(node.OrderNumber)
            ?? _functions.GetOrNull(id, args.Length);

        if (someFunc is IConcreteFunction concrete) {
            CompileCallExtern(concrete, args, node);
            return 0;
        }

        if (someFunc is IGenericFunction genericFunction) {
            var concreteFunc = ResolveGenericFunction(genericFunction, node);
            CompileCallExtern(concreteFunc, args, node);
            return 0;
        }

        throw new NotSupportedException($"VM: function {id}/{args.Length} not found");
    }

    public byte Visit(ArraySyntaxNode node) {
        var outputType = GetOutputType(node);
        var elementType = outputType.BaseType == BaseFunnyType.ArrayOf
            ? outputType.ArrayTypeSpecification.FunnyType
            : FunnyType.Any;

        for (int i = 0; i < node.Expressions.Count; i++) {
            CompileExpression(node.Expressions[i]);
            EmitConvertIfNeeded(GetOutputType(node.Expressions[i]), elementType);
        }

        _e.EmitWithArg(Op.NewArray, (byte)node.Expressions.Count);
        _e.Code.Add((byte)GetOrAddTypeTableEntry(elementType));
        return 0;
    }

    public byte Visit(StructInitSyntaxNode node) {
        var layoutId = GetOrCreateStructLayout(node);
        for (int i = 0; i < node.Fields.Count; i++)
            CompileExpression(node.Fields[i].Node);
        _e.EmitWithArg(Op.NewStruct, (byte)layoutId);
        _e.Code.Add((byte)node.Fields.Count);
        return 0;
    }

    public byte Visit(StructFieldAccessSyntaxNode node) {
        CompileExpression(node.Source);

        if (node.IsSafeAccess) {
            // Safe field access on optional — emit GetFieldSafe with field index + layout
            var fieldIdx = ResolveFieldIndex(node.Source, node.FieldName);
            var sourceType = GetOutputType(node.Source);
            while (sourceType.BaseType == BaseFunnyType.Optional)
                sourceType = sourceType.OptionalTypeSpecification.ElementType;
            var layoutId = GetOrCreateStructLayoutFromType(sourceType);
            _e.EmitWithArg(Op.GetFieldSafe, (byte)fieldIdx);
            _e.Code.Add((byte)layoutId);
            return 0;
        }

        var sourceType2 = GetOutputType(node.Source);
        if (sourceType2.BaseType == BaseFunnyType.Optional) {
            var fieldIdx = ResolveFieldIndex(node.Source, node.FieldName);
            var unwrapped = sourceType2;
            while (unwrapped.BaseType == BaseFunnyType.Optional)
                unwrapped = unwrapped.OptionalTypeSpecification.ElementType;
            var layoutId = GetOrCreateStructLayoutFromType(unwrapped);
            _e.EmitWithArg(Op.GetFieldSafe, (byte)fieldIdx);
            _e.Code.Add((byte)layoutId);
            return 0;
        }

        {
            var fieldIdx = ResolveFieldIndex(node.Source, node.FieldName);
            var layoutId = GetOrCreateStructLayoutFromType(sourceType2);
            _e.EmitWithArg(Op.GetField, (byte)fieldIdx);
            _e.Code.Add((byte)layoutId);
        }
        return 0;
    }

    public byte Visit(DefaultValueSyntaxNode node) {
        // Emit the default value for the type
        var type = GetOutputType(node);
        EmitDefaultValue(type);
        return 0;
    }

    public byte Visit(ComparisonChainSyntaxNode node) {
        // a < b < c  →  (a < b) and (b < c)
        // Compile first comparison
        CompileExpression(node.Operands[0]);
        CompileExpression(node.Operands[1]);

        var firstOpType = GetOutputType(node.Operands[0]);
        EmitComparisonOp(node.Operators[0].Type, firstOpType);

        for (int i = 1; i < node.Operators.Count; i++) {
            // We need to duplicate the right operand of the previous comparison
            // to use as the left of the next. But since we already consumed it,
            // we need to re-compile it. This is simpler than stack manipulation.
            CompileExpression(node.Operands[i]);
            CompileExpression(node.Operands[i + 1]);
            var opType = GetOutputType(node.Operands[i]);
            EmitComparisonOp(node.Operators[i].Type, opType);
            _e.Emit(Op.And);
        }
        return 0;
    }

    public byte Visit(ResultFunCallSyntaxNode node) {
        // Higher-order call: result(args) — fall back to pre-built tree-walker expression
        if (_preBuiltExpressions.TryGetValue(node.OrderNumber, out var preBuilt)) {
            var wrapper = new TreeWalkerWrapper(preBuilt);
            var funcId = GetOrRegisterExternFunc(wrapper);
            _e.EmitWithArg(Op.CallExtern, (byte)funcId);
            _e.Code.Add(0);
            return 0;
        }
        throw new NotSupportedException($"VM: ResultFunCallSyntaxNode not pre-built for node {node.OrderNumber}");
    }

    public byte Visit(AnonymFunctionSyntaxNode node) {
        var argNames = node.ArgumentsDefinition
            .Select(a => a is SyntaxParsing.SyntaxNodes.TypedVarDefSyntaxNode tv ? tv.Id
                       : a is SyntaxParsing.SyntaxNodes.NamedIdSyntaxNode ni ? ni.Id
                       : "it")
            .ToArray();
        return CompileLambda(node.Body, argNames, node);
    }

    public byte Visit(SuperAnonymFunctionSyntaxNode node) {
        // SuperAnonymFunction uses 'it' (or it1, it2, it3) as implicit args
        var argNames = ResolveAnonymArgNames(node);
        return CompileLambda(node.Body, argNames, node);
    }

    private string[] ResolveAnonymArgNames(SuperAnonymFunctionSyntaxNode node) {
        // Determine arg names from parent function type or body scan
        var parentType = node.OutputType;
        if (parentType.BaseType == BaseFunnyType.Fun) {
            int argc = parentType.FunTypeSpecification.Inputs.Length;
            if (argc == 1) return new[] { "it" };
            var names = new string[argc];
            for (int i = 0; i < argc; i++) names[i] = $"it{i + 1}";
            return names;
        }
        // Fallback: scan body for 'it' references
        bool hasIt = false;
        int maxItN = 0;
        node.Body.ComeOver(n => {
            if (n is SyntaxParsing.SyntaxNodes.NamedIdSyntaxNode named) {
                if (named.Id == "it") hasIt = true;
                else if (named.Id.StartsWith("it") && int.TryParse(named.Id.AsSpan(2), out int num))
                    maxItN = Math.Max(maxItN, num);
            }
            return SyntaxParsing.Visitors.DfsEnterResult.Continue;
        });
        if (hasIt) return new[] { "it" };
        if (maxItN > 0) {
            var names = new string[maxItN];
            for (int i = 0; i < maxItN; i++) names[i] = $"it{i + 1}";
            return names;
        }
        return new[] { "it" }; // default
    }

    /// <summary>Compile lambda body as anonymous UserFunc, emit MakeClosure.</summary>
    private byte CompileLambda(ISyntaxNode body, string[] argNames, ISyntaxNode lambdaNode) {
        // Save current scope
        var savedSlots = new Dictionary<string, int>(_localSlots, StringComparer.OrdinalIgnoreCase);
        var savedNextSlot = _nextLocalSlot;
        _localSlots.Clear();
        _nextLocalSlot = 0;

        // Jump over lambda body in main bytecode
        int jumpOver = _e.EmitJump(Op.Jump);
        int entryIP = _e.Position;
        int funcId = UserFunctions.Count;

        // Allocate arg slots
        var argTypes = new FunnyType[argNames.Length];
        for (int i = 0; i < argNames.Length; i++) {
            _localSlots[argNames[i]] = _nextLocalSlot++;
            // Resolve arg type from the typed syntax tree
            argTypes[i] = ResolveArgType(body, argNames[i], lambdaNode);
        }

        // Find captured variables: outer scope variables referenced in body
        var capturedVars = new List<(string Name, int OuterSlot)>();
        ScanCapturedVars(body, argNames, savedSlots, capturedVars);
        // Allocate slots for captured vars in lambda scope
        foreach (var (name, _) in capturedVars)
            _localSlots[name] = _nextLocalSlot++;

        var returnType = lambdaNode.OutputType.BaseType == BaseFunnyType.Fun
            ? lambdaNode.OutputType.FunTypeSpecification.Output
            : body.OutputType;

        // Register UserFunc BEFORE body compilation (for potential recursion, unlikely but safe)
        UserFunctions.Add(new UserFunc {
            EntryIP = entryIP,
            LocalsCount = 0, // patched below
            Name = $"__lambda_{funcId}__",
            ReturnType = returnType,
            ArgTypes = argTypes,
        });

        // Compile body
        CompileExpression(body);
        _e.Emit(Op.Return);

        // Patch LocalsCount
        var uf = UserFunctions[funcId];
        uf.LocalsCount = _nextLocalSlot;
        UserFunctions[funcId] = uf;

        // Restore scope
        _localSlots.Clear();
        foreach (var kv in savedSlots) _localSlots[kv.Key] = kv.Value;
        _nextLocalSlot = savedNextSlot;
        _e.PatchJump(jumpOver);

        // Emit MakeClosure: pushes BytecodeLambda onto stack
        _e.EmitWithArg(Op.MakeClosure, (byte)funcId);
        _e.Code.Add((byte)capturedVars.Count);
        foreach (var (_, outerSlot) in capturedVars)
            _e.Code.Add((byte)outerSlot);

        return 0;
    }

    private FunnyType ResolveArgType(ISyntaxNode body, string argName, ISyntaxNode lambdaNode) {
        // Try to get from the lambda's resolved Fun type
        if (lambdaNode.OutputType.BaseType == BaseFunnyType.Fun) {
            var funSpec = lambdaNode.OutputType.FunTypeSpecification;
            // For 'it' (index 0), it1/it2 (index from name)
            int idx = argName == "it" ? 0 : int.Parse(argName.Substring(2)) - 1;
            if (idx >= 0 && idx < funSpec.Inputs.Length)
                return funSpec.Inputs[idx];
        }
        // Fallback: scan body for the variable and use its type
        return ScanForVarType(body, argName) ?? FunnyType.Any;
    }

    private static FunnyType? ScanForVarType(ISyntaxNode node, string name) {
        if (node is SyntaxParsing.SyntaxNodes.NamedIdSyntaxNode named && named.Id == name
            && named.OutputType.BaseType != BaseFunnyType.Empty)
            return named.OutputType;
        foreach (var child in node.Children) {
            var result = ScanForVarType(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static void ScanCapturedVars(ISyntaxNode node, string[] argNames,
        Dictionary<string, int> outerSlots, List<(string, int)> captured) {
        if (node is SyntaxParsing.SyntaxNodes.NamedIdSyntaxNode named
            && named.IdType == SyntaxParsing.SyntaxNodes.NamedIdNodeType.Variable
            && !Array.Exists(argNames, a => a == named.Id)
            && outerSlots.TryGetValue(named.Id, out var slot)
            && !captured.Exists(c => c.Item1 == named.Id)) {
            captured.Add((named.Id, slot));
        }
        // Don't recurse into nested lambdas
        if (node is SyntaxParsing.SyntaxNodes.AnonymFunctionSyntaxNode
            || node is SyntaxParsing.SyntaxNodes.SuperAnonymFunctionSyntaxNode)
            return;
        foreach (var child in node.Children)
            ScanCapturedVars(child, argNames, outerSlots, captured);
    }

    public byte Visit(TryCatchSyntaxNode node) {
        // try expr catch fallback → if try succeeds, use result; if throws, use catch
        // Compile as: TRY_START, expr, JUMP over catch, CATCH_START, fallback
        // Using handler table approach from the spec.

        // For now: compile try body, then catch body. VM handles via handler table.
        // Simplified: emit try body inline, register handler, emit catch body.
        int tryStartIP = _e.Position;

        CompileExpression(node.TryExpr);
        int jumpOverCatch = _e.EmitJump(Op.Jump);

        int catchStartIP = _e.Position;

        // If catch has error variable (try expr catch(e) fallback)
        int errorSlot = -1;
        if (node.ErrorVariableName != null) {
            errorSlot = GetOrAllocateSlot(node.ErrorVariableName, false, GetOutputType(node.CatchExpr));
        }

        CompileExpression(node.CatchExpr);

        _e.PatchJump(jumpOverCatch);

        // Register exception handler
        _exceptionHandlers.Add(new ExceptionHandler {
            TryStartIP = tryStartIP,
            TryEndIP = catchStartIP,
            CatchStartIP = catchStartIP,
            ErrorVarSlot = errorSlot,
        });

        return 0;
    }

    public byte Visit(TypeDeclarationSyntaxNode node) {
        // Type declarations are removed by NamedTypeElaborator before compilation.
        // If we reach here, it's a bug in elaboration.
        throw new InvalidOperationException("VM: TypeDeclarationSyntaxNode should be removed by elaborator");
    }

    public byte Visit(NamedTypeConstructorSyntaxNode node) {
        // Named type constructors are desugared to StructInitSyntaxNode by elaborator.
        // If we reach here, it's a bug in elaboration.
        throw new InvalidOperationException("VM: NamedTypeConstructorSyntaxNode should be removed by elaborator");
    }

    // ── Not expressions — should never be visited ──

    public byte Visit(EquationSyntaxNode node) =>
        throw new InvalidOperationException("EquationSyntaxNode is not an expression");
    public byte Visit(IfCaseSyntaxNode node) =>
        throw new InvalidOperationException("IfCaseSyntaxNode is not an expression");
    public byte Visit(ListOfExpressionsSyntaxNode node) =>
        throw new InvalidOperationException("ListOfExpressionsSyntaxNode is not an expression");
    public byte Visit(SyntaxTree node) =>
        throw new InvalidOperationException("SyntaxTree is not an expression");
    public byte Visit(TypedVarDefSyntaxNode node) =>
        throw new InvalidOperationException("TypedVarDefSyntaxNode is not an expression");
    public byte Visit(UserFunctionDefinitionSyntaxNode node) =>
        throw new InvalidOperationException("UserFunctionDefinitionSyntaxNode is not an expression");
    public byte Visit(VarDefinitionSyntaxNode node) =>
        throw new InvalidOperationException("VarDefinitionSyntaxNode is not an expression");

    // ═══════════════════════════════════════════════════════════
    //  Private helpers
    // ═══════════════════════════════════════════════════════════

    private int AllocateSlot(string name, bool isOutput, FunnyType type) {
        if (_localSlots.TryGetValue(name, out var existing))
            return existing;
        var slot = _nextLocalSlot++;
        _localSlots[name] = slot;
        _variableSlots.Add(new VariableSlot {
            Name = name,
            Slot = slot,
            Type = type,
            IsOutput = isOutput,
        });
        return slot;
    }

    private int GetOrAllocateSlot(string name, bool isOutput, FunnyType type) {
        if (_localSlots.TryGetValue(name, out var slot))
            return slot;
        return AllocateSlot(name, isOutput, type);
    }

    /// <summary>
    /// Emits a constant value to the bytecode stream.
    /// </summary>
    private void EmitConstant(object value, FunnyType type) {
        switch (value) {
            case long l:
                EmitTypedIntConstant(l, type);
                break;
            case ulong u:
                EmitTypedIntConstant(u, type);
                break;
            case int i:
                EmitTypedIntConstant((long)i, type);
                break;
            case double d:
                _e.EmitWithArg(Op.LoadConstR, (byte)_e.AddConstant(FunValue.FromReal(d)));
                break;
            case bool b:
                _e.EmitWithArg(Op.LoadConstI, (byte)_e.AddConstant(FunValue.FromBool(b)));
                break;
            case char c:
                _e.EmitWithArg(Op.LoadConstI, (byte)_e.AddConstant(FunValue.FromChar(c)));
                break;
            case string s:
                // Real constants are stored as strings by the parser
                if (type.BaseType == BaseFunnyType.Real && double.TryParse(s,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    _e.EmitWithArg(Op.LoadConstR, (byte)_e.AddConstant(FunValue.FromReal(parsed)));
                else
                    _e.EmitWithArg(Op.LoadConstRef, (byte)_e.AddConstant(FunValue.FromRef(s)));
                break;
            case FunnyNone:
                _e.Emit(Op.LoadNone);
                break;
            default:
                _e.EmitWithArg(Op.LoadConstRef, (byte)_e.AddConstant(FunValue.FromRef(value)));
                break;
        }
    }

    private void EmitTypedIntConstant(object value, FunnyType type) {
        long longVal;
        if (value is long l) longVal = l;
        else if (value is ulong u) longVal = unchecked((long)u);
        else if (value is int i) longVal = i;
        else throw new NotSupportedException($"VM: unexpected int type {value?.GetType().Name}");

        if (type.BaseType == BaseFunnyType.Real) {
            _e.EmitWithArg(Op.LoadConstR, (byte)_e.AddConstant(FunValue.FromReal(longVal)));
        } else {
            _e.EmitWithArg(Op.LoadConstI, (byte)_e.AddConstant(FunValue.FromI64(longVal)));
            // No truncation for constants: value is already within type range.
            // Truncation only needed for computed results that may exceed target type range.
        }
    }

    /// <summary>
    /// Emits type conversion instruction if source and target types differ.
    /// </summary>
    private void EmitConvertIfNeeded(FunnyType from, FunnyType to) {
        if (from.Equals(to))
            return;

        // Int → Real
        if (IsIntegerType(from) && IsRealType(to)) {
            _e.Emit(Op.IntToReal);
            return;
        }

        // Real → Int
        if (IsRealType(from) && IsIntegerType(to)) {
            _e.Emit(Op.RealToInt);
            EmitTruncation(to);
            return;
        }

        // Integer narrowing/widening (all go through I64, truncation makes it narrower)
        if (IsIntegerType(from) && IsIntegerType(to)) {
            EmitTruncation(to);
            return;
        }

        // Primitive → Any: box the value into Ref so CALL_EXTERN can read it
        if (to.BaseType == BaseFunnyType.Any) {
            if (IsIntegerType(from))      { _e.Emit(Op.BoxInt); return; }
            if (IsRealType(from))         { _e.Emit(Op.BoxReal); return; }
            if (from.BaseType == BaseFunnyType.Bool) { _e.Emit(Op.BoxBool); return; }
            if (from.BaseType == BaseFunnyType.Char) { _e.Emit(Op.BoxInt); return; }
            // Composite types already stored in Ref — no boxing needed
            return;
        }

        // For composite types (arrays, structs, optionals) — no bytecode conversion needed;
        // the VM handles these as reference types.
    }

    private void EmitTruncation(FunnyType type) {
        switch (type.BaseType) {
            case BaseFunnyType.UInt8:  _e.Emit(Op.TruncU8);  break;
            case BaseFunnyType.UInt16: _e.Emit(Op.TruncU16); break;
            case BaseFunnyType.UInt32: _e.Emit(Op.TruncU32); break;
            case BaseFunnyType.Int16:  _e.Emit(Op.TruncI16); break;
            case BaseFunnyType.Int32:  _e.Emit(Op.TruncI32); break;
            // Int64, UInt64 — no truncation needed, I64 is native size
        }
    }

    private void EmitDefaultValue(FunnyType type) {
        switch (type.BaseType) {
            case BaseFunnyType.Bool:
            case BaseFunnyType.Char:
            case BaseFunnyType.UInt8:
            case BaseFunnyType.UInt16:
            case BaseFunnyType.UInt32:
            case BaseFunnyType.UInt64:
            case BaseFunnyType.Int16:
            case BaseFunnyType.Int32:
            case BaseFunnyType.Int64:
                _e.EmitWithArg(Op.LoadConstI, (byte)_e.AddConstant(FunValue.FromI64(0)));
                break;
            case BaseFunnyType.Real:
                _e.EmitWithArg(Op.LoadConstR, (byte)_e.AddConstant(FunValue.FromReal(0.0)));
                break;
            case BaseFunnyType.None:
            case BaseFunnyType.Optional:
                _e.Emit(Op.LoadNone);
                break;
            default:
                // For reference types (arrays, structs, etc.), emit null/none
                _e.Emit(Op.LoadNone);
                break;
        }
    }

    /// <summary>
    /// Determines the operation type for a binary operator.
    /// Arithmetic ops execute in either Int64 or Real domain.
    /// Comparison and logic ops execute in the domain of their operands.
    /// </summary>
    private FunnyType ResolveBinOpType(BinOperatorSyntaxNode node, FunnyType outputType) {
        switch (node.Op) {
            // Arithmetic: output type determines domain
            case BinOp.Add:
            case BinOp.Subtract:
            case BinOp.Multiply:
            case BinOp.DivideInt:
            case BinOp.Pow:
            case BinOp.Remainder:
                return outputType;
            case BinOp.DivideReal:
                return FunnyType.Real;
            // Comparison: operate in the domain of the wider operand
            case BinOp.Equal:
            case BinOp.NotEqual:
            case BinOp.Less:
            case BinOp.LessOrEqual:
            case BinOp.More:
            case BinOp.MoreOrEqual: {
                var leftType = GetOutputType(node.Left);
                var rightType = GetOutputType(node.Right);
                if (IsRealType(leftType) || IsRealType(rightType))
                    return FunnyType.Real;
                if (IsIntegerType(leftType) || IsIntegerType(rightType))
                    return FunnyType.Int64;
                // Reference comparison (bool, structs, etc.)
                return leftType;
            }
            // Logic: bool domain
            case BinOp.And:
            case BinOp.Or:
            case BinOp.Xor:
                return FunnyType.Bool;
            // Bitwise: integer domain
            case BinOp.BitAnd:
            case BinOp.BitOr:
            case BinOp.BitXor:
            case BinOp.BitShiftLeft:
            case BinOp.BitShiftRight:
                return FunnyType.Int64;
            default:
                return outputType;
        }
    }

    private void EmitBinOp(BinOp op, FunnyType opType, FunnyType outputType) {
        bool isReal = IsRealType(opType);

        switch (op) {
            case BinOp.Add:
                _e.Emit(isReal ? Op.AddReal : Op.AddInt);
                break;
            case BinOp.Subtract:
                _e.Emit(isReal ? Op.SubReal : Op.SubInt);
                break;
            case BinOp.Multiply:
                _e.Emit(isReal ? Op.MulReal : Op.MulInt);
                break;
            case BinOp.DivideReal:
                _e.Emit(Op.DivReal);
                break;
            case BinOp.DivideInt:
                _e.Emit(Op.DivInt);
                break;
            case BinOp.Pow:
                if (isReal)
                    _e.Emit(Op.PowReal);
                else {
                    // Integer power: convert to real, pow, convert back
                    // Actually, the VM has PowInt only as a placeholder — use real pow
                    _e.Emit(Op.PowInt);
                }
                break;
            case BinOp.Remainder:
                _e.Emit(isReal ? Op.ModReal : Op.ModInt);
                break;
            case BinOp.And:
                _e.Emit(Op.And);
                break;
            case BinOp.Or:
                _e.Emit(Op.Or);
                break;
            case BinOp.Xor:
                // XOR = (a OR b) AND NOT (a AND b), but we can use bitwise XOR on bools (0/1)
                _e.Emit(Op.BitXor);
                break;
            case BinOp.BitAnd:
                _e.Emit(Op.BitAnd);
                break;
            case BinOp.BitOr:
                _e.Emit(Op.BitOr);
                break;
            case BinOp.BitXor:
                _e.Emit(Op.BitXor);
                break;
            case BinOp.BitShiftLeft:
                _e.Emit(Op.Shl);
                break;
            case BinOp.BitShiftRight:
                _e.Emit(Op.Shr);
                break;
            case BinOp.Equal:
                if (isReal) _e.Emit(Op.EqReal);
                else if (IsIntegerType(opType) || opType.BaseType == BaseFunnyType.Bool || opType.BaseType == BaseFunnyType.Char)
                    _e.Emit(Op.EqInt);
                else _e.Emit(Op.EqRef);
                break;
            case BinOp.NotEqual:
                if (isReal) { _e.Emit(Op.EqReal); _e.Emit(Op.Not); }
                else if (IsIntegerType(opType) || opType.BaseType == BaseFunnyType.Bool || opType.BaseType == BaseFunnyType.Char)
                    _e.Emit(Op.NeqInt);
                else _e.Emit(Op.NeqRef);
                break;
            case BinOp.Less:
                if (isReal) _e.Emit(Op.LtReal);
                else if (IsIntegerType(opType) || opType.BaseType == BaseFunnyType.Char || opType.BaseType == BaseFunnyType.Bool)
                    _e.Emit(Op.LtInt);
                else EmitComparisonCallExtern(CoreFunNames.Less, opType);
                break;
            case BinOp.LessOrEqual:
                if (isReal) _e.Emit(Op.LteReal);
                else if (IsIntegerType(opType) || opType.BaseType == BaseFunnyType.Char || opType.BaseType == BaseFunnyType.Bool)
                    _e.Emit(Op.LteInt);
                else EmitComparisonCallExtern(CoreFunNames.LessOrEqual, opType);
                break;
            case BinOp.More:
                if (isReal) _e.Emit(Op.GtReal);
                else if (IsIntegerType(opType) || opType.BaseType == BaseFunnyType.Char || opType.BaseType == BaseFunnyType.Bool)
                    _e.Emit(Op.GtInt);
                else EmitComparisonCallExtern(CoreFunNames.More, opType);
                break;
            case BinOp.MoreOrEqual:
                if (isReal) _e.Emit(Op.GteReal);
                else if (IsIntegerType(opType) || opType.BaseType == BaseFunnyType.Char || opType.BaseType == BaseFunnyType.Bool)
                    _e.Emit(Op.GteInt);
                else EmitComparisonCallExtern(CoreFunNames.MoreOrEqual, opType);
                break;
        }

        // Truncate only if output type is narrower than I64 (U8, U16, U32, I16, I32).
        // For I64 and comparisons (Bool), no truncation needed.
        if (!isReal && NeedsTruncation(outputType))
            EmitTruncation(outputType);
    }

    private bool TryEmitNativeFunction(string funcName, ISyntaxNode[] args) {
        if (funcName == "max" && args.Length == 2) {
            var t = GetOutputType(args[0]);
            if (IsIntegerType(t)) {
                CompileExpression(args[0]); CompileExpression(args[1]);
                _e.Emit(Op.MaxInt); return true;
            }
            if (IsRealType(t)) {
                CompileExpression(args[0]); CompileExpression(args[1]);
                _e.Emit(Op.MaxReal); return true;
            }
        }
        if (funcName == "min" && args.Length == 2) {
            var t = GetOutputType(args[0]);
            if (IsIntegerType(t)) {
                CompileExpression(args[0]); CompileExpression(args[1]);
                _e.Emit(Op.MinInt); return true;
            }
            if (IsRealType(t)) {
                CompileExpression(args[0]); CompileExpression(args[1]);
                _e.Emit(Op.MinReal); return true;
            }
        }
        if (funcName == "abs" && args.Length == 1) {
            var t = GetOutputType(args[0]);
            if (IsIntegerType(t)) {
                CompileExpression(args[0]);
                _e.Emit(Op.AbsInt); return true;
            }
            if (IsRealType(t)) {
                CompileExpression(args[0]);
                _e.Emit(Op.AbsReal); return true;
            }
        }
        if (funcName == CoreFunNames.ToText && args.Length == 1) {
            var t = GetOutputType(args[0]);
            if (IsIntegerType(t)) {
                CompileExpression(args[0]);
                _e.Emit(Op.ToTextInt); return true;
            }
            if (IsRealType(t)) {
                CompileExpression(args[0]);
                _e.Emit(Op.ToTextReal); return true;
            }
        }
        return false;
    }

    private void EmitComparisonCallExtern(string opName, FunnyType operandType) {
        var func = _functions.GetOrNull(opName, 2);
        if (func is IGenericFunction gf) {
            var concrete = gf.CreateConcrete(new[] { operandType }, _dialect);
            var funcId = GetOrRegisterExternFunc(concrete);
            _e.EmitWithArg(Op.CallExtern, (byte)funcId);
            _e.Code.Add(2);
        } else if (func is IConcreteFunction cf) {
            var funcId = GetOrRegisterExternFunc(cf);
            _e.EmitWithArg(Op.CallExtern, (byte)funcId);
            _e.Code.Add(2);
        }
    }

    private void EmitComparisonOp(TokType tokType, FunnyType operandType) {
        bool isReal = IsRealType(operandType);
        bool isInt = IsIntegerType(operandType) || operandType.BaseType == BaseFunnyType.Char || operandType.BaseType == BaseFunnyType.Bool;
        if (!isReal && !isInt) {
            // Non-numeric comparison (e.g., text arrays) — use CALL_EXTERN
            string opName = tokType switch {
                TokType.Less => CoreFunNames.Less,
                TokType.LessOrEqual => CoreFunNames.LessOrEqual,
                TokType.More => CoreFunNames.More,
                TokType.MoreOrEqual => CoreFunNames.MoreOrEqual,
                _ => throw new NotSupportedException($"VM: comparison chain operator {tokType} not supported"),
            };
            var func = _functions.GetOrNull(opName, 2);
            if (func is IGenericFunction gf) {
                var concrete = gf.CreateConcrete(new[] { operandType }, _dialect);
                var funcId = GetOrRegisterExternFunc(concrete);
                _e.EmitWithArg(Op.CallExtern, (byte)funcId);
                _e.Code.Add(2);
            } else if (func is IConcreteFunction cf) {
                var funcId = GetOrRegisterExternFunc(cf);
                _e.EmitWithArg(Op.CallExtern, (byte)funcId);
                _e.Code.Add(2);
            } else {
                throw new NotSupportedException($"VM: comparison for type {operandType} not supported");
            }
            return;
        }
        switch (tokType) {
            case TokType.Less:
                _e.Emit(isReal ? Op.LtReal : Op.LtInt);
                break;
            case TokType.LessOrEqual:
                _e.Emit(isReal ? Op.LteReal : Op.LteInt);
                break;
            case TokType.More:
                _e.Emit(isReal ? Op.GtReal : Op.GtInt);
                break;
            case TokType.MoreOrEqual:
                _e.Emit(isReal ? Op.GteReal : Op.GteInt);
                break;
            default:
                throw new NotSupportedException($"VM: comparison chain operator {tokType} not supported");
        }
    }

    private void CompileCallExtern(IConcreteFunction function, ISyntaxNode[] args, FunCallSyntaxNode node) {
        // Compile arguments
        for (int i = 0; i < args.Length; i++) {
            CompileExpression(args[i]);
            EmitConvertIfNeeded(GetOutputType(args[i]), function.ArgTypes[i]);
        }

        var funcId = GetOrRegisterExternFunc(function);
        _e.EmitWithArg(Op.CallExtern, (byte)funcId);
        _e.Code.Add((byte)args.Length);
    }

    private int GetOrRegisterExternFunc(IConcreteFunction function) {
        var name = function.Name;
        var arity = function.ArgTypes.Length;

        if (!ExternFuncIds.TryGetValue(name, out var byArity)) {
            byArity = new Dictionary<int, int>();
            ExternFuncIds[name] = byArity;
        }

        // Check if an extern with same name, arity, and matching types already exists
        if (byArity.TryGetValue(arity, out var existingId)) {
            var existing = ExternFunctions[existingId];
            if (existing.ReturnType.Equals(function.ReturnType) && ArgsMatch(existing.ArgTypes, function.ArgTypes))
                return existingId;
        }

        // Register new extern function — use unique id keyed by name+arity+types
        var id = ExternFunctions.Count;
        ExternFunctions.Add(new ExternFunc {
            Function = function,
            ReturnType = function.ReturnType,
            ArgTypes = function.ArgTypes,
            ArityKind = function is Interpretation.Functions.FunctionWithSingleArg ? (byte)1
                      : function is Interpretation.Functions.FunctionWithTwoArgs ? (byte)2
                      : (byte)0,
        });

        // Store with a unique key (might overwrite if same name+arity but different types)
        byArity[arity] = id;
        return id;
    }

    private static bool ArgsMatch(FunnyType[] a, FunnyType[] b) {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (!a[i].Equals(b[i])) return false;
        return true;
    }

    private IConcreteFunction ResolveGenericFunction(IGenericFunction genericFunction, FunCallSyntaxNode node) {
        FunnyType[] genericArgs;
        var genericTypes = _typeInferenceResults.GetGenericCallArguments(node.OrderNumber);
        if (genericTypes == null) {
            var recCallSignature = _typeInferenceResults.GetRecursiveCallOrNull(node.OrderNumber);
            if (recCallSignature == null)
                throw new InvalidOperationException($"VM: generic function {node.Id}/{node.Args.Length} types not resolved");
            var varTypeCallSignature = _typesConverter.Convert(recCallSignature);
            genericArgs = genericFunction.CalcGenericArgTypeList(varTypeCallSignature.FunTypeSpecification);
        } else {
            genericArgs = new FunnyType[genericTypes.Length];
            for (int i = 0; i < genericTypes.Length; i++)
                genericArgs[i] = _typesConverter.Convert(genericTypes[i]);
        }
        return genericFunction.CreateConcrete(genericArgs, _dialect);
    }

    private int GetOrCreateStructLayout(StructInitSyntaxNode node) {
        var names = new string[node.Fields.Count];
        var types = new FunnyType[node.Fields.Count];
        for (int i = 0; i < node.Fields.Count; i++) {
            names[i] = node.Fields[i].Name;
            types[i] = GetOutputType(node.Fields[i].Node);
        }

        var key = MakeStructLayoutKey(names);
        if (StructLayoutIds.TryGetValue(key, out var id))
            return id;

        id = StructLayouts.Count;
        StructLayouts.Add(new StructLayout { FieldNames = names, FieldTypes = types });
        StructLayoutIds[key] = id;
        return id;
    }

    private int GetOrCreateStructLayoutFromType(FunnyType type) {
        if (type.BaseType != BaseFunnyType.Struct)
            return 0;

        var spec = type.StructTypeSpecification;
        var names = new List<string>();
        var types = new List<FunnyType>();
        foreach (var (name, fieldType) in spec) {
            names.Add(name);
            types.Add(fieldType);
        }

        var key = MakeStructLayoutKey(names.ToArray());
        if (StructLayoutIds.TryGetValue(key, out var id))
            return id;

        id = StructLayouts.Count;
        StructLayouts.Add(new StructLayout { FieldNames = names.ToArray(), FieldTypes = types.ToArray() });
        StructLayoutIds[key] = id;
        return id;
    }

    private int ResolveFieldIndex(ISyntaxNode source, string fieldName) {
        var sourceType = GetOutputType(source);
        // Unwrap optional to get to the struct
        while (sourceType.BaseType == BaseFunnyType.Optional)
            sourceType = sourceType.OptionalTypeSpecification.ElementType;

        if (sourceType.BaseType == BaseFunnyType.Struct) {
            int idx = 0;
            foreach (var (name, _) in sourceType.StructTypeSpecification) {
                if (string.Equals(name, fieldName, StringComparison.InvariantCultureIgnoreCase))
                    return idx;
                idx++;
            }
        }

        // Field not found in struct type — return 0 as fallback (runtime will handle)
        return 0;
    }

    /// <summary>
    /// Scan syntax tree for input variables (NamedId nodes with Variable type that aren't outputs)
    /// and allocate VM slots for them. Needed when tree-walker fallback captures inputs.
    /// </summary>
    private void AllocateInputVariables(SyntaxTree tree) {
        foreach (var node in tree.Nodes) {
            if (node is UserFunctionDefinitionSyntaxNode) continue;
            ScanForInputVariables(node);
        }
    }

    private void ScanForInputVariables(ISyntaxNode node) {
        if (node is SyntaxParsing.SyntaxNodes.NamedIdSyntaxNode named
            && named.IdType == SyntaxParsing.SyntaxNodes.NamedIdNodeType.Variable
            && named.OutputType.BaseType != BaseFunnyType.Empty) {
            GetOrAllocateSlot(named.Id, isOutput: false, named.OutputType);
        }
        foreach (var child in node.Children)
            ScanForInputVariables(child);
    }

    private int GetOrAddTypeTableEntry(FunnyType type) {
        if (TypeTableIndex.TryGetValue(type, out var idx)) return idx;
        idx = TypeTable.Count;
        TypeTable.Add(type);
        TypeTableIndex[type] = idx;
        return idx;
    }

    private static string MakeStructLayoutKey(string[] names) => string.Join(",", names);

    /// <summary>
    /// Peephole optimizer: scan bytecode for common patterns and replace with fused superinstructions.
    /// Operates on the raw byte list in-place.
    /// </summary>
    private static void PeepholeOptimize(BytecodeEmitter e) {
        var code = e.Code;

        // Skip peephole if code contains jumps or complex ops — RemoveRange breaks addresses.
        for (int j = 0; j < code.Count;) {
            var op = (Op)code[j];
            if (op == Op.Jump || op == Op.JumpIfFalse || op == Op.JumpIfTrue
                || op == Op.Call || op == Op.TailCall
                || op == Op.NewStruct || op == Op.GetField || op == Op.GetFieldSafe
                || op == Op.CallExtern || op == Op.NewArray || op == Op.GetElement
                || op == Op.MakeClosure)
                return; // bail out — too complex for simple peephole
            j += InstructionWidth(op);
        }

        int i = 0;
        while (i < code.Count - 4) {
            var op1 = (Op)code[i];
            // Pattern: LoadConstI #a, LoadLocal #b, MulInt → MulLocalConstI #b, #a
            if (op1 == Op.LoadConstI && i + 4 < code.Count
                && (Op)code[i + 2] == Op.LoadLocal && (Op)code[i + 4] == Op.MulInt) {
                var constIdx = code[i + 1];
                var localSlot = code[i + 3];
                code[i] = (byte)Op.MulLocalConstI;
                code[i + 1] = localSlot;
                code[i + 2] = constIdx;
                code.RemoveRange(i + 3, 2); // remove LoadLocal arg + MulInt
                continue;
            }
            // Pattern: LoadConstI #a, LoadLocal #b, AddInt → AddLocalConstI #b, #a
            if (op1 == Op.LoadConstI && i + 4 < code.Count
                && (Op)code[i + 2] == Op.LoadLocal && (Op)code[i + 4] == Op.AddInt) {
                var constIdx = code[i + 1];
                var localSlot = code[i + 3];
                code[i] = (byte)Op.AddLocalConstI;
                code[i + 1] = localSlot;
                code[i + 2] = constIdx;
                code.RemoveRange(i + 3, 2);
                continue;
            }
            // Pattern: LoadLocal #a, LoadConstI #b, MulInt → MulLocalConstI #a, #b
            if (op1 == Op.LoadLocal && i + 4 < code.Count
                && (Op)code[i + 2] == Op.LoadConstI && (Op)code[i + 4] == Op.MulInt) {
                var localSlot = code[i + 1];
                var constIdx = code[i + 3];
                code[i] = (byte)Op.MulLocalConstI;
                code[i + 1] = localSlot;
                code[i + 2] = constIdx;
                code.RemoveRange(i + 3, 2);
                continue;
            }
            // Pattern: LoadLocal #a, LoadConstI #b, AddInt → AddLocalConstI #a, #b
            if (op1 == Op.LoadLocal && i + 4 < code.Count
                && (Op)code[i + 2] == Op.LoadConstI && (Op)code[i + 4] == Op.AddInt) {
                var localSlot = code[i + 1];
                var constIdx = code[i + 3];
                code[i] = (byte)Op.AddLocalConstI;
                code[i + 1] = localSlot;
                code[i + 2] = constIdx;
                code.RemoveRange(i + 3, 2);
                continue;
            }
            // Pattern: LoadConstI #a, AddInt → AddTopConstI #a
            if (op1 == Op.LoadConstI && i + 2 < code.Count && (Op)code[i + 2] == Op.AddInt) {
                var constIdx = code[i + 1];
                code[i] = (byte)Op.AddTopConstI;
                code[i + 1] = constIdx;
                code.RemoveAt(i + 2); // remove AddInt
                continue;
            }
            // Pattern: LoadConstI #a, MulInt → MulTopConstI #a
            if (op1 == Op.LoadConstI && i + 2 < code.Count && (Op)code[i + 2] == Op.MulInt) {
                var constIdx = code[i + 1];
                code[i] = (byte)Op.MulTopConstI;
                code[i + 1] = constIdx;
                code.RemoveAt(i + 2);
                continue;
            }
            // Pattern: StoreLocal #a, Halt → StoreHalt #a
            if (op1 == Op.StoreLocal && i + 2 < code.Count && (Op)code[i + 2] == Op.Halt) {
                code[i] = (byte)Op.StoreHalt;
                code.RemoveAt(i + 2); // remove Halt
                continue;
            }
            i += InstructionWidth((Op)code[i]);
        }
    }

    /// <summary>Simulate stack depth to compute maximum. Conservative estimate.</summary>
    private static int ComputeMaxStackDepth(byte[] code) {
        int sp = 0, max = 0;
        int ip = 0;
        while (ip < code.Length) {
            var op = (Op)code[ip];
            int width = InstructionWidth(op);
            switch (op) {
                // push: sp++
                case Op.LoadConstI: case Op.LoadConstR: case Op.LoadConstRef:
                case Op.LoadLocal: case Op.LoadNone: case Op.Dup:
                case Op.AddLocalConstI: case Op.SubLocalConstI: case Op.MulLocalConstI:
                case Op.AddConstConstI: case Op.MulConstConstI:
                case Op.AddLocalConstR: case Op.MulLocalConstR:
                    sp++; break;
                // pop: sp--
                case Op.StoreLocal: case Op.Pop: case Op.StoreHalt:
                    sp--; break;
                // binary (pop 2, push 1 = net -1):
                case Op.AddInt: case Op.SubInt: case Op.MulInt: case Op.DivInt:
                case Op.ModInt: case Op.PowInt:
                case Op.AddReal: case Op.SubReal: case Op.MulReal: case Op.DivReal:
                case Op.ModReal: case Op.PowReal:
                case Op.EqInt: case Op.NeqInt: case Op.LtInt: case Op.LteInt:
                case Op.GtInt: case Op.GteInt: case Op.LtUint:
                case Op.EqReal: case Op.NeqReal: case Op.LtReal: case Op.LteReal:
                case Op.GtReal: case Op.GteReal:
                case Op.EqRef: case Op.NeqRef:
                case Op.And: case Op.Or:
                case Op.BitAnd: case Op.BitOr: case Op.BitXor: case Op.Shl: case Op.Shr:
                case Op.MaxInt: case Op.MinInt: case Op.MaxReal: case Op.MinReal:
                case Op.Coalesce: case Op.GetElement: case Op.GetElementSafe:
                    sp--; break;
                // CallExtern: pop argc, push 1 (net = 1 - argc)
                case Op.CallExtern:
                    sp -= code[ip + 2]; sp++; break; // argc at ip+2
                case Op.Call: case Op.TailCall:
                    sp -= code[ip + 2]; sp++; break;
                // NewArray: pop count, push 1
                case Op.NewArray:
                    sp -= code[ip + 1]; sp++; break; // count at ip+1
                // NewStruct: pop fieldCount, push 1
                case Op.NewStruct:
                    sp -= code[ip + 2]; sp++; break; // fieldCount at ip+2
                // Unary (no sp change): Not, NegInt, NegReal, BitNot, AbsInt, AbsReal, etc.
                // GetField/GetFieldSafe: pop 1, push 1 = no change
                // Jumps: no stack change (JumpIfFalse/True pop 1)
                case Op.JumpIfFalse: case Op.JumpIfTrue:
                    sp--; break;
                // AddTopConstI/R, MulTopConstI/R: no sp change (modify top in-place)
                // BoxInt/BoxReal/BoxBool: no sp change
                // IsNone, Unwrap, ToTextInt, ToTextReal: no sp change
                // Return: pop 1 (handled by call frame, conservative)
                case Op.Return:
                    sp--; break;
                case Op.Halt:
                    ip = code.Length; continue;
            }
            if (sp > max) max = sp;
            ip += width;
        }
        return max;
    }

    private static int InstructionWidth(Op op) => op switch {
        // 2-byte: opcode + 1 arg
        Op.LoadConstI or Op.LoadConstR or Op.LoadConstRef
            or Op.LoadLocal or Op.StoreLocal or Op.StoreHalt
            or Op.AddTopConstI or Op.MulTopConstI or Op.AddTopConstR or Op.MulTopConstR => 2,
        // 3-byte: opcode + 2 args
        Op.Jump or Op.JumpIfFalse or Op.JumpIfTrue
            or Op.Call or Op.TailCall or Op.CallExtern
            or Op.NewStruct or Op.GetField or Op.GetFieldSafe or Op.NewArray
            or Op.AddLocalConstI or Op.SubLocalConstI or Op.MulLocalConstI
            or Op.AddConstConstI or Op.MulConstConstI
            or Op.AddLocalConstR or Op.MulLocalConstR => 3,
        // 1-byte: all others
        _ => 1,
    };

    private static bool HasLambdaArg(ISyntaxNode[] args) {
        foreach (var arg in args)
            if (arg is SyntaxParsing.SyntaxNodes.AnonymFunctionSyntaxNode
                || arg is SyntaxParsing.SyntaxNodes.SuperAnonymFunctionSyntaxNode)
                return true;
        return false;
    }

    /// <summary>
    /// Fall back to tree-walker for a function call with lambda args.
    /// Compiles non-lambda args via bytecode, builds lambda via tree-walker,
    /// then emits CALL_EXTERN with all args properly mixed.
    /// </summary>
    /// <summary>
    /// Fall back to tree-walker for the ENTIRE function call (including lambda args).
    /// Builds the complete call as IExpressionNode, wraps as zero-arg CALL_EXTERN.
    /// Non-lambda args that reference locals need to be passed via the wrapper.
    /// </summary>
    private byte CompileViaTreeWalker(SyntaxParsing.SyntaxNodes.FunCallSyntaxNode node) {
        // Check if this expression was pre-built by VMRuntime
        if (_preBuiltExpressions.TryGetValue(node.OrderNumber, out var preBuilt)) {
            // Wrap as CALL_EXTERN with 0 args — all state captured in closure
            var wrapper = new TreeWalkerWrapper(preBuilt);
            var funcId = GetOrRegisterExternFunc(wrapper);
            _e.EmitWithArg(Op.CallExtern, (byte)funcId);
            _e.Code.Add(0);
            return 0;
        }

        // If not pre-built, the entire containing equation was pre-built.
        // Check parent equation's expression node.
        throw new NotSupportedException($"VM: lambda call not pre-built: {node.Id}. Ensure VMRuntime.Build pre-builds lambda expressions.");
    }

    /// <summary>Wraps a tree-walker IExpressionNode as IConcreteFunction for CALL_EXTERN.</summary>
    internal class TreeWalkerWrapper : Interpretation.Functions.FunctionWithManyArguments {
        private readonly Interpretation.Nodes.IExpressionNode _node;
        internal (Runtime.VariableSource Var, int Slot, FunnyType Type)[] CapturedVarBridges;
        internal FunValue[] Locals;
        public TreeWalkerWrapper(Interpretation.Nodes.IExpressionNode node)
            : base("__tw_wrapper__", node.Type, System.Array.Empty<FunnyType>()) {
            _node = node;
        }
        public override object Calc(object[] args) {
            // Sync captured variables from VM locals before evaluating
            if (Locals != null && CapturedVarBridges != null) {
                foreach (var (varSource, slot, type) in CapturedVarBridges)
                    varSource.SetFunnyValueUnsafe(Locals[slot].Box(type));
            }
            return _node.Calc();
        }
        public override Interpretation.Functions.IConcreteFunction Clone(
            Interpretation.Nodes.ICloneContext context) => this;
    }

    private static bool IsRealType(FunnyType type) => type.BaseType == BaseFunnyType.Real;

    private static bool IsIntegerType(FunnyType type) =>
        type.BaseType >= BaseFunnyType.UInt8 && type.BaseType <= BaseFunnyType.Int64;

    /// <summary>True if type needs truncation (narrower than I64). I32, I64, U64, Bool don't.</summary>
    private static bool NeedsTruncation(FunnyType type) => type.BaseType switch {
        BaseFunnyType.UInt8  => true,
        BaseFunnyType.UInt16 => true,
        BaseFunnyType.Int16  => true,
        _ => false, // I32, I64, U32, U64, Bool — fit in I64 natively
    };

    /// <summary>
    /// Try to emit native VM opcodes for arithmetic/comparison operators.
    /// Returns true if handled natively (no CALL_EXTERN needed).
    /// This is the key performance optimization: avoids boxing/unboxing for every arithmetic op.
    /// </summary>
    private bool TryEmitNativeOperator(string opName, ISyntaxNode[] args, FunCallSyntaxNode node) {
        var outputType = GetOutputType(node);
        var leftType = GetOutputType(args[0]);
        var rightType = GetOutputType(args[1]);
        bool isReal = IsRealType(outputType) || IsRealType(leftType) || IsRealType(rightType);
        bool isInt = !isReal && (IsIntegerType(outputType) || IsIntegerType(leftType) || IsIntegerType(rightType));

        if (!isReal && !isInt) return false; // not numeric — can't use native opcodes

        Op? op = opName switch {
            CoreFunNames.Add        => isReal ? Op.AddReal : Op.AddInt,
            CoreFunNames.Substract  => isReal ? Op.SubReal : Op.SubInt,
            CoreFunNames.Multiply   => isReal ? Op.MulReal : Op.MulInt,
            CoreFunNames.DivideReal => Op.DivReal,
            CoreFunNames.DivideInt  => Op.DivInt,
            CoreFunNames.Remainder  => isReal ? Op.ModReal : Op.ModInt,
            CoreFunNames.Pow        => isReal ? Op.PowReal : Op.PowInt,
            CoreFunNames.More       => isReal ? Op.GtReal : Op.GtInt,
            CoreFunNames.Less       => isReal ? Op.LtReal : Op.LtInt,
            CoreFunNames.MoreOrEqual => isReal ? Op.GteReal : Op.GteInt,
            CoreFunNames.LessOrEqual => isReal ? Op.LteReal : Op.LteInt,
            CoreFunNames.Equal      => isReal ? Op.EqReal : Op.EqInt,
            CoreFunNames.NotEqual   => isReal ? Op.NeqReal : Op.NeqInt,
            CoreFunNames.BitAnd     => isInt ? Op.BitAnd : null,
            CoreFunNames.BitOr      => isInt ? Op.BitOr : null,
            CoreFunNames.BitXor     => isInt ? Op.BitXor : null,
            CoreFunNames.BitShiftLeft  => isInt ? Op.Shl : null,
            CoreFunNames.BitShiftRight => isInt ? Op.Shr : null,
            CoreFunNames.And        => Op.And,
            CoreFunNames.Or         => Op.Or,
            _ => null,
        };

        if (op == null) return false;

        var domain = isReal ? FunnyType.Real : FunnyType.Int64;

        CompileExpression(args[0]);
        EmitConvertIfNeeded(leftType, domain);

        CompileExpression(args[1]);
        EmitConvertIfNeeded(rightType, domain);

        _e.Emit(op.Value);
        return true;
    }
}

/// <summary>
/// Builds a byte[] code buffer with a constants pool.
/// Provides helpers for emitting opcodes, operands, and patching jumps.
/// </summary>
internal sealed class BytecodeEmitter {
    internal readonly List<byte> Code = new(32);     // typical simple expr = 7-20 bytes
    private readonly List<FunValue> _constants = new(4); // typical 1-4 constants

    /// <summary>Current write position in the code buffer.</summary>
    public int Position => Code.Count;

    public void Emit(Op op) => Code.Add((byte)op);

    public void EmitWithArg(Op op, byte arg) {
        Code.Add((byte)op);
        Code.Add(arg);
    }

    /// <summary>
    /// Emits a jump instruction with a placeholder 16-bit address.
    /// Returns the index of the address bytes so they can be patched later.
    /// </summary>
    public int EmitJump(Op op) {
        Code.Add((byte)op);
        var patchAddr = Code.Count;
        Code.Add(0); // low byte
        Code.Add(0); // high byte
        return patchAddr;
    }

    /// <summary>
    /// Patches a previously emitted jump to target the current code position.
    /// </summary>
    public void PatchJump(int patchAddr) {
        var target = Code.Count;
        Code[patchAddr] = (byte)(target & 0xFF);
        Code[patchAddr + 1] = (byte)((target >> 8) & 0xFF);
    }

    /// <summary>Adds a constant to the pool and returns its index.</summary>
    public int AddConstant(FunValue value) {
        // Check for duplicate (small pool — linear scan is fine)
        for (int i = 0; i < _constants.Count; i++) {
            if (_constants[i].I64 == value.I64 && _constants[i].Ref == value.Ref)
                return i;
        }
        var index = _constants.Count;
        _constants.Add(value);
        return index;
    }

    public byte[] ToArray() => Code.ToArray();
    public FunValue[] ConstantsToArray() => _constants.ToArray();
}
