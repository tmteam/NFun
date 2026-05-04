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
using NFun.Functions;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// Compiles typed AST to register-based bytecode.
/// Each Visit returns the register index holding the result.
/// Registers = locals[] slots. Temps allocated beyond user variables.
/// </summary>
internal sealed class RegisterCompiler : ISyntaxNodeVisitor<byte> {
    private readonly TypeInferenceResults _typeResults;
    private readonly TicTypesConverter _typesConverter;
    private readonly IFunctionRegistry _functions;
    private readonly DialectSettings _dialect;
    private readonly List<byte> _code = new(32);
    private readonly List<FunValue> _constants = new(8);
    private readonly Dictionary<string, int> _slots = new(StringComparer.OrdinalIgnoreCase);
    private int _nextSlot;

    // Extern functions (resolved during compilation)
    private List<ExternFunc> _externFunctions;
    private Dictionary<string, Dictionary<int, int>> _externFuncIds;
    private List<ExternFunc> ExternFunctions => _externFunctions ??= new();
    private Dictionary<string, Dictionary<int, int>> ExternFuncIds => _externFuncIds ??= new();

    // Struct layouts
    private List<StructLayout> _structLayouts;
    private Dictionary<string, int> _structLayoutIds;
    private List<StructLayout> StructLayouts => _structLayouts ??= new();
    private Dictionary<string, int> StructLayoutIds => _structLayoutIds ??= new();

    // Type table (for array element types)
    private List<FunnyType> _typeTable;
    private Dictionary<FunnyType, int> _typeTableIndex;
    private List<FunnyType> TypeTable => _typeTable ??= new();
    private Dictionary<FunnyType, int> TypeTableIndex => _typeTableIndex ??= new();

    private RegisterCompiler(
        TypeInferenceResults typeResults, TicTypesConverter typesConverter,
        IFunctionRegistry functions, DialectSettings dialect) {
        _typeResults = typeResults;
        _typesConverter = typesConverter;
        _functions = functions;
        _dialect = dialect;
    }

    public static (byte[] Code, FunValue[] Constants, int LocalsCount, Dictionary<string, int> Slots,
        ExternFunc[] ExternFuncs, StructLayout[] StructLayouts, FunnyType[] TypeTable)
        Compile(SyntaxTree tree, TypeInferenceResults typeResults, TicTypesConverter typesConverter,
            IFunctionRegistry functions = null, DialectSettings dialect = null) {
        var c = new RegisterCompiler(typeResults, typesConverter, functions, dialect);

        // Allocate slots for equation outputs first
        foreach (var node in tree.Nodes) {
            if (node is EquationSyntaxNode eq) {
                var type = eq.OutputType.BaseType != BaseFunnyType.Empty
                    ? eq.OutputType : c.GetOutputType(eq.Expression);
                c.AllocSlot(eq.Id, type);
            }
        }

        // Compile each equation
        foreach (var node in tree.Nodes) {
            if (node is EquationSyntaxNode eq) {
                var resultReg = c.CompileExpr(eq.Expression);
                var outSlot = c._slots[eq.Id];
                if (resultReg != outSlot)
                    c.Emit(RegisterOp.Mov, (byte)outSlot, resultReg, 0);
            }
        }
        c.Emit(RegisterOp.Halt, 0, 0, 0);

        return (
            c._code.ToArray(),
            c._constants.ToArray(),
            c._nextSlot,
            c._slots,
            c._externFunctions is { Count: > 0 } ? c._externFunctions.ToArray() : Array.Empty<ExternFunc>(),
            c._structLayouts is { Count: > 0 } ? c._structLayouts.ToArray() : Array.Empty<StructLayout>(),
            c._typeTable is { Count: > 0 } ? c._typeTable.ToArray() : Array.Empty<FunnyType>()
        );
    }

    private byte CompileExpr(ISyntaxNode node) => node.Accept(this);

    private int AllocSlot(string name, FunnyType type) {
        if (_slots.TryGetValue(name, out var existing)) return existing;
        var slot = _nextSlot++;
        _slots[name] = slot;
        return slot;
    }

    private int AllocTemp() => _nextSlot++;

    private int AddConst(FunValue v) {
        var idx = _constants.Count;
        _constants.Add(v);
        return idx;
    }

    private void Emit(RegisterOp op, byte dst, byte src1, byte src2) {
        _code.Add((byte)op);
        _code.Add(dst);
        _code.Add(src1);
        _code.Add(src2);
    }

    private FunnyType GetOutputType(ISyntaxNode node) {
        if (node.OutputType.BaseType != BaseFunnyType.Empty) return node.OutputType;
        var tic = _typeResults.GetSyntaxNodeTypeOrNull(node.OrderNumber);
        return tic != null ? _typesConverter.Convert(tic) : node.OutputType;
    }

