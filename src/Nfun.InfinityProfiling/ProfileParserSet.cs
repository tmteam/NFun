using System.Runtime.CompilerServices;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace Nfun.InfinityProfiling
{
    public class ProfileParserSet: IProfileSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]     
        private SyntaxTree Parse(string expr) => Parser.Parse(Tokenizer.ToFlow(expr));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstTrue() => Parse(InfinityProfiling.Scripts.ConstTrue);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void Const1() => Parse(InfinityProfiling.Scripts.Const1);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstText() => Parse(InfinityProfiling.Scripts.CalcText);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstBoolArray() => Parse(InfinityProfiling.Scripts.ConstBoolArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstRealArray() => Parse(InfinityProfiling.Scripts.ConstRealArray);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcBool()=> Parse(InfinityProfiling.Scripts.CalcBool);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcReal()=> Parse(InfinityProfiling.Scripts.CalcReal);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcText()=> Parse(InfinityProfiling.Scripts.CalcText);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcKxb()=> Parse(InfinityProfiling.Scripts.CalcKxb);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealArray()=> Parse(InfinityProfiling.Scripts.CalcRealArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcFourArgs()=> Parse(InfinityProfiling.Scripts.CalcFourArgs);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstMultiArrays()=> Parse(InfinityProfiling.Scripts.MultiplyArrayItems);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstDummyBubble() => Parse(InfinityProfiling.Scripts.DummyBubbleSort);
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstEverything() => Parse(InfinityProfiling.Scripts.Everything);
    }
}