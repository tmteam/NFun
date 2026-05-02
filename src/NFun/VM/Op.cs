namespace NFun.VM;

/// <summary>
/// VM opcodes. Dense numbering (0..N) for JIT jump table optimization.
/// Sparse numbering causes binary search dispatch (~7 comparisons).
/// Dense numbering gives O(1) indexed jump.
/// </summary>
public enum Op : byte {
    // ── Load / Store ──
    LoadConstI    = 0,
    LoadConstR    = 1,
    LoadConstRef  = 2,
    LoadLocal     = 3,
    StoreLocal    = 4,
    LoadNone      = 5,

    // ── Integer arithmetic ──
    AddInt        = 6,
    SubInt        = 7,
    MulInt        = 8,
    DivInt        = 9,
    ModInt        = 10,
    PowInt        = 11,
    NegInt        = 12,

    // ── Real arithmetic ──
    AddReal       = 13,
    SubReal       = 14,
    MulReal       = 15,
    DivReal       = 16,
    ModReal       = 17,
    PowReal       = 18,
    NegReal       = 19,

    // ── Truncation ──
    TruncU8       = 20,
    TruncU16      = 21,
    TruncU32      = 22,
    TruncI16      = 23,
    TruncI32      = 24,

    // ── Type conversion ──
    IntToReal     = 25,
    RealToInt     = 26,

    // ── Integer comparison ──
    EqInt         = 27,
    NeqInt        = 28,
    LtInt         = 29,
    LteInt        = 30,
    GtInt         = 31,
    GteInt        = 32,
    LtUint        = 33,

    // ── Real comparison ──
    EqReal        = 34,
    NeqReal       = 35,
    LtReal        = 36,
    LteReal       = 37,
    GtReal        = 38,
    GteReal       = 39,

    // ── Reference comparison ──
    EqRef         = 40,
    NeqRef        = 41,

    // ── Logic ──
    And           = 42,
    Or            = 43,
    Not           = 44,

    // ── Bitwise ──
    BitAnd        = 45,
    BitOr         = 46,
    BitXor        = 47,
    BitNot        = 48,
    Shl           = 49,
    Shr           = 50,

    // ── Control flow ──
    Jump          = 51,
    JumpIfFalse   = 52,
    JumpIfTrue    = 53,

    // ── Function calls ──
    Call          = 54,
    CallExtern    = 55,
    TailCall      = 56,
    Return        = 57,

    // ── Array ──
    NewArray      = 58,
    GetElement    = 59,
    GetElementSafe= 60,

    // ── Struct ──
    NewStruct     = 61,
    GetField      = 62,
    GetFieldSafe  = 63,

    // ── Optional ──
    IsNone        = 64,
    Coalesce      = 65,
    Unwrap        = 66,

    // ── Stack ──
    Dup           = 67,
    Pop           = 68,

    // ── VM control ──
    Halt          = 69,

    // ── Boxing (primitive → Ref for Any type) ──
    BoxInt        = 70,
    BoxReal       = 71,
    BoxBool       = 72,

    // ── Superinstructions (fused opcodes for hot patterns) ──
    // Each saves 1-2 loop iterations by combining common sequences.

    /// <summary>push locals[arg1] + constants[arg2] (int)</summary>
    AddLocalConstI = 73,
    /// <summary>push locals[arg1] - constants[arg2] (int)</summary>
    SubLocalConstI = 74,
    /// <summary>push locals[arg1] * constants[arg2] (int)</summary>
    MulLocalConstI = 75,
    /// <summary>push constants[arg1] + constants[arg2] (int)</summary>
    AddConstConstI = 76,
    /// <summary>push constants[arg1] * constants[arg2] (int)</summary>
    MulConstConstI = 77,
    /// <summary>stack[top] += constants[arg] (int)</summary>
    AddTopConstI   = 78,
    /// <summary>stack[top] *= constants[arg] (int)</summary>
    MulTopConstI   = 79,
    /// <summary>pop and store + halt (fused end-of-expression)</summary>
    StoreHalt      = 80,

    /// <summary>push locals[arg1] + constants[arg2] (real)</summary>
    AddLocalConstR = 81,
    /// <summary>push locals[arg1] * constants[arg2] (real)</summary>
    MulLocalConstR = 82,
    /// <summary>stack[top] += constants[arg] (real)</summary>
    AddTopConstR   = 83,
    /// <summary>stack[top] *= constants[arg] (real)</summary>
    MulTopConstR   = 84,
}

