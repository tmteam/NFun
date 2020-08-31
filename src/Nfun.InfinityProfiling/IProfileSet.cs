using System;

namespace Nfun.InfinityProfiling
{
    public interface IProfileSet
    {
        void ConstTrue(); 
        void Const1();
        void ConstText();
        void ConstBoolArray();
        void ConstRealArray();
        void CalcBool();
        void CalcReal();
        void CalcText();
        void CalcKxb();
        void CalcRealArray();
        void CalcFourArgs();
        void ConstMultiArrays();
        void ConstDummyBubble();
        void ConstEverything();
    }
}