﻿using System;
using System.Runtime.CompilerServices;

namespace NFun.Tic;

using SolvingStates;

public class FunnyTraceScope : IDisposable {
    public FunnyTraceScope() => TraceLog.IsEnabled = true;

    public void Dispose() => TraceLog.IsEnabled = false;
}
public static class TraceLog {

    public static FunnyTraceScope Scope => new();

    public static void WithTrace(Action action) {
        using (Scope) action();
    }

    public static bool IsEnabled { get; set; } = false;

    public static void Write(Func<string> locator) {
        if (IsEnabled)
            Console.Write(locator());
    }

    public static void WriteLine(Func<string> locator) {
        if (IsEnabled)
            Console.WriteLine(locator());
    }

    public static void WriteLine() {
        if (IsEnabled)
            Console.WriteLine();
    }

    public static void WriteLine(string val, ConsoleColor color) {
        if (IsEnabled)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(val);
            Console.ResetColor();
        }
    }

    public static void WriteLine(Func<string> val, ConsoleColor color) {
        if (IsEnabled)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(val());
            Console.ResetColor();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(string message) {
#if DEBUG
        if (IsEnabled)
            Console.Write(message);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLine(string message) {
#if DEBUG
        if (IsEnabled)
            Console.WriteLine(message);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(string locator, ConsoleColor green) {
#if DEBUG
        if (IsEnabled)
        {
            Console.ForegroundColor = green;
            Console.Write(locator);
            Console.ResetColor();
        }
#endif
    }
}
