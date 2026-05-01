using System.Runtime.InteropServices;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// Universal value representation for the VM. 16 bytes, no heap allocation for primitives.
/// I64 and Real overlap (C union). Ref holds reference types.
/// The opcode determines interpretation — no runtime tag needed.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct FunValue {
    [FieldOffset(0)] public long I64;
    [FieldOffset(0)] public double Real;
    [FieldOffset(8)] public object Ref;

    public static FunValue FromI64(long v) => new() { I64 = v };
    public static FunValue FromReal(double v) => new() { Real = v };
    public static FunValue FromBool(bool v) => new() { I64 = v ? 1 : 0 };
    public static FunValue FromChar(char v) => new() { I64 = v };
    public static FunValue FromRef(object v) => new() { Ref = v };
    public static readonly FunValue None = new() { Ref = FunnyNone.Instance };
    public static readonly FunValue True = new() { I64 = 1 };
    public static readonly FunValue False = new() { I64 = 0 };

    public bool IsNone => Ref is FunnyNone;

    public object Box(FunnyType type) => type.BaseType switch {
        BaseFunnyType.UInt8  => (byte)I64,
        BaseFunnyType.UInt16 => (ushort)I64,
        BaseFunnyType.UInt32 => (uint)I64,
        BaseFunnyType.UInt64 => (ulong)I64,
        BaseFunnyType.Int16  => (short)I64,
        BaseFunnyType.Int32  => (int)I64,
        BaseFunnyType.Int64  => I64,
        BaseFunnyType.Real   => Real,
        BaseFunnyType.Bool   => I64 != 0,
        BaseFunnyType.Char   => (char)I64,
        _                    => Ref,
    };

    public static FunValue Unbox(object obj, FunnyType type) => type.BaseType switch {
        BaseFunnyType.UInt8  => new FunValue { I64 = System.Convert.ToByte(obj) },
        BaseFunnyType.UInt16 => new FunValue { I64 = System.Convert.ToUInt16(obj) },
        BaseFunnyType.UInt32 => new FunValue { I64 = System.Convert.ToUInt32(obj) },
        BaseFunnyType.UInt64 => new FunValue { I64 = unchecked((long)System.Convert.ToUInt64(obj)) },
        BaseFunnyType.Int16  => new FunValue { I64 = System.Convert.ToInt16(obj) },
        BaseFunnyType.Int32  => new FunValue { I64 = System.Convert.ToInt32(obj) },
        BaseFunnyType.Int64  => new FunValue { I64 = System.Convert.ToInt64(obj) },
        BaseFunnyType.Real   => new FunValue { Real = System.Convert.ToDouble(obj) },
        BaseFunnyType.Bool   => new FunValue { I64 = System.Convert.ToBoolean(obj) ? 1 : 0 },
        BaseFunnyType.Char   => new FunValue { I64 = System.Convert.ToChar(obj) },
        _                    => new FunValue { Ref = obj },
    };
}
