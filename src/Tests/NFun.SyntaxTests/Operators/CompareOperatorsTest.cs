﻿using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Operators;

public class CompareOperatorsTest {
    [TestCase("true == false", false)]
    [TestCase("true==true==1", false)]
    [TestCase("8==8==1", false)]
    [TestCase("true==true==true", true)]
    [TestCase("8==8==8", false)]
    [TestCase("[0,0,1]!=[0,false,1]", true)]
    [TestCase("[0,0,1]==[0,false,1]", false)]
    [TestCase("[false,0,1]!=[0,false,1]", true)]
    [TestCase("[false,0,1,'vasa',[1,2,3]]==[false,0,1,'vasa',[1,2,3]]", true)]
    [TestCase("[false,0,1,'vasa',[1,2,3]]!=[false,0,1,'vasa',[1,2,3]]", false)]
    [TestCase("[false,0,1,'peta',[1,2,3]]==[false,0,1,'vasa',[1,2,3]]", false)]
    [TestCase("[false,0,1,'peta',[1,2,3]]!=[false,0,1,'vasa',[1,2,3]]", true)]
    [TestCase("[false,0,1,'vasa',[1,2,[1,2]]]==[false,0,1,'vasa',[1,2,[1,2]]]", true)]
    [TestCase("[false,0,1,'vasa',[1,2,[10000,2]]]==[false,0,1,'vasa',[1,2,[1,2]]]", false)]
    [TestCase("[false,0,1,'vasa',[1,2,[10000,2]]]!=[false,0,1,'vasa',[1,2,[1,2]]]", true)]
    [TestCase("[false,0,1]==[false,0,1]", true)]
    [TestCase("[false,0,1]!=[false,0,1]", false)]
    [TestCase("[false,0,1]==[0,false,1]", false)]
    [TestCase("[false,0,1]!=[0,false,1]", true)]
    [TestCase("[0,0,1]==[0,0,1]", true)]
    [TestCase("[0,0,1]!=[0,0,1]", false)]
    [TestCase("[0,1,1]==[0,0,1]", false)]
    [TestCase("[0,1,1]!=[0,0,1]", true)]
    [TestCase("[0,0,1.0]==[0,0,1]", true)]
    [TestCase("[0,0,1]!=[0,0,1.0]", false)]
    [TestCase("[0,1.0,1]==[0,0,1]", false)]
    [TestCase("[0,1,1]!=[0,0.0,1]", true)]
    [TestCase("[false,true, false]==[false,true, false]", true)]
    [TestCase("[false,true, false]!=[false,true, false]", false)]
    [TestCase("0 == 0 == 8", false)]
    [TestCase("8 == 1 == 0", false)]
    [TestCase("true == 1", false)]
    [TestCase("1==1.0", true)]
    [TestCase("0==0.0", true)]
    [TestCase("1==1", true)]
    [TestCase("1==0", false)]
    [TestCase("1==true", false)]
    [TestCase("1==false", false)]
    [TestCase("0==true", false)]
    [TestCase("0==false", false)]
    [TestCase("true==true", true)]
    [TestCase("true==false", false)]
    [TestCase("'a'[0]=='b'[0] ", false)]
    [TestCase("'a'[0]!='b'[0] ", true)]
    [TestCase("'a'[0]== 'a'[0] ", true)]
    [TestCase("'a'[0]!='a'[0] ", false)]
    [TestCase("'avatar'== 'bigben' ", false)]
    [TestCase("'avatar'!= 'bigben' ", true)]
    public void ConstantEquality(string expr, bool expected)
        => expr.AssertReturns("out", expected);

