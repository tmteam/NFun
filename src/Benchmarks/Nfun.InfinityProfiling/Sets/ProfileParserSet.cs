using System.Runtime.CompilerServices;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.InfinityProfiling.Sets {

public class ProfileParserSet : IProfileSet {
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitivesConstTrue() { Parse(Scripts.ConstTrue); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitivesConstBool() { Parse(Scripts.ConstBoolOp); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitivesConst1() { Parse(Scripts.Const1); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstText() { Parse(Scripts.ConstText); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstBoolArray() { Parse(Scripts.ConstBoolArray); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstRealArray() { Parse(Scripts.ConstRealArray); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstInterpolation() { Parse(Scripts.ConstInterpolation); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstGenericFunc() { Parse(Scripts.ConstGenericFunc); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ConstSquareEquation() { Parse(Scripts.ConstSquareEquation); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveConstIntSimpleArithmetics() { Parse(Scripts.PrimitiveConstIntSimpleArithmetics); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveConstRealSimpleArithmetics() { Parse(Scripts.PrimitiveConstRealSimpleArithmetics); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveConstBoolSimpleArithmetics() { Parse(Scripts.PrimitiveConstBoolSimpleArithmetics); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcReal2Var() { Parse(Scripts.PrimitiveCalcReal2Var); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcInt2Var() { Parse(Scripts.PrimitiveCalcInt2Var); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcSingleBool() { Parse(Scripts.CalcSingleBool); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcSingleReal() { Parse(Scripts.CalcSingleReal); }


    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcIntOp() { Parse(Scripts.CalcIntOp); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcRealOp() { Parse(Scripts.CalcRealOp); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitiveCalcBoolOp() { Parse(Scripts.CalcBoolOp); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcSingleText() { Parse(Scripts.CalcSingleText); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcTextOp() { Parse(Scripts.CalcTextOp); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcInterpolation() { Parse(Scripts.CalcInterpolation); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcGenericFunc() { Parse(Scripts.CalcGenericFunc); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcSquareEquation() { Parse(Scripts.CalcSquareEquation); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PrimitivesCalcKxb() { Parse(Scripts.CalcKxb); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcRealArray() { Parse(Scripts.CalcRealArray); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void CalcFourArgs() { Parse(Scripts.CalcFourArgs); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ComplexConstMultiArrays() { Parse(Scripts.MultiplyArrayItems); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ComplexDummyBubble() { Parse(Scripts.DummyBubbleSort); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void ComplexConstEverything() { Parse(Scripts.Everything); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SyntaxTree Parse(string expr) { return Parser.Parse(Tokenizer.ToFlow(expr)); }
}

}