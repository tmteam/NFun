using System.Collections.Generic;

namespace Nfun.InfinityProfiling
{
    public static class ProfileTools
    {
        public static void AddAndTruncate<T>( this LinkedList<T> list, T value, int maxSize)
        {
            list.AddLast(value);
            while (list.Count>maxSize) list.RemoveFirst();
        }
        public static void RunAll(IProfileSet set)
        {
            set.Const1();
            set.ConstEverything();
            set.ConstText();
            set.ConstTrue();
            set.ConstBoolArray();
            set.ConstDummyBubble();
            set.ConstMultiArrays();
            set.ConstRealArray();
            set.CalcBool();
            set.CalcKxb();
            set.CalcReal();
            set.CalcText();
            set.CalcFourArgs();
            set.CalcRealArray();
        }
    }
}