    [TestCase("1!=0", true)]
    [TestCase("0!=1", true)]
    [TestCase("5!=5", false)]
    [TestCase("5>5", false)]
    [TestCase("5>3", true)]
    [TestCase("5>6", false)]
    [TestCase("5>=5", true)]
    [TestCase("5>=3", true)]
    [TestCase("5>=6", false)]
    [TestCase("5<=5", true)]
    [TestCase("5<=3", false)]
    [TestCase("5<=6", true)]
    [TestCase("'a'[0]< 'b'[0] ", true)]
    [TestCase("'a'[0]<='b'[0] ", true)]
    [TestCase("'a'[0]>='b'[0] ", false)]
    [TestCase("'a'[0]> 'b'[0] ", false)]
    [TestCase("'a'[0]< 'a'[0] ", false)]
    [TestCase("'a'[0]<='a'[0] ", true)]
    [TestCase("'a'[0]>='a'[0] ", true)]
    [TestCase("'a'[0]> 'a'[0] ", false)]
    [TestCase("'a'[0] < 'z'[0]", true)]
    [TestCase("'b'[0] > 'z'[0]", false)]
    [TestCase("'b'[0] <= 'z'[0]", true)]
    [TestCase("'A'[0] >= 'B'[0]", false)]
    [TestCase("'B'[0] > 'C'[0]", false)]
    [TestCase("'A'[0] <= 'z'[0]", true)]
    [TestCase("'B'[0] > 'y'[0]", false)]
    [TestCase("'C'[0] >= 'x'[0]", false)]
    [TestCase("'D'[0] < 'w'[0]", true)]
    [TestCase("'E'[0] <= 'v'[0]", true)]
    [TestCase("'F'[0] <= '1'[0]", false)]
    [TestCase("'1'[0] <= 'a'[0]", true)]
    [TestCase("cmp(a,b) = a>b; 'a'[0].cmp('b'[0])", false)]
    [TestCase("'avatar'< 'bigben' ", true)]
    [TestCase("'avatar'<= 'bigben' ", true)]
    [TestCase("'avatar'>= 'bigben' ", false)]
    [TestCase("'avatar'>  'bigben' ", false)]
    [TestCase("'avatar'< 'avatar' ", false)]
    [TestCase("'avatar'<= 'avatar' ", true)]
    [TestCase("'avatar'>= 'avatar' ", true)]
    [TestCase("'avatar'>  'avatar' ", false)]
    //todo
    //[TestCase("'avatar'.reverse() >  reverse('avatar') ", false)]
    //[TestCase("('avatar'.reverse()) >  reverse('avatar') ", false)]
    //[TestCase("'avatar'.reverse() <  'avatar'", false)]
    [TestCase("0==0.0", true)]
    [TestCase("0!=0.5", true)]
    [TestCase("-1.0>=-0x1", true)]
    [TestCase("1>=-1.0", true)]
    [TestCase("1==0", false)]
    [TestCase("true==true", true)]
    [TestCase("true==false", false)]
    [TestCase("5!=5", false)]
    [TestCase("5>3", true)]
    [TestCase("5>=5", true)]
    [TestCase("5<=5", true)]
    [TestCase("1<2 == 2<3", true)]
    [TestCase("1<2 == 2<3 == true", true)]
    [TestCase("1<2 and 2<3", true)]
    [TestCase("true or 1>2 and 2<3", true)]
    [TestCase("true or 1>2 or 2>3", true)]
    [TestCase("true and 1>2 or 2>3", false)]
    [TestCase("true and 1>2 or 2<3", true)]
    public void ConstantEquation(string expr, bool expected)
        => expr.AssertReturns("out", expected);


