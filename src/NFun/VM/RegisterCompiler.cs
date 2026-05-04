using System;
using System.Collections.Generic;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
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
    private readonly List<byte> _code = new(32);
    private readonly List<FunValue> _constants = new(8);
    private readonly Dictionary<string, int> _slots = new(StringComparer.OrdinalIgnoreCase);
    private int _nextSlot;

    private RegisterCompiler(TypeInferenceResults typeResults, TicTypesConverter typesConverter) {
        _typeResults = typeResults;
        _typesConverter = typesConverter;
    }

    public static (byte[] Code, FunValue[] Constants, int LocalsCount, Dictionary<string, int> Slots)
        Compile(SyntaxTree tree, TypeInferenceResults typeResults, TicTypesConverter typesConverter) {
        var c = new RegisterCompiler(typeResults, typesConverter);

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

        return (c._code.ToArray(), c._constants.ToArray(), c._nextSlot, c._slots);
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
        var ci = AddConstValue(node.Value, type);
        Emit(IsReal(type) ? RegisterOp.LoadC_D : RegisterOp.LoadC_I, r, (byte)ci, 0);
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
            var ci = AddConstValue(cv.FunnyValue, cv.Type);
            Emit(IsReal(cv.Type) ? RegisterOp.LoadC_D : RegisterOp.LoadC_I, r, (byte)ci, 0);
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

        RegisterOp op = node.Op switch {
            BinOp.Add => isReal ? RegisterOp.AddRR_D : RegisterOp.AddRR_I,
            BinOp.Subtract => isReal ? RegisterOp.SubRR_D : RegisterOp.SubRR_I,
            BinOp.Multiply => isReal ? RegisterOp.MulRR_D : RegisterOp.MulRR_I,
            BinOp.DivideInt => RegisterOp.DivRR_I,
            BinOp.DivideReal => RegisterOp.DivRR_D,
            BinOp.Remainder => RegisterOp.ModRR_I,
            BinOp.More => RegisterOp.GtRR_I,
            BinOp.Less => RegisterOp.LtRR_I,
            BinOp.MoreOrEqual => RegisterOp.GteRR_I,
            BinOp.LessOrEqual => RegisterOp.LteRR_I,
            BinOp.Equal => RegisterOp.EqRR_I,
            BinOp.NotEqual => RegisterOp.NeqRR_I,
            BinOp.And => RegisterOp.AndRR,
            BinOp.Or => RegisterOp.OrRR,
            _ => throw new NotSupportedException($"RegisterVM: BinOp {node.Op} not supported"),
        };

        Emit(op, dst, left, right);

        // Bool/Char equality should use I64 comparison (same as stack VM fix)
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
            default:
                throw new NotSupportedException($"RegisterVM: UnOp {node.Op}");
        }
        return dst;
    }

    public byte Visit(IfThenElseSyntaxNode node) {
        var dst = (byte)AllocTemp();
        var jumpToEnds = new List<int>();

        // if(cond1) expr1 if(cond2) expr2 ... else exprN
        for (int i = 0; i < node.Ifs.Length; i++) {
            var condReg = CompileExpr(node.Ifs[i].Condition);

            // JmpIfNot → next branch
            int jmpToNext = _code.Count;
            Emit(RegisterOp.JmpIfNot, condReg, 0, 0);

            // Then branch
            var thenReg = CompileExpr(node.Ifs[i].Expression);
            if (thenReg != dst) Emit(RegisterOp.Mov, dst, thenReg, 0);

            // Jmp → end
            jumpToEnds.Add(_code.Count);
            Emit(RegisterOp.Jmp, 0, 0, 0);

            // Patch JmpIfNot → here
            PatchJump(jmpToNext, _code.Count);
        }

        // Else branch
        var elseReg = CompileExpr(node.ElseExpr);
        if (elseReg != dst) Emit(RegisterOp.Mov, dst, elseReg, 0);

        // Patch all Jmp → end
        int end = _code.Count;
        foreach (var addr in jumpToEnds)
            PatchJump(addr, end);

        return dst;
    }

    private void PatchJump(int instrAddr, int target) {
        _code[instrAddr + 2] = (byte)(target >> 8);
        _code[instrAddr + 3] = (byte)(target & 0xFF);
    }

    public byte Visit(FunCallSyntaxNode node) {
        // For now, only handle max/min natively
        var id = node.Id;
        var args = node.Args;
        if (id == "max" && args.Length == 2) {
            var a = CompileExpr(args[0]);
            var b = CompileExpr(args[1]);
            var dst = (byte)AllocTemp();
            Emit(RegisterOp.MaxRR_I, dst, a, b);
            return dst;
        }
        if (id == "min" && args.Length == 2) {
            var a = CompileExpr(args[0]);
            var b = CompileExpr(args[1]);
            var dst = (byte)AllocTemp();
            Emit(RegisterOp.MinRR_I, dst, a, b);
            return dst;
        }
        throw new NotSupportedException($"RegisterVM: function {id}/{args.Length} not supported");
    }

    // ── Helpers ──

    private int AddConstValue(object value, FunnyType type) {
        switch (value) {
            case long l: return AddConst(FunValue.FromI64(l));
            case ulong u: return AddConst(FunValue.FromI64((long)u));
            case int i: return AddConst(FunValue.FromI64(i));
            case double d: return AddConst(FunValue.FromReal(d));
            case bool b: return AddConst(FunValue.FromBool(b));
            case string s when IsReal(type) && double.TryParse(s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                return AddConst(FunValue.FromReal(parsed));
            default: return AddConst(FunValue.FromRef(value));
        }
    }

    // ── Unsupported visitors (fallback to stack VM) ──
    public byte Visit(ArraySyntaxNode n) => throw new NotSupportedException("RegisterVM: arrays");
    public byte Visit(StructInitSyntaxNode n) => throw new NotSupportedException("RegisterVM: structs");
    public byte Visit(StructFieldAccessSyntaxNode n) => throw new NotSupportedException("RegisterVM: field access");
    public byte Visit(SuperAnonymFunctionSyntaxNode n) => throw new NotSupportedException("RegisterVM: lambda");
    public byte Visit(AnonymFunctionSyntaxNode n) => throw new NotSupportedException("RegisterVM: lambda");
    public byte Visit(ResultFunCallSyntaxNode n) => throw new NotSupportedException("RegisterVM: hi-order");
    public byte Visit(ComparisonChainSyntaxNode n) => throw new NotSupportedException("RegisterVM: chain");
    public byte Visit(DefaultValueSyntaxNode n) => throw new NotSupportedException("RegisterVM: default");
    public byte Visit(TryCatchSyntaxNode n) => throw new NotSupportedException("RegisterVM: try-catch");
    public byte Visit(TypedVarDefSyntaxNode n) => throw new NotSupportedException("RegisterVM: typed var");
    public byte Visit(ListOfExpressionsSyntaxNode n) => throw new NotSupportedException("RegisterVM: list");
    public byte Visit(EquationSyntaxNode n) => throw new NotSupportedException("RegisterVM: equation");
    public byte Visit(UserFunctionDefinitionSyntaxNode n) => throw new NotSupportedException("RegisterVM: user func");
    public byte Visit(TypeDeclarationSyntaxNode n) => throw new NotSupportedException();
    public byte Visit(NamedTypeConstructorSyntaxNode n) => throw new NotSupportedException();
    public byte Visit(IpAddressConstantSyntaxNode n) => throw new NotSupportedException("RegisterVM: IP");
    public byte Visit(IfCaseSyntaxNode n) => throw new NotSupportedException();
    public byte Visit(SyntaxTree n) => throw new NotSupportedException();
    public byte Visit(VarDefinitionSyntaxNode n) => throw new NotSupportedException();
}
