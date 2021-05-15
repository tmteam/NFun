using System.Runtime.CompilerServices;
using NFun;
using NFun.Interpritation;
using NFun.Runtime;

namespace NFun.InfinityProfiling.Sets
{
    public class ProfileBuildAllSet: IProfileSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining|MethodImplOptions.NoOptimization)]     
        private FunRuntime Build(string expr) => Funny.Hardcore.Build(expr);

        [MethodImpl(MethodImplOptions.NoOptimization)]

        public void PrimitiveConstIntSimpleArithmetics() => Build(Scripts.PrimitiveConstIntSimpleArithmetics);
        [MethodImpl(MethodImplOptions.NoOptimization)]

        public void PrimitiveConstRealSimpleArithmetics() => Build(Scripts.PrimitiveConstRealSimpleArithmetics);
        [MethodImpl(MethodImplOptions.NoOptimization)]

        public void PrimitiveConstBoolSimpleArithmetics() => Build(Scripts.PrimitiveConstBoolSimpleArithmetics);
        [MethodImpl(MethodImplOptions.NoOptimization)]

        public void PrimitiveCalcReal2Var() => Build(Scripts.PrimitiveCalcReal2Var);
        [MethodImpl(MethodImplOptions.NoOptimization)]

        public void PrimitiveCalcInt2Var()=> Build(Scripts.PrimitiveCalcInt2Var);

        
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesConstTrue() => Build(Scripts.ConstTrue);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesConstBool() => Build(Scripts.ConstBoolOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesConst1() => Build(Scripts.Const1);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstText() => Build(Scripts.ConstText);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstBoolArray() => Build(Scripts.ConstBoolArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstRealArray() => Build(Scripts.ConstRealArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstInterpolation() => Build(Scripts.ConstInterpolation);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstGenericFunc() => Build(Scripts.ConstGenericFunc);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstSquareEquation() => Build(Scripts.ConstSquareEquation);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcSingleBool()=> Build(Scripts.CalcSingleBool);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcSingleReal()=> Build(Scripts.CalcSingleReal);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSingleText()=> Build(Scripts.CalcSingleText);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcIntOp() => Build(Scripts.CalcIntOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcRealOp()=> Build(Scripts.CalcRealOp);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcBoolOp()=> Build(Scripts.CalcBoolOp);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcTextOp()=> Build(Scripts.CalcTextOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcInterpolation() => Build(Scripts.CalcInterpolation);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcGenericFunc()=> Build(Scripts.CalcGenericFunc);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSquareEquation()=> Build(Scripts.CalcSquareEquation);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesCalcKxb()=> Build(Scripts.CalcKxb);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealArray()=> Build(Scripts.CalcRealArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcFourArgs()=> Build(Scripts.CalcFourArgs);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ComplexConstMultiArrays()=> Build(Scripts.MultiplyArrayItems);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ComplexDummyBubble() => Build(Scripts.DummyBubbleSort);
       
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ComplexConstEverything() => Build(Scripts.Everything);
    }
}