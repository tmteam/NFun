using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// Register-based VM. No stack, no sp — registers = locals[] array.
/// Fixed 4-byte instructions: [op:8][dst:8][src1:8][src2:8].
/// </summary>
public static class RegisterVM {

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Execute(byte[] code, FunValue[] locals, FunValue[] constants) {
        Execute(code, locals, constants, null, Array.Empty<ExternFunc>(), Array.Empty<UserFunc>(), 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Execute(byte[] code, FunValue[] locals, FunValue[] constants,
        CompiledProgram program, ExternFunc[] externFuncs,
        UserFunc[] userFuncs, int startIP) {

        int ip = startIP;

        // HOT LOOP: only ~15 most common opcodes. JIT sees a tiny method → aggressive optimization.
        while (true) {
            var dst = code[ip + 1];
            var s1 = code[ip + 2];
            var s2 = code[ip + 3];
            switch ((RegisterOp)code[ip]) {
                case RegisterOp.AddRI_I:  ip+=4; locals[dst].I64 = locals[s1].I64 + constants[s2].I64; continue;
                case RegisterOp.MulRI_I:  ip+=4; locals[dst].I64 = locals[s1].I64 * constants[s2].I64; continue;
                case RegisterOp.SubRI_I:  ip+=4; locals[dst].I64 = locals[s1].I64 - constants[s2].I64; continue;
                case RegisterOp.AddRR_I:  ip+=4; locals[dst].I64 = locals[s1].I64 + locals[s2].I64; continue;
                case RegisterOp.MulRR_I:  ip+=4; locals[dst].I64 = locals[s1].I64 * locals[s2].I64; continue;
                case RegisterOp.SubRR_I:  ip+=4; locals[dst].I64 = locals[s1].I64 - locals[s2].I64; continue;
                case RegisterOp.AddRI_D:  ip+=4; locals[dst].Real = locals[s1].Real + constants[s2].Real; continue;
                case RegisterOp.MulRI_D:  ip+=4; locals[dst].Real = locals[s1].Real * constants[s2].Real; continue;
                case RegisterOp.Mov:      ip+=4; locals[dst] = locals[s1]; continue;
                case RegisterOp.LoadC_I:  ip+=4; locals[dst].I64 = constants[s1].I64; continue;
                case RegisterOp.LoadC_D:  ip+=4; locals[dst].I64 = constants[s1].I64; continue;
                case RegisterOp.GtRI_I:   ip+=4; locals[dst].I64 = locals[s1].I64 > constants[s2].I64 ? 1 : 0; continue;
                case RegisterOp.LtRI_I:   ip+=4; locals[dst].I64 = locals[s1].I64 < constants[s2].I64 ? 1 : 0; continue;
                case RegisterOp.NegR_I:   ip+=4; locals[dst].I64 = -locals[s1].I64; continue;
                case RegisterOp.NotR:     ip+=4; locals[dst].I64 = locals[s1].I64 == 0 ? 1 : 0; continue;
                case RegisterOp.I2D:      ip+=4; locals[dst].Real = locals[s1].I64; continue;
                case RegisterOp.MaxRR_I:  ip+=4; locals[dst].I64 = Math.Max(locals[s1].I64, locals[s2].I64); continue;
                case RegisterOp.JmpIfNot: if (locals[dst].I64 == 0) ip = (s1 << 8) | s2; else ip += 4; continue;
                case RegisterOp.Jmp:      ip = (s1 << 8) | s2; continue;
                case RegisterOp.Halt:     return;
                default:
                    ip += 4;
                    DispatchCold((RegisterOp)code[ip - 4], dst, s1, s2, code, locals, constants, program, externFuncs, userFuncs, ref ip);
                    continue;
            }
        }
    }

    /// <summary>Cold path: rare opcodes. Separate method to keep hot loop small for JIT.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DispatchCold(RegisterOp op, byte dst, byte s1, byte s2,
        byte[] code, FunValue[] locals, FunValue[] constants,
        CompiledProgram program, ExternFunc[] externFuncs, UserFunc[] userFuncs, ref int ip) {
        switch (op) {
            // ── All opcodes (cold duplicates hot ones for correctness) ──
            case RegisterOp.AddRR_I:  locals[dst].I64 = locals[s1].I64 + locals[s2].I64; break;
            case RegisterOp.AddRI_I:  locals[dst].I64 = locals[s1].I64 + constants[s2].I64; break;
            case RegisterOp.SubRR_I:  locals[dst].I64 = locals[s1].I64 - locals[s2].I64; break;
            case RegisterOp.SubRI_I:  locals[dst].I64 = locals[s1].I64 - constants[s2].I64; break;
            case RegisterOp.MulRR_I:  locals[dst].I64 = locals[s1].I64 * locals[s2].I64; break;
            case RegisterOp.MulRI_I:  locals[dst].I64 = locals[s1].I64 * constants[s2].I64; break;
                case RegisterOp.DivRR_I:
                    if (locals[s2].I64 == 0) throw new FunnyRuntimeException("Division by zero");
                    locals[dst].I64 = locals[s1].I64 / locals[s2].I64; break;
                case RegisterOp.ModRR_I:  locals[dst].I64 = locals[s1].I64 % locals[s2].I64; break;
                case RegisterOp.PowRR_I:  locals[dst].I64 = IntPow(locals[s1].I64, locals[s2].I64); break;
                case RegisterOp.NegR_I:   locals[dst].I64 = -locals[s1].I64; break;

                // ── Real arithmetic ──
                case RegisterOp.AddRR_D:  locals[dst].Real = locals[s1].Real + locals[s2].Real; break;
                case RegisterOp.AddRI_D:  locals[dst].Real = locals[s1].Real + constants[s2].Real; break;
                case RegisterOp.SubRR_D:  locals[dst].Real = locals[s1].Real - locals[s2].Real; break;
                case RegisterOp.SubRI_D:  locals[dst].Real = locals[s1].Real - constants[s2].Real; break;
                case RegisterOp.MulRR_D:  locals[dst].Real = locals[s1].Real * locals[s2].Real; break;
                case RegisterOp.MulRI_D:  locals[dst].Real = locals[s1].Real * constants[s2].Real; break;
                case RegisterOp.DivRR_D:  locals[dst].Real = locals[s1].Real / locals[s2].Real; break;
                case RegisterOp.ModRR_D:  locals[dst].Real = locals[s1].Real % locals[s2].Real; break;
                case RegisterOp.PowRR_D:  locals[dst].Real = Math.Pow(locals[s1].Real, locals[s2].Real); break;
                case RegisterOp.NegR_D:   locals[dst].Real = -locals[s1].Real; break;

                // ── Comparison (int) ──
                case RegisterOp.GtRR_I:   locals[dst].I64 = locals[s1].I64 > locals[s2].I64 ? 1 : 0; break;
                case RegisterOp.GtRI_I:   locals[dst].I64 = locals[s1].I64 > constants[s2].I64 ? 1 : 0; break;
                case RegisterOp.GteRR_I:  locals[dst].I64 = locals[s1].I64 >= locals[s2].I64 ? 1 : 0; break;
                case RegisterOp.LtRR_I:   locals[dst].I64 = locals[s1].I64 < locals[s2].I64 ? 1 : 0; break;
                case RegisterOp.LtRI_I:   locals[dst].I64 = locals[s1].I64 < constants[s2].I64 ? 1 : 0; break;
                case RegisterOp.LteRR_I:  locals[dst].I64 = locals[s1].I64 <= locals[s2].I64 ? 1 : 0; break;
                case RegisterOp.EqRR_I:   locals[dst].I64 = locals[s1].I64 == locals[s2].I64 ? 1 : 0; break;
                case RegisterOp.NeqRR_I:  locals[dst].I64 = locals[s1].I64 != locals[s2].I64 ? 1 : 0; break;

                // ── Comparison (real) ──
                case RegisterOp.GtRR_D:   locals[dst].I64 = locals[s1].Real > locals[s2].Real ? 1 : 0; break;
                case RegisterOp.LtRR_D:   locals[dst].I64 = locals[s1].Real < locals[s2].Real ? 1 : 0; break;
                case RegisterOp.GteRR_D:  locals[dst].I64 = locals[s1].Real >= locals[s2].Real ? 1 : 0; break;
                case RegisterOp.LteRR_D:  locals[dst].I64 = locals[s1].Real <= locals[s2].Real ? 1 : 0; break;
                case RegisterOp.EqRR_D:   locals[dst].I64 = locals[s1].Real == locals[s2].Real ? 1 : 0; break;

                // ── Reference comparison ──
                case RegisterOp.EqRef:    locals[dst].I64 = Equals(locals[s1].Ref, locals[s2].Ref) ? 1 : 0; break;
                case RegisterOp.NeqRef:   locals[dst].I64 = !Equals(locals[s1].Ref, locals[s2].Ref) ? 1 : 0; break;

                // ── Logic ──
                case RegisterOp.AndRR:    locals[dst].I64 = (locals[s1].I64 != 0 && locals[s2].I64 != 0) ? 1 : 0; break;
                case RegisterOp.OrRR:     locals[dst].I64 = (locals[s1].I64 != 0 || locals[s2].I64 != 0) ? 1 : 0; break;
                case RegisterOp.NotR:     locals[dst].I64 = locals[s1].I64 == 0 ? 1 : 0; break;
                case RegisterOp.XorRR:    locals[dst].I64 = locals[s1].I64 ^ locals[s2].I64; break;

                // ── Bitwise ──
                case RegisterOp.BitAndRR: locals[dst].I64 = locals[s1].I64 & locals[s2].I64; break;
                case RegisterOp.BitOrRR:  locals[dst].I64 = locals[s1].I64 | locals[s2].I64; break;
                case RegisterOp.BitXorRR: locals[dst].I64 = locals[s1].I64 ^ locals[s2].I64; break;
                case RegisterOp.BitNotR:  locals[dst].I64 = ~locals[s1].I64; break;
                case RegisterOp.ShlRR:    locals[dst].I64 = locals[s1].I64 << (int)locals[s2].I64; break;
                case RegisterOp.ShrRR:    locals[dst].I64 = locals[s1].I64 >> (int)locals[s2].I64; break;

                // ── Control flow ──
                case RegisterOp.Jmp:      ip = (s1 << 8) | s2; break;
                case RegisterOp.JmpIfNot: if (locals[dst].I64 == 0) ip = (s1 << 8) | s2; break;
                case RegisterOp.JmpIf:    if (locals[dst].I64 != 0) ip = (s1 << 8) | s2; break;

                // ── Data movement ──
                case RegisterOp.Mov:      locals[dst] = locals[s1]; break;
                case RegisterOp.LoadC_I:  locals[dst].I64 = constants[s1].I64; break;
                case RegisterOp.LoadC_D:  locals[dst].I64 = constants[s1].I64; break; // same bits
                case RegisterOp.LoadC_Ref:locals[dst].Ref = constants[s1].Ref; break;
                case RegisterOp.LoadNone: locals[dst] = FunValue.None; break;
                case RegisterOp.Halt:     return;

                // ── Type conversion ──
                case RegisterOp.I2D:      locals[dst].Real = locals[s1].I64; break;
                case RegisterOp.D2I:      locals[dst].I64 = (long)locals[s1].Real; break;
                case RegisterOp.Truncate:
                    locals[dst].I64 = s2 switch {
                        0 => (byte)locals[s1].I64,
                        1 => (ushort)locals[s1].I64,
                        2 => (uint)locals[s1].I64,
                        3 => (short)locals[s1].I64,
                        4 => (int)locals[s1].I64,
                        _ => locals[s1].I64,
                    }; break;
                case RegisterOp.BoxInt:   locals[dst].Ref = (object)locals[s1].I64; break;
                case RegisterOp.BoxReal:  locals[dst].Ref = (object)locals[s1].Real; break;
                case RegisterOp.BoxBool:  locals[dst].Ref = (object)(locals[s1].I64 != 0); break;

                // ── Native functions ──
                case RegisterOp.MaxRR_I:  locals[dst].I64 = Math.Max(locals[s1].I64, locals[s2].I64); break;
                case RegisterOp.MinRR_I:  locals[dst].I64 = Math.Min(locals[s1].I64, locals[s2].I64); break;
                case RegisterOp.AbsR_I:   locals[dst].I64 = Math.Abs(locals[s1].I64); break;
                case RegisterOp.MaxRR_D:  locals[dst].Real = Math.Max(locals[s1].Real, locals[s2].Real); break;
                case RegisterOp.MinRR_D:  locals[dst].Real = Math.Min(locals[s1].Real, locals[s2].Real); break;
                case RegisterOp.AbsR_D:   locals[dst].Real = Math.Abs(locals[s1].Real); break;
                case RegisterOp.ToTextI:  locals[dst] = new FunValue { Ref = new TextFunnyArray(locals[s1].I64.ToString()) }; break;
                case RegisterOp.ToTextD:  locals[dst] = new FunValue { Ref = new TextFunnyArray(locals[s1].Real.ToString(System.Globalization.CultureInfo.InvariantCulture)) }; break;

                // ── Arrays ──
                case RegisterOp.NewArr: {
                    int count = s1;
                    var typeIdx = s2;
                    var elemType = program?.TypeTable != null && typeIdx < program.TypeTable.Length
                        ? program.TypeTable[typeIdx] : FunnyType.Any;
                    if (VirtualMachine.IsPrimitiveType(elemType.BaseType)) {
                        var arr = new FunValue[count];
                        for (int i = 0; i < count; i++)
                            arr[i] = locals[code[ip + i]];
                        locals[dst].Ref = new FunValueArray(arr, elemType);
                    } else {
                        var arr = new object[count];
                        for (int i = 0; i < count; i++)
                            arr[i] = locals[code[ip + i]].Box(elemType);
                        locals[dst].Ref = new ImmutableFunnyArray(arr, elemType);
                    }
                    ip += ((count + 3) / 4) * 4; // pad to 4-byte boundary
                    break;
                }
                case RegisterOp.GetElem: {
                    var arr = (IFunnyArray)locals[s1].Ref;
                    var idx = (int)locals[s2].I64;
                    if (arr is FunValueArray fva)
                        locals[dst] = fva.GetDirect(idx);
                    else
                        locals[dst] = UnboxElement(arr.GetElementOrNull(idx));
                    break;
                }
                case RegisterOp.GetElemSafe: {
                    var arrRef = locals[s1].Ref;
                    if (arrRef is FunnyNone || arrRef == null) { locals[dst] = FunValue.None; break; }
                    var arr = (IFunnyArray)arrRef;
                    var idx = (int)locals[s2].I64;
                    if (idx < 0 || idx >= arr.Count) { locals[dst] = FunValue.None; break; }
                    if (arr is FunValueArray fva2)
                        locals[dst] = fva2.GetDirect(idx);
                    else
                        locals[dst] = UnboxElement(arr.GetElementOrNull(idx));
                    break;
                }

                // ── Structs ──
                case RegisterOp.NewStruct: {
                    var layoutId = s1;
                    var fieldCount = s2;
                    var layout = program.StructLayouts[layoutId];
                    var fields = new (string, object)[fieldCount];
                    for (int i = 0; i < fieldCount; i++)
                        fields[i] = (layout.FieldNames[i], locals[code[ip + i]].Box(layout.FieldTypes[i]));
                    locals[dst].Ref = FunnyStruct.Create(fields);
                    ip += ((fieldCount + 3) / 4) * 4;
                    break;
                }
                case RegisterOp.GetField: {
                    var fieldIdx = s1;
                    var layoutId = s2;
                    var layout = program.StructLayouts[layoutId];
                    locals[dst] = FunValue.Unbox(
                        ((FunnyStruct)locals[dst].Ref).GetValue(layout.FieldNames[fieldIdx]),
                        layout.FieldTypes[fieldIdx]);
                    break;
                }
                case RegisterOp.GetFieldSafe: {
                    var src = locals[dst]; // struct is in dst register (will be overwritten)
                    if (src.Ref is FunnyNone || src.Ref == null) { locals[dst] = FunValue.None; break; }
                    var layout = program.StructLayouts[s2];
                    locals[dst] = FunValue.Unbox(
                        ((FunnyStruct)src.Ref).GetValue(layout.FieldNames[s1]),
                        layout.FieldTypes[s1]);
                    break;
                }

                // ── Optional ──
                case RegisterOp.IsNone:   locals[dst].I64 = locals[s1].Ref is FunnyNone ? 1 : 0; break;
                case RegisterOp.Coalesce: locals[dst] = locals[s1].Ref is FunnyNone ? locals[s2] : locals[s1]; break;
                case RegisterOp.Unwrap:
                    if (locals[s1].Ref is FunnyNone) throw new FunnyRuntimeException("Force unwrap of none value");
                    locals[dst] = locals[s1]; break;

                // ── Function calls ──
                case RegisterOp.CallExt: {
                    ref var ext = ref externFuncs[s1];
                    var baseR = s2;
                    var argc = ext.ArgTypes.Length;
                    object cr;
                    if (ext.ArityKind == 2) {
                        cr = ((Interpretation.Functions.FunctionWithTwoArgs)ext.Function)
                            .Calc(locals[baseR].Box(ext.ArgTypes[0]), locals[baseR + 1].Box(ext.ArgTypes[1]));
                    } else if (ext.ArityKind == 1) {
                        cr = ((Interpretation.Functions.FunctionWithSingleArg)ext.Function)
                            .Calc(locals[baseR].Box(ext.ArgTypes[0]));
                    } else {
                        var args = new object[argc];
                        for (int i = 0; i < argc; i++) args[i] = locals[baseR + i].Box(ext.ArgTypes[i]);
                        cr = ext.Function.Calc(args);
                    }
                    locals[dst] = FunValue.Unbox(cr, ext.ReturnType);
                    break;
                }
                case RegisterOp.CallUser: {
                    // Simple: save IP, jump to function entry, restore on Return
                    // For now, use re-entrant execution (like BytecodeLambda)
                    ref var func = ref userFuncs[s1];
                    var baseR = s2;
                    var newLocals = new FunValue[func.LocalsCount];
                    for (int i = 0; i < func.ArgTypes.Length; i++)
                        newLocals[i] = locals[baseR + i];
                    Execute(code, newLocals, constants, program, externFuncs, userFuncs, func.EntryIP);
                    locals[dst] = newLocals[0]; // convention: return value in r0
                    break;
                }
                case RegisterOp.Return:
                    locals[0] = locals[s1]; // put return value in r0
                    return;

                case RegisterOp.MakeClosure: {
                    var funcId = s1;
                    var captureCount = s2;
                    FunValue[] captured = null;
                    if (captureCount > 0) {
                        captured = new FunValue[captureCount];
                        for (int i = 0; i < captureCount; i++)
                            captured[i] = locals[code[ip + i]];
                        ip += ((captureCount + 3) / 4) * 4;
                    }
                    ref var uf = ref userFuncs[funcId];
                    locals[dst].Ref = uf.ArgTypes.Length == 1
                        ? (object)new BytecodeLambda1(program, funcId, captured)
                        : new BytecodeLambda2(program, funcId, captured);
                    break;
                }

                default: throw new FunnyRuntimeException($"Unknown register opcode 0x{(byte)op:X2} at IP={ip}");
        }
    }

    /// <summary>Converts a boxed element from IFunnyArray into FunValue with both I64 and Ref populated.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FunValue UnboxElement(object elem) {
        if (elem is char c) return new FunValue { I64 = c, Ref = elem };
        if (elem is int i) return new FunValue { I64 = i, Ref = elem };
        if (elem is long l) return new FunValue { I64 = l, Ref = elem };
        if (elem is bool b) return new FunValue { I64 = b ? 1 : 0, Ref = elem };
        if (elem is double d) return new FunValue { Real = d, Ref = elem };
        if (elem is byte bt) return new FunValue { I64 = bt, Ref = elem };
        if (elem is short s) return new FunValue { I64 = s, Ref = elem };
        return new FunValue { Ref = elem };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long IntPow(long b, long e) {
        if (e < 0) return 0;
        long result = 1;
        while (e > 0) {
            if ((e & 1) == 1) result *= b;
            b *= b;
            e >>= 1;
        }
        return result;
    }
}
