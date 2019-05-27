using System.Diagnostics.CodeAnalysis;
using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SelectOverloadsTest
    {
        SolvingNode Int => SolvingNode.CreateStrict(FType.Int32);
        SolvingNode Long => SolvingNode.CreateStrict(FType.Int64);
        SolvingNode Real  => SolvingNode.CreateStrict(FType.Real);
        SolvingNode Text  => SolvingNode.CreateStrict(FType.Text);
        SolvingNode Any   => SolvingNode.CreateStrict(FType.Any);
        
        SolvingNode IntLimit   => SolvingNode.CreateLimit(FType.Int32);
        SolvingNode LongLimit  => SolvingNode.CreateLimit(FType.Int64);
        SolvingNode RealLimit  => SolvingNode.CreateLimit(FType.Real);
        SolvingNode TextLimit  => SolvingNode.CreateLimit(FType.Text);
        SolvingNode AnyLimit   => SolvingNode.CreateLimit(FType.Any);
        SolvingNode SomeIntegerLimit => SolvingNode.CreateLimit(new FType(HmTypeName.SomeInteger));
        SolvingNode Lca(params SolvingNode[] nodes) => SolvingNode.CreateLca(nodes);
        SolvingNode Generic   => new SolvingNode();

        FunSignature RRRSignature = new FunSignature(FType.Real, FType.Real, FType.Real);
        FunSignature LLLSignature = new FunSignature(FType.Int64, FType.Int64, FType.Int64);
        FunSignature IIISignature = new FunSignature(FType.Int32, FType.Int32, FType.Int32);
        
        FunSignature R2Signature = new FunSignature(FType.Real,  FType.Real);
        FunSignature L2Signature = new FunSignature(FType.Int64, FType.Int64);
        FunSignature I2Signature = new FunSignature(FType.Int32, FType.Int32);

        private FunSignature[] Rli_2Arg_Overload => new[] {RRRSignature, LLLSignature, IIISignature};
        private FunSignature[] Rli_1Arg_Overload => new[] {R2Signature, L2Signature, I2Signature};

        
        
        #region strict
        
        [Test]
        public void StrictRli_R2_fitsR2() =>Assert.AreEqual(
            expected: R2Signature, 
            actual: FunSignature.GetBestFitOrNull(Rli_1Arg_Overload, Real, Real));
        
        [Test]
        public void StrictRli_L2_fitsL2() =>Assert.AreEqual(
            expected: L2Signature, 
            actual: FunSignature.GetBestFitOrNull(Rli_1Arg_Overload, Long, Long));
        
        [Test]
        public void StrictRli_I2_fitsI2() =>Assert.AreEqual(
            expected: I2Signature, 
            actual: FunSignature.GetBestFitOrNull(Rli_1Arg_Overload, Int, Int));

        [Test]
        public void StrictRli_RI_fitsI2() =>Assert.AreEqual(
            expected: I2Signature, 
            actual: FunSignature.GetBestFitOrNull(Rli_1Arg_Overload, Real, Int));
        
        [Test]
        public void StrictRli_IR_returnsNull() =>Assert.AreEqual(
            expected: null, 
            actual: FunSignature.GetBestFitOrNull(Rli_1Arg_Overload, Int, Real));

        
        [Test]
        public void StrictRli_RRR_fitsRRR() =>Assert.AreEqual(
            expected: RRRSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, Real, Real));
        [Test]
        public void StrictRli_LLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Long, Long, Long));
        [Test]
        public void StrictRli_III_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Int, Int, Int));
        [Test]
        public void StrictRli_RLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, Long, Long));
        [Test]
        public void StrictRli_RRI_fitsRRR() => Assert.AreEqual(
            expected: RRRSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, Real, Int));
        [Test]
        public void StrictRli_GenericRI_fitsRRR() => Assert.AreEqual(
            expected: RRRSignature, 
            actual:   FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, Real, Int) );
        [Test]
        public void StrictRli_GenericLI_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature,
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, Long, Int));
        [Test]
        public void StrictRli_GenericII_fitsIII() => Assert.AreEqual(
            expected: IIISignature,
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, Int, Int));
        [Test]
        public void StrictRli_LGenericGeneric_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Long, Generic, Generic));
        [Test]
        public void StrictRli_AnyAnyAny_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Any, Any, Any));
        [Test]
        public void StrictRli_RealRealText_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, Real, Text));
        [Test]
        public void StrictRli_TextRealReal_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Text, Real, Real));
        
        [Test]
        public void StrictRli_ILI_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Int, Long, Int));
        [Test]
        public void StrictRli_ILL_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Int, Long, Long));
        #endregion

        #region limit
        [Test]
        public void LimitRli_RRR_fitsRRR() =>Assert.AreEqual(
            expected: RRRSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, Real, Real));
        [Test]
        public void LimitRli_LLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, LongLimit, LongLimit, LongLimit));
        [Test]
        public void LimitRli_III_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, IntLimit, IntLimit, IntLimit));
        [Test]
        public void LimitRli_RLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, LongLimit, LongLimit));
        [Test]
        public void _controversial_LimitRli_RlimRlimIlim_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, RealLimit, IntLimit));

        [Test]
        public void _сontroversial_LimitRli_GenericRI_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual:   FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, RealLimit, IntLimit) );
        [Test]
        public void _сontroversial_LimitRli_GenericLI_fitsIII() => Assert.AreEqual(
            expected: IIISignature,
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, LongLimit, IntLimit));
        [Test]
        public void LimitRli_GenericII_fitsIII() => Assert.AreEqual(
            expected: IIISignature,
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, IntLimit, IntLimit));
        [Test]
        public void LimitRli_LGenericGeneric_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, LongLimit, Generic, Generic));
        [Test]
        public void LimitRli_AnyAnyAny_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, AnyLimit, AnyLimit, AnyLimit));
        [Test]
        public void LimitRli_RealLimitRealLimitTextLimit_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, RealLimit, TextLimit));
        [Test]
        public void LimitRli_TextLimitRealLimitRealLimit_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, TextLimit, RealLimit, RealLimit));
       
        [Test]
        public void LimitRli_SomeIntgerRealReal_returnsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, SomeIntegerLimit, RealLimit, RealLimit));

        
        #endregion

        
        #region mixed
        
        [Test]
        public void IMPORTANT_LimitRli_RlimRlimIStrict_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, RealLimit, Int));
        
        [Test]
        public void IMPORTANT_LimitRli_RlimIStrictIStrict_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, Int, Int));

        [Test]
        public void IMPORTANT_LimitRli_GenericLcaOfLimRealAndInt_fitsI2() => Assert.AreEqual(
            expected: I2Signature, 
            actual: FunSignature.GetBestFitOrNull(Rli_1Arg_Overload, Generic, Lca(RealLimit, Int)));

        [Test]
        public void IMPORTANT_LimitRli_GenericRlimIStrict_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic,RealLimit, Int));

        
        [Test]
        public void MixRli_RRR_fitsRRR() =>Assert.AreEqual(
            expected: RRRSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, Real, Real));
        [Test]
        public void MixRli_LLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, LongLimit, LongLimit, Long));
        [Test]
        public void MixRli_III_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Int, IntLimit, IntLimit));
        [Test]
        public void MixRli_RLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, LongLimit, Long));
        [Test]
        public void MixRli_RLimitRLimitIStrict_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, RealLimit, Int));
        
        [Test]
        public void MixRli_RLimitLstringIlimit_fitsIII() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, Long, IntLimit));
        
        [Test]
        public void MixRli_GenericRI_fitsRRR() => Assert.AreEqual(
            expected: RRRSignature, 
            actual:   FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, Real, IntLimit));
        [Test]
        public void MixRli_GenericLlimitIstring_fitsIII() => Assert.AreEqual(
            expected: IIISignature,
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, LongLimit, Int));
        [Test]
        public void MixRli_GenericII_fitsIII() => Assert.AreEqual(
            expected: IIISignature,
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, Int, IntLimit));
        [Test]
        public void MixRli_LGenericGeneric_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, LongLimit, Generic, Generic));
        [Test]
        public void MixRli_AnyAnyAny_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Any, AnyLimit, Any));
        [Test]
        public void MixRli_RealRealText_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, Real, TextLimit));
        [Test]
        public void MixRli_TextRealReal_returnsNull() => Assert.IsNull(
            FunSignature.GetBestFitOrNull(Rli_2Arg_Overload, TextLimit, Real, RealLimit));
        #endregion

    }
}