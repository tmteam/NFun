using System;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// Stack-based bytecode VM. No unsafe, no pinning.
/// Per-Run overhead minimized: no fixed, no ThreadStatic, no try/catch for non-exception programs.
/// </summary>
public static class VirtualMachine {

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Execute(CompiledProgram program, FunValue[] locals,
        FunValue[] stack, CallFrame[] callStack, int maxOps = 10_000_000) {
        var code = program.Code;
        var constants = program.Constants;
        int ip = 0, sp = 0, callDepth = 0;

        // No try/catch in the hot path for programs without exception handlers.
        // Exception handlers only needed when program has try-catch expressions.
        if (program.ExceptionHandlers.Length > 0) {
            ExecuteWithHandlers(program, locals, stack, callStack, maxOps);
            return;
        }

        while (true) {
            switch ((Op)code[ip++]) {

                case Op.LoadConstI: stack[sp++].I64 = constants[code[ip++]].I64; break;
                case Op.LoadConstR: stack[sp++].I64 = constants[code[ip++]].I64; break;
                case Op.LoadConstRef: stack[sp++].Ref = constants[code[ip++]].Ref; break;
                case Op.LoadLocal: stack[sp++] = locals[code[ip++]]; break;
                case Op.StoreLocal: locals[code[ip++]] = stack[--sp]; break;
                case Op.LoadNone: stack[sp++] = FunValue.None; break;

                // ── Integer arithmetic ──
                case Op.AddInt: sp--; stack[sp - 1].I64 += stack[sp].I64; break;
                case Op.SubInt: sp--; stack[sp - 1].I64 -= stack[sp].I64; break;
                case Op.MulInt: sp--; stack[sp - 1].I64 *= stack[sp].I64; break;
                case Op.DivInt:
                    sp--;
                    if (stack[sp].I64 == 0) throw new FunnyRuntimeException("Division by zero");
                    stack[sp - 1].I64 /= stack[sp].I64;
                    break;
                case Op.ModInt: sp--; stack[sp - 1].I64 %= stack[sp].I64; break;
                case Op.PowInt: sp--; stack[sp - 1].I64 = IntPow(stack[sp - 1].I64, stack[sp].I64); break;
                case Op.NegInt: stack[sp - 1].I64 = -stack[sp - 1].I64; break;

                // ── Real arithmetic ──
                case Op.AddReal: sp--; stack[sp - 1].Real += stack[sp].Real; break;
                case Op.SubReal: sp--; stack[sp - 1].Real -= stack[sp].Real; break;
                case Op.MulReal: sp--; stack[sp - 1].Real *= stack[sp].Real; break;
                case Op.DivReal: sp--; stack[sp - 1].Real /= stack[sp].Real; break;
                case Op.ModReal: sp--; stack[sp - 1].Real %= stack[sp].Real; break;
                case Op.NegReal: stack[sp - 1].Real = -stack[sp - 1].Real; break;
                case Op.PowReal: sp--; stack[sp - 1].Real = Math.Pow(stack[sp - 1].Real, stack[sp].Real); break;

                // ── Truncation ──
                case Op.TruncU8:  stack[sp - 1].I64 = (byte)stack[sp - 1].I64; break;
                case Op.TruncU16: stack[sp - 1].I64 = (ushort)stack[sp - 1].I64; break;
                case Op.TruncU32: stack[sp - 1].I64 = (uint)stack[sp - 1].I64; break;
                case Op.TruncI16: stack[sp - 1].I64 = (short)stack[sp - 1].I64; break;
                case Op.TruncI32: stack[sp - 1].I64 = (int)stack[sp - 1].I64; break;

                // ── Type conversion ──
                case Op.IntToReal: stack[sp - 1].Real = stack[sp - 1].I64; break;
                case Op.RealToInt: stack[sp - 1].I64 = (long)stack[sp - 1].Real; break;

                // ── Boxing (primitive → Ref for Any type) ──
                case Op.BoxInt:  stack[sp - 1].Ref = (object)stack[sp - 1].I64; break;
                case Op.BoxReal: stack[sp - 1].Ref = (object)stack[sp - 1].Real; break;
                case Op.BoxBool: stack[sp - 1].Ref = (object)(stack[sp - 1].I64 != 0); break;

                // ── Native built-in functions (zero boxing) ──
                case Op.MaxInt:  sp--; stack[sp - 1].I64 = Math.Max(stack[sp - 1].I64, stack[sp].I64); break;
                case Op.MinInt:  sp--; stack[sp - 1].I64 = Math.Min(stack[sp - 1].I64, stack[sp].I64); break;
                case Op.MaxReal: sp--; stack[sp - 1].Real = Math.Max(stack[sp - 1].Real, stack[sp].Real); break;
                case Op.MinReal: sp--; stack[sp - 1].Real = Math.Min(stack[sp - 1].Real, stack[sp].Real); break;
                case Op.AbsInt:  stack[sp - 1].I64 = Math.Abs(stack[sp - 1].I64); break;
                case Op.AbsReal: stack[sp - 1].Real = Math.Abs(stack[sp - 1].Real); break;
                case Op.ToTextInt:  stack[sp - 1] = new FunValue { Ref = new Runtime.Arrays.TextFunnyArray(stack[sp - 1].I64.ToString()) }; break;
                case Op.ToTextReal: stack[sp - 1] = new FunValue { Ref = new Runtime.Arrays.TextFunnyArray(stack[sp - 1].Real.ToString(System.Globalization.CultureInfo.InvariantCulture)) }; break;

                // ── Integer comparison ──
                case Op.EqInt:  sp--; stack[sp - 1].I64 = stack[sp - 1].I64 == stack[sp].I64 ? 1 : 0; break;
                case Op.NeqInt: sp--; stack[sp - 1].I64 = stack[sp - 1].I64 != stack[sp].I64 ? 1 : 0; break;
                case Op.LtInt:  sp--; stack[sp - 1].I64 = stack[sp - 1].I64 < stack[sp].I64 ? 1 : 0; break;
                case Op.LteInt: sp--; stack[sp - 1].I64 = stack[sp - 1].I64 <= stack[sp].I64 ? 1 : 0; break;
                case Op.GtInt:  sp--; stack[sp - 1].I64 = stack[sp - 1].I64 > stack[sp].I64 ? 1 : 0; break;
                case Op.GteInt: sp--; stack[sp - 1].I64 = stack[sp - 1].I64 >= stack[sp].I64 ? 1 : 0; break;
                case Op.LtUint: sp--; stack[sp - 1].I64 = (ulong)stack[sp - 1].I64 < (ulong)stack[sp].I64 ? 1 : 0; break;

                // ── Real comparison ──
                case Op.EqReal:  sp--; stack[sp - 1].I64 = stack[sp - 1].Real == stack[sp].Real ? 1 : 0; break;
                case Op.NeqReal: sp--; stack[sp - 1].I64 = stack[sp - 1].Real != stack[sp].Real ? 1 : 0; break;
                case Op.LtReal:  sp--; stack[sp - 1].I64 = stack[sp - 1].Real < stack[sp].Real ? 1 : 0; break;
                case Op.LteReal: sp--; stack[sp - 1].I64 = stack[sp - 1].Real <= stack[sp].Real ? 1 : 0; break;
                case Op.GtReal:  sp--; stack[sp - 1].I64 = stack[sp - 1].Real > stack[sp].Real ? 1 : 0; break;
                case Op.GteReal: sp--; stack[sp - 1].I64 = stack[sp - 1].Real >= stack[sp].Real ? 1 : 0; break;

                // ── Reference comparison ──
                case Op.EqRef:  sp--; stack[sp - 1].I64 = Equals(stack[sp - 1].Ref, stack[sp].Ref) ? 1 : 0; break;
                case Op.NeqRef: sp--; stack[sp - 1].I64 = !Equals(stack[sp - 1].Ref, stack[sp].Ref) ? 1 : 0; break;

                // ── Logic ──
                case Op.And: sp--; stack[sp - 1].I64 = (stack[sp - 1].I64 != 0 && stack[sp].I64 != 0) ? 1 : 0; break;
                case Op.Or:  sp--; stack[sp - 1].I64 = (stack[sp - 1].I64 != 0 || stack[sp].I64 != 0) ? 1 : 0; break;
                case Op.Not: stack[sp - 1].I64 = stack[sp - 1].I64 == 0 ? 1 : 0; break;

                // ── Bitwise ──
                case Op.BitAnd: sp--; stack[sp - 1].I64 &= stack[sp].I64; break;
                case Op.BitOr:  sp--; stack[sp - 1].I64 |= stack[sp].I64; break;
                case Op.BitXor: sp--; stack[sp - 1].I64 ^= stack[sp].I64; break;
                case Op.BitNot: stack[sp - 1].I64 = ~stack[sp - 1].I64; break;
                case Op.Shl:    sp--; stack[sp - 1].I64 <<= (int)stack[sp].I64; break;
                case Op.Shr:    sp--; stack[sp - 1].I64 >>= (int)stack[sp].I64; break;

                // ── Control flow ──
                case Op.Jump: ip = code[ip] | (code[ip + 1] << 8); break;
                case Op.JumpIfFalse: {
                    var addr = code[ip] | (code[ip + 1] << 8); ip += 2;
                    if (stack[--sp].I64 == 0) ip = addr;
                    break;
                }
                case Op.JumpIfTrue: {
                    var addr = code[ip] | (code[ip + 1] << 8); ip += 2;
                    if (stack[--sp].I64 != 0) ip = addr;
                    break;
                }

                // ── Function calls ──
                case Op.Call: {
                    var funcId = code[ip++]; var argc = code[ip++];
                    var func = program.UserFunctions[funcId];
                    callStack[callDepth++] = new CallFrame { ReturnIP = ip, ReturnSP = sp - argc, CallerLocals = locals, FunctionId = funcId };
                    var newLocals = new FunValue[func.LocalsCount];
                    for (int i = argc - 1; i >= 0; i--) newLocals[i] = stack[--sp];
                    locals = newLocals; ip = func.EntryIP;
                    break;
                }
                case Op.TailCall: {
                    var funcId = code[ip++]; var argc = code[ip++];
                    for (int i = argc - 1; i >= 0; i--) locals[i] = stack[--sp];
                    ip = program.UserFunctions[funcId].EntryIP;
                    break;
                }
                case Op.Return: {
                    var result = stack[--sp]; var frame = callStack[--callDepth];
                    ip = frame.ReturnIP; sp = frame.ReturnSP; locals = frame.CallerLocals;
                    stack[sp++] = result;
                    break;
                }
                case Op.CallExtern: {
                    var funcId = code[ip++]; var argc = code[ip++];
                    var ext = program.ExternFunctions[funcId];
                    object cResult;
                    if (argc == 2 && ext.Function is Interpretation.Functions.FunctionWithTwoArgs cf2) {
                        sp -= 2;
                        cResult = cf2.Calc(stack[sp].Box(ext.ArgTypes[0]), stack[sp + 1].Box(ext.ArgTypes[1]));
                    } else if (argc == 1 && ext.Function is Interpretation.Functions.FunctionWithSingleArg cf1) {
                        cResult = cf1.Calc(stack[--sp].Box(ext.ArgTypes[0]));
                    } else {
                        var cArgs = new object[argc];
                        for (int i = argc - 1; i >= 0; i--) cArgs[i] = stack[--sp].Box(ext.ArgTypes[i]);
                        cResult = ext.Function.Calc(cArgs);
                    }
                    stack[sp++] = FunValue.Unbox(cResult, ext.ReturnType);
                    break;
                }

                // ── Array ──
                case Op.NewArray: {
                    var count = code[ip++];
                    var typeIdx = code[ip++];
                    var elemType = program.TypeTable[typeIdx];
                    var arr = new object[count];
                    for (int i = count - 1; i >= 0; i--) {
                        sp--;
                        arr[i] = BoxElement(ref stack[sp], elemType.BaseType);
                    }
                    stack[sp++].Ref = new Runtime.Arrays.ImmutableFunnyArray(arr, elemType);
                    break;
                }
                case Op.GetElement: {
                    sp -= 2; var arr = (Runtime.Arrays.IFunnyArray)stack[sp].Ref;
                    stack[sp++].Ref = arr.GetElementOrNull((int)stack[sp + 1].I64);
                    break;
                }

                // ── Struct ──
                case Op.NewStruct: {
                    var layoutId = code[ip++]; var fieldCount = code[ip++];
                    var layout = program.StructLayouts[layoutId];
                    var fields = new (string, object)[fieldCount];
                    for (int i = fieldCount - 1; i >= 0; i--) fields[i] = (layout.FieldNames[i], stack[--sp].Box(layout.FieldTypes[i]));
                    stack[sp++].Ref = FunnyStruct.Create(fields);
                    break;
                }
                case Op.GetField: {
                    var fieldIdx = code[ip++]; var layoutId = code[ip++];
                    var layout = program.StructLayouts[layoutId];
                    stack[sp - 1] = FunValue.Unbox(((FunnyStruct)stack[--sp].Ref).GetValue(layout.FieldNames[fieldIdx]), layout.FieldTypes[fieldIdx]);
                    sp++;
                    break;
                }
                case Op.GetFieldSafe: {
                    var fieldIdx = code[ip++]; var layoutId = code[ip++];
                    var layout = program.StructLayouts[layoutId];
                    var src = stack[--sp];
                    if (src.Ref is FunnyNone || src.Ref == null)
                        stack[sp++] = FunValue.None;
                    else {
                        stack[sp] = FunValue.Unbox(((FunnyStruct)src.Ref).GetValue(layout.FieldNames[fieldIdx]), layout.FieldTypes[fieldIdx]);
                        sp++;
                    }
                    break;
                }
                case Op.GetElementSafe: {
                    sp -= 2;
                    var arr = stack[sp].Ref;
                    var idx = (int)stack[sp + 1].I64;
                    if (arr is FunnyNone || arr == null) {
                        stack[sp++] = FunValue.None;
                    } else {
                        var funArr = (Runtime.Arrays.IFunnyArray)arr;
                        if (idx < 0 || idx >= funArr.Count)
                            stack[sp++] = FunValue.None;
                        else
                            stack[sp++].Ref = funArr.GetElementOrNull(idx);
                    }
                    break;
                }

                // ── Optional ──
                case Op.IsNone: stack[sp - 1].I64 = stack[sp - 1].Ref is FunnyNone ? 1 : 0; break;
                case Op.Coalesce: sp--; if (stack[sp - 1].Ref is FunnyNone) stack[sp - 1] = stack[sp]; break;
                case Op.Unwrap: if (stack[sp - 1].Ref is FunnyNone) throw new FunnyRuntimeException("Force unwrap of none value"); break;

                // ── Stack ──
                case Op.Dup: stack[sp] = stack[sp - 1]; sp++; break;
                case Op.Pop: sp--; break;

                // ── Superinstructions ──
                case Op.MulLocalConstI: { var s = code[ip++]; var c = code[ip++]; stack[sp++].I64 = locals[s].I64 * constants[c].I64; break; }
                case Op.AddLocalConstI: { var s = code[ip++]; var c = code[ip++]; stack[sp++].I64 = locals[s].I64 + constants[c].I64; break; }
                case Op.SubLocalConstI: { var s = code[ip++]; var c = code[ip++]; stack[sp++].I64 = locals[s].I64 - constants[c].I64; break; }
                case Op.AddTopConstI: stack[sp - 1].I64 += constants[code[ip++]].I64; break;
                case Op.MulTopConstI: stack[sp - 1].I64 *= constants[code[ip++]].I64; break;
                case Op.AddConstConstI: stack[sp++].I64 = constants[code[ip++]].I64 + constants[code[ip++]].I64; break;
                case Op.MulConstConstI: stack[sp++].I64 = constants[code[ip++]].I64 * constants[code[ip++]].I64; break;
                case Op.StoreHalt: locals[code[ip]] = stack[--sp]; return;
                case Op.AddLocalConstR: { var s = code[ip++]; var c = code[ip++]; stack[sp++].Real = locals[s].Real + constants[c].Real; break; }
                case Op.MulLocalConstR: { var s = code[ip++]; var c = code[ip++]; stack[sp++].Real = locals[s].Real * constants[c].Real; break; }
                case Op.AddTopConstR: stack[sp - 1].Real += constants[code[ip++]].Real; break;
                case Op.MulTopConstR: stack[sp - 1].Real *= constants[code[ip++]].Real; break;

                case Op.Halt: return;
                default: throw new FunnyRuntimeException($"Unknown opcode {code[ip - 1]} at IP={ip - 1}");
            }
        }
    }

