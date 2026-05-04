using System;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

/// <summary>
/// Tests for NFun Indent Rules Specification (Specs/IndentRules.md).
/// </summary>
[TestFixture]
public class IndentRulesTest {

    // ═══════════════════════════════════════════════
    //  Rule 1: Tabs vs Spaces
    // ═══════════════════════════════════════════════

    [Test]
    public void Rule1_SpacesOnly_Works() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    return x * 2\ny = f(3)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    [Test]
    public void Rule1_TabsOnly_Works() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n\treturn x * 2\ny = f(3)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    [Test]
    public void Rule1_MixedTabsAndSpaces_InSameLine_Error() {
        // Tab + spaces on same line
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f(x):\n\t   return x\ny = f(1)"));
    }

    [Test]
    public void Rule1_SpacesThenTabs_InDifferentLines_Error() {
        // First indent uses spaces, second uses tab
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f(x):\n    x = 1\n\treturn x\ny = f(1)"));
    }

    [Test]
    public void Rule1_TabsThenSpaces_InDifferentLines_Error() {
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f(x):\n\tx = 1\n    return x\ny = f(1)"));
    }

    // ═══════════════════════════════════════════════
    //  Rule 2: Indent Size
    // ═══════════════════════════════════════════════

    [Test]
    public void Rule2_OneSpaceIndent_Works() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n return x + 1\ny = f(5)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    [Test]
    public void Rule2_TwoSpaceIndent_Works() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n  return x + 1\ny = f(5)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    [Test]
    public void Rule2_ThreeSpaceIndent_Works() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n   return x + 1\ny = f(5)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    [Test]
    public void Rule2_FourSpaceIndent_Works() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    return x + 1\ny = f(5)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    [Test]
    public void Rule2_EightSpaceIndent_Works() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n        return x + 1\ny = f(5)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    [Test]
    public void Rule2_DifferentSizesInDifferentBlocks_Works() {
        // Outer block: 4 spaces, inner block: 2 extra (6 total)
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    if x > 0:\n" +
            "      return x\n" +
            "    return -x\n" +
            "y = f(5)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    // ═══════════════════════════════════════════════
    //  Rule 3: Inconsistent Dedent → ERROR
    // ═══════════════════════════════════════════════

    [Test]
    public void Rule3_DedentToNonExistentLevel_Error() {
        // Indent 4, then dedent to 2 (never established)
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f(x):\n    x = 1\n  return x\ny = f(1)"));
    }

    [Test]
    public void Rule3_DedentToExactPriorLevel_Works() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    if x > 0:\n" +
            "        return x\n" +
            "    return -x\n" +    // dedent from 8 to 4 — exact prior level
            "y = f(5)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void Rule3_DedentToZero_Works() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    return x + 1\n" +
            "y = f(5)");           // dedent from 4 to 0
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    // ═══════════════════════════════════════════════
    //  Rule 4: Empty Lines Inside Blocks — Ignored
    // ═══════════════════════════════════════════════

