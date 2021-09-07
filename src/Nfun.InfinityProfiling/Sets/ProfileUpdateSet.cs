using System.Runtime.CompilerServices;
using NFun.Runtime;

namespace NFun.InfinityProfiling.Sets {

public class ProfileUpdateSet : IProfileSet {
    private readonly FunnyRuntime _calcBoolOp;
    private readonly FunnyRuntime _calcFourArgs;
    private readonly FunnyRuntime _calcGenericFunc;
    private readonly FunnyRuntime _calcInterpolation;
    private readonly FunnyRuntime _calcIntOp;
    private readonly FunnyRuntime _calcKxb;
    private readonly FunnyRuntime _calcRealArray;
    private readonly FunnyRuntime _calcRealOp;
    private readonly FunnyRuntime _calcSingleBool;
    private readonly FunnyRuntime _calcSingleReal;
    private readonly FunnyRuntime _calcSingleText;
    private readonly FunnyRuntime _calcSquareEquation;
    private readonly FunnyRuntime _calcTextOp;
    private readonly FunnyRuntime _const1;
    private readonly FunnyRuntime _constBool;
    private readonly FunnyRuntime _constBoolArray;
    private readonly FunnyRuntime _constDummyBubbleSort;
    private readonly FunnyRuntime _constEverything;
    private readonly FunnyRuntime _constGenericFunc;
    private readonly FunnyRuntime _constInterpolation;
    private readonly FunnyRuntime _constMultiplyArrayItems;
    private readonly FunnyRuntime _constRealArray;
    private readonly FunnyRuntime _constSingleText;
    private readonly FunnyRuntime _constSquareEquation;
    private readonly FunnyRuntime _constTrue;
    private readonly FunnyRuntime _primitiveCalcInt2Var;
    private readonly FunnyRuntime _primitiveCalcReal2Var;
    private readonly FunnyRuntime _primitiveConstBoolSimpleArithmetics;
    private readonly FunnyRuntime _primitiveConstIntSimpleArithmetics;
    private readonly FunnyRuntime _primitiveConstRealSimpleArithmetics;

    public ProfileUpdateSet() {
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

        _constInterpolation = Build(Scripts.ConstInterpolation);
        _constSquareEquation = Build(Scripts.ConstSquareEquation);
        _constGenericFunc = Build(Scripts.ConstGenericFunc);

        _primitiveConstIntSimpleArithmetics = Build(Scripts.PrimitiveConstIntSimpleArithmetics);
        _primitiveConstRealSimpleArithmetics = Build(Scripts.PrimitiveConstRealSimpleArithmetics);
        _primitiveConstBoolSimpleArithmetics = Build(Scripts.PrimitiveConstBoolSimpleArithmetics);
        _primitiveCalcReal2Var = Build(Scripts.PrimitiveCalcReal2Var);
        _primitiveCalcInt2Var = Build(Scripts.PrimitiveCalcInt2Var);

        _primitiveCalcReal2Var["a"].Value = 42.1;
        _primitiveCalcReal2Var["b"].Value = 24.0;

        _primitiveCalcInt2Var["a"].Value = 42;
        _primitiveCalcInt2Var["b"].Value = 24;

        _calcSingleReal["x"].Value = 1.0;
        _calcSingleText["x"].Value = "foo";
        _calcBoolOp["x"].Value = false;
        _calcKxb["x"].Value = 42.2;
        _calcRealArray["x"].Value = 24.6;
        _calcFourArgs["a"].Value = 24.6;
        _calcFourArgs["b"].Value = 12.2;
        _calcFourArgs["c"].Value = 654.3;
        _calcFourArgs["d"].Value = "bbbaaaaa";
        _calcInterpolation["a"].Value = 2.0;
        _calcInterpolation["b"].Value = 4.0;

        _calcGenericFunc["a"].Value = 1.0;
        _calcGenericFunc["b"].Value = 2.0;
        _calcGenericFunc["c"].Value = 3.0;

        _calcSquareEquation["a"].Value = 1.0;
        _calcSquareEquation["b"].Value = 10.0;
        _calcSquareEquation["c"].Value = 1.5;

        _calcIntOp["x"].Value = 42;
        _calcRealOp["x"].Value = 42.0;
        _calcBoolOp["x"].Value = true;
        _calcTextOp["x"].Value = "vasa";

        _calcInterpolation["a"].Value = 2.0;
        _calcInterpolation["b"].Value = 1.0;

        _calcSingleBool["x"].Value = true;
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveConstIntSimpleArithmetics() { _primitiveConstIntSimpleArithmetics.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveConstRealSimpleArithmetics() { _primitiveConstRealSimpleArithmetics.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveConstBoolSimpleArithmetics() { _primitiveConstBoolSimpleArithmetics.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcReal2Var() { _primitiveCalcReal2Var.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcInt2Var() { _primitiveCalcInt2Var.Run(); }


    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitivesConstTrue() { _constTrue.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitivesConstBool() { _constBool.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitivesConst1() { _const1.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstText() { _constSingleText.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstBoolArray() { _constBoolArray.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstRealArray() { _constRealArray.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstInterpolation() { _constInterpolation.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstGenericFunc() { _constGenericFunc.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstSquareEquation() { _constSquareEquation.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcSingleBool() { _calcSingleBool.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcSingleReal() { _calcSingleReal.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcSingleText() { _calcSingleText.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcIntOp() { _calcIntOp.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcRealOp() { _calcRealOp.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcBoolOp() { _calcBoolOp.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcTextOp() { _calcTextOp.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcInterpolation() { _calcInterpolation.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcGenericFunc() { _calcGenericFunc.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcSquareEquation() { _calcSquareEquation.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitivesCalcKxb() { _calcKxb.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcRealArray() { _calcRealArray.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcFourArgs() { _calcFourArgs.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ComplexConstMultiArrays() { _constMultiplyArrayItems.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ComplexDummyBubble() { _constDummyBubbleSort.Run(); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ComplexConstEverything() { _constEverything.Run(); }

    private FunnyRuntime Build(string expr) { return Funny.Hardcore.Build(expr); }
}

}