    /// <summary>Slow path with exception handler support. Wraps entire execution in try/catch.</summary>
    private static void ExecuteWithHandlers(CompiledProgram program, FunValue[] locals,
        FunValue[] stack, CallFrame[] callStack, int maxOps) {
        var code = program.Code;
        var constants = program.Constants;
        int ip = 0, sp = 0, callDepth = 0;

        while (true) {
            try {
                // Run from current IP until Halt or exception
                while (true) {
                    switch ((Op)code[ip++]) {
                        // Minimal dispatch for catch body — handles basic ops
                        case Op.LoadConstI: stack[sp++].I64 = constants[code[ip++]].I64; break;
                        case Op.LoadConstR: stack[sp++].I64 = constants[code[ip++]].I64; break;
                        case Op.LoadConstRef: stack[sp++].Ref = constants[code[ip++]].Ref; break;
                        case Op.LoadLocal: stack[sp++] = locals[code[ip++]]; break;
                        case Op.StoreLocal: locals[code[ip++]] = stack[--sp]; break;
                        case Op.AddInt: sp--; stack[sp - 1].I64 += stack[sp].I64; break;
                        case Op.SubInt: sp--; stack[sp - 1].I64 -= stack[sp].I64; break;
                        case Op.MulInt: sp--; stack[sp - 1].I64 *= stack[sp].I64; break;
                        case Op.Jump: ip = code[ip] | (code[ip + 1] << 8); break;
                        case Op.JumpIfFalse: {
                            var addr = code[ip] | (code[ip + 1] << 8); ip += 2;
                            if (stack[--sp].I64 == 0) ip = addr; break;
                        }
                        case Op.CallExtern: {
                            var funcId = code[ip++]; var argc = code[ip++];
                            var ext = program.ExternFunctions[funcId];
                            var cArgs2 = new object[argc];
                            for (int i = argc - 1; i >= 0; i--) cArgs2[i] = stack[--sp].Box(ext.ArgTypes[i]);
                            stack[sp++] = FunValue.Unbox(ext.Function.Calc(cArgs2), ext.ReturnType);
                            break;
                        }
                        case Op.StoreHalt: locals[code[ip]] = stack[--sp]; return;
                        case Op.Halt: return;
                        default:
                            // For unhandled opcodes, delegate to the fast path (no handlers)
                            ip--;
                            Execute(new CompiledProgram {
                                Code = code, Constants = constants,
                                StructLayouts = program.StructLayouts,
                                ExternFunctions = program.ExternFunctions,
                                UserFunctions = program.UserFunctions,
                                Variables = program.Variables,
                                ExceptionHandlers = Array.Empty<ExceptionHandler>(),
                                LocalsCount = program.LocalsCount
                            }, locals, stack, callStack, maxOps);
                            return;
                    }
                }
            }
            catch (FunnyRuntimeException ex) {
                var handler = FindHandler(ip - 1, program.ExceptionHandlers);
                if (handler != null) {
                    if (handler.Value.ErrorVarSlot >= 0)
                        locals[handler.Value.ErrorVarSlot] = FunValue.FromRef(
                            FunnyStruct.Create(("message", ex.Message ?? ""), ("data", ex.OopsData)));
                    ip = handler.Value.CatchStartIP;
                    continue; // resume loop at catch IP
                }
                throw;
            }
        }
    }

