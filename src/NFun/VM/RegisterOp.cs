namespace NFun.VM;

/// <summary>
/// Register VM opcodes. Fixed 4-byte instructions: [op][dst][src1][src2|constIdx].
/// Registers = locals[] array. No stack, no sp.
/// </summary>
public enum RegisterOp : byte {
    // ── Integer arithmetic: dst = src1 op src2 ──
    AddRR_I   = 0x01,
    AddRI_I   = 0x02,
    SubRR_I   = 0x03,
    SubRI_I   = 0x04,
    MulRR_I   = 0x05,
    MulRI_I   = 0x06,
    DivRR_I   = 0x07,
    ModRR_I   = 0x09,
    PowRR_I   = 0x0A,
    NegR_I    = 0x0B,

    // ── Real arithmetic ──
    AddRR_D   = 0x11,
    AddRI_D   = 0x12,
    SubRR_D   = 0x13,
    SubRI_D   = 0x14,
    MulRR_D   = 0x15,
    MulRI_D   = 0x16,
    DivRR_D   = 0x17,
    ModRR_D   = 0x18,
    PowRR_D   = 0x19,
    NegR_D    = 0x1B,

    // ── Comparison (int) → bool register ──
    GtRR_I    = 0x20,
    GtRI_I    = 0x21,
    GteRR_I   = 0x22,
    LtRR_I    = 0x23,
    LtRI_I    = 0x24,
    LteRR_I   = 0x25,
    EqRR_I    = 0x26,
    NeqRR_I   = 0x27,
    // ── Comparison (real) ──
    GtRR_D    = 0x28,
    LtRR_D    = 0x29,
    GteRR_D   = 0x2A,
    LteRR_D   = 0x2B,
    EqRR_D    = 0x2C,
    // ── Reference comparison ──
    EqRef     = 0x2D,
    NeqRef    = 0x2E,

    // ── Logic ──
    AndRR     = 0x30,
    OrRR      = 0x31,
    NotR      = 0x32,
    XorRR     = 0x33,

    // ── Bitwise ──
    BitAndRR  = 0x34,
    BitOrRR   = 0x35,
    BitXorRR  = 0x36,
    BitNotR   = 0x37,
    ShlRR     = 0x38,
    ShrRR     = 0x39,

    // ── Control flow ──
    Jmp       = 0x40,     // ip = (src1 << 8) | src2
    JmpIfNot  = 0x41,     // if locals[dst].I64 == 0, ip = (src1 << 8) | src2
    JmpIf     = 0x42,     // if locals[dst].I64 != 0, ip = (src1 << 8) | src2

    // ── Data movement ──
    Mov       = 0x50,     // locals[dst] = locals[src1]
    LoadC_I   = 0x51,     // locals[dst].I64 = constants[src1].I64
    LoadC_D   = 0x52,     // locals[dst].I64 = constants[src1].I64 (same bits — Real at offset 0)
    LoadC_Ref = 0x53,     // locals[dst].Ref = constants[src1].Ref
    LoadNone  = 0x54,     // locals[dst] = FunValue.None
    Halt      = 0x55,     // return

    // ── Type conversion ──
    I2D       = 0x58,     // locals[dst].Real = (double)locals[src1].I64
    D2I       = 0x59,     // locals[dst].I64 = (long)locals[src1].Real
    Truncate  = 0x5A,     // locals[dst].I64 = truncate(locals[src1].I64, src2=truncType)
    BoxInt    = 0x5B,     // locals[dst].Ref = (object)locals[src1].I64
    BoxReal   = 0x5C,     // locals[dst].Ref = (object)locals[src1].Real
    BoxBool   = 0x5D,     // locals[dst].Ref = (object)(locals[src1].I64 != 0)

    // ── Native functions ──
    MaxRR_I   = 0x60,
    MinRR_I   = 0x61,
    AbsR_I    = 0x62,
    MaxRR_D   = 0x63,
    MinRR_D   = 0x64,
    AbsR_D    = 0x65,
    ToTextI   = 0x66,     // locals[dst].Ref = TextFunnyArray(locals[src1].I64.ToString())
    ToTextD   = 0x67,     // locals[dst].Ref = TextFunnyArray(locals[src1].Real.ToString())

    // ── Arrays ──
    NewArr    = 0x70,     // locals[dst].Ref = new FunValueArray(count=src1, typeIdx=src2)
                          // followed by 4-byte words with element register indices
    GetElem   = 0x71,     // locals[dst] = ((IFunnyArray)locals[src1].Ref)[locals[src2].I64]
    GetElemSafe = 0x72,   // same but returns None if out of bounds or src1 is None

    // ── Structs ──
    NewStruct = 0x73,     // locals[dst].Ref = new struct(layoutId=src1, fieldCount=src2)
                          // followed by field register indices
    GetField  = 0x74,     // locals[dst] = struct[fieldIdx=src1] from layout src2
    GetFieldSafe = 0x75,  // same but returns None if struct is None

    // ── Optional ──
    IsNone    = 0x78,     // locals[dst].I64 = (locals[src1].Ref is FunnyNone) ? 1 : 0
    Coalesce  = 0x79,     // locals[dst] = locals[src1].Ref is FunnyNone ? locals[src2] : locals[src1]
    Unwrap    = 0x7A,     // if locals[src1].Ref is FunnyNone throw; else locals[dst] = locals[src1]

    // ── Function calls (Lua convention: args in consecutive registers) ──
    CallExt   = 0x80,     // locals[dst] = extern(funcId=src1, baseR=src2, argc from metadata)
    CallUser  = 0x81,     // locals[dst] = userFunc(funcId=src1, baseR=src2)
    Return    = 0x82,     // return locals[src1] to caller
    MakeClosure = 0x83,   // locals[dst] = BytecodeLambda(funcId=src1, captureCount=src2)
                          // followed by captured register indices
}
