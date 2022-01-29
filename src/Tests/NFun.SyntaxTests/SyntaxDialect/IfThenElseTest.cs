using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.SyntaxDialect {

[TestFixture]
public class IfThenElseTest {
    [TestCase(IfExpressionSetup.IfIfElse)]
    [TestCase(IfExpressionSetup.IfElseIf)]
    public void Nested2IfThenElseParsing(IfExpressionSetup dialect) =>
        @"y =
	            if (true) 
		            if (true) 1
		            else 4 	
	            else 5
            ".BuildWithDialect(Dialects.ModifyOrigin(dialect));

    [TestCase(IfExpressionSetup.IfIfElse)]
    [TestCase(IfExpressionSetup.IfElseIf)]
    public void Nested3IfThenElseParsing(IfExpressionSetup dialect) =>
        @"y =
	            if (true) 
		            if (true)
		 	            if (true) 2
		 	            else 3
		            else 4 	
	            else 5
            ".BuildWithDialect(Dialects.ModifyOrigin(dialect));

    [TestCase(IfExpressionSetup.IfIfElse)]
    [TestCase(IfExpressionSetup.IfElseIf)]
    public void Nested4IfThenElseParsing(IfExpressionSetup dialect) =>
        @"y =
	            if (true) 
		            if (true) 
		 	            if (true)
                            if (true) 3		 	
                            else 4
                        else 5
		            else 6 	
	            else 7
            ".BuildWithDialect(Dialects.ModifyOrigin(dialect));

    [TestCase(1, 0, 0, 1)]
    [TestCase(2, 1, 0, 2)]
    [TestCase(2, 2, 0, 3)]
    [TestCase(2, 9, 0, 4)]
    [TestCase(3, 1, 1, 5)]
    [TestCase(3, 1, 9, 6)]
    [TestCase(3, 2, 0, 7)]
    [TestCase(3, 9, 0, 8)]
    [TestCase(9, 4, 0, 9)]
    [TestCase(9, 9, 0, 10)]
    public void NestedIfThenElse_ififelse(int x1, int x2, int x3, int expected) =>
        @"
                y = if (x1 == 1) 1 
                    if (x1 == 2)
                        if (x2 == 1) 2
                        if (x2 == 2) 3
                        else 4
                    if (x1 == 3) 
                        if (x2 == 1) 
                            if (x3 ==1) 5
                            else 6
                        if (x2 == 2) 7
                        else 8 	
                    else if (x2 == 4) 9 
                         else 10"
            .BuildWithDialect(Dialects.ModifyOrigin(IfExpressionSetup.IfIfElse))
            .Calc(("x1", x1), ("x2", x2), ("x3", x3))
            .AssertReturns("y", expected);

    [TestCase(1, 0, 0, 1)]
    [TestCase(2, 1, 0, 2)]
    [TestCase(2, 2, 0, 3)]
    [TestCase(2, 9, 0, 4)]
    [TestCase(3, 1, 1, 5)]
    [TestCase(3, 1, 9, 6)]
    [TestCase(3, 2, 0, 7)]
    [TestCase(3, 9, 0, 8)]
    [TestCase(9, 4, 0, 9)]
    [TestCase(9, 9, 0, 10)]
    public void NestedIfThenElse(int x1, int x2, int x3, int expected) {
        var expr = @"
                y = if (x1 == 1) 1 
                    else if (x1 == 2)
                        if (x2 == 1) 2
                        else if (x2 == 2) 3
                        else 4
                    else if (x1 == 3) 
                        if (x2 == 1) 
                            if (x3 ==1) 5
                            else 6
                        else if (x2 == 2) 7
                        else 8 	
                    else if (x2 == 4) 9 
                         else 10";
        foreach (var setup in new[] { IfExpressionSetup.IfIfElse, IfExpressionSetup.IfElseIf })
        {
            Funny.Hardcore
                 .WithDialect(Dialects.ModifyOrigin(setup))
                 .Build(expr)
                 .Calc(("x1", x1), ("x2", x2), ("x3", x3))
                 .AssertReturns("y", expected);
        }
    }

    [TestCase("y = if (1<2 )10 else -10", 10)]
    [TestCase("y = if (1>2 )-10 else 10", 10)]
    [TestCase("y = if (2>1 )10 else -10", 10)]
    [TestCase("y = if (2>1 )10\r else -10", 10)]
    [TestCase("y = if (2==1)10\r else -10", -10)]
    [TestCase("y = if (2<1 )10 \r if (2>1)  -10 else 0", -10)]
    [TestCase("y = if (1>2 )10\r if (1<2) -10\r else 0", -10)]
    public void ConstantIntEquation(string expr, int expected, IfExpressionSetup setup = IfExpressionSetup.IfIfElse)
        => expr.BuildWithDialect(Dialects.ModifyOrigin(setup)).Calc().AssertReturns(expected);

    [TestCase("y = if (1<2 ) true else false", true)]
    [TestCase("y = if (true) true else false", true)]
    [TestCase("y = if (true) true \r if (false) false else true", true)]
    [TestCase("y = if (true) true \r else if (false) false else true", true)]
    [TestCase("y = if (true) true else false", true, IfExpressionSetup.IfElseIf)]
    [TestCase("y = if (true) true \r else if (false) false else true", true, IfExpressionSetup.IfElseIf)]
    public void ConstantBoolEquation(
        string expr, bool expected,
        IfExpressionSetup setup = IfExpressionSetup.IfIfElse)
        => expr.BuildWithDialect(Dialects.ModifyOrigin(setup)).Calc().AssertReturns(expected);

    [TestCase(
        @"
if (x == 0) 'zero'
else 'positive' ", 2, "positive")]
    [TestCase(
        @"
if (x == 0) [0.0]
if (x == 1) [0.0,1.0]
if (x == 2) [0.0,1.0,2.0]
if (x == 3) [0.0,1.0,2.0,3.0]
else [0.0,0.0,0.0] ", 2, new[] { 0.0, 1.0, 2.0 })]
    [TestCase(
        @"
if (x==0) ['0']
if (x==1) ['0','1']
if (x==2) ['0','1','2']
if (x==3) ['0','1','2','3']
else ['0','0','0'] ", 2, new[] { "0", "1", "2" })]
    [TestCase(
        @"
if (x == 0) 'zero'
if (x == 1) 'one'
if (x == 2) 'two'
else 'not supported' ", 2, "two")]
    [TestCase("if (x==1) [1,2,3] else []", 1, new[] { 1, 2, 3 })]
    [TestCase("if (x==1) [1,2,3] else []", 0, new int[0])]
    public void SingleVariableEquatation_ififelse(string expression, int x, object expected)
        => Funny.Hardcore
                .WithApriori<int>("x")
                .WithDialect(Dialects.ModifyOrigin(IfExpressionSetup.IfIfElse))
                .Build(expression)
                .Calc("x", x)
                .AssertAnonymousOut(expected);


    [TestCase(
        @"
if (x == 0) 'zero'
else 'positive' ", 2, "positive")]
    [TestCase(
        @"
if (x == 0) [0.0]
else if (x == 1) [0.0,1.0]
else if (x == 2) [0.0,1.0,2.0]
else if (x == 3) [0.0,1.0,2.0,3.0]
else [0.0,0.0,0.0] ", 2, new[] { 0.0, 1.0, 2.0 })]
    [TestCase(
        @"
if (x==0) ['0']
else if (x==1) ['0','1']
else if (x==2) ['0','1','2']
else if (x==3) ['0','1','2','3']
else ['0','0','0'] ", 2, new[] { "0", "1", "2" })]
    [TestCase(
        @"
if (x == 0) 'zero'
else if (x == 1) 'one'
else if (x == 2) 'two'
else 'not supported' ", 2, "two")]
    [TestCase("if (x==1) [1,2,3] else []", 1, new[] { 1, 2, 3 })]
    [TestCase("if (x==1) [1,2,3] else []", 0, new int[0])]
    public void SingleVariableEquatation(string expression, int x, object expected) {
        foreach (var setup in new[] { IfExpressionSetup.IfIfElse, IfExpressionSetup.IfElseIf })
        {
            Funny.Hardcore
                 .WithApriori<int>("x")
                 .WithDialect(Dialects.ModifyOrigin(setup))
                 .Build(expression)
                 .Calc("x", x)
                 .AssertAnonymousOut(expected);
        }
    }

    [TestCase("y = if (1<2 )10 else -10.0", 10.0)]
    [TestCase("y = if (1>2 )-10.0 else 10", 10.0)]
    [TestCase("y = if (2>1 )10.0 else -10.0", 10.0)]
    [TestCase("y = if (2>1 )10.0\r else -10.0", 10.0)]
    [TestCase("y = if (2==1)10.0\r else -10", -10.0)]
    [TestCase("y = if (2<1 )10.0\r else if (2>1) -10.0 else 0", -10.0)]
    [TestCase("y = if (1>2 )10.0\r else if (1<2)-10.0\r else 0.0", -10.0)]
    public void ConstantRealEquation(string expr, double expected) {
        foreach (var setup in new[] { IfExpressionSetup.IfIfElse, IfExpressionSetup.IfElseIf })
        {
            expr.BuildWithDialect(Dialects.ModifyOrigin(setup)).Calc().AssertReturns(expected);
        }
    }


    [TestCase("y = if (2<1 )10.0\r if (2>1) -10.0 else 0", -10.0)]
    [TestCase("y = if (1>2 )10.0\r if (1<2)-10.0\r else 0.0", -10.0)]
    public void ConstantRealEquation_ififelse(string expr, double expected) {
        expr.BuildWithDialect(Dialects.ModifyOrigin(IfExpressionSetup.IfIfElse))
            .Calc()
            .AssertReturns(expected);
    }

    [Test]
    public void IfElseAsExpression() {
        foreach (var setup in new[] { IfExpressionSetup.IfIfElse, IfExpressionSetup.IfElseIf })
        {
            @"i:int  = 42 * if (x>0) x else -1
                arri = [if(x>0) x else -x, if(x<0) -1 else 1 ]"
                .BuildWithDialect(Dialects.ModifyOrigin(setup))
                .Calc("x", 10)
                .AssertResultHas(("i", 420), ("arri", new[] { 10, 1 }));
        }
    }

    [TestCase("y = if (3) else 4")]
    [TestCase("y = if 1 3")]
    [TestCase("y = if true then 3")]
    [TestCase("y = if 1>0  3")]
    [TestCase("y = if (1>0) 3 else")]
    [TestCase("y = if (1>0) else 4")]
    [TestCase("y = if (1>0) 2 if 3 else 4")]
    [TestCase("y = if (1>0) 3 5")]
    [TestCase("y = if (1>0) 5")]
    [TestCase("y = if else 3")]
    [TestCase("y = if (1>0) 3 if 2>0 then 2")]
    [TestCase("y = if (1>0) if 2>0 then 2 else 3")]
    [TestCase("y = then 3")]
    [TestCase("y = if 3")]
    [TestCase("y = if else 3")]
    [TestCase("y = else then 3")]
    [TestCase("y = if (2>1)  3 else true")]
    [TestCase("y = if (2>1)  3 if 2<1 then true else 1")]
    [TestCase("y = if (2>1)  false if 2<1 then true else 1")]
    public void ObviouslyFails(string expr) {
        foreach (var setup in new[]
            { IfExpressionSetup.IfIfElse, IfExpressionSetup.IfElseIf, IfExpressionSetup.Deny })
        {
            expr.AssertObviousFailsOnParse(Dialects.ModifyOrigin(setup));
        }
    }

    [TestCase("y = if (2<1 )10.0\r if (2>1) -10.0 else 0", IfExpressionSetup.IfElseIf)]
    [TestCase("y = if (1>2 )10.0\r if (1<2)-10.0\r else 0.0", IfExpressionSetup.IfElseIf)]
    [TestCase("if (true) false; if (false) true else false", IfExpressionSetup.IfElseIf)]
    [TestCase("y = if (2<1 )10.0\r if (2>1) -10.0 else 0", IfExpressionSetup.Deny)]
    [TestCase("y = if (1>2 )10.0\r if (1<2)-10.0\r else 0.0", IfExpressionSetup.Deny)]
    [TestCase("if (true) false; if (false) true else false", IfExpressionSetup.Deny)]
    [TestCase("if (true) false; else true", IfExpressionSetup.Deny)]
    [TestCase("if (true) false else true", IfExpressionSetup.Deny)]
    [TestCase(
        @"
                y = if (x1 == 1) 1 
                    else if (x1 == 2)
                        if (x2 == 1) 2
                        else if (x2 == 2) 3
                        else 4
                    #else is missing
                    if (x1 == 3) 
                        if (x2 == 1) 
                            if (x3 ==1) 5
                            else 6
                        if (x2 == 2) 7
                        else 8 	
                    else if (x2 == 4) 9 
                         else 10", IfExpressionSetup.IfElseIf)]
    public void ObviouslyFails(string expr, IfExpressionSetup setup) =>
        expr.AssertObviousFailsOnParse(Dialects.ModifyOrigin(setup));
}

}