    private bool IsInt(FunnyType t) => t.BaseType >= BaseFunnyType.UInt8 && t.BaseType <= BaseFunnyType.Int64;
    private bool IsReal(FunnyType t) => t.BaseType == BaseFunnyType.Real;

    // ═══════════════════════════════════════════════
    //  Visitors — each returns register index
    // ═══════════════════════════════════════════════

    public byte Visit(ConstantSyntaxNode node) {
        var type = GetOutputType(node);
        var r = (byte)AllocTemp();
        if (node.Value is FunnyNone) {
            Emit(RegisterOp.LoadNone, r, 0, 0);
            return r;
        }
        // Real constants: parser stores them as strings (e.g., "3.14") — parse to double
        if (IsReal(type) && node.Value is string realStr
            && double.TryParse(realStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed)) {
            var ci = AddConst(FunValue.FromReal(parsed));
            Emit(RegisterOp.LoadC_D, r, (byte)ci, 0);
            return r;
        }
        // Reference constants: text (char[]), arrays, structs, etc.
        if (type.BaseType == BaseFunnyType.ArrayOf || type.BaseType == BaseFunnyType.Struct
            || type.BaseType == BaseFunnyType.Any
            || node.Value is string || node.Value is Runtime.Arrays.IFunnyArray) {
            var ci = AddConst(FunValue.FromRef(node.Value));
            Emit(RegisterOp.LoadC_Ref, r, (byte)ci, 0);
            return r;
        }
        var ci2 = AddConstValue(node.Value, type);
        Emit(IsReal(type) ? RegisterOp.LoadC_D : RegisterOp.LoadC_I, r, (byte)ci2, 0);
        return r;
    }

    public byte Visit(GenericIntSyntaxNode node) {
        var type = GetOutputType(node);
        var r = (byte)AllocTemp();
        if (IsReal(type)) {
            // Generic int resolved to Real — store as double
            double dval = node.Value is long l ? l : node.Value is ulong u ? u : Convert.ToDouble(node.Value);
            var ci = AddConst(FunValue.FromReal(dval));
            Emit(RegisterOp.LoadC_D, r, (byte)ci, 0);
        } else {
            var ci = AddConstValue(node.Value, type);
            Emit(RegisterOp.LoadC_I, r, (byte)ci, 0);
        }
        return r;
    }

    public byte Visit(NamedIdSyntaxNode node) {
        if (node.IdType == NamedIdNodeType.Constant) {
            var cv = (ConstantValueAndType)node.IdContent;
            var r = (byte)AllocTemp();
            if (cv.FunnyValue is FunnyNone) {
                Emit(RegisterOp.LoadNone, r, 0, 0);
                return r;
            }
            if (cv.Type.BaseType == BaseFunnyType.ArrayOf || cv.Type.BaseType == BaseFunnyType.Struct
                || cv.Type.BaseType == BaseFunnyType.Any
                || cv.FunnyValue is string || cv.FunnyValue is Runtime.Arrays.IFunnyArray) {
                var ci = AddConst(FunValue.FromRef(cv.FunnyValue));
                Emit(RegisterOp.LoadC_Ref, r, (byte)ci, 0);
                return r;
            }
            var ci2 = AddConstValue(cv.FunnyValue, cv.Type);
            Emit(IsReal(cv.Type) ? RegisterOp.LoadC_D : RegisterOp.LoadC_I, r, (byte)ci2, 0);
            return r;
        }
        return (byte)AllocSlot(node.Id, GetOutputType(node));
    }

