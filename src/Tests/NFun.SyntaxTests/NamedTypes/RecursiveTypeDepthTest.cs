using System.Text;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.NamedTypes;

/// <summary>
/// Tests that recursive type access chains work at arbitrary depth,
/// not limited by any magic constant.
/// </summary>
public class RecursiveTypeDepthTest {

    static object Calc(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
            namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
        .Get("out");

    /// <summary>
    /// Build a linked list of given depth and access the last element via ?.next chain.
    /// </summary>
    static string BuildLinkedListExpr(int depth) {
        var sb = new StringBuilder();
        sb.Append("type node = {v:int, next:node? = none}; ");

        // Build nested constructor: node{v=1, next=node{v=2, next=...node{v=depth}...}}
        sb.Append("n = ");
        for (int i = 1; i <= depth; i++) {
            sb.Append($"node{{v={i}");
            if (i < depth) sb.Append(", next=");
        }
        for (int i = 1; i <= depth; i++)
            sb.Append('}');
        sb.Append("; ");

        // Access chain: n.next?.next?...next?.v ?? -1  (depth-1 times .next?)
        sb.Append("out = n");
        for (int i = 1; i < depth; i++)
            sb.Append(".next?");
        sb.Append(".v ?? -1");

        return sb.ToString();
    }

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    public void LinkedList_AccessAtDepth(int depth) {
        var expr = BuildLinkedListExpr(depth);
        Assert.AreEqual(depth, Calc(expr));
    }

    [TestCase(3, 5)]
    [TestCase(5, 8)]
    [TestCase(10, 15)]
    public void LinkedList_PastEnd_ReturnsDefault(int dataDepth, int accessDepth) {
        var sb = new StringBuilder();
        sb.Append("type node = {v:int, next:node? = none}; ");
        // Build constructor of dataDepth
        sb.Append("n = ");
        for (int i = 1; i <= dataDepth; i++) {
            sb.Append($"node{{v={i}");
            if (i < dataDepth) sb.Append(", next=");
        }
        for (int i = 1; i <= dataDepth; i++) sb.Append('}');
        sb.Append("; ");
        // Access beyond data
        sb.Append("out = n");
        for (int i = 1; i < accessDepth; i++)
            sb.Append(".next?");
        sb.Append(".v ?? -1");
        Assert.AreEqual(-1, Calc(sb.ToString()));
    }
}
