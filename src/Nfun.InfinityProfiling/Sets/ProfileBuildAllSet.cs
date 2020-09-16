using System.Runtime.CompilerServices;
using NFun;
using NFun.Interpritation;
using NFun.Runtime;

namespace Nfun.InfinityProfiling.Sets
{
    public class ProfileBuildAllSet: IProfileSet
    {
        private readonly FunctionDictionary _dictionary;

        public ProfileBuildAllSet() => _dictionary = BaseFunctions.CreateDefaultDictionary();

        [MethodImpl(MethodImplOptions.AggressiveInlining|MethodImplOptions.NoOptimization)]     
        private FunRuntime Build(string expr) => FunBuilder.With(expr).With(_dictionary).Build();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstTrue() => Build(Scripts.ConstTrue);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstBool() => Build(Scripts.ConstBoolOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void Const1() => Build(Scripts.Const1);

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
        public void CalcSingleBool()=> Build(Scripts.CalcSingleBool);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSingleReal()=> Build(Scripts.CalcSingleReal);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSingleText()=> Build(Scripts.CalcSingleText);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcIntOp() => Build(Scripts.CalcIntOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealOp()=> Build(Scripts.CalcRealOp);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcBoolOp()=> Build(Scripts.CalcBoolOp);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcTextOp()=> Build(Scripts.CalcTextOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcInterpolation() => Build(Scripts.CalcInterpolation);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcGenericFunc()=> Build(Scripts.CalcGenericFunc);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSquareEquation()=> Build(Scripts.CalcSquareEquation);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcKxb()=> Build(Scripts.CalcKxb);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealArray()=> Build(Scripts.CalcRealArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcFourArgs()=> Build(Scripts.CalcFourArgs);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstMultiArrays()=> Build(Scripts.MultiplyArrayItems);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstDummyBubble() => Build(Scripts.DummyBubbleSort);
       
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstEverything() => Build(Scripts.Everything);
    }
}