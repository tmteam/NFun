using System.Runtime.CompilerServices;
using NFun;
using NFun.Interpritation;
using NFun.Runtime;

namespace NFun.InfinityProfiling.Sets
{
    public class ProfileUpdateSet: IProfileSet
    {
        private readonly FunctionDictionary _dictionary;
        private readonly FunRuntime _constTrue;
        private readonly FunRuntime _const1;
        private readonly FunRuntime _constSingleText;
        private readonly FunRuntime _constBoolArray;
        private readonly FunRuntime _constRealArray;
        private readonly FunRuntime _calcSingleBool;
        private readonly FunRuntime _calcSingleReal;
        private readonly FunRuntime _calcSingleText;
        private readonly FunRuntime _calcKxb;
        private readonly FunRuntime _calcRealArray;
        private readonly FunRuntime _calcFourArgs;
        private readonly FunRuntime _constMultiplyArrayItems;
        private readonly FunRuntime _constDummyBubbleSort;
        private readonly FunRuntime _constEverything;
        private readonly FunRuntime _calcIntOp;
        private readonly FunRuntime _calcRealOp;
        private readonly FunRuntime _calcBoolOp;
        private readonly FunRuntime _calcSquareEquation;
        private readonly FunRuntime _calcTextOp;
        private readonly FunRuntime _calcInterpolation;
        private readonly FunRuntime _calcGenericFunc;
        private readonly FunRuntime _constBool;
        private readonly FunRuntime _constInterpolation;
        private readonly FunRuntime _constSquareEquation;
        private readonly FunRuntime _constGenericFunc;
        private readonly FunRuntime _primitiveConstIntSimpleArithmetics;
        private readonly FunRuntime _primitiveConstRealSimpleArithmetics;
        private readonly FunRuntime _primitiveConstBoolSimpleArithmetics;
        private readonly FunRuntime _primitiveCalcReal2Var;
        private readonly FunRuntime _primitiveCalcInt2Var;
        public ProfileUpdateSet()
        {
            _dictionary = BaseFunctions.CreateDefaultDictionary();
            _calcIntOp = Build(Scripts.CalcIntOp);
            _calcRealOp = Build(Scripts.CalcRealOp);
            _calcBoolOp = Build(Scripts.CalcBoolOp);
            _calcSquareEquation = Build(Scripts.CalcSquareEquation);
            _calcTextOp = Build(Scripts.CalcTextOp);
            _calcInterpolation = Build(Scripts.CalcInterpolation);
            _calcGenericFunc = Build(Scripts.CalcGenericFunc);

            _constTrue = Build(Scripts.ConstTrue);
            _const1 = Build(Scripts.Const1);
            _constSingleText = Build(Scripts.ConstText);
            _constBoolArray = Build(Scripts.ConstBoolArray);
            _constRealArray = Build(Scripts.ConstRealArray);
            _calcSingleBool = Build(Scripts.CalcSingleBool);
            _constBool = Build(Scripts.ConstBoolOp);
            _calcSingleReal = Build(Scripts.CalcSingleReal);
            _calcSingleText = Build(Scripts.CalcSingleText);
            _calcKxb = Build(Scripts.CalcKxb);
            _calcRealArray = Build(Scripts.CalcRealArray);
            _calcFourArgs = Build(Scripts.CalcFourArgs);
            _constMultiplyArrayItems = Build(Scripts.MultiplyArrayItems);
            _constDummyBubbleSort = Build(Scripts.DummyBubbleSort);
            _constEverything = Build(Scripts.Everything);

            _constInterpolation  = Build(Scripts.ConstInterpolation);
            _constSquareEquation = Build(Scripts.ConstSquareEquation);
            _constGenericFunc    = Build(Scripts.ConstGenericFunc);
            
            _primitiveConstIntSimpleArithmetics = Build(Scripts.PrimitiveConstIntSimpleArithmetics);
            _primitiveConstRealSimpleArithmetics = Build(Scripts.PrimitiveConstRealSimpleArithmetics);
            _primitiveConstBoolSimpleArithmetics = Build(Scripts.PrimitiveConstBoolSimpleArithmetics);
            _primitiveCalcReal2Var = Build(Scripts.PrimitiveCalcReal2Var);
            _primitiveCalcInt2Var = Build(Scripts.PrimitiveCalcInt2Var);

            _primitiveCalcReal2Var["a"] = 42.1;
            _primitiveCalcReal2Var["b"] = 24.0;

            _primitiveCalcInt2Var["a"] = 42;
            _primitiveCalcInt2Var["b"] = 24;

            _calcSingleReal["x"]= 1.0;
            _calcSingleText["x"]= "foo";
            _calcBoolOp["x"] = false;
            _calcKxb["x"]= 42.2;
            _calcRealArray["x"]= 24.6;
            _calcFourArgs["a"]= 24.6;
            _calcFourArgs["b"]= 12.2;
            _calcFourArgs["c"]= 654.3;
            _calcFourArgs["d"]= "bbbaaaaa";
            _calcInterpolation["a"] = 2.0;
            _calcInterpolation["b"] = 4.0;

            _calcGenericFunc["a"] = 1.0;
            _calcGenericFunc["b"] = 2.0;
            _calcGenericFunc["c"] = 3.0;

            _calcSquareEquation["a"] = 1.0;
            _calcSquareEquation["b"] = 10.0;
            _calcSquareEquation["c"] = 1.5;

            _calcIntOp ["x"] = 42;
            _calcRealOp["x"] = 42.0;
            _calcBoolOp["x"] = true;
            _calcTextOp["x"] = "vasa";

            _calcInterpolation["a"] = 2.0;
            _calcInterpolation["b"] = 1.0;

            _calcSingleBool["x"] = true;
            
        }

        private FunRuntime Build(string expr) => FunBuilder.With(expr).With(_dictionary).Build();

        
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveConstIntSimpleArithmetics() => _primitiveConstIntSimpleArithmetics.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]

        public void PrimitiveConstRealSimpleArithmetics() => _primitiveConstRealSimpleArithmetics.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]

        public void PrimitiveConstBoolSimpleArithmetics() => _primitiveConstBoolSimpleArithmetics.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]

        public void PrimitiveCalcReal2Var() => _primitiveCalcReal2Var.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcInt2Var() => _primitiveCalcInt2Var.Update();

        
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesConstTrue() => _constTrue.Update();
        
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesConstBool() => _constBool.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesConst1() => _const1.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstText() => _constSingleText.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstBoolArray() => _constBoolArray.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstRealArray() => _constRealArray.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstInterpolation() => _constInterpolation.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstGenericFunc() => _constGenericFunc.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ConstSquareEquation() => _constSquareEquation.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcSingleBool() => _calcSingleBool.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcSingleReal() => _calcSingleReal.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSingleText()=> _calcSingleText.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcIntOp() => _calcIntOp.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcRealOp() => _calcRealOp.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitiveCalcBoolOp() => _calcBoolOp.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcTextOp() => _calcTextOp.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcInterpolation() => _calcInterpolation.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcGenericFunc() => _calcGenericFunc.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcSquareEquation() => _calcSquareEquation.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void PrimitivesCalcKxb()=> _calcKxb.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcRealArray()=> _calcRealArray.Update();

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CalcFourArgs() => _calcFourArgs.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ComplexConstMultiArrays()=> _constMultiplyArrayItems.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ComplexDummyBubble() => _constDummyBubbleSort.Update();
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ComplexConstEverything() => _constEverything.Update();
    }
}