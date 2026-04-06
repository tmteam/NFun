using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated bug hunting.
/// </summary>
public class BugHuntResults {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    // FIXED: #2 (?? optionals), #3 (empty interpolation), #4 ([] else [300]),
    //        #5 (format specifier crash), #6 (max NaN), #7 (duplicate struct field),
    //        #9 (U12→UInt8 mapping caused overflow)
    // NOT A BUG: #8 (empty format specifier '{42:}' — by design)

    #region Unfixed bugs

    [Test][Ignore("Bug hunt #1: Optional element in non-optional array crashes")]
    public void Bug1_OptionalInNonOptionalArray_Crash() {
        "y:int? = none; out:int[] = [y, 1, 2]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
    }

    #endregion
}
