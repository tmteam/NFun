using System.Collections.Generic;
using Nfun.InfinityProfiling.Sets;

namespace Nfun.InfinityProfiling
{
    public static class ProfileTools
    {
        public static void AddAndTruncate<T>( this LinkedList<T> list, T value, int maxSize)
        {
            list.AddLast(value);
            while (list.Count>maxSize) list.RemoveFirst();
        }
        public static void RunFastExamples(IProfileSet set)
        {
            set.Const1();
            set.ConstText();
            set.ConstTrue();
            set.ConstBoolArray();
            set.ConstRealArray();
            set.CalcSingleBool();
            set.CalcKxb();
            set.CalcSingleReal();
            set.CalcSingleText();
            set.CalcFourArgs();
            set.CalcRealArray();
            set.ConstBool();
            set.ConstInterpolation();
            set.ConstGenericFunc();
            set.ConstSquareEquation();
            set.CalcIntOp();
            set.CalcRealOp();
            set.CalcBoolOp();
            set.CalcTextOp();
            set.CalcInterpolation();
            set.CalcGenericFunc();
            set.CalcSquareEquation();
        }

        public static void RunAllExamples(IProfileSet set)
        {
            RunFastExamples(set);
            RunSlowExamples(set);
        }

        public static void RunSlowExamples(IProfileSet set)
        {
            set.ConstEverything();
            set.ConstDummyBubble();
            set.ConstMultiArrays();
        }
    }
}