    private static ExceptionHandler? FindHandler(int ip, ExceptionHandler[] handlers) {
        for (int i = 0; i < handlers.Length; i++)
            if (ip >= handlers[i].TryStartIP && ip < handlers[i].TryEndIP) return handlers[i];
        return null;
    }

    private static FunnyType BaseTypeToFunnyType(BaseFunnyType bt) => bt switch {
        BaseFunnyType.UInt8  => FunnyType.UInt8,
        BaseFunnyType.UInt16 => FunnyType.UInt16,
        BaseFunnyType.UInt32 => FunnyType.UInt32,
        BaseFunnyType.UInt64 => FunnyType.UInt64,
        BaseFunnyType.Int16  => FunnyType.Int16,
        BaseFunnyType.Int32  => FunnyType.Int32,
        BaseFunnyType.Int64  => FunnyType.Int64,
        BaseFunnyType.Real   => FunnyType.Real,
        BaseFunnyType.Bool   => FunnyType.Bool,
        BaseFunnyType.Char   => FunnyType.Char,
        _                    => FunnyType.Any,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object BoxElement(ref FunValue v, BaseFunnyType elemType) {
        return elemType switch {
            BaseFunnyType.UInt8  => (byte)v.I64,
            BaseFunnyType.UInt16 => (ushort)v.I64,
            BaseFunnyType.UInt32 => (uint)v.I64,
            BaseFunnyType.UInt64 => (ulong)v.I64,
            BaseFunnyType.Int16  => (short)v.I64,
            BaseFunnyType.Int32  => (int)v.I64,
            BaseFunnyType.Int64  => v.I64,
            BaseFunnyType.Real   => v.Real,
            BaseFunnyType.Bool   => v.I64 != 0,
            BaseFunnyType.Char   => (char)v.I64,
            _                    => v.Ref ?? (object)v.I64,
        };
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