    public byte Visit(BinOperatorSyntaxNode node) {
        var outType = GetOutputType(node);
        var left = CompileExpr(node.Left);
        var right = CompileExpr(node.Right);
        var dst = (byte)AllocTemp();
        bool isReal = IsReal(outType) || IsReal(GetOutputType(node.Left)) || IsReal(GetOutputType(node.Right));

        // Convert if needed
        if (isReal && IsInt(GetOutputType(node.Left))) {
            var cv = (byte)AllocTemp();
            Emit(RegisterOp.I2D, cv, left, 0);
            left = cv;
        }
        if (isReal && IsInt(GetOutputType(node.Right))) {
            var cv = (byte)AllocTemp();
            Emit(RegisterOp.I2D, cv, right, 0);
            right = cv;
        }

        var leftType = GetOutputType(node.Left);
        var rightType = GetOutputType(node.Right);
        bool isNumeric = isReal || IsInt(leftType) || IsInt(rightType)
            || leftType.BaseType == BaseFunnyType.Bool || leftType.BaseType == BaseFunnyType.Char;

        // For ordering comparisons on non-numeric types (text, arrays, structs),
        // fall back to stack VM which handles them via CALL_EXTERN
        if (!isNumeric && (node.Op == BinOp.Less || node.Op == BinOp.LessOrEqual
            || node.Op == BinOp.More || node.Op == BinOp.MoreOrEqual))
            throw new NotSupportedException($"RegisterVM: BinOp {node.Op} on non-numeric type {leftType}");

        RegisterOp op = node.Op switch {
            BinOp.Add => isReal ? RegisterOp.AddRR_D : RegisterOp.AddRR_I,
            BinOp.Subtract => isReal ? RegisterOp.SubRR_D : RegisterOp.SubRR_I,
            BinOp.Multiply => isReal ? RegisterOp.MulRR_D : RegisterOp.MulRR_I,
            BinOp.DivideInt => RegisterOp.DivRR_I,
            BinOp.DivideReal => RegisterOp.DivRR_D,
            BinOp.Remainder => isReal ? RegisterOp.ModRR_D : RegisterOp.ModRR_I,
            BinOp.Pow => isReal ? RegisterOp.PowRR_D : RegisterOp.PowRR_I,
            BinOp.More => isReal ? RegisterOp.GtRR_D : RegisterOp.GtRR_I,
            BinOp.Less => isReal ? RegisterOp.LtRR_D : RegisterOp.LtRR_I,
            BinOp.MoreOrEqual => isReal ? RegisterOp.GteRR_D : RegisterOp.GteRR_I,
            BinOp.LessOrEqual => isReal ? RegisterOp.LteRR_D : RegisterOp.LteRR_I,
            BinOp.Equal when !isNumeric => RegisterOp.EqRef,
            BinOp.NotEqual when !isNumeric => RegisterOp.NeqRef,
            BinOp.Equal => isReal ? RegisterOp.EqRR_D : RegisterOp.EqRR_I,
            BinOp.NotEqual => RegisterOp.NeqRR_I,
            BinOp.And => RegisterOp.AndRR,
            BinOp.Or => RegisterOp.OrRR,
            BinOp.Xor => RegisterOp.XorRR,
            BinOp.BitAnd => RegisterOp.BitAndRR,
            BinOp.BitOr => RegisterOp.BitOrRR,
            BinOp.BitXor => RegisterOp.BitXorRR,
            BinOp.BitShiftLeft => RegisterOp.ShlRR,
            BinOp.BitShiftRight => RegisterOp.ShrRR,
            _ => throw new NotSupportedException($"RegisterVM: BinOp {node.Op} not supported"),
        };

        Emit(op, dst, left, right);
        return dst;
    }

    public byte Visit(UnaryOperatorSyntaxNode node) {
        var src = CompileExpr(node.Operand);
        var dst = (byte)AllocTemp();
        var type = GetOutputType(node);
        switch (node.Op) {
            case UnOp.Negate:
                Emit(IsReal(type) ? RegisterOp.NegR_D : RegisterOp.NegR_I, dst, src, 0);
                break;
            case UnOp.Not:
                Emit(RegisterOp.NotR, dst, src, 0);
                break;
            case UnOp.BitInverse:
                Emit(RegisterOp.BitNotR, dst, src, 0);
                break;
            default:
                throw new NotSupportedException($"RegisterVM: UnOp {node.Op}");
        }
        return dst;
    }

    public byte Visit(IfThenElseSyntaxNode node) {
        var dst = (byte)AllocTemp();
        var jumpToEnds = new List<int>();

        for (int i = 0; i < node.Ifs.Length; i++) {
            var condReg = CompileExpr(node.Ifs[i].Condition);

            int jmpToNext = _code.Count;
            Emit(RegisterOp.JmpIfNot, condReg, 0, 0);

            var thenReg = CompileExpr(node.Ifs[i].Expression);
            if (thenReg != dst) Emit(RegisterOp.Mov, dst, thenReg, 0);

            jumpToEnds.Add(_code.Count);
            Emit(RegisterOp.Jmp, 0, 0, 0);

            PatchJump(jmpToNext, _code.Count);
        }

        var elseReg = CompileExpr(node.ElseExpr);
        if (elseReg != dst) Emit(RegisterOp.Mov, dst, elseReg, 0);

        int end = _code.Count;
        foreach (var addr in jumpToEnds)
            PatchJump(addr, end);

        return dst;
    }

    private void PatchJump(int instrAddr, int target) {
        _code[instrAddr + 2] = (byte)(target >> 8);
        _code[instrAddr + 3] = (byte)(target & 0xFF);
    }

    // ═══════════════════════════════════════════════
    //  FunCallSyntaxNode — the big one
    // ═══════════════════════════════════════════════

