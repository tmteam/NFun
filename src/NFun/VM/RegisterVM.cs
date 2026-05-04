using System;
using System.Runtime.CompilerServices;
using NFun.Exceptions;

namespace NFun.VM;

/// <summary>
/// Register-based VM. No stack, no sp — registers = locals[] array.
/// Fixed 4-byte instructions: [op:8][dst:8][src1:8][src2:8].
/// </summary>
public static class RegisterVM {

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Execute(byte[] code, FunValue[] locals, FunValue[] constants) {
        int ip = 0;

        while (true) {
            var op = (RegisterOp)code[ip];
            var dst = code[ip + 1];
            var s1 = code[ip + 2];
            var s2 = code[ip + 3];
            ip += 4;

            switch (op) {
                // ── Integer arithmetic ──
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
                case RegisterOp.NegR_I:   locals[dst].I64 = -locals[s1].I64; break;

                // ── Real arithmetic ──
                case RegisterOp.AddRR_D:  locals[dst].Real = locals[s1].Real + locals[s2].Real; break;
                case RegisterOp.AddRI_D:  locals[dst].Real = locals[s1].Real + constants[s2].Real; break;
                case RegisterOp.SubRR_D:  locals[dst].Real = locals[s1].Real - locals[s2].Real; break;
                case RegisterOp.MulRR_D:  locals[dst].Real = locals[s1].Real * locals[s2].Real; break;
                case RegisterOp.MulRI_D:  locals[dst].Real = locals[s1].Real * constants[s2].Real; break;
                case RegisterOp.DivRR_D:  locals[dst].Real = locals[s1].Real / locals[s2].Real; break;
                case RegisterOp.NegR_D:   locals[dst].Real = -locals[s1].Real; break;

                // ── Comparison ──
                case RegisterOp.GtRR_I:   locals[dst].I64 = locals[s1].I64 > locals[s2].I64 ? 1 : 0; break;
                case RegisterOp.GtRI_I:   locals[dst].I64 = locals[s1].I64 > constants[s2].I64 ? 1 : 0; break;
                case RegisterOp.LtRR_I:   locals[dst].I64 = locals[s1].I64 < locals[s2].I64 ? 1 : 0; break;
                case RegisterOp.LtRI_I:   locals[dst].I64 = locals[s1].I64 < constants[s2].I64 ? 1 : 0; break;
                case RegisterOp.GteRR_I:  locals[dst].I64 = locals[s1].I64 >= locals[s2].I64 ? 1 : 0; break;
                case RegisterOp.LteRR_I:  locals[dst].I64 = locals[s1].I64 <= locals[s2].I64 ? 1 : 0; break;
                case RegisterOp.EqRR_I:   locals[dst].I64 = locals[s1].I64 == locals[s2].I64 ? 1 : 0; break;
                case RegisterOp.NeqRR_I:  locals[dst].I64 = locals[s1].I64 != locals[s2].I64 ? 1 : 0; break;

                // ── Logic ──
                case RegisterOp.AndRR:    locals[dst].I64 = (locals[s1].I64 != 0 && locals[s2].I64 != 0) ? 1 : 0; break;
                case RegisterOp.OrRR:     locals[dst].I64 = (locals[s1].I64 != 0 || locals[s2].I64 != 0) ? 1 : 0; break;
                case RegisterOp.NotR:     locals[dst].I64 = locals[s1].I64 == 0 ? 1 : 0; break;

                // ── Control flow ──
                case RegisterOp.Jmp:      ip = (s1 << 8) | s2; break;
                case RegisterOp.JmpIfNot: if (locals[dst].I64 == 0) ip = (s1 << 8) | s2; break;
                case RegisterOp.JmpIf:    if (locals[dst].I64 != 0) ip = (s1 << 8) | s2; break;

                // ── Data movement ──
                case RegisterOp.Mov:      locals[dst] = locals[s1]; break;
                case RegisterOp.LoadC_I:  locals[dst].I64 = constants[s1].I64; break;
                case RegisterOp.LoadC_D:  locals[dst].I64 = constants[s1].I64; break; // same bits
                case RegisterOp.LoadC_Ref:locals[dst].Ref = constants[s1].Ref; break;
                case RegisterOp.Halt:     return;

                // ── Type conversion ──
                case RegisterOp.I2D:      locals[dst].Real = locals[s1].I64; break;
                case RegisterOp.D2I:      locals[dst].I64 = (long)locals[s1].Real; break;

                // ── Native functions ──
                case RegisterOp.MaxRR_I:  locals[dst].I64 = Math.Max(locals[s1].I64, locals[s2].I64); break;
                case RegisterOp.MinRR_I:  locals[dst].I64 = Math.Min(locals[s1].I64, locals[s2].I64); break;
                case RegisterOp.AbsR_I:   locals[dst].I64 = Math.Abs(locals[s1].I64); break;

                default: throw new FunnyRuntimeException($"Unknown register opcode {op} at IP={ip - 4}");
            }
        }
    }
}
