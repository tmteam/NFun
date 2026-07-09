using NFun.Tic.SolvingStates;
using NFun.TypeInferenceAdapter;
using NUnit.Framework;

namespace NFun.UnitTests.Converters;

// Generic identity contract (TicResolution.md §2): the identity of a generic
// type variable is the POSITION OF ITS ConstraintsState OBJECT in the
// constrains map — object identity, not structural equality. Two independent
// generics with equal content (typical case: two fresh unconstrained [∅..∅])
// must map to distinct generic indices.
public class TicConverterGenericIdentityTest {
    [Test]
    public void SignatureConverter_TwoEqualContentGenerics_GetDistinctIndices() {
        var csA = ConstraintsState.Empty;
        var csB = ConstraintsState.Empty;
        var converter = TicTypesConverter.GenericSignatureConverter(new[] { csA, csB });

        Assert.AreEqual(FunnyType.Generic(0), converter.Convert(csA));
        Assert.AreEqual(FunnyType.Generic(1), converter.Convert(csB));
    }

    [Test]
    public void SignatureConverter_ThreeEqualContentGenerics_GetDistinctIndices() {
        var csA = ConstraintsState.Empty;
        var csB = ConstraintsState.Empty;
        var csC = ConstraintsState.Empty;
        var converter = TicTypesConverter.GenericSignatureConverter(new[] { csA, csB, csC });

        Assert.AreEqual(FunnyType.Generic(0), converter.Convert(csA));
        Assert.AreEqual(FunnyType.Generic(1), converter.Convert(csB));
        Assert.AreEqual(FunnyType.Generic(2), converter.Convert(csC));
    }

    [Test]
    public void GenericMapConverter_TwoEqualContentGenerics_SubstituteDistinctTypes() {
        var csA = ConstraintsState.Empty;
        var csB = ConstraintsState.Empty;
        var converter = TicTypesConverter.ReplaceGenericTypesConverter(
            new[] { csA, csB },
            new[] { FunnyType.Real, FunnyType.Char });

        Assert.AreEqual(FunnyType.Real, converter.Convert(csA));
        Assert.AreEqual(FunnyType.Char, converter.Convert(csB));
    }
}