    [TestCase("x:real; y = x>42", (double)1, false)]
    [TestCase("x:real; y = x>42", (double)42, false)]
    [TestCase("x:real; y = x>42", (double)43, true)]
    [TestCase("x:real; y = x>=42", (double)1, false)]
    [TestCase("x:real; y = x>=42", (double)42, true)]
    [TestCase("x:real; y = x>=42", (double)43, true)]
    [TestCase("x:real; y = x<42", (double)1, true)]
    [TestCase("x:real; y = x<42", (double)42, false)]
    [TestCase("x:real; y = x<42", (double)43, false)]
    [TestCase("x:real; y = x<=42", (double)1, true)]
    [TestCase("x:real; y = x<=42", (double)42, true)]
    [TestCase("x:real; y = x<=42", (double)43, false)]
    [TestCase("x:real; y = x==42", (double)1, false)]
    [TestCase("x:real; y = x==42", (double)42, true)]
    [TestCase("x:real; y = x==42", (double)43, false)]
    [TestCase("x:int64; y = x>42", (long)1, false)]
    [TestCase("x:int64; y = x>42", (long)42, false)]
    [TestCase("x:int64; y = x>42", (long)43, true)]
    [TestCase("x:int64; y = x>=42", (long)1, false)]
    [TestCase("x:int64; y = x>=42", (long)42, true)]
    [TestCase("x:int64; y = x>=42", (long)43, true)]
    [TestCase("x:int64; y = x<42", (long)1, true)]
    [TestCase("x:int64; y = x<42", (long)42, false)]
    [TestCase("x:int64; y = x<42", (long)43, false)]
    [TestCase("x:int64; y = x<=42", (long)1, true)]
    [TestCase("x:int64; y = x<=42", (long)42, true)]
    [TestCase("x:int64; y = x<=42", (long)43, false)]
    [TestCase("x:int64; y = x==42", (long)1, false)]
    [TestCase("x:int64; y = x==42", (long)42, true)]
    [TestCase("x:int64; y = x==42", (long)43, false)]
    [TestCase("x:uint64; y = x>42", (ulong)1, false)]
    [TestCase("x:uint64; y = x>42", (ulong)42, false)]
    [TestCase("x:uint64; y = x>42", (ulong)43, true)]
    [TestCase("x:uint64; y = x>=42", (ulong)1, false)]
    [TestCase("x:uint64; y = x>=42", (ulong)42, true)]
    [TestCase("x:uint64; y = x>=42", (ulong)43, true)]
    [TestCase("x:uint64; y = x<42", (ulong)1, true)]
    [TestCase("x:uint64; y = x<42", (ulong)42, false)]
    [TestCase("x:uint64; y = x<42", (ulong)43, false)]
    [TestCase("x:uint64; y = x<=42", (ulong)1, true)]
    [TestCase("x:uint64; y = x<=42", (ulong)42, true)]
    [TestCase("x:uint64; y = x<=42", (ulong)43, false)]
    [TestCase("x:uint64; y = x==42", (ulong)1, false)]
    [TestCase("x:uint64; y = x==42", (ulong)42, true)]
    [TestCase("x:uint64; y = x==42", (ulong)43, false)]
    [TestCase("x:int; y = x>42", 1, false)]
    [TestCase("x:int; y = x>42", 42, false)]
    [TestCase("x:int; y = x>42", 43, true)]
    [TestCase("x:int; y = x>=42", 1, false)]
    [TestCase("x:int; y = x>=42", 42, true)]
    [TestCase("x:int; y = x>=42", 43, true)]
    [TestCase("x:int; y = x<42", 1, true)]
    [TestCase("x:int; y = x<42", 42, false)]
    [TestCase("x:int; y = x<42", 43, false)]
    [TestCase("x:int; y = x<=42", 1, true)]
    [TestCase("x:int; y = x<=42", 42, true)]
    [TestCase("x:int; y = x<=42", 43, false)]
    [TestCase("x:int; y = x==42", 1, false)]
    [TestCase("x:int; y = x==42", 42, true)]
    [TestCase("x:int; y = x==42", 43, false)]
    [TestCase("x:uint; y = x>42", (uint)1, false)]
    [TestCase("x:uint; y = x>42", (uint)42, false)]
    [TestCase("x:uint; y = x>42", (uint)43, true)]
    [TestCase("x:uint; y = x>=42", (uint)1, false)]
    [TestCase("x:uint; y = x>=42", (uint)42, true)]
    [TestCase("x:uint; y = x>=42", (uint)43, true)]
    [TestCase("x:uint; y = x<42", (uint)1, true)]
    [TestCase("x:uint; y = x<42", (uint)42, false)]
    [TestCase("x:uint; y = x<42", (uint)43, false)]
    [TestCase("x:uint; y = x<=42", (uint)1, true)]
    [TestCase("x:uint; y = x<=42", (uint)42, true)]
    [TestCase("x:uint; y = x<=42", (uint)43, false)]
    [TestCase("x:uint; y = x==42", (uint)1, false)]
    [TestCase("x:uint; y = x==42", (uint)42, true)]
    [TestCase("x:uint; y = x==42", (uint)43, false)]
    [TestCase("x:int16; y = x>42", (Int16)1, false)]
    [TestCase("x:int16; y = x>42", (Int16)42, false)]
    [TestCase("x:int16; y = x>42", (Int16)43, true)]
    [TestCase("x:int16; y = x>=42", (Int16)1, false)]
    [TestCase("x:int16; y = x>=42", (Int16)42, true)]
    [TestCase("x:int16; y = x>=42", (Int16)43, true)]
    [TestCase("x:int16; y = x<42", (Int16)1, true)]
    [TestCase("x:int16; y = x<42", (Int16)42, false)]
    [TestCase("x:int16; y = x<42", (Int16)43, false)]
    [TestCase("x:int16; y = x<=42", (Int16)1, true)]
    [TestCase("x:int16; y = x<=42", (Int16)42, true)]
    [TestCase("x:int16; y = x<=42", (Int16)43, false)]
    [TestCase("x:int16; y = x==42", (Int16)1, false)]
    [TestCase("x:int16; y = x==42", (Int16)42, true)]
    [TestCase("x:int16; y = x==42", (Int16)43, false)]
    [TestCase("x:uint16; y = x>42", (UInt16)1, false)]
    [TestCase("x:uint16; y = x>42", (UInt16)42, false)]
    [TestCase("x:uint16; y = x>42", (UInt16)43, true)]
    [TestCase("x:uint16; y = x>=42", (UInt16)1, false)]
    [TestCase("x:uint16; y = x>=42", (UInt16)42, true)]
    [TestCase("x:uint16; y = x>=42", (UInt16)43, true)]
    [TestCase("x:uint16; y = x<42", (UInt16)1, true)]
    [TestCase("x:uint16; y = x<42", (UInt16)42, false)]
    [TestCase("x:uint16; y = x<42", (UInt16)43, false)]
    [TestCase("x:uint16; y = x<=42", (UInt16)1, true)]
    [TestCase("x:uint16; y = x<=42", (UInt16)42, true)]
    [TestCase("x:uint16; y = x<=42", (UInt16)43, false)]
    [TestCase("x:uint16; y = x==42", (UInt16)1, false)]
    [TestCase("x:uint16; y = x==42", (UInt16)42, true)]
    [TestCase("x:uint16; y = x==42", (UInt16)43, false)]
    [TestCase("x:byte; y = x>42", (byte)1, false)]
    [TestCase("x:byte; y = x>42", (byte)42, false)]
    [TestCase("x:byte; y = x>42", (byte)43, true)]
    [TestCase("x:byte; y = x>=42", (byte)1, false)]
    [TestCase("x:byte; y = x>=42", (byte)42, true)]
    [TestCase("x:byte; y = x>=42", (byte)43, true)]
    [TestCase("x:byte; y = x<42", (byte)1, true)]
    [TestCase("x:byte; y = x<42", (byte)42, false)]
    [TestCase("x:byte; y = x<42", (byte)43, false)]
    [TestCase("x:byte; y = x<=42", (byte)1, true)]
    [TestCase("x:byte; y = x<=42", (byte)42, true)]
    [TestCase("x:byte; y = x<=42", (byte)43, false)]
    [TestCase("x:byte; y = x==42", (byte)1, false)]
    [TestCase("x:byte; y = x==42", (byte)42, true)]
    [TestCase("x:byte; y = x==42", (byte)43, false)]
    public void SingleVariableEquation(string expr, object arg, object expected) =>
        expr.Calc("x", arg).AssertReturns("y", expected);
}