    public byte Visit(FunCallSyntaxNode node) {
        var id = node.Id;
        var args = _typeResults.GetResolvedCallArgsOrNull(node.OrderNumber) ?? node.Args;

        // Reject lambda args — fallback to stack VM
        foreach (var arg in args)
            if (arg is AnonymFunctionSyntaxNode || arg is SuperAnonymFunctionSyntaxNode)
                throw new NotSupportedException("RegisterVM: lambda args");

        // Null coalesce: ??
        if (id == CoreFunNames.NullCoalesce && args.Length == 2) {
            var left = CompileExpr(args[0]);
            var right = CompileExpr(args[1]);
            var dst = (byte)AllocTemp();
            Emit(RegisterOp.Coalesce, dst, left, right);
            return dst;
        }

        // Safe array access: ?[]
        if (id == CoreFunNames.SafeGetElementName && args.Length == 2) {
            var arr = CompileExpr(args[0]);
            var idx = CompileExpr(args[1]);
            var dst = (byte)AllocTemp();
            Emit(RegisterOp.GetElemSafe, dst, arr, idx);
            return dst;
        }

        // Force unwrap: !
        if (id == CoreFunNames.ForceUnwrap && args.Length == 1) {
            var src = CompileExpr(args[0]);
            var dst = (byte)AllocTemp();
            Emit(RegisterOp.Unwrap, dst, src, 0);
            return dst;
        }

        // Array element access: []
        if (id == CoreFunNames.GetElementName && args.Length == 2) {
            var arr = CompileExpr(args[0]);
            var idx = CompileExpr(args[1]);
            var dst = (byte)AllocTemp();
            Emit(RegisterOp.GetElem, dst, arr, idx);
            return dst;
        }

        // IsNone check: == none
        if (id == CoreFunNames.Equal && args.Length == 2) {
            // Check if one arg is none literal
            bool leftIsNone = IsNoneLiteral(args[0]);
            bool rightIsNone = IsNoneLiteral(args[1]);
            if (leftIsNone || rightIsNone) {
                var src = CompileExpr(leftIsNone ? args[1] : args[0]);
                var dst = (byte)AllocTemp();
                Emit(RegisterOp.IsNone, dst, src, 0);
                return dst;
            }
        }

        // Native built-in functions (max, min, abs, toText)
        if (TryEmitNativeFunction(id, args, out var nativeResult))
            return nativeResult;

        // Native operators (arithmetic, comparison) via function-call syntax
        if (args.Length == 2 && TryEmitNativeOperator(id, args, node, out var opResult))
            return opResult;

        // Generic extern function call
        if (_functions == null)
            throw new NotSupportedException($"RegisterVM: function {id}/{args.Length} — no function registry");

        var someFunc = node.ResolvedSignature
            ?? _typeResults.GetResolvedCallSignatureOrNull(node.OrderNumber)
            ?? _functions.GetOrNull(id, args.Length);

        if (someFunc is IConcreteFunction concrete)
            return EmitCallExt(concrete, args);

        if (someFunc is IGenericFunction genericFunction) {
            var concreteFunc = ResolveGenericFunction(genericFunction, node);
            return EmitCallExt(concreteFunc, args);
        }

        throw new NotSupportedException($"RegisterVM: function {id}/{args.Length} not found");
    }

    // ═══════════════════════════════════════════════
    //  Arrays
    // ═══════════════════════════════════════════════

    public byte Visit(ArraySyntaxNode node) {
        var outputType = GetOutputType(node);
        var elementType = outputType.BaseType == BaseFunnyType.ArrayOf
            ? outputType.ArrayTypeSpecification.FunnyType
            : FunnyType.Any;

        int count = node.Expressions.Count;

        // Compile each element into a temp register
        var elemRegs = new byte[count];
        for (int i = 0; i < count; i++)
            elemRegs[i] = CompileExpr(node.Expressions[i]);

        var typeIdx = (byte)GetOrAddTypeTableEntry(elementType);
        var dst = (byte)AllocTemp();

        // Emit NewArr [dst][count][typeIdx]
        Emit(RegisterOp.NewArr, dst, (byte)count, typeIdx);

        // Followed by element register indices, padded to 4-byte boundary
        for (int i = 0; i < count; i++)
            _code.Add(elemRegs[i]);
        // Pad to 4-byte boundary
        int pad = ((count + 3) / 4) * 4 - count;
        for (int i = 0; i < pad; i++)
            _code.Add(0);

        return dst;
    }

    // ═══════════════════════════════════════════════
    //  Structs
    // ═══════════════════════════════════════════════

