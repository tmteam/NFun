using System.Runtime.CompilerServices;
using NFun.Runtime;

namespace NFun.InfinityProfiling.Sets
{
    public class ProfileCalculateSet : IProfileSet
    {
        private readonly FunnyRuntime _calcBoolOp;
        private readonly FunnyRuntime _calcFourArgs;
        private readonly FunnyRuntime _calcGenericFunc;
        private readonly FunnyRuntime _calcInterpolation;
        private readonly FunnyRuntime _calcIntOp;
        private readonly FunnyRuntime _calcKxb;
        private readonly FunnyRuntime _calcReal;
        private readonly FunnyRuntime _calcRealArray;
        private readonly FunnyRuntime _calcRealOp;
        private readonly FunnyRuntime _calcSingleBool;
        private readonly FunnyRuntime _calcSquareEquation;
        private readonly FunnyRuntime _calcText;
        private readonly FunnyRuntime _calcTextOp;
        private readonly FunnyRuntime _const1Runtime;
        private readonly FunnyRuntime _constBool;
        private readonly FunnyRuntime _constBoolArrayRuntime;
        private readonly FunnyRuntime _constDummyBubbleSort;
        private readonly FunnyRuntime _constEverything;
        private readonly FunnyRuntime _constGenericFunc;
        private readonly FunnyRuntime _constInterpolation;
        private readonly FunnyRuntime _constMultiplyArrayItems;
        private readonly FunnyRuntime _constRealArrayRuntime;
        private readonly FunnyRuntime _constSquareEquation;
        private readonly FunnyRuntime _constTextRuntime;
        private readonly FunnyRuntime _constTrueRuntime;
        private readonly FunnyRuntime _primitiveCalcInt2Var;
        private readonly FunnyRuntime _primitiveCalcReal2Var;
        private readonly FunnyRuntime _primitiveConstBoolSimpleArithmetics;
        private readonly FunnyRuntime _primitiveConstIntSimpleArithmetics;
        private readonly FunnyRuntime _primitiveConstRealSimpleArithmetics;

        public ProfileCalculateSet()
        {
            _primitiveConstIntSimpleArithmetics = Build(Scripts.PrimitiveConstIntSimpleArithmetics);
            _primitiveConstRealSimpleArithmetics = Build(Scripts.PrimitiveConstRealSimpleArithmetics);
            _primitiveConstBoolSimpleArithmetics = Build(Scripts.PrimitiveConstBoolSimpleArithmetics);
            _primitiveCalcReal2Var = Build(Scripts.PrimitiveCalcReal2Var);
            _primitiveCalcInt2Var = Build(Scripts.PrimitiveCalcInt2Var);

            _calcIntOp = Build(Scripts.CalcIntOp);
            _calcRealOp = Build(Scripts.CalcRealOp);
            _calcBoolOp = Build(Scripts.CalcBoolOp);
            _calcSquareEquation = Build(Scripts.CalcSquareEquation);
            _calcTextOp = Build(Scripts.CalcTextOp);
            _calcInterpolation = Build(Scripts.CalcInterpolation);
            _calcGenericFunc = Build(Scripts.CalcGenericFunc);

            _constTrueRuntime = Build(Scripts.ConstTrue);
            _const1Runtime = Build(Scripts.Const1);
            _constTextRuntime = Build(Scripts.ConstText);
            _constBoolArrayRuntime = Build(Scripts.ConstBoolArray);
            _constRealArrayRuntime = Build(Scripts.ConstRealArray);
            _calcSingleBool = Build(Scripts.CalcSingleBool);
            _constBool = Build(Scripts.ConstBoolOp);
            _calcReal = Build(Scripts.CalcSingleReal);
            _calcText = Build(Scripts.CalcSingleText);
            _calcKxb = Build(Scripts.CalcKxb);
            _calcRealArray = Build(Scripts.CalcRealArray);
            _calcFourArgs = Build(Scripts.CalcFourArgs);
            _constMultiplyArrayItems = Build(Scripts.MultiplyArrayItems);
            _constDummyBubbleSort = Build(Scripts.DummyBubbleSort);
            _constEverything = Build(Scripts.Everything);

            _constInterpolation = Build(Scripts.ConstInterpolation);
            _constSquareEquation = Build(Scripts.ConstSquareEquation);
            _constGenericFunc = Build(Scripts.ConstGenericFunc);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesConstTrue() => _constTrueRuntime.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesConstBool() => _constBool.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesConst1() => _const1Runtime.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstText() => _constTextRuntime.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstBoolArray() => _constBoolArrayRuntime.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstRealArray() => _constRealArrayRuntime.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstInterpolation() => _constInterpolation.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstGenericFunc() => _constGenericFunc.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstSquareEquation() => _constSquareEquation.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveConstIntSimpleArithmetics() => _primitiveConstIntSimpleArithmetics.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveConstRealSimpleArithmetics() => _primitiveConstRealSimpleArithmetics.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveConstBoolSimpleArithmetics() => _primitiveConstBoolSimpleArithmetics.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcReal2Var() => _primitiveCalcReal2Var.ProfileCalculation(("a", 42.1), ("b", 24.0));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcInt2Var() => _primitiveCalcInt2Var.ProfileCalculation(("a", 42), ("b", 24));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcSingleBool() => _calcSingleBool.ProfileCalculation(("x", true));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcSingleReal() => _calcReal.ProfileCalculation(("x", 42.1));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSingleText() => _calcText.ProfileCalculation(("x", "kavabanga"));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcIntOp() => _calcIntOp.ProfileCalculation(("x", 1));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcRealOp() => _calcRealOp.ProfileCalculation(("x", 2.0));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcBoolOp() => _calcBoolOp.ProfileCalculation(("x", true));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcTextOp() => _calcTextOp.ProfileCalculation(("x", "vasa"));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcInterpolation() => _calcInterpolation.ProfileCalculation(("a", 2.0), ("b", 3.4));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcGenericFunc() => _calcGenericFunc.ProfileCalculation(("a", 1.0), ("b", 2.0), ("c", 3.0));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSquareEquation() => _calcSquareEquation.ProfileCalculation(("a", 1.0), ("b", 10.1), ("c", 1.2));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesCalcKxb() => _calcKxb.ProfileCalculation(("x", 12.3));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealArray() => _calcRealArray.ProfileCalculation(("x", 3.14));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcFourArgs() =>
            _calcFourArgs.ProfileCalculation(
                ("a", 1.1),
                ("b", 42.1),
                ("c", 4.1),
                ("d", "kotopes")
            );

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ComplexConstMultiArrays() => _constMultiplyArrayItems.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ComplexDummyBubble() => _constDummyBubbleSort.ProfileCalculation();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ComplexConstEverything() => _constEverything.ProfileCalculation();

        private FunnyRuntime Build(string expr) => Funny.Hardcore.Build(expr);
    }
}