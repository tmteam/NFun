namespace NFun.UnitTests.ParserTests;

using System;
using NUnit.Framework;
using SyntaxParsing;
using Tokenization;

public class ParserTests {

    [Test]
    public void AnonymConstant() => AssertSyntaxTree(
        "1",
"""
syntax-tree
   equation 'out'
      gint '1'
""");

    [Test]
    public void Equations() => AssertSyntaxTree(
        "x = 1; y:real = x+1.0",
"""
syntax-tree
   equation 'x'
      gint '1'
   equation 'y'
      call '+'
         id 'x'
         constant '1.0'
""");

    [Test]
    public void Linq() => AssertSyntaxTree(
        "y = [1,x].filter(rule(z:int):int = z>2).map(rule it**it)",
"""
syntax-tree
   equation 'y'
      call 'map'
         call 'filter'
            array
               gint '1'
               id 'x'
            anonym-fun
               typed-var-def 'z:Int32'
               call '>'
                  id 'z'
                  gint '2'
         super-anonym-def
            call '**'
               id 'it'
               id 'it'
""");

    [Test]
    public void Function1() => AssertSyntaxTree(
        "y(x:int,z:int):int = x.min(z*(x-z))",
"""
syntax-tree
   fun 'y'
      typed-var-def 'x:Int32'
      typed-var-def 'z:Int32'
      call 'min'
         id 'x'
         call '*'
            id 'z'
            call '-'
               id 'x'
               id 'z'
""");

    [Test]
    public void Function2() => AssertSyntaxTree(
        "y(x) = default; g(x) = y(x); g(y(x))",
"""
syntax-tree
   fun 'y'
      typed-var-def 'x:Empty'
      default
   fun 'g'
      typed-var-def 'x:Empty'
      call 'y'
         id 'x'
   equation 'out'
      call 'g'
         call 'y'
            id 'x'
""");

    private static void AssertSyntaxTree(string expr, string expectedTree) {
        var tree = Parser.Parse(Tokenizer.ToFlow(expr));

        var res = SyntaxTreePrinter.Print(tree).Trim();
        Console.WriteLine(res);
        Assert.AreEqual(expectedTree, res);
    }
}
