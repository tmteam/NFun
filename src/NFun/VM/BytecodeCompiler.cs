using System;
using System.Collections.Generic;
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
    private readonly TypeInferenceResults _typeInferenceResults;
    private readonly TicTypesConverter _typesConverter;
    private readonly DialectSettings _dialect;

    // Variable name → local slot
    private readonly Dictionary<string, int> _localSlots = new(StringComparer.OrdinalIgnoreCase);
    private int _nextLocalSlot;

    // Output variables (equations): name → slot
    private readonly List<VariableSlot> _variableSlots = new();

    // Extern functions registry (built-in .NET functions called via CALL_EXTERN)
    private readonly List<ExternFunc> _externFunctions = new();
    private readonly Dictionary<string, Dictionary<int, int>> _externFuncIds = new();

    // User-defined functions
    private readonly List<UserFunc> _userFunctions = new();

    // Struct layouts
    private readonly List<StructLayout> _structLayouts = new();
    private readonly Dictionary<string, int> _structLayoutIds = new();

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
        DialectSettings dialect) {
        var compiler = new BytecodeCompiler(functions, typeInferenceResults, typesConverter, dialect);
        return compiler.CompileTree(tree);
    }

    private CompiledProgram CompileTree(SyntaxTree tree) {
        // First pass: allocate slots for all equation outputs and collect user function defs
        var equations = new List<EquationSyntaxNode>();
        var userFuncDefs = new List<UserFunctionDefinitionSyntaxNode>();

        foreach (var node in tree.Nodes) {
            if (node is EquationSyntaxNode eq) {
                equations.Add(eq);
                var eqType = eq.OutputType.BaseType != BaseFunnyType.Empty
                    ? eq.OutputType : GetOutputType(eq.Expression);
                AllocateSlot(eq.Id, isOutput: true, eqType);
            }
            else if (node is UserFunctionDefinitionSyntaxNode funcDef) {
                userFuncDefs.Add(funcDef);
            }
        }

        // Compile user function definitions (emit code bodies, register in UserFunctions table)
        foreach (var funcDef in userFuncDefs) {
            CompileUserFunction(funcDef);
        }

        // Compile each equation: emit expression code, then STORE_LOCAL to output slot
        foreach (var eq in equations) {
            CompileExpression(eq.Expression);
            var targetType = eq.OutputType.BaseType != BaseFunnyType.Empty
                ? eq.OutputType : GetOutputType(eq.Expression);
            EmitConvertIfNeeded(GetOutputType(eq.Expression), targetType);
            var slot = _localSlots[eq.Id];
            _e.EmitWithArg(Op.StoreLocal, (byte)slot);
        }

        _e.Emit(Op.Halt);

        return new CompiledProgram {
            Code = _e.ToArray(),
            Constants = _e.ConstantsToArray(),
            StructLayouts = _structLayouts.ToArray(),
            ExternFunctions = _externFunctions.ToArray(),
            UserFunctions = _userFunctions.ToArray(),
            Variables = _variableSlots.ToArray(),
            ExceptionHandlers = Array.Empty<ExceptionHandler>(),
            LocalsCount = _nextLocalSlot,
        };
    }

    private void CompileUserFunction(UserFunctionDefinitionSyntaxNode funcDef) {
        // Placeholder — jump over function body
        int jumpOver = _e.EmitJump(Op.Jump);

        int entryIP = _e.Position;
        var funcId = _userFunctions.Count;

        // Save current slot state — function has its own scope
        var savedSlots = new Dictionary<string, int>(_localSlots, StringComparer.OrdinalIgnoreCase);
        var savedNextSlot = _nextLocalSlot;
        _localSlots.Clear();
        _nextLocalSlot = 0;

        // Allocate argument slots
        var argTypes = new FunnyType[funcDef.Args.Count];
        for (int i = 0; i < funcDef.Args.Count; i++) {
            var arg = funcDef.Args[i];
            var argType = arg.OutputType;
            argTypes[i] = argType;
            _localSlots[arg.Id] = _nextLocalSlot++;
        }

        // Compile body
        CompileExpression(funcDef.Body);
        _e.Emit(Op.Return);

        var funcLocals = _nextLocalSlot;

        // Restore scope
        _localSlots.Clear();
        foreach (var kv in savedSlots) _localSlots[kv.Key] = kv.Value;
        _nextLocalSlot = savedNextSlot;

        _e.PatchJump(jumpOver);

        var returnType = funcDef.OutputType;

        _userFunctions.Add(new UserFunc {
            EntryIP = entryIP,
            LocalsCount = funcLocals,
            Name = funcDef.Id,
            ReturnType = returnType,
            ArgTypes = argTypes,
        });
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

        // Resolve function
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

        // User function call
        for (int i = 0; i < _userFunctions.Count; i++) {
            if (string.Equals(_userFunctions[i].Name, id, StringComparison.OrdinalIgnoreCase)
                && _userFunctions[i].ArgTypes.Length == args.Length) {
                // Compile arguments
                for (int j = 0; j < args.Length; j++)
                    CompileExpression(args[j]);
                _e.EmitWithArg(Op.Call, (byte)i);
                _e.Code.Add((byte)args.Length);
                return 0;
            }
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
            // Safe field access on optional — emit GetFieldSafe
            var fieldIdx = ResolveFieldIndex(node.Source, node.FieldName);
            _e.EmitWithArg(Op.GetFieldSafe, (byte)fieldIdx);
            return 0;
        }

        var sourceType = GetOutputType(node.Source);
        if (sourceType.BaseType == BaseFunnyType.Optional) {
            var fieldIdx = ResolveFieldIndex(node.Source, node.FieldName);
            _e.EmitWithArg(Op.GetFieldSafe, (byte)fieldIdx);
            return 0;
        }

        {
            var fieldIdx = ResolveFieldIndex(node.Source, node.FieldName);
            var layoutId = GetOrCreateStructLayoutFromType(sourceType);
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
        // Higher-order call: result(args) — compile as CALL_EXTERN via the resolved function
        throw new NotSupportedException($"VM: ResultFunCallSyntaxNode not yet supported");
    }

    public byte Visit(AnonymFunctionSyntaxNode node) {
        throw new NotSupportedException($"VM: AnonymFunctionSyntaxNode not yet supported");
    }

    public byte Visit(SuperAnonymFunctionSyntaxNode node) {
        throw new NotSupportedException($"VM: SuperAnonymFunctionSyntaxNode not yet supported");
    }

    public byte Visit(TryCatchSyntaxNode node) {
        throw new NotSupportedException($"VM: TryCatchSyntaxNode not yet supported");
    }

    public byte Visit(TypeDeclarationSyntaxNode node) {
        throw new NotSupportedException($"VM: TypeDeclarationSyntaxNode not yet supported");
    }

    public byte Visit(NamedTypeConstructorSyntaxNode node) {
        throw new NotSupportedException($"VM: NamedTypeConstructorSyntaxNode not yet supported");
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
            EmitTruncation(type);
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
                else if (IsIntegerType(opType)) _e.Emit(Op.EqInt);
                else _e.Emit(Op.EqRef);
                break;
            case BinOp.NotEqual:
                if (isReal) { _e.Emit(Op.EqReal); _e.Emit(Op.Not); }
                else if (IsIntegerType(opType)) _e.Emit(Op.NeqInt);
                else _e.Emit(Op.NeqRef);
                break;
            case BinOp.Less:
                _e.Emit(isReal ? Op.LtReal : Op.LtInt);
                break;
            case BinOp.LessOrEqual:
                _e.Emit(isReal ? Op.LteReal : Op.LteInt);
                break;
            case BinOp.More:
                _e.Emit(isReal ? Op.GtReal : Op.GtInt);
                break;
            case BinOp.MoreOrEqual:
                _e.Emit(isReal ? Op.GteReal : Op.GteInt);
                break;
        }

        // After the operation, truncate the result if the output type is narrower than I64
        if (!isReal && IsIntegerType(outputType))
            EmitTruncation(outputType);
    }

    private void EmitComparisonOp(TokType tokType, FunnyType operandType) {
        bool isReal = IsRealType(operandType);
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

        if (!_externFuncIds.TryGetValue(name, out var byArity)) {
            byArity = new Dictionary<int, int>();
            _externFuncIds[name] = byArity;
        }

        // Check if an extern with same name, arity, and matching types already exists
        if (byArity.TryGetValue(arity, out var existingId)) {
            var existing = _externFunctions[existingId];
            if (existing.ReturnType.Equals(function.ReturnType) && ArgsMatch(existing.ArgTypes, function.ArgTypes))
                return existingId;
        }

        // Register new extern function — use unique id keyed by name+arity+types
        var id = _externFunctions.Count;
        _externFunctions.Add(new ExternFunc {
            Function = function,
            ReturnType = function.ReturnType,
            ArgTypes = function.ArgTypes,
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
        if (_structLayoutIds.TryGetValue(key, out var id))
            return id;

        id = _structLayouts.Count;
        _structLayouts.Add(new StructLayout { FieldNames = names, FieldTypes = types });
        _structLayoutIds[key] = id;
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
        if (_structLayoutIds.TryGetValue(key, out var id))
            return id;

        id = _structLayouts.Count;
        _structLayouts.Add(new StructLayout { FieldNames = names.ToArray(), FieldTypes = types.ToArray() });
        _structLayoutIds[key] = id;
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

    private static string MakeStructLayoutKey(string[] names) => string.Join(",", names);

    private static bool IsRealType(FunnyType type) => type.BaseType == BaseFunnyType.Real;

    private static bool IsIntegerType(FunnyType type) =>
        type.BaseType >= BaseFunnyType.UInt8 && type.BaseType <= BaseFunnyType.Int64;
}

/// <summary>
/// Builds a byte[] code buffer with a constants pool.
/// Provides helpers for emitting opcodes, operands, and patching jumps.
/// </summary>
internal sealed class BytecodeEmitter {
    internal readonly List<byte> Code = new();
    private readonly List<FunValue> _constants = new();

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
