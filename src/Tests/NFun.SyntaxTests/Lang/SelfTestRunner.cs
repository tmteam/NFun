using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NFun.Functions;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

/// <summary>
/// Runs .fun self-test files. Two modes:
/// 1. Files with top-level asserts — just run (assert fails = test fail)
/// 2. Files with @Test functions — discover and invoke each
///
/// @Test annotation:
///   @Test
///   fun myTest(): ...              # called with no args
///
///   @Test(1, 2, 3)
///   @Test(4, 5, 9)
///   fun paramTest(a, b, c): ...   # called once per @Test with those args
///
/// Each @Test case compiles the full script with a single call appended,
/// so functions are compiled with call-site type constraints present.
/// </summary>
[TestFixture]
public class SelfTestRunner {

    // Scans every `.fun` file under Lang/ recursively. Currently picks up:
    //   - Lang/SelfTests/        — language-feature self-tests
    //   - Lang/LeetCode/         — algorithmic problem solutions
    // New subdirectories are discovered automatically — drop `.fun` files in
    // and they show up as test cases on the next run.
    private static string LangRoot =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "Lang");

    // Per-test timeout. Bug-hunt and leetcode problems occasionally hit TIC
    // solver fixpoint loops that don't converge — without a cap, a single bad
    // .fun file freezes the whole suite. 30s is well above the milliseconds a
    // healthy compile+run takes (full suite finishes in ~1s).
    [TestCaseSource(nameof(GetTestCases))]
    [Timeout(30000)]
    public void RunSelfTest(string relativePath, string testName, object[] args) {
        var filePath = Path.Combine(LangRoot, relativePath);
        var script = File.ReadAllText(filePath);

        if (testName == "__toplevel__") {
            // Run top-level code only
            var output = new StringWriter();
            try {
                // Only add TestKit if script uses test functions (assert, assertEqual, etc.)
                var needsTestKit = script.Contains("assert") || script.Contains("assertEqual") || script.Contains("assertType");
                var builder = needsTestKit ? Funny.Hardcore.WithTestKit() : Funny.Hardcore;
                var rt = builder.BuildLang(script);
                rt.IO.Output = output;
                rt.Run();
            } catch (Exception ex) {
                Assert.Fail($"{relativePath}: {ex.Message}\n{output}");
            }
        } else {
            // Build script with the test function call appended.
            // Single compilation — function types resolve with call-site constraints.
            var argStr = string.Join(", ", args.Select(FormatArg));
            var call = args.Length > 0 ? $"{testName}({argStr})" : $"{testName}()";
            var fullScript = script + "\n" + call;

            var output = new StringWriter();
            try {
                var rt = Funny.Hardcore.WithTestKit().BuildLang(fullScript);
                rt.IO.Output = output;
                rt.Run();
            } catch (Exception ex) {
                var label = args.Length > 0 ? $"{testName}({argStr})" : testName;
                Assert.Fail($"{relativePath} → {label}: {ex.Message}\n{output}");
            }
        }
    }

    public static IEnumerable<TestCaseData> GetTestCases() {
        var root = LangRoot;
        if (!Directory.Exists(root))
            yield break;

        foreach (var filePath in Directory.GetFiles(root, "*.fun", SearchOption.AllDirectories).OrderBy(f => f)) {
            // Use the path relative to Lang/ as the file identifier (e.g.
            // `SelfTests/01_arithmetic.fun` or `LeetCode/0001_two_sum.fun`).
            var relativePath = Path.GetRelativePath(root, filePath);
            // Stable test-case label: directory + filename without extension.
            var label = Path.Combine(
                Path.GetDirectoryName(relativePath) ?? "",
                Path.GetFileNameWithoutExtension(relativePath))
                .Replace(Path.DirectorySeparatorChar, '/');

            var script = File.ReadAllText(filePath);

            // Parse to find @Test functions
            var testFuncs = FindTestFunctions(script);

            if (testFuncs.Count == 0) {
                // No @Test functions — run as top-level assert file
                yield return new TestCaseData(relativePath, "__toplevel__", Array.Empty<object>())
                    .SetName(label);
            } else {
                foreach (var (funcName, argSets) in testFuncs) {
                    if (argSets.Count == 0) {
                        yield return new TestCaseData(relativePath, funcName, Array.Empty<object>())
                            .SetName($"{label}.{funcName}");
                    } else {
                        for (int i = 0; i < argSets.Count; i++) {
                            var a = argSets[i];
                            var argsLabel = string.Join(",", a.Select(x => x?.ToString() ?? "none"));
                            yield return new TestCaseData(relativePath, funcName, a)
                                .SetName($"{label}.{funcName}({argsLabel})");
                        }
                    }
                }
                // Also run top-level code (non-@Test asserts)
                yield return new TestCaseData(relativePath, "__toplevel__", Array.Empty<object>())
                    .SetName($"{label}.__toplevel__");
            }
        }
    }

    /// <summary>Parse script to find functions with @Test attributes.</summary>
    private static Dictionary<string, List<object[]>> FindTestFunctions(string script) {
        var result = new Dictionary<string, List<object[]>>();
        try {
            var flow = Tokenizer.ToFlowWithIndents(script);
            var tree = Parser.ParseLang(flow);
            foreach (var node in tree.Nodes) {
                if (node is SyntaxParsing.SyntaxNodes.UserFunctionDefinitionSyntaxNode funcDef) {
                    var testAttrs = funcDef.Attributes
                        .Where(a => string.Equals(a.Name, "Test", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (testAttrs.Count > 0) {
                        var argSets = new List<object[]>();
                        foreach (var attr in testAttrs) {
                            if (attr.Values.Length > 0)
                                argSets.Add(attr.Values);
                        }
                        result[funcDef.Id] = argSets;
                    }
                }
            }
        } catch { /* parsing errors — will be caught when running */ }
        return result;
    }

    private static string FormatArg(object arg) => arg switch {
        long l => l.ToString(),
        double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        string s => $"'{s.Replace("\\", "\\\\").Replace("'", "\\'")}'",
        _ => arg?.ToString() ?? "none"
    };
}
