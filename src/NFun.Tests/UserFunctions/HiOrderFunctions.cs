using System;
using System.Collections.Generic;
using System.Text;
using NFun;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests.UserFunctions
{
    public class HiOrderFunctionsTest
    {
        [TestCase(@"y = (x->x+1)(3.0)", 4.0)]
        [TestCase(@"f = x->x+1; y = f(3.0)", 4.0)]
        [TestCase(@"f = a->b->a+b; y = f(3.0)(5.0)", 8.0)]
        public void HiOrderFunctionInitialization(string expr, object yExpected)
            => FunBuilder.BuildDefault(expr).Calculate().AssertHas(VarVal.New("y",yExpected));

        [TestCase(@"choose(f1, f2,  selector, arg1, arg2) = if(selector) f1(arg1,arg2) else f2(arg1,arg2); 
                    choose(max, min, true, 1,2)", 2.0)]
        [TestCase(@"car2(f, x) = i->f(x, i); max.car2(1)(2)", 2.0)]
        [TestCase(@"mult(x)=y->z->x*y*z;    mult(2)(3)(4)", 24.0)]
        [TestCase(@"mult()= x->y->z->x* y*z; mult()(2)(3)(4)", 24.0)]
        [TestCase(@"car2(g) = g(2,4); car2(max)    ", 4.0)]
        [TestCase(@"car2(g) = g(2,4); car2(min)    ", 2.0)]
        [TestCase(@"car1(g) = g(2);   car1(x->x-1)   ", 1.0)]
        [TestCase(@"car1(g) = g(2);   car1(x->x)   ", 2.0)]
        [TestCase(@"car1(g) = g(2); my(x)=x-1; car1(my)   ", 1.0)]
        [TestCase(@"car1(g) = g(2,3,4); my(a,b,c)=a+b+c; car1(my)   ", 9.0)]
        public void HiOrderGenericFunction(string expr, object expected)
            => FunBuilder.BuildDefault(expr).Calculate().AssertOutEquals(expected);

        [TestCase(@"car2(g):real = g(2.0,4.0); car2(max)    ", 4.0)]
        public void HiOrderConcreteFunction(string expr, object expected)
            => FunBuilder.BuildDefault(expr).Calculate().AssertOutEquals(expected);
    }
}
