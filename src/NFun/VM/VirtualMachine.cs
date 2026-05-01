using System;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// Stack-based bytecode VM. Single FunValue[] stack (no split).
/// try/catch outside loop. Stack pre-allocated on VMRuntime.
/// </summary>
public static class VirtualMachine {

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe void Execute(CompiledProgram program, FunValue[] locals,
        FunValue[] stack, CallFrame[] callStack, int maxOps = 10_000_000) {

        int callDepth = 0;

        fixed (byte* codeBase = program.Code)
        fixed (FunValue* constBase = program.Constants)
        fixed (FunValue* stackBase = stack) {

        byte* ip = codeBase;
        FunValue* sp = stackBase;

        try {
        while (true) {
            switch ((Op)(*ip++)) {

                case Op.LoadConstI:
                    sp->I64 = constBase[*ip++].I64;
                    sp++;
                    break;
                case Op.LoadConstR:
                    sp->I64 = constBase[*ip++].I64;
                    sp++;
                    break;
                case Op.LoadConstRef:
                    sp->Ref = constBase[*ip++].Ref;
                    sp++;
                    break;
                case Op.LoadLocal:
                    *sp++ = locals[*ip++]; // locals stays managed (Call can swap it)
                    break;
                case Op.StoreLocal:
                    locals[*ip++] = *--sp;
                    break;
                case Op.LoadNone:
                    *sp++ = FunValue.None;
                    break;

                // ── Integer arithmetic ──
                case Op.AddInt: sp--; (sp - 1)->I64 += sp->I64; break;
                case Op.SubInt: sp--; (sp - 1)->I64 -= sp->I64; break;
                case Op.MulInt: sp--; (sp - 1)->I64 *= sp->I64; break;
                case Op.DivInt:
                    sp--;
                    if (sp->I64 == 0) throw new FunnyRuntimeException("Division by zero");
                    (sp - 1)->I64 /= sp->I64;
                    break;
                case Op.ModInt: sp--; (sp - 1)->I64 %= sp->I64; break;
                case Op.NegInt: (sp - 1)->I64 = -(sp - 1)->I64; break;

                // ── Real arithmetic ──
                case Op.AddReal: sp--; (sp - 1)->Real += sp->Real; break;
                case Op.SubReal: sp--; (sp - 1)->Real -= sp->Real; break;
                case Op.MulReal: sp--; (sp - 1)->Real *= sp->Real; break;
                case Op.DivReal: sp--; (sp - 1)->Real /= sp->Real; break;
                case Op.ModReal: sp--; (sp - 1)->Real %= sp->Real; break;
                case Op.NegReal: (sp - 1)->Real = -(sp - 1)->Real; break;
                case Op.PowReal: sp--; (sp - 1)->Real = Math.Pow((sp - 1)->Real, sp->Real); break;

                // ── Truncation ──
                case Op.TruncU8:  (sp - 1)->I64 = (byte)(sp - 1)->I64; break;
                case Op.TruncU16: (sp - 1)->I64 = (ushort)(sp - 1)->I64; break;
                case Op.TruncU32: (sp - 1)->I64 = (uint)(sp - 1)->I64; break;
                case Op.TruncI16: (sp - 1)->I64 = (short)(sp - 1)->I64; break;
                case Op.TruncI32: (sp - 1)->I64 = (int)(sp - 1)->I64; break;

                // ── Type conversion ──
                case Op.IntToReal: (sp - 1)->Real = (sp - 1)->I64; break;
                case Op.RealToInt: (sp - 1)->I64 = (long)(sp - 1)->Real; break;

                // ── Integer comparison ──
                case Op.EqInt:  sp--; (sp - 1)->I64 = (sp - 1)->I64 == sp->I64 ? 1 : 0; break;
                case Op.NeqInt: sp--; (sp - 1)->I64 = (sp - 1)->I64 != sp->I64 ? 1 : 0; break;
                case Op.LtInt:  sp--; (sp - 1)->I64 = (sp - 1)->I64 < sp->I64 ? 1 : 0; break;
                case Op.LteInt: sp--; (sp - 1)->I64 = (sp - 1)->I64 <= sp->I64 ? 1 : 0; break;
                case Op.GtInt:  sp--; (sp - 1)->I64 = (sp - 1)->I64 > sp->I64 ? 1 : 0; break;
                case Op.GteInt: sp--; (sp - 1)->I64 = (sp - 1)->I64 >= sp->I64 ? 1 : 0; break;

                // ── Real comparison ──
                case Op.EqReal:  sp--; (sp - 1)->I64 = (sp - 1)->Real == sp->Real ? 1 : 0; break;
                case Op.LtReal:  sp--; (sp - 1)->I64 = (sp - 1)->Real < sp->Real ? 1 : 0; break;
                case Op.LteReal: sp--; (sp - 1)->I64 = (sp - 1)->Real <= sp->Real ? 1 : 0; break;
                case Op.GtReal:  sp--; (sp - 1)->I64 = (sp - 1)->Real > sp->Real ? 1 : 0; break;
                case Op.GteReal: sp--; (sp - 1)->I64 = (sp - 1)->Real >= sp->Real ? 1 : 0; break;

                // ── Logic ──
                case Op.And: sp--; (sp - 1)->I64 = ((sp - 1)->I64 != 0 && sp->I64 != 0) ? 1 : 0; break;
                case Op.Or:  sp--; (sp - 1)->I64 = ((sp - 1)->I64 != 0 || sp->I64 != 0) ? 1 : 0; break;
                case Op.Not: (sp - 1)->I64 = (sp - 1)->I64 == 0 ? 1 : 0; break;

                // ── Bitwise ──
                case Op.BitAnd: sp--; (sp - 1)->I64 &= sp->I64; break;
                case Op.BitOr:  sp--; (sp - 1)->I64 |= sp->I64; break;
                case Op.BitXor: sp--; (sp - 1)->I64 ^= sp->I64; break;
                case Op.BitNot: (sp - 1)->I64 = ~(sp - 1)->I64; break;
                case Op.Shl:    sp--; (sp - 1)->I64 <<= (int)sp->I64; break;
                case Op.Shr:    sp--; (sp - 1)->I64 >>= (int)sp->I64; break;

                // ── Control flow ──
                case Op.Jump:
                    ip = codeBase + ReadU16(ip);
                    break;
                case Op.JumpIfFalse: {
                    var addr = ReadU16(ip); ip += 2;
                    if ((--sp)->I64 == 0) ip = codeBase + addr;
                    break;
                }
                case Op.JumpIfTrue: {
                    var addr = ReadU16(ip); ip += 2;
                    if ((--sp)->I64 != 0) ip = codeBase + addr;
                    break;
                }

                // ── Function calls (locals stays managed — Call swaps it) ──
                case Op.Call: {
                    var funcId = *ip++; var argc = *ip++;
                    var func = program.UserFunctions[funcId];
                    var intIp = (int)(ip - codeBase);
                    var intSp = (int)(sp - stackBase) - argc;
                    callStack[callDepth++] = new CallFrame {
                        ReturnIP = intIp, ReturnSP = intSp,
                        CallerLocals = locals, FunctionId = funcId
                    };
                    var newLocals = new FunValue[func.LocalsCount];
                    for (int i = argc - 1; i >= 0; i--) newLocals[i] = *--sp;
                    locals = newLocals;
                    ip = codeBase + func.EntryIP;
                    break;
                }
                case Op.TailCall: {
                    var funcId = *ip++; var argc = *ip++;
                    for (int i = argc - 1; i >= 0; i--) locals[i] = *--sp;
                    ip = codeBase + program.UserFunctions[funcId].EntryIP;
                    break;
                }
                case Op.Return: {
                    var result = *--sp;
                    var frame = callStack[--callDepth];
                    ip = codeBase + frame.ReturnIP;
                    sp = stackBase + frame.ReturnSP;
                    locals = frame.CallerLocals;
                    *sp++ = result;
                    break;
                }
                case Op.CallExtern: {
                    var funcId = *ip++; var argc = *ip++;
                    var ext = program.ExternFunctions[funcId];
                    var args = new object[argc];
                    for (int i = argc - 1; i >= 0; i--) args[i] = (--sp)->Box(ext.ArgTypes[i]);
                    var result = ext.Function.Calc(args);
                    *sp++ = FunValue.Unbox(result, ext.ReturnType);
                    break;
                }

                // ── Array ──
                case Op.NewArray: {
                    var count = *ip++;
                    var arr = new object[count];
                    for (int i = count - 1; i >= 0; i--) {
                        sp--;
                        arr[i] = sp->Ref ?? (object)sp->I64;
                    }
                    (sp++)->Ref = new Runtime.Arrays.ImmutableFunnyArray(arr, FunnyType.Any);
                    break;
                }
                case Op.GetElement: {
                    sp -= 2;
                    var arr = (Runtime.Arrays.IFunnyArray)sp->Ref;
                    (sp++)->Ref = arr.GetElementOrNull((int)(sp + 1)->I64);
                    break;
                }

                // ── Struct ──
                case Op.NewStruct: {
                    var layoutId = *ip++; var fieldCount = *ip++;
                    var layout = program.StructLayouts[layoutId];
                    var fields = new (string, object)[fieldCount];
                    for (int i = fieldCount - 1; i >= 0; i--)
                        fields[i] = (layout.FieldNames[i], (--sp)->Box(layout.FieldTypes[i]));
                    (sp++)->Ref = FunnyStruct.Create(fields);
                    break;
                }
                case Op.GetField: {
                    var fieldIdx = *ip++; var layoutId = *ip++;
                    var layout = program.StructLayouts[layoutId];
                    var s = (FunnyStruct)(--sp)->Ref;
                    *sp++ = FunValue.Unbox(s.GetValue(layout.FieldNames[fieldIdx]), layout.FieldTypes[fieldIdx]);
                    break;
                }

                // ── Optional ──
                case Op.IsNone: (sp - 1)->I64 = (sp - 1)->Ref is FunnyNone ? 1 : 0; break;
                case Op.Coalesce:
                    sp--;
                    if ((sp - 1)->Ref is FunnyNone) *(sp - 1) = *sp;
                    break;
                case Op.Unwrap:
                    if ((sp - 1)->Ref is FunnyNone) throw new FunnyRuntimeException("Force unwrap of none value");
                    break;

                // ── Stack ──
                case Op.Dup: *sp = *(sp - 1); sp++; break;
                case Op.Pop: sp--; break;

                // ── Superinstructions ──
                case Op.MulLocalConstI: {
                    var slot = *ip++; var cidx = *ip++;
                    (sp++)->I64 = locals[slot].I64 * constBase[cidx].I64;
                    break;
                }
                case Op.AddLocalConstI: {
                    var slot = *ip++; var cidx = *ip++;
                    (sp++)->I64 = locals[slot].I64 + constBase[cidx].I64;
                    break;
                }
                case Op.SubLocalConstI: {
                    var slot = *ip++; var cidx = *ip++;
                    (sp++)->I64 = locals[slot].I64 - constBase[cidx].I64;
                    break;
                }
                case Op.AddTopConstI: (sp - 1)->I64 += constBase[*ip++].I64; break;
                case Op.MulTopConstI: (sp - 1)->I64 *= constBase[*ip++].I64; break;
                case Op.AddConstConstI: {
                    (sp++)->I64 = constBase[*ip++].I64 + constBase[*ip++].I64;
                    break;
                }
                case Op.MulConstConstI: {
                    (sp++)->I64 = constBase[*ip++].I64 * constBase[*ip++].I64;
                    break;
                }
                case Op.StoreHalt:
                    locals[*ip] = *--sp;
                    return;

                case Op.AddLocalConstR: {
                    var slot = *ip++; var cidx = *ip++;
                    (sp++)->Real = locals[slot].Real + constBase[cidx].Real;
                    break;
                }
                case Op.MulLocalConstR: {
                    var slot = *ip++; var cidx = *ip++;
                    (sp++)->Real = locals[slot].Real * constBase[cidx].Real;
                    break;
                }
                case Op.AddTopConstR: (sp - 1)->Real += constBase[*ip++].Real; break;
                case Op.MulTopConstR: (sp - 1)->Real *= constBase[*ip++].Real; break;

                // ── Halt ──
                case Op.Halt: return;

                default:
                    throw new FunnyRuntimeException($"Unknown opcode {*(ip - 1)} at IP={(int)(ip - 1 - codeBase)}");
            }
        }
        }
        catch (FunnyRuntimeException ex) {
            var intIp = (int)(ip - 1 - codeBase);
            var handler = FindHandler(intIp, program.ExceptionHandlers);
            if (handler != null) {
                if (handler.Value.ErrorVarSlot >= 0) {
                    var err = FunnyStruct.Create(("message", ex.Message ?? ""), ("data", ex.OopsData));
                    locals[handler.Value.ErrorVarSlot] = FunValue.FromRef(err);
                }
                ip = codeBase + handler.Value.CatchStartIP;
                // Resume: need to re-enter the loop. Use recursive call.
                Execute(program, locals, stack, callStack, maxOps);
                return;
            }
            throw;
        }
        } // end fixed
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int ReadU16(byte* p) => p[0] | (p[1] << 8);

    private static ExceptionHandler? FindHandler(int ip, ExceptionHandler[] handlers) {
        for (int i = 0; i < handlers.Length; i++)
            if (ip >= handlers[i].TryStartIP && ip < handlers[i].TryEndIP) return handlers[i];
        return null;
    }
}
