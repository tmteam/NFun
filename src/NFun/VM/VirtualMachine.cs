using System;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// Stack-based bytecode VM. Split stack: long[] for values, object[] for references.
/// No LayoutKind.Explicit — JIT can enregister stack operations.
/// </summary>
public static class VirtualMachine {

    [ThreadStatic] private static long[] t_vstack;
    [ThreadStatic] private static object[] t_rstack;
    [ThreadStatic] private static CallFrame[] t_callStack;

    public static void Execute(CompiledProgram program, FunValue[] locals, int maxOps = 10_000_000) {
        var code = program.Code;
        var constants = program.Constants;
        var vs = t_vstack ??= new long[256];     // value stack (int/real/bool)
        var rs = t_rstack ??= new object[256];    // reference stack
        var callStack = t_callStack ??= new CallFrame[64];
        int ip = 0, sp = 0, callDepth = 0;

        try {
        while (true) {
            switch ((Op)code[ip++]) {

                // ── Load / Store ──

                case Op.LoadConstI:
                    vs[sp++] = constants[code[ip++]].I64;
                    break;
                case Op.LoadConstR:
                    vs[sp] = constants[code[ip++]].I64; // bit-copy: Real stored as I64 bits
                    sp++;
                    break;
                case Op.LoadConstRef:
                    rs[sp++] = constants[code[ip++]].Ref;
                    break;
                case Op.LoadLocal: {
                    var slot = code[ip++];
                    vs[sp] = locals[slot].I64;
                    rs[sp] = locals[slot].Ref;
                    sp++;
                    break;
                }
                case Op.StoreLocal: {
                    sp--;
                    var slot = code[ip++];
                    locals[slot].I64 = vs[sp];
                    locals[slot].Ref = rs[sp];
                    rs[sp] = null; // help GC
                    break;
                }
                case Op.LoadNone:
                    rs[sp] = FunnyNone.Instance;
                    sp++;
                    break;

                // ── Integer arithmetic ──

                case Op.AddInt:
                    sp--;
                    vs[sp - 1] += vs[sp];
                    break;
                case Op.SubInt:
                    sp--;
                    vs[sp - 1] -= vs[sp];
                    break;
                case Op.MulInt:
                    sp--;
                    vs[sp - 1] *= vs[sp];
                    break;
                case Op.DivInt:
                    sp--;
                    if (vs[sp] == 0) throw new FunnyRuntimeException("Division by zero");
                    vs[sp - 1] /= vs[sp];
                    break;
                case Op.ModInt:
                    sp--;
                    vs[sp - 1] %= vs[sp];
                    break;
                case Op.NegInt:
                    vs[sp - 1] = -vs[sp - 1];
                    break;

                // ── Real arithmetic (via Unsafe bit reinterpretation) ──

                case Op.AddReal:
                    sp--;
                    SetReal(vs, sp - 1, GetReal(vs, sp - 1) + GetReal(vs, sp));
                    break;
                case Op.SubReal:
                    sp--;
                    SetReal(vs, sp - 1, GetReal(vs, sp - 1) - GetReal(vs, sp));
                    break;
                case Op.MulReal:
                    sp--;
                    SetReal(vs, sp - 1, GetReal(vs, sp - 1) * GetReal(vs, sp));
                    break;
                case Op.DivReal:
                    sp--;
                    SetReal(vs, sp - 1, GetReal(vs, sp - 1) / GetReal(vs, sp));
                    break;
                case Op.ModReal:
                    sp--;
                    SetReal(vs, sp - 1, GetReal(vs, sp - 1) % GetReal(vs, sp));
                    break;
                case Op.NegReal:
                    SetReal(vs, sp - 1, -GetReal(vs, sp - 1));
                    break;
                case Op.PowReal:
                    sp--;
                    SetReal(vs, sp - 1, Math.Pow(GetReal(vs, sp - 1), GetReal(vs, sp)));
                    break;

                // ── Truncation ──
                case Op.TruncU8:  vs[sp - 1] = (byte)vs[sp - 1]; break;
                case Op.TruncU16: vs[sp - 1] = (ushort)vs[sp - 1]; break;
                case Op.TruncU32: vs[sp - 1] = (uint)vs[sp - 1]; break;
                case Op.TruncI16: vs[sp - 1] = (short)vs[sp - 1]; break;
                case Op.TruncI32: vs[sp - 1] = (int)vs[sp - 1]; break;

                // ── Type conversion ──
                case Op.IntToReal: SetReal(vs, sp - 1, (double)vs[sp - 1]); break;
                case Op.RealToInt: vs[sp - 1] = (long)GetReal(vs, sp - 1); break;

                // ── Integer comparison ──
                case Op.EqInt:  sp--; vs[sp - 1] = vs[sp - 1] == vs[sp] ? 1 : 0; break;
                case Op.NeqInt: sp--; vs[sp - 1] = vs[sp - 1] != vs[sp] ? 1 : 0; break;
                case Op.LtInt:  sp--; vs[sp - 1] = vs[sp - 1] < vs[sp] ? 1 : 0; break;
                case Op.LteInt: sp--; vs[sp - 1] = vs[sp - 1] <= vs[sp] ? 1 : 0; break;
                case Op.GtInt:  sp--; vs[sp - 1] = vs[sp - 1] > vs[sp] ? 1 : 0; break;
                case Op.GteInt: sp--; vs[sp - 1] = vs[sp - 1] >= vs[sp] ? 1 : 0; break;

                // ── Real comparison ──
                case Op.EqReal:  sp--; vs[sp - 1] = GetReal(vs, sp - 1) == GetReal(vs, sp) ? 1 : 0; break;
                case Op.LtReal:  sp--; vs[sp - 1] = GetReal(vs, sp - 1) < GetReal(vs, sp) ? 1 : 0; break;
                case Op.LteReal: sp--; vs[sp - 1] = GetReal(vs, sp - 1) <= GetReal(vs, sp) ? 1 : 0; break;
                case Op.GtReal:  sp--; vs[sp - 1] = GetReal(vs, sp - 1) > GetReal(vs, sp) ? 1 : 0; break;
                case Op.GteReal: sp--; vs[sp - 1] = GetReal(vs, sp - 1) >= GetReal(vs, sp) ? 1 : 0; break;

                // ── Logic ──
                case Op.And: sp--; vs[sp - 1] = (vs[sp - 1] != 0 && vs[sp] != 0) ? 1 : 0; break;
                case Op.Or:  sp--; vs[sp - 1] = (vs[sp - 1] != 0 || vs[sp] != 0) ? 1 : 0; break;
                case Op.Not: vs[sp - 1] = vs[sp - 1] == 0 ? 1 : 0; break;

                // ── Bitwise ──
                case Op.BitAnd: sp--; vs[sp - 1] &= vs[sp]; break;
                case Op.BitOr:  sp--; vs[sp - 1] |= vs[sp]; break;
                case Op.BitXor: sp--; vs[sp - 1] ^= vs[sp]; break;
                case Op.BitNot: vs[sp - 1] = ~vs[sp - 1]; break;
                case Op.Shl:    sp--; vs[sp - 1] <<= (int)vs[sp]; break;
                case Op.Shr:    sp--; vs[sp - 1] >>= (int)vs[sp]; break;

                // ── Control flow ──

                case Op.Jump:
                    ip = ReadU16(code, ip);
                    break;
                case Op.JumpIfFalse: {
                    var addr = ReadU16(code, ip);
                    ip += 2;
                    if (vs[--sp] == 0) ip = addr;
                    break;
                }
                case Op.JumpIfTrue: {
                    var addr = ReadU16(code, ip);
                    ip += 2;
                    if (vs[--sp] != 0) ip = addr;
                    break;
                }

                // ── Function calls ──

                case Op.Call: {
                    var funcId = code[ip++];
                    var argc = code[ip++];
                    var func = program.UserFunctions[funcId];
                    callStack[callDepth++] = new CallFrame {
                        ReturnIP = ip, ReturnSP = sp - argc,
                        CallerLocals = locals, FunctionId = funcId
                    };
                    var newLocals = new FunValue[func.LocalsCount];
                    for (int i = argc - 1; i >= 0; i--) {
                        sp--;
                        newLocals[i].I64 = vs[sp];
                        newLocals[i].Ref = rs[sp];
                    }
                    locals = newLocals;
                    ip = func.EntryIP;
                    break;
                }
                case Op.TailCall: {
                    var funcId = code[ip++];
                    var argc = code[ip++];
                    for (int i = argc - 1; i >= 0; i--) {
                        sp--;
                        locals[i].I64 = vs[sp];
                        locals[i].Ref = rs[sp];
                    }
                    ip = program.UserFunctions[funcId].EntryIP;
                    break;
                }
                case Op.Return: {
                    sp--;
                    var retV = vs[sp];
                    var retR = rs[sp];
                    var frame = callStack[--callDepth];
                    ip = frame.ReturnIP;
                    sp = frame.ReturnSP;
                    locals = frame.CallerLocals;
                    vs[sp] = retV;
                    rs[sp] = retR;
                    sp++;
                    break;
                }
                case Op.CallExtern: {
                    var funcId = code[ip++];
                    var argc = code[ip++];
                    var ext = program.ExternFunctions[funcId];
                    var args = new object[argc];
                    for (int i = argc - 1; i >= 0; i--) {
                        sp--;
                        // Box from split stack
                        var fv = new FunValue { I64 = vs[sp], Ref = rs[sp] };
                        args[i] = fv.Box(ext.ArgTypes[i]);
                    }
                    var result = ext.Function.Calc(args);
                    var unboxed = FunValue.Unbox(result, ext.ReturnType);
                    vs[sp] = unboxed.I64;
                    rs[sp] = unboxed.Ref;
                    sp++;
                    break;
                }

                // ── Array ──

                case Op.NewArray: {
                    var count = code[ip++];
                    var arr = new object[count];
                    for (int i = count - 1; i >= 0; i--) {
                        sp--;
                        arr[i] = rs[sp] ?? (object)vs[sp];
                    }
                    rs[sp++] = new Runtime.Arrays.ImmutableFunnyArray(arr, FunnyType.Any);
                    break;
                }
                case Op.GetElement: {
                    sp -= 2;
                    var arr = (Runtime.Arrays.IFunnyArray)rs[sp];
                    var idx = (int)vs[sp + 1];
                    rs[sp++] = arr.GetElementOrNull(idx);
                    break;
                }

                // ── Struct ──

                case Op.NewStruct: {
                    var layoutId = code[ip++];
                    var fieldCount = code[ip++];
                    var layout = program.StructLayouts[layoutId];
                    var fields = new (string, object)[fieldCount];
                    for (int i = fieldCount - 1; i >= 0; i--) {
                        sp--;
                        var fv = new FunValue { I64 = vs[sp], Ref = rs[sp] };
                        fields[i] = (layout.FieldNames[i], fv.Box(layout.FieldTypes[i]));
                    }
                    rs[sp++] = FunnyStruct.Create(fields);
                    break;
                }
                case Op.GetField: {
                    var fieldIdx = code[ip++];
                    var layoutId = code[ip++];
                    var layout = program.StructLayouts[layoutId];
                    sp--;
                    var s = (FunnyStruct)rs[sp];
                    var fieldVal = s.GetValue(layout.FieldNames[fieldIdx]);
                    var unboxed = FunValue.Unbox(fieldVal, layout.FieldTypes[fieldIdx]);
                    vs[sp] = unboxed.I64;
                    rs[sp] = unboxed.Ref;
                    sp++;
                    break;
                }

                // ── Optional ──

                case Op.IsNone:
                    vs[sp - 1] = rs[sp - 1] is FunnyNone ? 1 : 0;
                    break;
                case Op.Coalesce:
                    sp--;
                    if (rs[sp - 1] is FunnyNone) {
                        vs[sp - 1] = vs[sp];
                        rs[sp - 1] = rs[sp];
                    }
                    break;
                case Op.Unwrap:
                    if (rs[sp - 1] is FunnyNone)
                        throw new FunnyRuntimeException("Force unwrap of none value");
                    break;

                // ── Stack ──
                case Op.Dup:
                    vs[sp] = vs[sp - 1];
                    rs[sp] = rs[sp - 1];
                    sp++;
                    break;
                case Op.Pop:
                    sp--;
                    break;

                // ── Superinstructions ──

                case Op.AddLocalConstI: {
                    var slot = code[ip++];
                    var cidx = code[ip++];
                    vs[sp++] = locals[slot].I64 + constants[cidx].I64;
                    break;
                }
                case Op.SubLocalConstI: {
                    var slot = code[ip++];
                    var cidx = code[ip++];
                    vs[sp++] = locals[slot].I64 - constants[cidx].I64;
                    break;
                }
                case Op.MulLocalConstI: {
                    var slot = code[ip++];
                    var cidx = code[ip++];
                    vs[sp++] = locals[slot].I64 * constants[cidx].I64;
                    break;
                }
                case Op.AddConstConstI: {
                    vs[sp++] = constants[code[ip++]].I64 + constants[code[ip++]].I64;
                    break;
                }
                case Op.MulConstConstI: {
                    vs[sp++] = constants[code[ip++]].I64 * constants[code[ip++]].I64;
                    break;
                }
                case Op.AddTopConstI:
                    vs[sp - 1] += constants[code[ip++]].I64;
                    break;
                case Op.MulTopConstI:
                    vs[sp - 1] *= constants[code[ip++]].I64;
                    break;
                case Op.StoreHalt:
                    sp--;
                    locals[code[ip++]].I64 = vs[sp];
                    locals[code[ip - 1]].Ref = rs[sp];
                    return;
                case Op.AddLocalConstR: {
                    var slot = code[ip++];
                    var cidx = code[ip++];
                    SetReal(vs, sp, GetReal(locals[slot].I64) + GetRealConst(constants, cidx));
                    sp++;
                    break;
                }
                case Op.MulLocalConstR: {
                    var slot = code[ip++];
                    var cidx = code[ip++];
                    SetReal(vs, sp, GetReal(locals[slot].I64) * GetRealConst(constants, cidx));
                    sp++;
                    break;
                }
                case Op.AddTopConstR:
                    SetReal(vs, sp - 1, GetReal(vs, sp - 1) + GetRealConst(constants, code[ip++]));
                    break;
                case Op.MulTopConstR:
                    SetReal(vs, sp - 1, GetReal(vs, sp - 1) * GetRealConst(constants, code[ip++]));
                    break;

                // ── Halt ──
                case Op.Halt:
                    return;

                default:
                    throw new FunnyRuntimeException($"Unknown opcode {code[ip - 1]} at IP={ip - 1}");
            }
        }
        }
        catch (FunnyRuntimeException ex) {
            var handler = FindHandler(ip - 1, program.ExceptionHandlers);
            if (handler != null) {
                if (handler.Value.ErrorVarSlot >= 0) {
                    var errorStruct = FunnyStruct.Create(
                        ("message", ex.Message ?? ""),
                        ("data", (object)(ex is FunnyRuntimeException fre ? fre.OopsData : null)));
                    locals[handler.Value.ErrorVarSlot] = FunValue.FromRef(errorStruct);
                }
                // For exception handler resume, we'd need to re-enter Execute.
                // Simplified: set IP and recurse.
                ip = handler.Value.CatchStartIP;
                Execute(program, locals, maxOps); // recursive resume
                return;
            }
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double GetReal(long bits) {
        var l = bits;
        return Unsafe.As<long, double>(ref l);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double GetRealConst(FunValue[] constants, int idx) {
        var l = constants[idx].I64;
        return Unsafe.As<long, double>(ref l);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double GetReal(long[] vs, int idx) =>
        Unsafe.As<long, double>(ref vs[idx]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetReal(long[] vs, int idx, double val) =>
        vs[idx] = Unsafe.As<double, long>(ref val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadU16(byte[] code, int offset) =>
        code[offset] | (code[offset + 1] << 8);

    private static ExceptionHandler? FindHandler(int ip, ExceptionHandler[] handlers) {
        for (int i = 0; i < handlers.Length; i++) {
            if (ip >= handlers[i].TryStartIP && ip < handlers[i].TryEndIP)
                return handlers[i];
        }
        return null;
    }
}
