using System;
using System.Collections.Generic;
using NFun.InfinityProfiling.Sets;

namespace NFun.InfinityProfiling
{
    public static class ProfileTools
    {
        public static void AddAndTruncate<T>(this LinkedList<T> list, T value, int maxSize)
        {
            list.AddLast(value);
            while (list.Count > maxSize) list.RemoveFirst();
        }

        public static Action<IProfileSet> GetSet(ProfileSet set)
        {
            switch (set)
            {
                case ProfileSet.Primitives:
                    return RunPrimitiveExamples;
                case ProfileSet.Middle:
                    return RunMiddleExamples;
                case ProfileSet.Complex:
                    return RunComplexExamples;
                case ProfileSet.All:
                    return RunAllExamples;
                default:
                    throw new ArgumentOutOfRangeException(nameof(set), set, null);
            }
        }

        public static void RunPrimitiveExamples(IProfileSet set)
        {
            set.PrimitiveConstIntSimpleArithmetics();
            set.PrimitiveConstRealSimpleArithmetics();
            set.PrimitiveConstBoolSimpleArithmetics();
            set.PrimitiveCalcReal2Var();
            set.PrimitiveCalcInt2Var();
            set.PrimitivesConst1();
            set.PrimitivesConstTrue();
            set.PrimitiveCalcSingleBool();
            set.PrimitivesCalcKxb();
            set.PrimitiveCalcSingleReal();
            set.PrimitivesConstBool();
            set.PrimitiveCalcIntOp();
            set.PrimitiveCalcRealOp();
            set.PrimitiveCalcBoolOp();
        }

        public static void RunMiddleExamples(IProfileSet set)
        {
            set.ConstText();
            set.ConstBoolArray();
            set.ConstRealArray();
            set.CalcSingleText();
            set.CalcFourArgs();
            set.CalcRealArray();
            set.ConstInterpolation();
            set.ConstGenericFunc();
            set.ConstSquareEquation();
            set.CalcTextOp();
            set.CalcInterpolation();
            set.CalcGenericFunc();
            set.CalcSquareEquation();
        }

        public static void RunAllExamples(IProfileSet set)
        {
            RunPrimitiveExamples(set);
            RunMiddleExamples(set);
            RunComplexExamples(set);
        }

        public static void RunComplexExamples(IProfileSet set)
        {
            set.ComplexConstEverything();
            set.ComplexDummyBubble();
            set.ComplexConstMultiArrays();
        }
    }
}