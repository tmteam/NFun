using NFun;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on master (round 2,
/// post Round-1 fixes). 3 agents × ~100 iterations (SIMPLE_AND_TRICKY,
/// HELL_AND_NESTED, EDGE_AND_CREATIVE). After filtering FP-class
/// (small-int → Real promotion inconsistencies, deferred), 3 confirmed
/// bugs remain. Each marked [Ignore] until fixed.
/// </summary>
public class BugHuntMasterRound2 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ───────────────────────────────────────────────────────────────
    // MR2Bug1 — 2D array literal of struct with optional field, mixed
    //   asymmetric none/value across sub-arrays, FAILS when the outer
    //   variable carries the matching type annotation. Without the
    //   annotation the same expression infers exactly the declared type.
    //
    //   Works:  out = [[{v=none}], [{v=2}]]              → {v:Int32?}[][]
    //   Works:  out:int?[][] = [[none], [2]]             → Int32?[][]
    //   Fails:  out:{v:int?}[][] = [[{v=none}], [{v=2}]] → FU761 "Seems like expression `2` cannot be used here"
    //
    //   Symptom is TIC failing to push the annotated `int?` element
    //   constraint through the 2-level array nesting onto a struct field
    //   when one sub-array is none-only and another is value-only.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR2Bug1_AnnotatedNestedOptionalStructArray_ParseError() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("out:{v:int?}[][] = [[{v=none}], [{v=2}]]"));
    }

    [Test]
    public void MR2Bug1_AnnotatedNestedOptionalStructArray_Variant3D() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("out:{v:int?}[][][] = [[[{v=none}]], [[{v=2}]]]"));
    }

    [Test]
    public void MR2Bug1_AnnotatedNestedOptionalStructArray_MultipleFields() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("out:{v:int?,w:int?}[][] = [[{v=none,w=1}], [{v=2,w=none}]]"));
    }

    // MR2Bug2 was a doc bug — Specs/Basics.md showed `+` for text concat.
    // Fixed by switching the example to interpolation: `'{greeting} {name}'`.

    // ───────────────────────────────────────────────────────────────
    // MR2Bug3 — `none.toHexText()` throws NFunImpossibleException at
    //   RUNTIME ("toHexText: unsupported type Int64?") instead of giving
    //   a clean compile-time FU783 like the equivalent annotated form.
    //
    //   Compile-time rejection works:
    //     x:int? = none; y = x.toHexText()  → FU783 "Invalid function call argument"
    //
    //   Crash variants (all: NFunImpossibleException):
    //     x = none.toHexText()
    //     x = none.toBinText()
    //     x = '{none:hex}'   (format spec lowers to toHexText)
    //     x = '{none:HEX}'   (case-insensitive variant)
    //     x = '{none:bin}'
    //
    //   toNumText/toSciText reject correctly at compile-time, so the
    //   asymmetry isolates toHexText/toBinText signature handling as
    //   the source. Internal exception cannot be caught by try/catch.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR2Bug3_NoneToHexText_InternalCrash() {
        Assert.Throws<FunnyParseException>(() => "x = none.toHexText()".Calc());
    }

    [Test]
    public void MR2Bug3_NoneToBinText_InternalCrash() {
        Assert.Throws<FunnyParseException>(() => "x = none.toBinText()".Calc());
    }

    [Test]
    public void MR2Bug3_NoneHexFormatSpec_InternalCrash() {
        Assert.Throws<FunnyParseException>(() => "x = '{none:hex}'".Calc());
    }

    [Test]
    public void MR2Bug3_NoneBitInvert_InternalCrash() {
        Assert.Throws<FunnyParseException>(() => "x = ~none".Calc());
    }

    [Test]
    public void MR2Bug3_NoneBitShift_InternalCrash() {
        Assert.Throws<FunnyParseException>(() => "x = none << 1".Calc());
    }

    // ───────────────────────────────────────────────────────────────
    // MR2Bug4 — Arithmetic operators (`+`, `-`, `*`) over-promote
    //   sub-Arithmetics operands to Real in two corners; `%` does not
    //   promote at all. All three are facets of the same gap in
    //   TIC preferred-type resolution for operands below the
    //   Arithmetics range (`int32 | uint32 ≤ T ≤ real`):
    //
    //   Over-promote → Real (should be Int32 / UInt32):
    //     y:byte = 5, z:byte = 5; out = y + z       → out:Real     (uint16+uint16 → Int32, so byte should too)
    //     y:int16 = -5, z:int16 = -5; out = y + z   → out:Real     (int16+int16 pos → Int32)
    //
    //   No promotion (operator definition says Arithmetics but accepts byte):
    //     y:byte = 5, z:byte = 2; out = y % z       → out:UInt8    (% is Arithmetics per spec)
    //
    //   Filed as one bug since the underlying inference defect — TIC
    //   defaulting to the top of Arithmetics (Real) for some preferred
    //   chains while leaving others entirely unpromoted — is the same.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR2Bug4a_BytePlusByte_OverPromotesToReal() {
        "y:byte = 5\rz:byte = 5\rout = y + z".AssertReturns(
            ("y", (byte)5), ("z", (byte)5), ("out", 10));
    }

    [Test]
    public void MR2Bug4b_NegInt16PlusNegInt16_OverPromotesToReal() {
        "y:int16 = -5\rz:int16 = -5\rout = y + z".AssertReturns(
            ("y", (short)-5), ("z", (short)-5), ("out", -10));
    }

    // (c) was filed as a bug but turned out to be a spec/impl mismatch: `%` uses
    // GenericConstrains.Numbers in the implementation (any numeric type, no widening),
    // so byte%byte → byte is correct. Spec table updated to say `%` is Numbers,
    // not Arithmetics. Test asserts the correct byte-typed result.
    [Test]
    public void MR2Bug4c_ByteModuloByte_KeepsByteType() {
        "y:byte = 5\rz:byte = 2\rout = y % z".AssertReturns(
            ("y", (byte)5), ("z", (byte)2), ("out", (byte)1));
    }
}
