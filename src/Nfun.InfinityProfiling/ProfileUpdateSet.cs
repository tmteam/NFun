using System.Runtime.CompilerServices;
using NFun;
using NFun.Interpritation;
using NFun.Runtime;

namespace Nfun.InfinityProfiling
{
    public class ProfileUpdateSet: IProfileSet
    {
        private readonly FunctionDictionary _dictionary;
        private readonly FunRuntime _constTrueRuntime;
        private readonly FunRuntime _const1Runtime;
        private readonly FunRuntime _constTextRuntime;
        private readonly FunRuntime _constBoolArray;
        private readonly FunRuntime _constRealArray;
        private readonly FunRuntime _calcBool;
        private readonly FunRuntime _calcReal;
        private readonly FunRuntime _calcText;
        private readonly FunRuntime _calcKxb;
        private readonly FunRuntime _calcRealArray;
        private readonly FunRuntime _calcFourArgs;
        private readonly FunRuntime _constMultiplyArrayItems;
        private readonly FunRuntime _constDummyBubbleSort;
        private readonly FunRuntime _constEverithing;

        public ProfileUpdateSet()
        {
            _dictionary = BaseFunctions.CreateDefaultDictionary();
            _constTrueRuntime = Build(InfinityProfiling.Scripts.ConstTrue);
            _const1Runtime = Build(InfinityProfiling.Scripts.Const1);
            _constTextRuntime = Build(InfinityProfiling.Scripts.ConstText);
            _constBoolArray = Build(InfinityProfiling.Scripts.ConstBoolArray);
            _constRealArray = Build(InfinityProfiling.Scripts.ConstRealArray);
            
            _constMultiplyArrayItems = Build(InfinityProfiling.Scripts.MultiplyArrayItems);
            _constDummyBubbleSort = Build(InfinityProfiling.Scripts.DummyBubbleSort);
            _constEverithing = Build(InfinityProfiling.Scripts.Everything);
            
            _calcReal = Build(InfinityProfiling.Scripts.CalcReal);
            _calcReal["x"]= 1.0;
            _calcText = Build(InfinityProfiling.Scripts.CalcText);
            _calcText["x"]= "foo";
            _calcBool = Build(InfinityProfiling.Scripts.CalcBool);
            _calcBool["x"] = false;
            _calcKxb = Build(InfinityProfiling.Scripts.CalcKxb);
            _calcKxb["x"]= 42.2;
            _calcRealArray = Build(InfinityProfiling.Scripts.CalcRealArray);
            _calcRealArray["x"]= 24.6;
            _calcFourArgs = Build(InfinityProfiling.Scripts.CalcFourArgs);
            _calcFourArgs["a"]= 24.6;
            _calcFourArgs["b"]= 12.2;
            _calcFourArgs["c"]= 654.3;
            _calcFourArgs["d"]= "bbbaaaaa";
        }

        private FunRuntime Build(string expr) => FunBuilder.With(expr).With(_dictionary).Build();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstTrue() => _constTrueRuntime.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void Const1() => _const1Runtime.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstText() => _constTextRuntime.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstBoolArray() => _constBoolArray.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstRealArray() => _constRealArray.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcBool() => _calcBool.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcReal() => _calcReal.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcText()=> _calcText.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcKxb()=> _calcKxb.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealArray()=> _calcRealArray.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcFourArgs() => _calcFourArgs.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstMultiArrays()=> _constMultiplyArrayItems.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstDummyBubble() => _constDummyBubbleSort.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstEverything() => _constEverithing.Update();
    }
}