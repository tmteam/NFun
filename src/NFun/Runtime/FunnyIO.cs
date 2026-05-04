using System;
using System.IO;

namespace NFun.Runtime;

/// <summary>
/// IO context for NFun runtime. Per-runtime instance.
/// Provides Input/Output streams for print/readLine/readChar functions.
/// Default: Console.In / Console.Out.
/// For tests: supply StringReader/StringWriter.
/// </summary>
public sealed class FunnyIO {
    public TextReader Input { get; set; } = Console.In;
    public TextWriter Output { get; set; } = Console.Out;

    /// <summary>
    /// Current IO context for built-in functions.
    /// Set by FunnyRuntime.Run() before execution, restored after.
    /// Thread-safe: each thread has its own context.
    /// </summary>
    [ThreadStatic] internal static FunnyIO Current;

    /// <summary>Get current IO or fallback to Console.</summary>
    internal static TextWriter ActiveOutput => Current?.Output ?? Console.Out;
    internal static TextReader ActiveInput => Current?.Input ?? Console.In;
}