    public byte Visit(StructInitSyntaxNode node) {
        int fieldCount = node.Fields.Count;

        // Compile each field value
        var fieldRegs = new byte[fieldCount];
        for (int i = 0; i < fieldCount; i++)
            fieldRegs[i] = CompileExpr(node.Fields[i].Node);

        var layoutId = GetOrCreateStructLayout(node);
        var dst = (byte)AllocTemp();

        // Emit NewStruct [dst][layoutId][fieldCount]
        Emit(RegisterOp.NewStruct, dst, (byte)layoutId, (byte)fieldCount);

        // Followed by field register indices, padded to 4-byte boundary
        for (int i = 0; i < fieldCount; i++)
            _code.Add(fieldRegs[i]);
        int pad = ((fieldCount + 3) / 4) * 4 - fieldCount;
        for (int i = 0; i < pad; i++)
            _code.Add(0);

        return dst;
    }

    // ═══════════════════════════════════════════════
    //  Struct field access
    // ═══════════════════════════════════════════════

    public byte Visit(StructFieldAccessSyntaxNode node) {
        var srcReg = CompileExpr(node.Source);

        var sourceType = GetOutputType(node.Source);
        bool isOptional = node.IsSafeAccess
            || sourceType.BaseType == BaseFunnyType.Optional;

        // Unwrap optional layers to get to struct type
        var structType = sourceType;
        while (structType.BaseType == BaseFunnyType.Optional)
            structType = structType.OptionalTypeSpecification.ElementType;

        var fieldIdx = ResolveFieldIndex(structType, node.FieldName);
        var layoutId = GetOrCreateStructLayoutFromType(structType);

        var dst = (byte)AllocTemp();
        // GetField/GetFieldSafe: [dst][fieldIdx][layoutId]
        // But our encoding puts struct in dst (overwritten), so we need to Mov source there first
        // Actually RegisterVM reads from locals[dst] for GetField — so put source in dst
        Emit(RegisterOp.Mov, dst, srcReg, 0);
        Emit(isOptional ? RegisterOp.GetFieldSafe : RegisterOp.GetField,
            dst, (byte)fieldIdx, (byte)layoutId);

        return dst;
    }

    // ═══════════════════════════════════════════════
    //  Comparison chains: a < b < c
    // ═══════════════════════════════════════════════

    public byte Visit(ComparisonChainSyntaxNode node) {
        // a < b < c → (a < b) AND (b < c)
        // Compile first pair
        var leftReg = CompileExpr(node.Operands[0]);
        var midReg = CompileExpr(node.Operands[1]);

        var leftType = GetOutputType(node.Operands[0]);
        bool isReal = IsReal(leftType) || IsReal(GetOutputType(node.Operands[1]));

        var cmp1Dst = (byte)AllocTemp();
        EmitComparisonOp(node.Operators[0].Type, cmp1Dst, leftReg, midReg, isReal);

        var accDst = cmp1Dst;

        for (int i = 1; i < node.Operators.Count; i++) {
            var nextReg = CompileExpr(node.Operands[i + 1]);
            // Re-compile mid operand for next comparison
            var prevMidReg = CompileExpr(node.Operands[i]);
            bool isRealI = IsReal(GetOutputType(node.Operands[i])) || IsReal(GetOutputType(node.Operands[i + 1]));

            var cmpDst = (byte)AllocTemp();
            EmitComparisonOp(node.Operators[i].Type, cmpDst, prevMidReg, nextReg, isRealI);

            var andDst = (byte)AllocTemp();
            Emit(RegisterOp.AndRR, andDst, accDst, cmpDst);
            accDst = andDst;
        }

        return accDst;
    }

    private void EmitComparisonOp(TokType tokType, byte dst, byte left, byte right, bool isReal) {
        RegisterOp op = tokType switch {
            TokType.Less => isReal ? RegisterOp.LtRR_D : RegisterOp.LtRR_I,
            TokType.LessOrEqual => isReal ? RegisterOp.LteRR_D : RegisterOp.LteRR_I,
            TokType.More => isReal ? RegisterOp.GtRR_D : RegisterOp.GtRR_I,
            TokType.MoreOrEqual => isReal ? RegisterOp.GteRR_D : RegisterOp.GteRR_I,
            _ => throw new NotSupportedException($"RegisterVM: comparison chain op {tokType}"),
        };
        Emit(op, dst, left, right);
    }

    // ═══════════════════════════════════════════════
    //  Default value
    // ═══════════════════════════════════════════════

