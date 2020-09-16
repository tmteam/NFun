namespace Nfun.InfinityProfiling.Sets
{
    public interface IProfileSet
    {
        void ConstTrue(); 
        void ConstBool(); 
        void Const1();
        void ConstText();
        void ConstBoolArray();
        void ConstRealArray();
        void ConstInterpolation();
        void ConstGenericFunc();
        void ConstSquareEquation();
        void CalcSingleBool();
        void CalcSingleReal();
        void CalcSingleText();
        void CalcIntOp();
        void CalcRealOp();
        void CalcBoolOp();
        void CalcTextOp();
        void CalcInterpolation();
        void CalcGenericFunc();
        void CalcSquareEquation();
        void CalcKxb();
        void CalcRealArray();
        void CalcFourArgs();
        void ConstMultiArrays();
        void ConstDummyBubble();
        void ConstEverything();
    }
}