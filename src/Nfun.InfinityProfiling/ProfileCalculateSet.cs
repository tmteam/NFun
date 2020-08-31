using System.Runtime.CompilerServices;
using NFun;
using NFun.Interpritation;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace Nfun.InfinityProfiling
{
    public class ProfileCalculateSet: IProfileSet
    {
        private readonly FunctionDictionary _dictionary;
        private readonly FunRuntime _constTrueRuntime;
        private readonly FunRuntime _const1Runtime;
        private readonly FunRuntime _constTextRuntime;
        private readonly FunRuntime _constBoolArrayRuntime;
        private readonly FunRuntime _constRealArrayRuntime;
        private readonly FunRuntime _calcBoolRuntime;
        private readonly FunRuntime _calcReal;
        private readonly FunRuntime _calcText;
        private readonly FunRuntime _calcKxb;
        private readonly FunRuntime _calcRealArray;
        private readonly FunRuntime _calcFourArgs;
        private readonly FunRuntime _constMultiplyArrayItems;
        private readonly FunRuntime _constDummyBubbleSort;
        private readonly FunRuntime _constEverithing;

        public ProfileCalculateSet()
        {
            _dictionary = BaseFunctions.CreateDefaultDictionary();
            _constTrueRuntime = Build(InfinityProfiling.Scripts.ConstTrue);
            _const1Runtime = Build(InfinityProfiling.Scripts.Const1);
            _constTextRuntime = Build(InfinityProfiling.Scripts.ConstText);
            _constBoolArrayRuntime = Build(InfinityProfiling.Scripts.ConstBoolArray);
            _constRealArrayRuntime = Build(InfinityProfiling.Scripts.ConstRealArray);
            _calcBoolRuntime = Build(InfinityProfiling.Scripts.CalcBool);
            _calcReal = Build(InfinityProfiling.Scripts.CalcReal);
            _calcText = Build(InfinityProfiling.Scripts.CalcText);
            _calcKxb = Build(InfinityProfiling.Scripts.CalcKxb);
            _calcRealArray = Build(InfinityProfiling.Scripts.CalcRealArray);
            _calcFourArgs = Build(InfinityProfiling.Scripts.CalcFourArgs);
            _constMultiplyArrayItems = Build(InfinityProfiling.Scripts.MultiplyArrayItems);
            _constDummyBubbleSort = Build(InfinityProfiling.Scripts.DummyBubbleSort);
            _constEverithing = Build(InfinityProfiling.Scripts.Everything);
        }

        private FunRuntime Build(string expr) => FunBuilder.With(expr).With(_dictionary).Build();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstTrue() => _constTrueRuntime.Calculate();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void Const1() => _const1Runtime.Calculate();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstText() => _constTextRuntime.Calculate();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstBoolArray() => _constBoolArrayRuntime.Calculate();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstRealArray() => _constRealArrayRuntime.Calculate();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcBool() => _calcBoolRuntime.Calculate(VarVal.New("x",true));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcReal() => _calcReal.Calculate(VarVal.New("x", 42.1));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcText()=> _calcText.Calculate(VarVal.New("x", "kavabanga"));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcKxb()=> _calcKxb.Calculate(VarVal.New("x", 12.3));

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealArray()=> _calcRealArray.Calculate(VarVal.New("x", 3.14));
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcFourArgs()=> _calcFourArgs.Calculate(
            VarVal.New("a", 1.1),
            VarVal.New("b", 42.1),
            VarVal.New("c", 4.1),
            VarVal.New("d", "kotopes")
            );
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstMultiArrays()=> _constMultiplyArrayItems.Calculate();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstDummyBubble() => _constDummyBubbleSort.Calculate();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstEverything() => _constEverithing.Calculate();
    }
}