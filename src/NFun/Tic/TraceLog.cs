using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NFun.Tic;

public class FunnyTraceScope : IDisposable {
    public FunnyTraceScope() => TraceLog.IsEnabled = true;

    public void Dispose() => TraceLog.IsEnabled = false;
}

/// <summary>
/// All methods carry [Conditional("DEBUG")]: in Release the C# compiler elides the call
/// AND the entire argument list at every call site, so $"..." string interpolation costs
/// nothing in non-debug builds. In DEBUG the IsEnabled flag still gates console output.
/// </summary>
public static class TraceLog {

    public static FunnyTraceScope Scope => new();

    public static void WithTrace(Action action) {
        using (Scope) action();
    }

    public static bool IsEnabled { get; set; } = false;

    [Conditional("DEBUG")]
    public static void Write(Func<string> locator) {
        if (IsEnabled)
            Console.Write(locator());
    }

    [Conditional("DEBUG")]
    public static void WriteLine(Func<string> locator) {
        if (IsEnabled)
            Console.WriteLine(locator());
    }

    [Conditional("DEBUG")]
    public static void WriteLine() {
        if (IsEnabled)
            Console.WriteLine();
    }

    [Conditional("DEBUG")]
    public static void WriteLine(string val, ConsoleColor color) {
        if (IsEnabled)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(val);
            Console.ResetColor();
        }
    }

    [Conditional("DEBUG")]
    public static void WriteLine(Func<string> val, ConsoleColor color) {
        if (IsEnabled)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(val());
            Console.ResetColor();
        }
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(string message) {
        if (IsEnabled)
            Console.Write(message);
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLine(string message) {
        if (IsEnabled)
            Console.WriteLine(message);
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(string locator, ConsoleColor green) {
        if (IsEnabled)
        {
            Console.ForegroundColor = green;
            Console.Write(locator);
            Console.ResetColor();
        }
    }
}
