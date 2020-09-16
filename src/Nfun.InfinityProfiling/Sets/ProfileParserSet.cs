using System.Runtime.CompilerServices;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace Nfun.InfinityProfiling.Sets
{
    public class ProfileParserSet: IProfileSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]     
        private SyntaxTree Parse(string expr) => Parser.Parse(Tokenizer.ToFlow(expr));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstTrue() => Parse(Scripts.ConstTrue);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstBool() => Parse(Scripts.ConstBoolOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void Const1() => Parse(Scripts.Const1);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstText() => Parse(Scripts.ConstText);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstBoolArray() => Parse(Scripts.ConstBoolArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstRealArray() => Parse(Scripts.ConstRealArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstInterpolation() => Parse(Scripts.ConstInterpolation);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstGenericFunc() => Parse(Scripts.ConstGenericFunc);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstSquareEquation() => Parse(Scripts.ConstSquareEquation);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSingleBool()=> Parse(Scripts.CalcSingleBool);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSingleReal()=> Parse(Scripts.CalcSingleReal);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSingleText()=> Parse(Scripts.CalcSingleText);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcIntOp() => Parse(Scripts.CalcIntOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealOp() => Parse(Scripts.CalcRealOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcBoolOp() => Parse(Scripts.CalcBoolOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcTextOp() => Parse(Scripts.CalcTextOp);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcInterpolation() => Parse(Scripts.CalcInterpolation);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcGenericFunc() => Parse(Scripts.CalcGenericFunc);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSquareEquation() => Parse(Scripts.CalcSquareEquation);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcKxb()=> Parse(Scripts.CalcKxb);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealArray()=> Parse(Scripts.CalcRealArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcFourArgs()=> Parse(Scripts.CalcFourArgs);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstMultiArrays()=> Parse(Scripts.MultiplyArrayItems);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstDummyBubble() => Parse(Scripts.DummyBubbleSort);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstEverything() => Parse(Scripts.Everything);
    }
}