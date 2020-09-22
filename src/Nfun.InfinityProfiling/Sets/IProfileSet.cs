namespace Nfun.InfinityProfiling.Sets
{
    public interface IProfileSet
    {
        void PrimitiveConstIntSimpleArithmetics();
        void PrimitiveConstRealSimpleArithmetics();
        void PrimitiveConstBoolSimpleArithmetics();
        void PrimitiveCalcReal2Var ();
        void PrimitiveCalcInt2Var ();
        void PrimitiveCalcSingleBool();
        void PrimitiveCalcSingleReal();

        void PrimitivesConstTrue(); 
        void PrimitivesConstBool(); 
        void PrimitivesConst1();
        void PrimitiveCalcIntOp();
        void PrimitiveCalcRealOp();
        void PrimitiveCalcBoolOp();
        void ConstText();
        void ConstBoolArray();
        void ConstRealArray();
        void ConstInterpolation();
        void ConstGenericFunc();
        void ConstSquareEquation();
        void CalcSingleText();
      
        void CalcTextOp();
        void CalcInterpolation();
        void CalcGenericFunc();
        void CalcSquareEquation();
        void PrimitivesCalcKxb();
        void CalcRealArray();
        void CalcFourArgs();
        void ComplexConstMultiArrays();
        void ComplexDummyBubble();
        void ComplexConstEverything();
    }
}