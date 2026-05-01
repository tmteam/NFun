namespace NFun.VM;

/// <summary>
/// VM opcodes. Single byte — max 256 opcodes.
/// Naming: TYPE_OP for typed ops, plain name for type-agnostic.
/// </summary>
public enum Op : byte {
    // ── Load / Store ──
    LoadConstI    = 0x01, // [idx] → push constants[idx].I64
    LoadConstR    = 0x02, // [idx] → push constants[idx].Real
    LoadConstRef  = 0x03, // [idx] → push constants[idx].Ref
    LoadLocal     = 0x04, // [slot] → push locals[slot]
    StoreLocal    = 0x05, // [slot] → pop to locals[slot]
    LoadNone      = 0x06, // → push FunnyNone

    // ── Integer arithmetic (I64) ──
    AddInt        = 0x10,
    SubInt        = 0x11,
    MulInt        = 0x12,
    DivInt        = 0x13,
    ModInt        = 0x14,
    PowInt        = 0x15,
    NegInt        = 0x16,

    // ── Real arithmetic (double) ──
    AddReal       = 0x20,
    SubReal       = 0x21,
    MulReal       = 0x22,
    DivReal       = 0x23,
    ModReal       = 0x24,
    PowReal       = 0x25,
    NegReal       = 0x26,

    // ── Truncation (narrow after operation) ──
    TruncU8       = 0x30,
    TruncU16      = 0x31,
    TruncU32      = 0x32,
    TruncI16      = 0x33,
    TruncI32      = 0x34,

    // ── Type conversion ──
    IntToReal     = 0x38,
    RealToInt     = 0x39,

    // ── Integer comparison ──
    EqInt         = 0x40,
    NeqInt        = 0x41,
    LtInt         = 0x42,
    LteInt        = 0x43,
    GtInt         = 0x44,
    GteInt        = 0x45,
    LtUint        = 0x46,

    // ── Real comparison ──
    EqReal        = 0x48,
    NeqReal       = 0x49,
    LtReal        = 0x4A,
    LteReal       = 0x4B,
    GtReal        = 0x4C,
    GteReal       = 0x4D,

    // ── Reference comparison ──
    EqRef         = 0x4E,
    NeqRef        = 0x4F,

    // ── Logic (I64: 0=false, nonzero=true) ──
    And           = 0x50,
    Or            = 0x51,
    Not           = 0x52,

    // ── Bitwise ──
    BitAnd        = 0x58,
    BitOr         = 0x59,
    BitXor        = 0x5A,
    BitNot        = 0x5B,
    Shl           = 0x5C,
    Shr           = 0x5D,

    // ── Control flow ──
    Jump          = 0x60, // [addr16] → unconditional jump
    JumpIfFalse   = 0x61, // [addr16] → pop, jump if I64==0
    JumpIfTrue    = 0x62, // [addr16] → pop, jump if I64!=0

    // ── Function calls ──
    Call          = 0x68, // [func_id] [argc] → call user function
    CallExtern    = 0x69, // [func_id] [argc] → call .NET function
    TailCall      = 0x6A, // [func_id] [argc] → tail call (reuse frame)
    Return        = 0x6B, // → return from function

    // ── Array ──
    NewArray      = 0x70, // [count] → create array from stack
    GetElement    = 0x71, // arr, idx → val
    GetElementSafe= 0x72, // arr, idx → val (None if null/OOB)

    // ── Struct ──
    NewStruct     = 0x78, // [layout_id] [count] → create struct
    GetField      = 0x79, // [field_idx] struct → val
    GetFieldSafe  = 0x7A, // [field_idx] struct → val (None if null)

    // ── Optional ──
    IsNone        = 0x80, // val → bool
    Coalesce      = 0x81, // a, b → a ?? b
    Unwrap        = 0x82, // val → val (throw if None)

    // ── Stack ──
    Dup           = 0x90,
    Pop           = 0x91,

    // ── VM control ──
    Halt          = 0xFF,
}
