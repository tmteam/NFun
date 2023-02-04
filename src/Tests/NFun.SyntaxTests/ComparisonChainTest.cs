namespace NFun.SyntaxTests;

using NUnit.Framework;
using TestTools;

public class ComparisonChainTest {

    [TestCase("1<2", true)]
    [TestCase("1<2<3", true)]
    [TestCase("1<2<3<4", true)]
    [TestCase("1>2", false)]
    [TestCase("1<2>3", false)]
    [TestCase("1<2>3<4", false)]
    [TestCase("1<2>3>4", false)]

    [TestCase("1<=2", true)]
    [TestCase("1<=1<=3", true)]
    [TestCase("1<=2<=3<=4", true)]
    [TestCase("1>=2", false)]
    [TestCase("1<=2>=3", false)]
    [TestCase("1<=1>=3<=4", false)]
    [TestCase("1<=2>=4>=4", false)]

    [TestCase("(0+1)<(1+1)", true)]
    [TestCase("1<=(2+1)", true)]
    [TestCase("if(1<2<3) true else false", true)]
    [TestCase("if(1<2<3 == true) true else false", true)]
    [TestCase("1<=(2+1)<=4", true)]
    [TestCase("1<2<(2+1)", true)]
    [TestCase("1<2<(1+2)<(2+2)", true)]
    [TestCase("1<2>(1+2)", false)]

    [TestCase("-1<-1.0>-0x1", false)]
    [TestCase("-1<=-1.0>=-0x1", true)]
    [TestCase("-1<=-1.0>=-0x1<1<0x1<0x2", false)]
    //todo
    //[TestCase("'a'<= 'b'.reverse()>=reverse('a')<''", true)]
    //[TestCase("out = 'a'<='b'.reverse()>=reverse('a')<''", true)]
    //[TestCase("out = 'a'<='b'.reverse()>=reverse('a')<''", true)]
    [TestCase("(1<2<3>-100>-150) != (1<4<3>-100>-150)", true)]
    [TestCase("1<2<3>-100>-150 != 1<4<3>-100>-150", true)]
    [TestCase("(1<2<3>-100>-150) == (1<4<3>-100>-150) == true", false)]
    [TestCase(" 1<2<3>-100>-150  ==  1<4<3>-100>-150  == true", false)]
    [TestCase("(1<2<3>-100>-150 != 1<4<3>-100>-150) != ((1<2<3>-100>-150) == (1<4<3>-100>-150))", true)]
    [TestCase("(1<2<3>-100>-150) == (1<4<3>-100>-150) == true", false)]
    [TestCase("out = true.toText()<=false.toText()>='abc'", false)]
    [TestCase("1 != 2<5>3", true)]
    public void AnonymousExpressionConstantEquation(string expr, object expected)
        => expr.AssertAnonymousOut(expected);


    [TestCase("x<=(x+1)<=1", 0, true)]
    [TestCase("'a'<=x<='c'", "b", true)]
    [TestCase("'a'<=x<='c'", "e", false)]
    [TestCase("'a'<='b'<=x<='g'", "f", true)]
    [TestCase("x:real; 0x0<=x<=0x10", 5.0, true)]
    [TestCase("x:real; 0x0<=x<=0x5", 11.0, false)]
    public void SingleVariableEquation(string expr, object input, object expected)
        => expr.Calc("x", input).AssertReturns(expected);

    [TestCase("x:text; 0x0<=x<='too'   ")]
    public void ObviousFail(string expr) => expr.AssertObviousFailsOnParse();
}
