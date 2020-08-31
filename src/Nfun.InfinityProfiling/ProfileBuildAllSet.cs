using System.Runtime.CompilerServices;
using NFun;
using NFun.Interpritation;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace Nfun.InfinityProfiling
{
    public class ProfileBuildAllSet: IProfileSet
    {
        private readonly FunctionDictionary _dictionary;

        public ProfileBuildAllSet() => _dictionary = BaseFunctions.CreateDefaultDictionary();

        [MethodImpl(MethodImplOptions.AggressiveInlining|MethodImplOptions.NoOptimization)]     
        private FunRuntime Build(string expr) => FunBuilder.With(expr).With(_dictionary).Build();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstTrue() => Build(InfinityProfiling.Scripts.ConstTrue);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void Const1() => Build(InfinityProfiling.Scripts.Const1);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstText() => Build(InfinityProfiling.Scripts.CalcText);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstBoolArray() => Build(InfinityProfiling.Scripts.ConstBoolArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstRealArray() => Build(InfinityProfiling.Scripts.ConstRealArray);
       
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcBool()=> Build(InfinityProfiling.Scripts.CalcBool);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcReal()=> Build(InfinityProfiling.Scripts.CalcReal);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcText()=> Build(InfinityProfiling.Scripts.CalcText);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcKxb()=> Build(InfinityProfiling.Scripts.CalcKxb);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealArray()=> Build(InfinityProfiling.Scripts.CalcRealArray);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcFourArgs()=> Build(InfinityProfiling.Scripts.CalcFourArgs);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstMultiArrays()=> Build(InfinityProfiling.Scripts.MultiplyArrayItems);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstDummyBubble() => Build(InfinityProfiling.Scripts.DummyBubbleSort);
       
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstEverything() => Build(InfinityProfiling.Scripts.Everything);
    }
}