    public byte Visit(DefaultValueSyntaxNode node) {
        var type = GetOutputType(node);
        var r = (byte)AllocTemp();
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
                var ci = AddConst(FunValue.FromI64(0));
                Emit(RegisterOp.LoadC_I, r, (byte)ci, 0);
                break;
            case BaseFunnyType.Real:
                var cir = AddConst(FunValue.FromReal(0.0));
                Emit(RegisterOp.LoadC_D, r, (byte)cir, 0);
                break;
            default:
                Emit(RegisterOp.LoadNone, r, 0, 0);
                break;
        }
        return r;
    }

    // ═══════════════════════════════════════════════
    //  IP Address constant
    // ═══════════════════════════════════════════════

    public byte Visit(IpAddressConstantSyntaxNode node) {
        var r = (byte)AllocTemp();
        var ci = AddConst(FunValue.FromRef(node.Value));
        Emit(RegisterOp.LoadC_Ref, r, (byte)ci, 0);
        return r;
    }

    // ═══════════════════════════════════════════════
    //  Helpers: function calls
    // ═══════════════════════════════════════════════

    /// <summary>Emit CallExt: args in consecutive temp registers (Lua convention).</summary>
    private byte EmitCallExt(IConcreteFunction function, ISyntaxNode[] args) {
        int argc = args.Length;
        // Allocate consecutive temp registers for args
        int baseR = _nextSlot;
        for (int i = 0; i < argc; i++)
            AllocTemp(); // reserve [baseR..baseR+argc-1]

        // Compile each arg into its designated register
        for (int i = 0; i < argc; i++) {
            var argReg = CompileExpr(args[i]);
            var targetReg = baseR + i;
            if (argReg != targetReg)
                Emit(RegisterOp.Mov, (byte)targetReg, argReg, 0);

            // Box if needed (primitive → Any for extern func args)
            var argType = GetOutputType(args[i]);
            var expectedType = function.ArgTypes[i];
            if (expectedType.BaseType == BaseFunnyType.Any && argType.BaseType != BaseFunnyType.Any) {
                if (IsInt(argType)) {
                    Emit(RegisterOp.BoxInt, (byte)targetReg, (byte)targetReg, 0);
                } else if (IsReal(argType)) {
                    Emit(RegisterOp.BoxReal, (byte)targetReg, (byte)targetReg, 0);
                } else if (argType.BaseType == BaseFunnyType.Bool) {
                    Emit(RegisterOp.BoxBool, (byte)targetReg, (byte)targetReg, 0);
                } else if (argType.BaseType == BaseFunnyType.Char) {
                    Emit(RegisterOp.BoxInt, (byte)targetReg, (byte)targetReg, 0);
                }
                // Composite types (arrays, structs) already in Ref — no boxing needed
            }
        }

        var funcId = GetOrRegisterExternFunc(function);
        var dst = (byte)AllocTemp();
        Emit(RegisterOp.CallExt, dst, (byte)funcId, (byte)baseR);
        return dst;
    }

    private int GetOrRegisterExternFunc(IConcreteFunction function) {
        var name = function.Name;
        var arity = function.ArgTypes.Length;

        if (!ExternFuncIds.TryGetValue(name, out var byArity)) {
            byArity = new Dictionary<int, int>();
            ExternFuncIds[name] = byArity;
        }

        if (byArity.TryGetValue(arity, out var existingId)) {
            var existing = ExternFunctions[existingId];
            if (existing.ReturnType.Equals(function.ReturnType) && ArgsMatch(existing.ArgTypes, function.ArgTypes))
                return existingId;
        }

        var id = ExternFunctions.Count;
        ExternFunctions.Add(new ExternFunc {
            Function = function,
            ReturnType = function.ReturnType,
            ArgTypes = function.ArgTypes,
            ArityKind = function is FunctionWithSingleArg ? (byte)1
                      : function is FunctionWithTwoArgs ? (byte)2
                      : (byte)0,
        });
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
        var genericTypes = _typeResults.GetGenericCallArguments(node.OrderNumber);
        if (genericTypes == null) {
            var recCallSignature = _typeResults.GetRecursiveCallOrNull(node.OrderNumber);
            if (recCallSignature == null)
                throw new NotSupportedException($"RegisterVM: generic function {node.Id}/{node.Args.Length} types not resolved");
            var varTypeCallSignature = _typesConverter.Convert(recCallSignature);
            genericArgs = genericFunction.CalcGenericArgTypeList(varTypeCallSignature.FunTypeSpecification);
        } else {
            genericArgs = new FunnyType[genericTypes.Length];
            for (int i = 0; i < genericTypes.Length; i++)
                genericArgs[i] = _typesConverter.Convert(genericTypes[i]);
        }
        return genericFunction.CreateConcrete(genericArgs, _dialect);
    }

    private bool TryEmitNativeFunction(string funcName, ISyntaxNode[] args, out byte result) {
        result = 0;
        if (funcName == "max" && args.Length == 2) {
            var t = GetOutputType(args[0]);
            var a = CompileExpr(args[0]);
            var b = CompileExpr(args[1]);
            var dst = (byte)AllocTemp();
            Emit(IsReal(t) ? RegisterOp.MaxRR_D : RegisterOp.MaxRR_I, dst, a, b);
            result = dst;
            return true;
        }
        if (funcName == "min" && args.Length == 2) {
            var t = GetOutputType(args[0]);
            var a = CompileExpr(args[0]);
            var b = CompileExpr(args[1]);
            var dst = (byte)AllocTemp();
            Emit(IsReal(t) ? RegisterOp.MinRR_D : RegisterOp.MinRR_I, dst, a, b);
            result = dst;
            return true;
        }
        if (funcName == "abs" && args.Length == 1) {
            var t = GetOutputType(args[0]);
            if (IsInt(t) || IsReal(t)) {
                var a = CompileExpr(args[0]);
                var dst = (byte)AllocTemp();
                Emit(IsReal(t) ? RegisterOp.AbsR_D : RegisterOp.AbsR_I, dst, a, 0);
                result = dst;
                return true;
            }
        }
        if (funcName == CoreFunNames.ToText && args.Length == 1) {
            var t = GetOutputType(args[0]);
            if (IsInt(t)) {
                var a = CompileExpr(args[0]);
                var dst = (byte)AllocTemp();
                Emit(RegisterOp.ToTextI, dst, a, 0);
                result = dst;
                return true;
            }
            if (IsReal(t)) {
                var a = CompileExpr(args[0]);
                var dst = (byte)AllocTemp();
                Emit(RegisterOp.ToTextD, dst, a, 0);
                result = dst;
                return true;
            }
        }
        return false;
    }

    private bool TryEmitNativeOperator(string opName, ISyntaxNode[] args, FunCallSyntaxNode node, out byte result) {
        result = 0;
        var outputType = GetOutputType(node);
        var leftType = GetOutputType(args[0]);
        var rightType = GetOutputType(args[1]);
        bool isReal = IsReal(outputType) || IsReal(leftType) || IsReal(rightType);
        bool isInt = !isReal && (IsInt(outputType) || IsInt(leftType) || IsInt(rightType));

        if (!isReal && !isInt) return false;

        RegisterOp? op = opName switch {
            CoreFunNames.Add        => isReal ? RegisterOp.AddRR_D : RegisterOp.AddRR_I,
            CoreFunNames.Substract  => isReal ? RegisterOp.SubRR_D : RegisterOp.SubRR_I,
            CoreFunNames.Multiply   => isReal ? RegisterOp.MulRR_D : RegisterOp.MulRR_I,
            CoreFunNames.DivideReal => RegisterOp.DivRR_D,
            CoreFunNames.DivideInt  => RegisterOp.DivRR_I,
            CoreFunNames.Remainder  => isReal ? RegisterOp.ModRR_D : RegisterOp.ModRR_I,
            CoreFunNames.Pow        => isReal ? RegisterOp.PowRR_D : RegisterOp.PowRR_I,
            CoreFunNames.More       => isReal ? RegisterOp.GtRR_D : RegisterOp.GtRR_I,
            CoreFunNames.Less       => isReal ? RegisterOp.LtRR_D : RegisterOp.LtRR_I,
            CoreFunNames.MoreOrEqual => isReal ? RegisterOp.GteRR_D : RegisterOp.GteRR_I,
            CoreFunNames.LessOrEqual => isReal ? RegisterOp.LteRR_D : RegisterOp.LteRR_I,
            CoreFunNames.Equal      => isReal ? RegisterOp.EqRR_D : RegisterOp.EqRR_I,
            CoreFunNames.NotEqual   => isReal ? (RegisterOp?)RegisterOp.EqRR_D : RegisterOp.NeqRR_I,
            CoreFunNames.BitAnd     => isInt ? RegisterOp.BitAndRR : null,
            CoreFunNames.BitOr      => isInt ? RegisterOp.BitOrRR : null,
            CoreFunNames.BitXor     => isInt ? RegisterOp.BitXorRR : null,
            CoreFunNames.BitShiftLeft  => isInt ? RegisterOp.ShlRR : null,
            CoreFunNames.BitShiftRight => isInt ? RegisterOp.ShrRR : null,
            CoreFunNames.And        => RegisterOp.AndRR,
            CoreFunNames.Or         => RegisterOp.OrRR,
            _ => null,
        };

        if (op == null) return false;

        var left = CompileExpr(args[0]);
        var right = CompileExpr(args[1]);

        // Convert int→real if needed
        if (isReal && IsInt(leftType)) {
            var cv = (byte)AllocTemp();
            Emit(RegisterOp.I2D, cv, left, 0);
            left = cv;
        }
        if (isReal && IsInt(rightType)) {
            var cv = (byte)AllocTemp();
            Emit(RegisterOp.I2D, cv, right, 0);
            right = cv;
        }

        var dst = (byte)AllocTemp();
        Emit(op.Value, dst, left, right);

        // For real NotEqual, negate the EqRR_D result
        if (opName == CoreFunNames.NotEqual && isReal) {
            var notDst = (byte)AllocTemp();
            Emit(RegisterOp.NotR, notDst, dst, 0);
            result = notDst;
            return true;
        }

        result = dst;
        return true;
    }

    // ═══════════════════════════════════════════════
    //  Struct/Type helpers
    // ═══════════════════════════════════════════════

    private int GetOrCreateStructLayout(StructInitSyntaxNode node) {
        var names = new string[node.Fields.Count];
        var types = new FunnyType[node.Fields.Count];
        for (int i = 0; i < node.Fields.Count; i++) {
            names[i] = node.Fields[i].Name;
            types[i] = GetOutputType(node.Fields[i].Node);
        }
        var key = string.Join(",", names);
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
        var key = string.Join(",", names);
        if (StructLayoutIds.TryGetValue(key, out var id))
            return id;
        id = StructLayouts.Count;
        StructLayouts.Add(new StructLayout { FieldNames = names.ToArray(), FieldTypes = types.ToArray() });
        StructLayoutIds[key] = id;
        return id;
    }

    private int ResolveFieldIndex(FunnyType structType, string fieldName) {
        if (structType.BaseType == BaseFunnyType.Struct) {
            int idx = 0;
            foreach (var (name, _) in structType.StructTypeSpecification) {
                if (string.Equals(name, fieldName, StringComparison.InvariantCultureIgnoreCase))
                    return idx;
                idx++;
            }
        }
        return 0;
    }

    private int GetOrAddTypeTableEntry(FunnyType type) {
        if (TypeTableIndex.TryGetValue(type, out var idx)) return idx;
        idx = TypeTable.Count;
        TypeTable.Add(type);
        TypeTableIndex[type] = idx;
        return idx;
    }

    private static bool IsNoneLiteral(ISyntaxNode node) {
        if (node is ConstantSyntaxNode c && c.Value is FunnyNone) return true;
        if (node is NamedIdSyntaxNode n && n.IdType == NamedIdNodeType.Constant
            && n.IdContent is ConstantValueAndType cv && cv.FunnyValue is FunnyNone) return true;
        return false;
    }

    // ── Constant helpers ──

    private int AddConstValue(object value, FunnyType type) {
        switch (value) {
            case long l: return AddConst(FunValue.FromI64(l));
            case ulong u: return AddConst(FunValue.FromI64((long)u));
            case int i: return AddConst(FunValue.FromI64(i));
            case double d: return AddConst(FunValue.FromReal(d));
            case bool b: return AddConst(FunValue.FromBool(b));
            case char c: return AddConst(FunValue.FromI64(c));
            case string s when IsReal(type) && double.TryParse(s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                return AddConst(FunValue.FromReal(parsed));
            default: return AddConst(FunValue.FromRef(value));
        }
    }

    // ── Unsupported visitors (fallback to stack VM) ──
    public byte Visit(SuperAnonymFunctionSyntaxNode n) => throw new NotSupportedException("RegisterVM: lambda");
    public byte Visit(AnonymFunctionSyntaxNode n) => throw new NotSupportedException("RegisterVM: lambda");
    public byte Visit(ResultFunCallSyntaxNode n) => throw new NotSupportedException("RegisterVM: hi-order");
    public byte Visit(TryCatchSyntaxNode n) => throw new NotSupportedException("RegisterVM: try-catch");
    public byte Visit(TypedVarDefSyntaxNode n) => throw new NotSupportedException("RegisterVM: typed var");
    public byte Visit(ListOfExpressionsSyntaxNode n) => throw new NotSupportedException("RegisterVM: list");
    public byte Visit(EquationSyntaxNode n) => throw new NotSupportedException("RegisterVM: equation");
    public byte Visit(UserFunctionDefinitionSyntaxNode n) => throw new NotSupportedException("RegisterVM: user func");
    public byte Visit(TypeDeclarationSyntaxNode n) => throw new NotSupportedException();
    public byte Visit(NamedTypeConstructorSyntaxNode n) => throw new NotSupportedException();
    public byte Visit(IfCaseSyntaxNode n) => throw new NotSupportedException();
    public byte Visit(SyntaxTree n) => throw new NotSupportedException();
    public byte Visit(VarDefinitionSyntaxNode n) => throw new NotSupportedException();
}
