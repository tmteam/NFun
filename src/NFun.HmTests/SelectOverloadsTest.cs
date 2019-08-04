using System.Diagnostics.CodeAnalysis;
using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SelectOverloadsTest
    {
        SolvingNode Int16 => SolvingNode.CreateStrict(TiType.Int16);

        SolvingNode Int32 => SolvingNode.CreateStrict(TiType.Int32);
        SolvingNode Long => SolvingNode.CreateStrict(TiType.Int64);
        SolvingNode Real  => SolvingNode.CreateStrict(TiType.Real);
        SolvingNode Text  => SolvingNode.CreateStrict(TiType.Text);
        SolvingNode Any   => SolvingNode.CreateStrict(TiType.Any);
        
        SolvingNode Int16Lca   => SolvingNode.CreateLca(Int16);

        SolvingNode Int32Lca   => SolvingNode.CreateLca(Int32);
        SolvingNode LongLca  => SolvingNode.CreateLca(Long);
        SolvingNode RealLca  => SolvingNode.CreateLca(Real);
        SolvingNode IntLimit   => SolvingNode.CreateLimit(TiType.Int32);
        SolvingNode LongLimit  => SolvingNode.CreateLimit(TiType.Int64);
        SolvingNode RealLimit  => SolvingNode.CreateLimit(TiType.Real);
        SolvingNode TextLimit  => SolvingNode.CreateLimit(TiType.Text);
        SolvingNode AnyLimit   => SolvingNode.CreateLimit(TiType.Any);
        SolvingNode SomeIntegerLimit => SolvingNode.CreateLimit(new TiType(TiTypeName.SomeInteger));
        SolvingNode Lca(params SolvingNode[] nodes) => SolvingNode.CreateLca(nodes);
        SolvingNode Generic   => new SolvingNode();

        TiFunctionSignature RRRSignature = new TiFunctionSignature(TiType.Real, TiType.Real, TiType.Real);
        TiFunctionSignature LLLSignature = new TiFunctionSignature(TiType.Int64, TiType.Int64, TiType.Int64);
        TiFunctionSignature IIISignature = new TiFunctionSignature(TiType.Int32, TiType.Int32, TiType.Int32);
        
        TiFunctionSignature R2Signature = new TiFunctionSignature(TiType.Real,  TiType.Real);
        TiFunctionSignature L2Signature = new TiFunctionSignature(TiType.Int64, TiType.Int64);
        TiFunctionSignature I2Signature = new TiFunctionSignature(TiType.Int32, TiType.Int32);

        private TiFunctionSignature[] Rli_2Arg_Overload => new[] {RRRSignature, LLLSignature, IIISignature};
        private TiFunctionSignature[] Rli_1Arg_Overload => new[] {R2Signature, L2Signature, I2Signature};

        
        
        #region strict
        
        [Test]
        public void StrictRli_R2_fitsR2() =>Assert.AreEqual(
            expected: R2Signature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_1Arg_Overload, Real, Real));
        
        [Test]
        public void StrictRli_L2_fitsL2() =>Assert.AreEqual(
            expected: L2Signature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_1Arg_Overload, Long, Long));
        
        [Test]
        public void StrictRli_I2_fitsI2() =>Assert.AreEqual(
            expected: I2Signature, 
            actual:   TiFunctionSignature.GetBestFitOrNull(Rli_1Arg_Overload, Int32, Int32));

        [Test]
        public void _controversial_StrictRli_RI_fitsI2() =>Assert.AreEqual(
            expected: I2Signature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_1Arg_Overload, Real, Int32));
        
        [Test]
        public void StrictRli_IR_returnsNull() =>Assert.AreEqual(
            expected: null, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_1Arg_Overload, Int32, Real));

        
        [Test]
        public void StrictRli_RRR_fitsRRR() =>Assert.AreEqual(
            expected: RRRSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, Real, Real));
        [Test]
        public void StrictRli_LLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Long, Long, Long));
        [Test]
        public void StrictRli_III_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Int32, Int32, Int32));
        [Test]
        public void StrictRli_RLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, Long, Long));
        [Test]
        public void StrictRli_RRI_fitsRRR() => Assert.AreEqual(
            expected: RRRSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, Real, Int32));
        [Test]
        public void StrictRli_GenericRI_fitsRRR() => Assert.AreEqual(
            expected: RRRSignature, 
            actual:   TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, Real, Int32) );
        [Test]
        public void StrictRli_GenericLI_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature,
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, Long, Int32));
        [Test]
        public void StrictRli_GenericII_fitsIII() => Assert.AreEqual(
            expected: IIISignature,
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, Int32, Int32));
        [Test]
        public void StrictRli_LGenericGeneric_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Long, Generic, Generic));
        [Test]
        public void StrictRli_AnyAnyAny_returnsNull() => Assert.IsNull(
            TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Any, Any, Any));
        [Test]
        public void StrictRli_RealRealText_returnsNull() => Assert.IsNull(
            TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, Real, Text));
        [Test]
        public void StrictRli_TextRealReal_returnsNull() => Assert.IsNull(
            TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Text, Real, Real));
        
        [Test]
        public void StrictRli_ILI_returnsNull() => Assert.IsNull(
            TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Int32, Long, Int32));
        [Test]
        public void StrictRli_ILL_returnsNull() => Assert.IsNull(
            TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Int32, Long, Long));
        #endregion

        #region limit
        [Test]
        public void LimitRli_RRR_fitsRRR() =>Assert.AreEqual(
            expected: RRRSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, Real, Real));
        [Test]
        public void LimitRli_LLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, LongLimit, LongLimit, LongLimit));
        [Test]
        public void LimitRli_III_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, IntLimit, IntLimit, IntLimit));
        [Test]
        public void LimitRli_RLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, LongLimit, LongLimit));
        [Test]
        public void _controversial_LimitRli_RlimRlimIlim_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, RealLimit, IntLimit));

        [Test]
        public void _сontroversial_LimitRli_GenericRI_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual:   TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, RealLimit, IntLimit) );
        [Test]
        public void _сontroversial_LimitRli_GenericLI_fitsIII() => Assert.AreEqual(
            expected: IIISignature,
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, LongLimit, IntLimit));
        [Test]
        public void LimitRli_GenericII_fitsIII() => Assert.AreEqual(
            expected: IIISignature,
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, IntLimit, IntLimit));
        [Test]
        public void LimitRli_LGenericGeneric_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, LongLimit, Generic, Generic));
        [Test]
        public void LimitRli_AnyAnyAny_returnsNull() => Assert.AreEqual(
            expected: RRRSignature,
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, AnyLimit, AnyLimit, AnyLimit));
        [Test]
        public void LimitRli_RealLimitRealLimitTextLimit_returnsNull() => Assert.IsNull(
            TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, RealLimit, TextLimit));
        [Test]
        public void LimitRli_TextLimitRealLimitRealLimit_returnsNull() => Assert.IsNull(
            TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, TextLimit, RealLimit, RealLimit));
       
        [Test]
        public void LimitRli_SomeIntgerRealReal_returnsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, SomeIntegerLimit, RealLimit, RealLimit));

        
        #endregion

        
        #region mixed
        
        [Test]
        public void IMPORTANT_LimitRli_RlimRlimIStrict_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, RealLimit, Int32));
        
        [Test]
        public void IMPORTANT_LimitRli_RlimIStrictIStrict_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, Int32, Int32));


        [Test]
        public void IMPORTANT_LimitRli_GenericRlimIStrict_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic,RealLimit, Int32));

        
        [Test]
        public void MixRli_RRR_fitsRRR() =>Assert.AreEqual(
            expected: RRRSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, Real, Real));
        [Test]
        public void MixRli_LLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, LongLimit, LongLimit, Long));
        [Test]
        public void MixRli_III_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Int32, IntLimit, IntLimit));
        [Test]
        public void MixRli_RLL_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, LongLimit, Long));
        [Test]
        public void MixRli_RLimitRLimitIStrict_fitsIII() => Assert.AreEqual(
            expected: IIISignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, RealLimit, Int32));
        
        [Test]
        public void MixRli_RLimitLstringIlimit_fitsIII() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, RealLimit, Long, IntLimit));
        
        [Test]
        public void MixRli_GenericRI_fitsRRR() => Assert.AreEqual(
            expected: RRRSignature, 
            actual:   TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, Real, IntLimit));
        [Test]
        public void MixRli_GenericLlimitIstring_fitsIII() => Assert.AreEqual(
            expected: IIISignature,
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, LongLimit, Int32));
        [Test]
        public void MixRli_GenericII_fitsIII() => Assert.AreEqual(
            expected: IIISignature,
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Generic, Int32, IntLimit));
        [Test]
        public void MixRli_LGenericGeneric_fitsLLL() => Assert.AreEqual(
            expected: LLLSignature, 
            actual: TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, LongLimit, Generic, Generic));
        [Test]
        public void MixRli_AnyAnyAny_returnsNull() => Assert.IsNull(
            TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Any, AnyLimit, Any));
        [Test]
        public void MixRli_RealRealText_returnsNull() => Assert.IsNull(
            TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, Real, Real, TextLimit));
        [Test]
        public void MixRli_TextRealReal_returnsNull() => Assert.IsNull(
            TiFunctionSignature.GetBestFitOrNull(Rli_2Arg_Overload, TextLimit, Real, RealLimit));
        #endregion

        #region retutnsLca

        //Return type is LCA (bottom limited)
        //Example:
        
        //y: LCA(int16) # it can be i16, i32, i64, real, any
        //y = myFun(z:int) 
        //myFunOverload: (int32)->int32
        // myFunOverload should fit, because y can be i32
        
        [Test]
        public void LcaRli_i32toLCAi32_fitsI2() =>CollectionAssert.AreEqual(
            expected: new[]{I2Signature}, 
            actual:   TiFunctionSignature.GetBestFits(Rli_1Arg_Overload, Int32Lca, Int32));
        
        [Test]
        public void LcaRli_i32toLCAi16_fitsI2() =>CollectionAssert.AreEqual(
            expected: new[]{I2Signature}, 
            actual:   TiFunctionSignature.GetBestFits(Rli_1Arg_Overload, Int16Lca, Int32));
        
        [Test]
        public void LcaRli_i32toLCAi64_fitsL2() =>CollectionAssert.AreEqual(
            expected: new []{L2Signature}, 
            actual:   TiFunctionSignature.GetBestFits(Rli_1Arg_Overload, LongLca, Int32));
        
        [Test]
        public void LcaRli_i32toLCAReal_fitsR2() =>CollectionAssert.AreEqual(
            expected: new []{R2Signature}, 
            actual:   TiFunctionSignature.GetBestFits(Rli_1Arg_Overload, RealLca, Int32));
        #endregion
    }
}