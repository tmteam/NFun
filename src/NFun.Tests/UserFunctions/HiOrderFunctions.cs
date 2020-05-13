using System;
using System.Collections.Generic;
using System.Text;
using NFun;
using NFun.TypeInferenceCalculator;
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
        
        [TestCase(@"mult(x)=y->z->x*y*z;    mult(2)(3)(4)", 24.0)]
        [TestCase(@"mult()= x->y->z->x* y*z; mult()(2)(3)(4)", 24.0)]
        [TestCase(@"car0(g) = g(2,4); car0(max)    ", 4.0)]
        [TestCase(@"car2(g) = g(2,4); car2(min)    ", 2.0)]
        [TestCase(@"car3(g) = g(2);   car3(x->x-1)   ", 1.0)]
        [TestCase(@"car4(g) = g(2);   car4(x->x)   ", 2.0)]
        [TestCase(@"call5(f, x) = f(x); call5(x->x+1,  1)", 2.0)]
        [TestCase(@"call6(f, x) = f(x); call6(x->x+1.0, 1.0)", 2.0)]
        [TestCase(@"call7(f, x) = f(x); call7(((x:real)->x+1.0), 1.0)", 2.0)]

        //[TestCase(@"call7(f, x) = f(x); call7(((x):real)->x+1.0, 1.0)", 2.0)]
        //[TestCase(@"call8(f, x) = f(x); call8((x:real):real->x+1.0, 1.0)", 2.0)]

        [TestCase(@"call8(f) = i->f(i); call8(x->x+1)(2)", 3.0)]
        [TestCase(@"call9(f) = i->f(i); (x->x+1).call9()(2)", 3.0)]

        //[TestCase(@"call95(f) = i->f(i); m =1; j = (x->x+1).call95()(2)", 3.0)]

        [TestCase(@"call10(f,x) = i->f(x,i); max.call10(3)(2)", 3.0)]
        [TestCase(@"call11() = i->i; call11()(2)", 2.0)]

        [TestCase(@"car1(g) = g(2); my(x)=x-1; car1(my)   ", 1.0)]
        [TestCase(@"car1(g) = g(2,3,4); my(a,b,c)=a+b+c; car1(my)   ", 9.0)]

        public void HiOrderGenericFunction(string expr, object expected)
        {
            TraceLog.IsEnabled = true;
            FunBuilder.BuildDefault(expr).Calculate().AssertOutEquals(expected);
        }

        [TestCase(@"car2(g):real = g(2.0,4.0); car2(max)    ", 4.0)]
        public void HiOrderConcreteFunction(string expr, object expected)
            => FunBuilder.BuildDefault(expr).Calculate().AssertOutEquals(expected);


        
    }
}
