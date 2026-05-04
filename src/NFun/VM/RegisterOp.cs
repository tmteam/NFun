namespace NFun.VM;

/// <summary>
/// Register VM opcodes. Fixed 4-byte instructions: [op][dst][src1][src2|constIdx].
/// Registers = locals[] array. No stack, no sp.
/// </summary>
public enum RegisterOp : byte {
    // ── Integer arithmetic: dst = src1 op src2 ──
    AddRR_I   = 0x01, // locals[dst].I64 = locals[src1].I64 + locals[src2].I64
    AddRI_I   = 0x02, // locals[dst].I64 = locals[src1].I64 + constants[src2].I64
    SubRR_I   = 0x03,
    SubRI_I   = 0x04,
    MulRR_I   = 0x05,
    MulRI_I   = 0x06,
    DivRR_I   = 0x07,
    ModRR_I   = 0x09,
    NegR_I    = 0x0B, // locals[dst].I64 = -locals[src1].I64

    // ── Real arithmetic ──
    AddRR_D   = 0x11,
    AddRI_D   = 0x12,
    SubRR_D   = 0x13,
    MulRR_D   = 0x15,
    MulRI_D   = 0x16,
    DivRR_D   = 0x17,
    NegR_D    = 0x1B,

    // ── Comparison (int) → bool register ──
    GtRR_I    = 0x20,
    GtRI_I    = 0x21,
    LtRR_I    = 0x23,
    LtRI_I    = 0x24,
    GteRR_I   = 0x22,
    LteRR_I   = 0x25,
    EqRR_I    = 0x26,
    NeqRR_I   = 0x27,

    // ── Logic ──
    AndRR     = 0x30,
    OrRR      = 0x31,
    NotR      = 0x32,

    // ── Control flow ──
    Jmp       = 0x40, // ip = (src1 << 8) | src2
    JmpIfNot  = 0x41, // if locals[dst].I64 == 0, ip = (src1 << 8) | src2
    JmpIf     = 0x42, // if locals[dst].I64 != 0, ip = (src1 << 8) | src2

    // ── Data movement ──
    Mov       = 0x50, // locals[dst] = locals[src1]
    LoadC_I   = 0x51, // locals[dst].I64 = constants[src1].I64
    LoadC_D   = 0x52, // locals[dst].Real = constants[src1].Real  (same bits as I64)
    LoadC_Ref = 0x53, // locals[dst].Ref = constants[src1].Ref
    Halt      = 0x54, // return

    // ── Type conversion ──
    I2D       = 0x58, // locals[dst].Real = (double)locals[src1].I64
    D2I       = 0x59, // locals[dst].I64 = (long)locals[src1].Real

    // ── Native functions ──
    MaxRR_I   = 0x60,
    MinRR_I   = 0x61,
    AbsR_I    = 0x62,
}