    [Test]
    public void Rule4_EmptyLinesBetweenStatements_Ignored() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    a = x + 1\n" +
            "\n" +                 // empty line
            "    b = a + 1\n" +
            "\n" +                 // empty line
            "    return b\n" +
            "y = f(5)");
        rt.Run();
        Assert.AreEqual(7, rt["y"].Value);
    }

    [Test]
    public void Rule4_MultipleEmptyLines_Ignored() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    a = x\n" +
            "\n\n\n" +            // three empty lines
            "    return a + 1\n" +
            "y = f(10)");
        rt.Run();
        Assert.AreEqual(11, rt["y"].Value);
    }

    [Test]
    public void Rule4_EmptyLineBeforeElse_Ignored() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    if x > 0:\n" +
            "        return x\n" +
            "\n" +                 // empty line before else
            "    else:\n" +
            "        return -x\n" +
            "y = f(-5)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    // ═══════════════════════════════════════════════
    //  Rule 5: Comment Lines — Ignored (any indent)
    // ═══════════════════════════════════════════════

    // Note: # comments are stripped by the tokenizer before IndentTokenizer runs.
    // So comment-only lines become empty lines, which are already ignored (Rule 4).
    // These tests verify the end-to-end behavior.

    [Test]
    public void Rule5_CommentBetweenStatements_Ignored() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    a = x + 1\n" +
            "    # this is a comment\n" +
            "    return a\n" +
            "y = f(5)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    [Test]
    public void Rule5_CommentAtZeroIndent_InsideBlock_Ignored() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    a = x + 1\n" +
            "# comment at zero indent — should not close block\n" +
            "    return a\n" +
            "y = f(5)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    [Test]
    public void Rule5_CommentOverIndented_Ignored() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    a = x\n" +
            "            # over-indented comment\n" +
            "    return a + 1\n" +
            "y = f(10)");
        rt.Run();
        Assert.AreEqual(11, rt["y"].Value);
    }

    // ═══════════════════════════════════════════════
    //  Rule 6: Multiple Dedent at Once
    // ═══════════════════════════════════════════════

    [Test]
    public void Rule6_DoubleDedent_Works() {
        // Three levels: 0, 4, 8 → dedent from 8 to 0
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    if x > 0:\n" +
            "        return x * 2\n" +
            "    return x\n" +
            "y = f(5)");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void Rule6_TripleDedent_Works() {
        // Four levels deep, then back to 0
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    if x > 10:\n" +
            "        if x > 100:\n" +
            "            return 100\n" +
            "        return x\n" +
            "    return 0\n" +
            "y = f(50)");
        rt.Run();
        Assert.AreEqual(50, rt["y"].Value);
    }

    [Test]
    public void Rule6_TripleDedent_DeepToZero() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    if x > 0:\n" +
            "        if x > 10:\n" +
            "            return 999\n" +
            "        return 99\n" +
            "    return 9\n" +
            "a = f(0)\n" +   // 9
            "b = f(5)\n" +   // 99
            "c = f(50)");    // 999
        rt.Run();
        Assert.AreEqual(9, rt["a"].Value);
        Assert.AreEqual(99, rt["b"].Value);
        Assert.AreEqual(999, rt["c"].Value);
    }

    // ═══════════════════════════════════════════════
    //  Rule 7: Trailing Whitespace — Ignored
    // ═══════════════════════════════════════════════

    [Test]
    public void Rule7_TrailingSpacesAfterColon_Ignored() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):   \n    return x + 1\ny = f(5)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    [Test]
    public void Rule7_TrailingSpacesAfterStatement_Ignored() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    a = x + 1   \n    return a   \ny = f(5)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    // ═══════════════════════════════════════════════
    //  Rule 8: Line Continuation Inside Brackets
    // ═══════════════════════════════════════════════

    [Test]
    public void Rule8_ArrayLiteral_MultiLine_FreeForm() {
        var rt = Funny.Hardcore.BuildLang(
            "y = [\n" +
            "    1, 2, 3,\n" +
            "        4, 5\n" +   // different indent — OK inside []
            "]");
        rt.Run();
        // y should be array [1,2,3,4,5]
        Assert.IsNotNull(rt["y"].Value);
    }

    [Test]
    public void Rule8_FunctionCall_MultiLineArgs() {
        var rt = Funny.Hardcore.BuildLang(
            "fun add(a, b):\n" +
            "    return a + b\n" +
            "y = add(\n" +
            "    10,\n" +
            "    20\n" +
            ")");
        rt.Run();
        Assert.AreEqual(30, rt["y"].Value);
    }

    [Test]
    public void Rule8_StructLiteral_MultiLine() {
        var rt = Funny.Hardcore.BuildLang(
            "s = {\n" +
            "    x = 1,\n" +
            "    y = 2\n" +
            "}\n" +
            "result = s.x + s.y");
        rt.Run();
        Assert.AreEqual(3, rt["result"].Value);
    }

    // ═══════════════════════════════════════════════
    //  Rule 9: Colon and Blocks
    // ═══════════════════════════════════════════════

    [Test]
    public void Rule9_ColonBeforeNewline_StartsBlock() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    return 42\ny = f()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    // ═══════════════════════════════════════════════
    //  Missing Indent After Colon — ERROR
    // ═══════════════════════════════════════════════

    [Test]
    public void MissingIndentAfterColon_Error() {
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f():\nreturn 42"));
    }

    // ═══════════════════════════════════════════════
    //  Complex Scenarios
    // ═══════════════════════════════════════════════

    [Test]
    public void Complex_FunctionWithIfElse_EmptyLines_Comments() {
        var rt = Funny.Hardcore.BuildLang(
            "# Top-level comment\n" +
            "\n" +
            "fun classify(x):\n" +
            "    # Check positive\n" +
            "    if x > 0:\n" +
            "        return 1\n" +
            "\n" +                     // empty line
            "    # Check negative\n" +
            "    elif x < 0:\n" +
            "        return -1\n" +
            "\n" +                     // empty line
            "    else:\n" +
            "        return 0\n" +
            "\n" +                     // empty line before top-level
            "a = classify(5)\n" +
            "b = classify(-3)\n" +
            "c = classify(0)");
        rt.Run();
        Assert.AreEqual(1, rt["a"].Value);
        Assert.AreEqual(-1, rt["b"].Value);
        Assert.AreEqual(0, rt["c"].Value);
    }

    [Test]
    public void Complex_TwoFunctions_DifferentIndent() {
        // First function uses 2-space indent, second uses 4-space
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n" +
            "  return x * 2\n" +
            "\n" +
            "fun triple(x):\n" +
            "    return x * 3\n" +
            "\n" +
            "y = double(5) + triple(5)");
        rt.Run();
        Assert.AreEqual(25, rt["y"].Value);
    }

    [Test]
    public void Complex_TabIndent_WithNestedIf() {
        var rt = Funny.Hardcore.BuildLang(
            "fun abs(x):\n" +
            "\tif x >= 0:\n" +
            "\t\treturn x\n" +
            "\telse:\n" +
            "\t\treturn -x\n" +
            "y = abs(-7)");
        rt.Run();
        Assert.AreEqual(7, rt["y"].Value);
    }
}
